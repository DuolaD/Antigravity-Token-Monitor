use crate::models::TokenCallItem;
use crate::pricing::PricingService;
use crate::protobuf::ProtobufReader;
use chrono::{DateTime, Local, TimeZone};
use regex::Regex;
use rusqlite::{Connection, OpenFlags};
use std::collections::HashMap;
use std::path::Path;

pub struct DatabaseService;

impl DatabaseService {
    pub fn read_calls_from_database(
        db_path: &Path,
        conv_id: &str,
        title: &str,
        mut workspace: String,
        default_time: DateTime<Local>,
        pricing_service: &PricingService,
    ) -> (Vec<TokenCallItem>, String) {
        let mut calls = Vec::new();
        if !db_path.exists() {
            return (calls, workspace);
        }

        let conn = match Connection::open_with_flags(db_path, OpenFlags::SQLITE_OPEN_READ_ONLY) {
            Ok(c) => c,
            Err(_) => return (calls, workspace),
        };

        // 1. Resolve workspace from trajectory_metadata_blob if empty
        if workspace.is_empty() {
            if let Some(ws) = Self::read_workspace(&conn) {
                workspace = ws;
            }
        }

        // 2. Read exact step timestamps from steps table
        let step_timestamps = Self::read_step_timestamps(&conn);

        // 3. Verify gen_metadata table exists
        let table_exists: bool = conn
            .query_row(
                "SELECT 1 FROM sqlite_master WHERE type='table' AND name='gen_metadata'",
                [],
                |_| Ok(true),
            )
            .unwrap_or(false);

        if !table_exists {
            return (calls, workspace);
        }

        let mut stmt = match conn.prepare("SELECT idx, data FROM gen_metadata ORDER BY idx ASC") {
            Ok(s) => s,
            Err(_) => return (calls, workspace),
        };

        let rows = stmt.query_map([], |row| {
            let idx: i32 = row.get(0)?;
            let data: Option<Vec<u8>> = row.get(1)?;
            Ok((idx, data))
        });

        if let Ok(iter) = rows {
            for item in iter.flatten() {
                let (idx, data_opt) = item;
                let data = match data_opt {
                    Some(d) => d,
                    None => continue,
                };

                let top_fields = ProtobufReader::parse(&data);
                let f1_data = top_fields
                    .iter()
                    .find(|f| f.field_number == 1 && !f.data.is_empty())
                    .map(|f| f.data);

                let f1_bytes = match f1_data {
                    Some(b) => b,
                    None => continue,
                };

                let sub1_fields = ProtobufReader::parse(f1_bytes);
                let mut f4_data = None;
                let mut model_name = "unknown".to_string();
                let mut last_step_index: Option<i32> = None;

                for f in &sub1_fields {
                    if f.field_number == 4 && !f.data.is_empty() {
                        f4_data = Some(f.data);
                    } else if f.field_number == 19 && !f.data.is_empty() {
                        if let Ok(name) = std::str::from_utf8(f.data) {
                            model_name = name.to_string();
                        }
                    } else if f.field_number == 20 && !f.data.is_empty() {
                        let kv_fields = ProtobufReader::parse(f.data);
                        let mut i = 0;
                        while i + 1 < kv_fields.len() {
                            let k = std::str::from_utf8(kv_fields[i].data).unwrap_or("");
                            let v = std::str::from_utf8(kv_fields[i + 1].data).unwrap_or("");
                            if k == "last_step_index" {
                                if let Ok(sidx) = v.parse::<i32>() {
                                    last_step_index = Some(sidx);
                                }
                            }
                            i += 2;
                        }
                    }
                }

                if let Some(f4_bytes) = f4_data {
                    let sub4_fields = ProtobufReader::parse(f4_bytes);
                    let mut varints: HashMap<u32, u64> = HashMap::new();
                    for f in &sub4_fields {
                        if f.wire_type == 0 {
                            varints.insert(f.field_number, f.varint);
                        }
                    }

                    let prompt_tokens = varints.get(&5).copied().unwrap_or(0) as i64;
                    let total_out = varints.get(&3).copied().unwrap_or(0) as i64;
                    let thinking_tokens = varints.get(&9).copied().unwrap_or(0) as i64;
                    let candidate_tokens = varints
                        .get(&10)
                        .map(|&c| c as i64)
                        .unwrap_or(total_out - thinking_tokens);
                    let latency_ms = varints.get(&2).copied().unwrap_or(0) as i64;

                    let (cost, currency) =
                        pricing_service.calculate_cost(prompt_tokens, total_out, &model_name);

                    let ws_short = if !workspace.trim().is_empty() {
                        let clean = workspace.trim_end_matches(['/', '\\']);
                        Path::new(clean)
                            .file_name()
                            .and_then(|n| n.to_str())
                            .unwrap_or(clean)
                            .to_string()
                    } else {
                        "默认工作区".to_string()
                    };

                    let call_time = if let Some(sidx) = last_step_index {
                        step_timestamps.get(&sidx).copied().unwrap_or(default_time)
                    } else if let Some(itime) = step_timestamps.get(&idx) {
                        *itime
                    } else {
                        default_time
                    };

                    calls.push(TokenCallItem {
                        id: format!("{}_{}", conv_id, idx),
                        conversation_id: conv_id.to_string(),
                        title: title.to_string(),
                        workspace: ws_short,
                        workspace_full: workspace.clone(),
                        timestamp: call_time.format("%Y-%m-%d %H:%M:%S").to_string(),
                        timestamp_epoch: call_time.timestamp(),
                        model: model_name,
                        prompt_tokens,
                        output_tokens: total_out,
                        thinking_tokens,
                        candidate_tokens,
                        latency_ms,
                        cost: (cost * 100000.0).round() / 100000.0,
                        currency,
                        step_index: last_step_index,
                    });
                }
            }
        }

        (calls, workspace)
    }

