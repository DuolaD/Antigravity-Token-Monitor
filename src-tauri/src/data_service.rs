use crate::db::DatabaseService;
use crate::models::{
    ConversationSummary, DailyTokenStat, FilterOptions, ModelUsageStat, OverviewStats,
    PagedLogsResult, TokenCallItem, TokenTrendPoint, WorkspaceUsageStat,
};
use crate::pricing::PricingService;
use crate::summaries::SummariesService;
use chrono::{Duration, Local, NaiveDate, TimeZone};
use std::collections::HashMap;
use std::fs;
use std::path::{Path, PathBuf};
use std::sync::{Arc, RwLock};
use std::time::SystemTime;

pub struct TokenDataService {
    pub monitored_directories: Vec<PathBuf>,
    pub conversations: RwLock<HashMap<String, ConversationSummary>>,
    file_mtimes: RwLock<HashMap<PathBuf, SystemTime>>,
    pricing_service: Arc<PricingService>,
}

impl TokenDataService {
    pub fn new(pricing_service: Arc<PricingService>) -> Self {
        let mut dirs_list = Vec::new();
        if let Some(home) = dirs::home_dir() {
            let agy_conv = home.join(".gemini").join("antigravity").join("conversations");
            let ide_conv = home.join(".gemini").join("antigravity-ide").join("conversations");

            if agy_conv.exists() {
                dirs_list.push(agy_conv);
            }
            if ide_conv.exists() {
                dirs_list.push(ide_conv);
            }
        }

        Self {
            monitored_directories: dirs_list,
            conversations: RwLock::new(HashMap::new()),
            file_mtimes: RwLock::new(HashMap::new()),
            pricing_service,
        }
    }

    pub fn refresh_all(&self, force: bool) {
        let home = match dirs::home_dir() {
            Some(h) => h,
            None => return,
        };

        let summaries_path = home
            .join(".gemini")
            .join("antigravity")
            .join("agyhub_summaries_proto.pb");
        let summaries_meta = SummariesService::parse_summaries(&summaries_path);

        for dir in &self.monitored_directories {
            if !dir.exists() {
                continue;
            }

            let entries = match fs::read_dir(dir) {
                Ok(e) => e,
                Err(_) => continue,
            };

            for entry in entries.flatten() {
                let path = entry.path();
                if path.extension().and_then(|ext| ext.to_str()) != Some("db") {
                    continue;
                }

                let conv_id = match path.file_stem().and_then(|s| s.to_str()) {
                    Some(s) => s.to_string(),
                    None => continue,
                };

                let cur_mtime = fs::metadata(&path)
                    .and_then(|m| m.modified())
                    .unwrap_or(SystemTime::UNIX_EPOCH);

                if !force {
                    let has_conv = self.conversations.read().unwrap().contains_key(&conv_id);
                    let cached_time = self.file_mtimes.read().unwrap().get(&path).copied();
                    if has_conv && cached_time == Some(cur_mtime) {
                        continue;
                    }
                }

                let mut title = String::new();
                let mut workspace = String::new();

                if let Some(meta) = summaries_meta.get(&conv_id) {
                    title = meta.0.clone();
                    workspace = meta.1.clone();
                }

                let brain_transcript = home
                    .join(".gemini")
                    .join("antigravity")
                    .join("brain")
                    .join(&conv_id)
                    .join(".system_generated")
                    .join("logs")
                    .join("transcript.jsonl");

                let (trans_title, trans_time) =
                    SummariesService::parse_transcript_step0(&brain_transcript);

                if title.is_empty() && !trans_title.is_empty() {
                    title = trans_title;
                }
                if title.is_empty() {
                    let prefix: String = conv_id.chars().take(8).collect();
                    title = format!("对话 {}", prefix);
                }

                let mtime_secs = cur_mtime
                    .duration_since(SystemTime::UNIX_EPOCH)
                    .map(|d| d.as_secs() as i64)
                    .unwrap_or(0);
                let default_time = trans_time
                    .unwrap_or_else(|| Local.timestamp_opt(mtime_secs, 0).single().unwrap_or_else(Local::now));

                let (calls, resolved_ws) = DatabaseService::read_calls_from_database(
                    &path,
                    &conv_id,
                    &title,
                    workspace,
                    default_time,
                    &self.pricing_service,
                );

                let total_prompt: i64 = calls.iter().map(|c| c.prompt_tokens).sum();
                let total_out: i64 = calls.iter().map(|c| c.output_tokens).sum();
                let total_thinking: i64 = calls.iter().map(|c| c.thinking_tokens).sum();
                let total_lat: i64 = calls.iter().map(|c| c.latency_ms).sum();
                let total_cost: f64 = calls.iter().map(|c| c.cost).sum();

                let mut models: Vec<String> = calls.iter().map(|c| c.model.clone()).collect();
                models.sort();
                models.dedup();

                let summary = ConversationSummary {
                    id: conv_id.clone(),
                    title,
                    workspace: resolved_ws,
                    created_at: default_time.timestamp(),
                    updated_at: mtime_secs,
                    db_path: path.to_string_lossy().to_string(),
                    total_calls: calls.len(),
                    total_prompt_tokens: total_prompt,
                    total_output_tokens: total_out,
                    total_thinking_tokens: total_thinking,
                    total_candidate_tokens: total_out - total_thinking,
                    total_latency_ms: total_lat,
                    total_cost,
                    models,
                    calls,
                };

                self.conversations.write().unwrap().insert(conv_id, summary);
                self.file_mtimes.write().unwrap().insert(path, cur_mtime);
            }
        }
    }

