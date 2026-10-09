use chrono::{DateTime, Local};
use regex::Regex;
use serde_json::Value;
use std::collections::HashMap;
use std::fs::{self, File};
use std::io::{BufRead, BufReader};
use std::path::Path;

pub struct SummariesService;

impl SummariesService {
    pub fn parse_summaries(path: &Path) -> HashMap<String, (String, String)> {
        let mut result = HashMap::new();
        if !path.exists() {
            return result;
        }

        let data = match fs::read(path) {
            Ok(d) => d,
            Err(_) => return result,
        };

        // Convert bytes to string lossily (Latin1/ISO-8859-1 equivalent for byte index scanning)
        let latin1_text: String = data.iter().map(|&b| b as char).collect();
        let uuid_regex = Regex::new(r"(?i)[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}").unwrap();
        let ws_regex = Regex::new(r"file:///([^\x00-\x1f\x7f-\x9f]+)").unwrap();

        let matches: Vec<_> = uuid_regex.find_iter(&latin1_text).collect();
        for i in 0..matches.len() {
            let m = matches[i];
            let uuid = m.as_str().to_string();
            let start = m.end();
            let next_start = if i + 1 < matches.len() {
                matches[i + 1].start()
            } else {
                std::cmp::min(latin1_text.len(), start + 800)
            };

            if next_start <= start {
                continue;
            }

            let chunk = &latin1_text[start..next_start];
            let mut ws = String::new();
            if let Some(ws_match) = ws_regex.captures(chunk) {
                if let Some(ws_path) = ws_match.get(1) {
                    if let Ok(unescaped) = urlencoding::decode(ws_path.as_str()) {
                        ws = unescaped.into_owned();
                    }
                }
            }

            // Extract title
            let chunk_bytes: Vec<u8> = chunk.chars().map(|c| c as u8).collect();
            let mut title = String::new();
            if chunk_bytes.len() >= 4 {
                for bi in 0..chunk_bytes.len() - 4 {
                    let b = chunk_bytes[bi];
                    if b == 0x12 || b == 0x0A {
                        let str_len = chunk_bytes[bi + 1] as usize;
                        if (3..=100).contains(&str_len) && bi + 2 + str_len <= chunk_bytes.len() {
                            if let Ok(s) = std::str::from_utf8(&chunk_bytes[bi + 2..bi + 2 + str_len]) {
                                let trimmed = s.trim();
                                if !trimmed.starts_with("file://")
                                    && !uuid_regex.is_match(trimmed)
                                    && trimmed.len() >= 2
                                {
                                    title = trimmed.to_string();
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if !result.contains_key(&uuid) || (!title.is_empty() && result[&uuid].0.is_empty()) {
                result.insert(uuid, (title, ws));
            }
        }

        result
    }

    pub fn parse_transcript_step0(path: &Path) -> (String, Option<DateTime<Local>>) {
        if !path.exists() {
            return (String::new(), None);
        }

        let file = match File::open(path) {
            Ok(f) => f,
            Err(_) => return (String::new(), None),
        };

        let reader = BufReader::new(file);
        let req_regex = Regex::new(r"(?s)<USER_REQUEST>(.*?)</USER_REQUEST>").unwrap();

        for line_res in reader.lines() {
            let line = match line_res {
                Ok(l) => l,
                Err(_) => continue,
            };

            if line.trim().is_empty() {
                continue;
            }

            let val: Value = match serde_json::from_str(&line) {
                Ok(v) => v,
                Err(_) => continue,
            };

            let step_index = val.get("step_index").and_then(|v| v.as_i64()).unwrap_or(-1);
            let created_at: Option<DateTime<Local>> = val
                .get("created_at")
                .and_then(|v| v.as_str())
                .and_then(|s| DateTime::parse_from_rfc3339(s).ok())
                .map(|dt| dt.with_timezone(&Local));

            if step_index == 0 {
                let content = val
                    .get("content")
                    .and_then(|v| v.as_str())
                    .unwrap_or("");

                let raw_text = if let Some(caps) = req_regex.captures(content) {
                    caps.get(1).map_or("", |m| m.as_str()).trim()
                } else {
                    content.trim()
                };

                let first_line = raw_text
                    .lines()
                    .map(|l| l.trim())
                    .find(|l| !l.is_empty())
                    .unwrap_or("");

                let title = if first_line.chars().count() > 50 {
                    let truncated: String = first_line.chars().take(50).collect();
                    format!("{}...", truncated)
                } else {
                    first_line.to_string()
                };

                return (title, created_at);
            }
        }

        (String::new(), None)
    }
}