    fn read_workspace(conn: &Connection) -> Option<String> {
        let table_exists: bool = conn
            .query_row(
                "SELECT 1 FROM sqlite_master WHERE type='table' AND name='trajectory_metadata_blob'",
                [],
                |_| Ok(true),
            )
            .unwrap_or(false);

        if !table_exists {
            return None;
        }

        let mut stmt = conn
            .prepare("SELECT data FROM trajectory_metadata_blob LIMIT 1")
            .ok()?;
        let data: Vec<u8> = stmt.query_row([], |row| row.get(0)).ok()?;

        let raw: String = data.iter().map(|&b| b as char).collect();
        let ws_regex = Regex::new(r"file:///([^\x00-\x1f\x7f-\x9f\s\x22\x27\)\*`]+)").ok()?;
        if let Some(caps) = ws_regex.captures(&raw) {
            if let Some(m) = caps.get(1) {
                if let Ok(unescaped) = urlencoding::decode(m.as_str()) {
                    return Some(unescaped.into_owned());
                }
            }
        }

        None
    }

    fn read_step_timestamps(conn: &Connection) -> HashMap<i32, DateTime<Local>> {
        let mut map = HashMap::new();
        let table_exists: bool = conn
            .query_row(
                "SELECT 1 FROM sqlite_master WHERE type='table' AND name='steps'",
                [],
                |_| Ok(true),
            )
            .unwrap_or(false);

        if !table_exists {
            return map;
        }

        let mut stmt = match conn.prepare("SELECT idx, metadata FROM steps WHERE metadata IS NOT NULL") {
            Ok(s) => s,
            Err(_) => return map,
        };

        if let Ok(rows) = stmt.query_map([], |row| {
            let idx: i32 = row.get(0)?;
            let meta: Option<Vec<u8>> = row.get(1)?;
            Ok((idx, meta))
        }) {
            for item in rows.flatten() {
                let (idx, meta_opt) = item;
                if let Some(meta) = meta_opt {
                    let fields = ProtobufReader::parse(&meta);
                    for f in fields {
                        if f.field_number == 1 && !f.data.is_empty() {
                            let sub_fields = ProtobufReader::parse(f.data);
                            for sf in sub_fields {
                                if sf.field_number == 1 && sf.wire_type == 0 {
                                    let sec = sf.varint as i64;
                                    if (1_000_000_000..=2_500_000_000).contains(&sec) {
                                        if let Some(dt) = Local.timestamp_opt(sec, 0).single() {
                                            map.insert(idx, dt);
                                            break;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        map
    }
}