    pub fn get_overview_stats(&self) -> OverviewStats {
        let convs_map = self.conversations.read().unwrap();
        let convs: Vec<&ConversationSummary> = convs_map.values().collect();

        let mut total_prompt = 0i64;
        let mut total_output = 0i64;
        let mut total_thinking = 0i64;
        let mut total_candidate = 0i64;
        let mut total_calls = 0usize;
        let mut total_cost = 0.0f64;
        let mut total_latency = 0i64;

        let today = Local::now().date_naive();
        let mut today_prompt = 0i64;
        let mut today_output = 0i64;
        let mut today_thinking = 0i64;
        let mut today_calls = 0usize;
        let mut today_cost = 0.0f64;

        let mut model_dict: HashMap<String, (i64, usize, f64)> = HashMap::new();
        let mut ws_dict: HashMap<String, (i64, usize, f64)> = HashMap::new();

        for c in &convs {
            let ws_short = if !c.workspace.trim().is_empty() {
                let clean = c.workspace.trim_end_matches(['/', '\\']);
                Path::new(clean)
                    .file_name()
                    .and_then(|n| n.to_str())
                    .unwrap_or(clean)
                    .to_string()
            } else {
                "默认工作区".to_string()
            };

            for call in &c.calls {
                let p = call.prompt_tokens;
                let o = call.output_tokens;
                let th = call.thinking_tokens;
                let tot = p + o;
                let (cost, _) = self
                    .pricing_service
                    .calculate_cost(p, o, &call.model);

                total_prompt += p;
                total_output += o;
                total_thinking += th;
                total_candidate += call.candidate_tokens;
                total_calls += 1;
                total_cost += cost;
                total_latency += call.latency_ms;

                if let Some(call_date) = Local.timestamp_opt(call.timestamp_epoch, 0).single() {
                    if call_date.date_naive() == today {
                        today_prompt += p;
                        today_output += o;
                        today_thinking += th;
                        today_calls += 1;
                        today_cost += cost;
                    }
                }

                let m_entry = model_dict.entry(call.model.clone()).or_insert((0, 0, 0.0));
                m_entry.0 += tot;
                m_entry.1 += 1;
                m_entry.2 += cost;

                let w_entry = ws_dict.entry(ws_short.clone()).or_insert((0, 0, 0.0));
                w_entry.0 += tot;
                w_entry.1 += 1;
                w_entry.2 += cost;
            }
        }

        let grand_total = total_prompt + total_output;
        let avg_lat = if total_calls > 0 {
            (total_latency as f64 / total_calls as f64) / 1000.0
        } else {
            0.0
        };

        let mut top_models: Vec<ModelUsageStat> = model_dict
            .into_iter()
            .map(|(model, (tokens, calls, cost))| {
                let pct = if grand_total > 0 {
                    (tokens as f64 / grand_total as f64) * 100.0
                } else {
                    0.0
                };
                ModelUsageStat {
                    model,
                    tokens,
                    calls,
                    cost: (cost * 10000.0).round() / 10000.0,
                    percentage: (pct * 10.0).round() / 10.0,
                }
            })
            .collect();
        top_models.sort_by(|a, b| b.tokens.cmp(&a.tokens));

        let mut top_workspaces: Vec<WorkspaceUsageStat> = ws_dict
            .into_iter()
            .map(|(workspace, (tokens, calls, cost))| {
                let pct = if grand_total > 0 {
                    (tokens as f64 / grand_total as f64) * 100.0
                } else {
                    0.0
                };
                WorkspaceUsageStat {
                    workspace,
                    tokens,
                    calls,
                    cost: (cost * 10000.0).round() / 10000.0,
                    percentage: (pct * 10.0).round() / 10.0,
                }
            })
            .collect();
        top_workspaces.sort_by(|a, b| b.tokens.cmp(&a.tokens));

        let thinking_ratio = if total_output > 0 {
            (total_thinking as f64 / total_output as f64) * 100.0
        } else {
            0.0
        };
        let thinking_ratio_text = format!("{:.1}%", thinking_ratio);

        let currency = self.pricing_service.get_currency();
        let currency_symbol = if currency == "CNY" { "¥".to_string() } else { "$".to_string() };

        OverviewStats {
            total_tokens: grand_total,
            total_prompt_tokens: total_prompt,
            total_output_tokens: total_output,
            total_thinking_tokens: total_thinking,
            total_candidate_tokens: total_candidate,
            thinking_ratio_text,
            total_calls,
            total_conversations: convs.len(),
            total_cost: (total_cost * 10000.0).round() / 10000.0,
            avg_latency_seconds: (avg_lat * 100.0).round() / 100.0,
            currency,
            currency_symbol,

            today_tokens: today_prompt + today_output,
            today_prompt_tokens: today_prompt,
            today_output_tokens: today_output,
            today_thinking_tokens: today_thinking,
            today_calls,
            today_cost: (today_cost * 10000.0).round() / 10000.0,

            top_models,
            top_workspaces,
        }
    }

    pub fn get_trend_data(
        &self,
        preset: &str,
        custom_start: Option<&str>,
        custom_end: Option<&str>,
    ) -> Vec<TokenTrendPoint> {
        let today = Local::now().date_naive();
        let (start_date, end_date) = match preset {
            "周" => (today - Duration::days(6), today),
            "月" => (today - Duration::days(29), today),
            "季度" => (today - Duration::days(89), today),
            "半年" => (today - Duration::days(179), today),
            "-1年" => (today - Duration::days(364), today),
            "自定义" => {
                let s = custom_start
                    .and_then(|st| NaiveDate::parse_from_str(st, "%Y-%m-%d").ok())
                    .unwrap_or(today - Duration::days(29));
                let e = custom_end
                    .and_then(|et| NaiveDate::parse_from_str(et, "%Y-%m-%d").ok())
                    .unwrap_or(today);
                (s, e)
            }
            _ => {
                // "全部"
                let mut min_date = today;
                let convs = self.conversations.read().unwrap();
                for c in convs.values() {
                    for call in &c.calls {
                        if let Some(dt) = Local.timestamp_opt(call.timestamp_epoch, 0).single() {
                            let d = dt.date_naive();
                            if d < min_date {
                                min_date = d;
                            }
                        }
                    }
                }
                (min_date, today)
            }
        };

        let mut date_map: HashMap<NaiveDate, TokenTrendPoint> = HashMap::new();
        let mut curr = start_date;
        while curr <= end_date {
            date_map.insert(
                curr,
                TokenTrendPoint {
                    date: curr.format("%Y-%m-%d").to_string(),
                    date_label: curr.format("%m/%d").to_string(),
                    full_date_label: curr.format("%Y年%m月%d日").to_string(),
                    prompt_tokens: 0,
                    output_tokens: 0,
                    thinking_tokens: 0,
                    total_tokens: 0,
                    calls: 0,
                    cost: 0.0,
                },
            );
            curr += Duration::days(1);
        }

        let convs = self.conversations.read().unwrap();
        for c in convs.values() {
            for call in &c.calls {
                if let Some(dt) = Local.timestamp_opt(call.timestamp_epoch, 0).single() {
                    let call_date = dt.date_naive();
                    if call_date >= start_date && call_date <= end_date {
                        if let Some(point) = date_map.get_mut(&call_date) {
                            point.prompt_tokens += call.prompt_tokens;
                            point.output_tokens += call.output_tokens;
                            point.thinking_tokens += call.thinking_tokens;
                            point.total_tokens += call.prompt_tokens + call.output_tokens;
                            point.calls += 1;
                            let (cost, _) = self.pricing_service.calculate_cost(
                                call.prompt_tokens,
                                call.output_tokens,
                                &call.model,
                            );
                            point.cost += cost;
                        }
                    }
                }
            }
        }

        let mut list: Vec<_> = date_map.into_iter().collect();
        list.sort_by_key(|(d, _)| *d);
        list.into_iter()
            .map(|(_, mut p)| {
                p.cost = (p.cost * 10000.0).round() / 10000.0;
                p
            })
            .collect()
    }

    pub fn get_paged_logs(
        &self,
        page: usize,
        page_size: usize,
        model_filter: Option<&str>,
        ws_filter: Option<&str>,
        search_query: Option<&str>,
        date_range: Option<&str>,
    ) -> PagedLogsResult {
        let convs = self.conversations.read().unwrap();
        let now = Local::now();
        let today = now.date_naive();

        let mut all_calls: Vec<TokenCallItem> = Vec::new();
        for c in convs.values() {
            all_calls.extend(c.calls.clone());
        }

        let filtered: Vec<TokenCallItem> = all_calls
            .into_iter()
            .filter(|call| {
                if let Some(dr) = date_range {
                    if dr != "全部" && !dr.is_empty() {
                        if let Some(dt) = Local.timestamp_opt(call.timestamp_epoch, 0).single() {
                            if dr == "今天" && dt.date_naive() != today {
                                return false;
                            } else if dr == "近7天" && (now - dt) > Duration::days(7) {
                                return false;
                            } else if dr == "近30天" && (now - dt) > Duration::days(30) {
                                return false;
                            }
                        }
                    }
                }

                if let Some(mf) = model_filter {
                    if mf != "全部模型" && !mf.is_empty() {
                        if !call.model.eq_ignore_ascii_case(mf) {
                            return false;
                        }
                    }
                }

                if let Some(wf) = ws_filter {
                    if wf != "全部工作区" && !wf.is_empty() {
                        if !call.workspace.eq_ignore_ascii_case(wf) {
                            return false;
                        }
                    }
                }

                if let Some(sq) = search_query {
                    let q = sq.trim().to_lowercase();
                    if !q.is_empty() {
                        let match_title = call.title.to_lowercase().contains(&q);
                        let match_model = call.model.to_lowercase().contains(&q);
                        let match_ws = call.workspace.to_lowercase().contains(&q);
                        let match_id = call.conversation_id.to_lowercase().contains(&q);
                        if !match_title && !match_model && !match_ws && !match_id {
                            return false;
                        }
                    }
                }

                true
            })
            .collect();

        let mut sorted = filtered;
        sorted.sort_by(|a, b| b.timestamp_epoch.cmp(&a.timestamp_epoch));

        let total_count = sorted.len();
        let page_size_safe = if page_size == 0 { 15 } else { page_size };
        let mut total_pages = (total_count + page_size_safe - 1) / page_size_safe;
        if total_pages == 0 {
            total_pages = 1;
        }

        let p = if page == 0 { 1 } else { page };
        let skip = (p - 1) * page_size_safe;
        let mut items: Vec<TokenCallItem> = sorted
            .into_iter()
            .skip(skip)
            .take(page_size_safe)
            .collect();

        // Recalculate cost with current currency
        for item in &mut items {
            let (c, cur) = self.pricing_service.calculate_cost(
                item.prompt_tokens,
                item.output_tokens,
                &item.model,
            );
            item.cost = (c * 100000.0).round() / 100000.0;
            item.currency = cur;
        }

        PagedLogsResult {
            items,
            total_count,
            total_pages,
            page: p,
            page_size: page_size_safe,
        }
    }

    pub fn get_daily_stats(&self) -> Vec<DailyTokenStat> {
        let mut daily_dict: HashMap<String, DailyTokenStat> = HashMap::new();
        let convs = self.conversations.read().unwrap();

        for c in convs.values() {
            for call in &c.calls {
                let date_key = if let Some(dt) = Local.timestamp_opt(call.timestamp_epoch, 0).single() {
                    dt.format("%Y-%m-%d").to_string()
                } else {
                    "未知日期".to_string()
                };

                let entry = daily_dict.entry(date_key.clone()).or_insert_with(|| DailyTokenStat {
                    date: date_key,
                    prompt_tokens: 0,
                    output_tokens: 0,
                    thinking_tokens: 0,
                    thinking_ratio: 0.0,
                    total_tokens: 0,
                    calls: 0,
                    cost: 0.0,
                });

                entry.prompt_tokens += call.prompt_tokens;
                entry.output_tokens += call.output_tokens;
                entry.thinking_tokens += call.thinking_tokens;
                entry.total_tokens += call.prompt_tokens + call.output_tokens;
                entry.calls += 1;
                let (cost, _) = self.pricing_service.calculate_cost(
                    call.prompt_tokens,
                    call.output_tokens,
                    &call.model,
                );
                entry.cost += cost;
            }
        }

        let mut list: Vec<DailyTokenStat> = daily_dict.into_values().collect();
        list.sort_by(|a, b| b.date.cmp(&a.date));

        for item in &mut list {
            if item.output_tokens > 0 {
                item.thinking_ratio = ((item.thinking_tokens as f64 / item.output_tokens as f64) * 1000.0).round() / 10.0;
            }
            item.cost = (item.cost * 10000.0).round() / 10000.0;
        }

        list
    }

    pub fn get_filter_options(&self) -> FilterOptions {
        let convs = self.conversations.read().unwrap();
        let mut models: Vec<String> = convs
            .values()
            .flat_map(|c| c.models.clone())
            .collect();
        models.sort();
        models.dedup();

        let mut workspaces: Vec<String> = convs
            .values()
            .map(|c| {
                if c.workspace.trim().is_empty() {
                    "默认工作区".to_string()
                } else {
                    let clean = c.workspace.trim_end_matches(['/', '\\']);
                    Path::new(clean)
                        .file_name()
                        .and_then(|n| n.to_str())
                        .unwrap_or(clean)
                        .to_string()
                }
            })
            .collect();
        workspaces.sort();
        workspaces.dedup();

        FilterOptions { models, workspaces }
    }

    pub fn export_logs_csv(
        &self,
        model_filter: Option<&str>,
        ws_filter: Option<&str>,
        search_query: Option<&str>,
        date_range: Option<&str>,
    ) -> String {
        let paged = self.get_paged_logs(
            1,
            usize::MAX,
            model_filter,
            ws_filter,
            search_query,
            date_range,
        );

        let mut csv = String::from("\u{FEFF}"); // UTF-8 BOM
        csv.push_str("时间,对话标题,项目工作区,模型,提示Tokens,补全Tokens,思考Tokens,总计Tokens,耗时(ms),预估费用,结算币种,会话ID\n");

        for item in paged.items {
            let safe_title = item.title.replace('"', "\"\"");
            let safe_ws = item.workspace.replace('"', "\"\"");
            csv.push_str(&format!(
                "\"{}\",\"{}\",\"{}\",\"{}\",{},{},{},{},{},{:.5},\"{}\",\"{}\"\n",
                item.timestamp,
                safe_title,
                safe_ws,
                item.model,
                item.prompt_tokens,
                item.candidate_tokens,
                item.thinking_tokens,
                item.prompt_tokens + item.output_tokens,
                item.latency_ms,
                item.cost,
                item.currency,
                item.conversation_id
            ));
        }

        csv
    }
}
