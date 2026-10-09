use crate::models::{ModelPricing, PricingConfig, PricingSettings};
use std::collections::HashMap;
use std::fs;
use std::path::{Path, PathBuf};
use std::sync::RwLock;

const DEFAULT_ORDER: &[&str] = &[
    "gemini-3.8-flash",
    "gemini-3.7-flash",
    "gemini-3.7-flash-exp-b",
    "gemini-3.7-flash-exp-a",
    "gemini-3.7-flash-control",
    "gemini-3p7-flash-exp-d",
    "gemini-3-flash-a",
    "gemini-3.6-flash",
    "gemini-default",
    "gemini-pro-default",
    "gemini-pro-a",
    "claude-opus-4-6-thinking",
    "claude-3-5-sonnet",
    "default",
];

pub struct PricingService {
    config_path: PathBuf,
    settings: RwLock<PricingSettings>,
    pricing: RwLock<HashMap<String, ModelPricing>>,
}

impl PricingService {
    fn resolve_config_paths() -> (PathBuf, Option<PathBuf>) {
        #[cfg(target_os = "windows")]
        {
            // Primary path: AppData\Local\AntigravityTokens\pricing.json (aligns with C# WinUI 3)
            let local_dir = dirs::data_local_dir()
                .unwrap_or_else(|| dirs::config_dir().unwrap_or_else(|| PathBuf::from(".")))
                .join("AntigravityTokens");
            let primary = local_dir.join("pricing.json");

            // Fallback path: AppData\Roaming\AntigravityTokens\pricing.json (for previous Tauri versions)
            let roaming_path = dirs::config_dir()
                .map(|p| p.join("AntigravityTokens").join("pricing.json"));

            (primary, roaming_path)
        }
        #[cfg(not(target_os = "windows"))]
        {
            let config_dir = dirs::config_dir()
                .unwrap_or_else(|| PathBuf::from("."))
                .join("AntigravityTokens");
            let primary = config_dir.join("pricing.json");
            (primary, None)
        }
    }

    pub fn new() -> Self {
        let (config_path, fallback_path) = Self::resolve_config_paths();
        if let Some(parent) = config_path.parent() {
            let _ = fs::create_dir_all(parent);
        }

        let service = Self {
            config_path,
            settings: RwLock::new(PricingSettings::default()),
            pricing: RwLock::new(HashMap::new()),
        };

        service.load_defaults();
        service.load_from_file(fallback_path.as_deref());
        service
    }

    pub fn new_defaults_only() -> Self {
        let service = Self {
            config_path: PathBuf::from("pricing_memory.json"),
            settings: RwLock::new(PricingSettings::default()),
            pricing: RwLock::new(HashMap::new()),
        };
        service.load_defaults();
        service
    }

    fn load_defaults(&self) {
        let mut map = self.pricing.write().unwrap();
        map.clear();

        // Aligned with Google official API rates for Gemini 3.8 Flash ($0.75/$3.75 per 1M tokens)
        // and user verified pricing preset.
        let defaults = vec![
            ("gemini-3.8-flash", "Gemini 3.8 Flash", 0.75, 3.75),
            ("gemini-3.7-flash", "Gemini 3.7 Flash", 0.75, 3.75),
            ("gemini-3.7-flash-exp-b", "Gemini 3.7 Flash Exp B", 0.75, 3.75),
            ("gemini-3.7-flash-exp-a", "Gemini 3.7 Flash Exp A", 0.75, 3.75),
            ("gemini-3.7-flash-control", "Gemini 3.7 Flash Control", 0.75, 3.75),
            ("gemini-3p7-flash-exp-d", "Gemini 3.7 Flash Exp D", 0.75, 3.75),
            ("gemini-3-flash-a", "Gemini 3 Flash A", 0.75, 3.75),
            ("gemini-3.6-flash", "Gemini 3.6 Flash", 0.75, 3.75),
            ("gemini-default", "Gemini Default", 0.75, 3.75),
            ("gemini-pro-default", "Gemini Pro Default", 0.75, 3.75),
            ("gemini-pro-a", "Gemini Pro A", 0.75, 3.75),
            ("claude-opus-4-6-thinking", "Claude Opus 4.6 (Thinking)", 5.00, 25.00),
            ("claude-3-5-sonnet", "Claude 3.5 Sonnet", 3.00, 15.00),
            ("default", "默认兜底 (Fallback)", 0.75, 3.75),
        ];

        for (id, name, prompt_p, out_p) in defaults {
            map.insert(
                id.to_lowercase(),
                ModelPricing {
                    model_id: id.to_string(),
                    display_name: name.to_string(),
                    prompt_price_per_million: prompt_p,
                    output_price_per_million: out_p,
                },
            );
        }
    }

    pub fn load_from_file(&self, fallback_path: Option<&Path>) {
        let target_path: Option<&Path> = if self.config_path.exists() {
            Some(self.config_path.as_path())
        } else if let Some(fb) = fallback_path {
            if fb.exists() {
                Some(fb)
            } else {
                None
            }
        } else {
            None
        };

        if let Some(path) = target_path {
            if let Ok(content) = fs::read_to_string(path) {
                if let Ok(cfg) = serde_json::from_str::<PricingConfig>(&content) {
                    *self.settings.write().unwrap() = cfg.settings;
                    let mut map = self.pricing.write().unwrap();
                    for item in cfg.pricing {
                        map.insert(item.model_id.to_lowercase(), item);
                    }
                }
            }
        }
    }

    pub fn save_to_file(&self) {
        if let Some(parent) = self.config_path.parent() {
            let _ = fs::create_dir_all(parent);
        }
        let cfg = self.get_config();
        if let Ok(json) = serde_json::to_string_pretty(&cfg) {
            let _ = fs::write(&self.config_path, json);
        }
    }

    pub fn get_config(&self) -> PricingConfig {
        let settings = self.settings.read().unwrap().clone();
        let map = self.pricing.read().unwrap();
        let mut list: Vec<ModelPricing> = map.values().cloned().collect();
        list.sort_by_key(|item| {
            let id_lower = item.model_id.to_lowercase();
            DEFAULT_ORDER
                .iter()
                .position(|&id| id == id_lower)
                .unwrap_or(usize::MAX)
        });
        PricingConfig {
            settings,
            pricing: list,
        }
    }

    pub fn save_config(&self, config: PricingConfig) {
        *self.settings.write().unwrap() = config.settings;
        let mut map = self.pricing.write().unwrap();
        for item in config.pricing {
            map.insert(item.model_id.to_lowercase(), item);
        }
        drop(map);
        self.save_to_file();
    }

    pub fn reset_defaults(&self) {
        self.load_defaults();
        *self.settings.write().unwrap() = PricingSettings::default();
        self.save_to_file();
    }

    pub fn get_currency(&self) -> String {
        self.settings.read().unwrap().currency.clone()
    }

    pub fn toggle_currency(&self) -> String {
        let mut s = self.settings.write().unwrap();
        if s.currency == "USD" {
            s.currency = "CNY".to_string();
        } else {
            s.currency = "USD".to_string();
        }
        let cur = s.currency.clone();
        drop(s);
        self.save_to_file();
        cur
    }

    pub fn calculate_cost(&self, prompt_tokens: i64, output_tokens: i64, model: &str) -> (f64, String) {
        let map = self.pricing.read().unwrap();
        let settings = self.settings.read().unwrap();

        let lower = model.to_lowercase();
        let rate = if let Some(exact) = map.get(&lower) {
            exact
        } else if lower.contains("flash") {
            map.get("gemini-3.8-flash")
                .or_else(|| map.get("default"))
                .unwrap()
        } else if lower.contains("pro") {
            map.get("gemini-pro-default")
                .or_else(|| map.get("default"))
                .unwrap()
        } else if lower.contains("sonnet") {
            map.get("claude-3-5-sonnet")
                .or_else(|| map.get("claude-opus-4-6-thinking"))
                .or_else(|| map.get("default"))
                .unwrap()
        } else if lower.contains("claude") || lower.contains("opus") {
            map.get("claude-opus-4-6-thinking")
                .or_else(|| map.get("default"))
                .unwrap()
        } else {
            map.get("default").unwrap()
        };

        let prompt_usd = (prompt_tokens as f64 / 1_000_000.0) * rate.prompt_price_per_million;
        let output_usd = (output_tokens as f64 / 1_000_000.0) * rate.output_price_per_million;
        let total_usd = prompt_usd + output_usd;

        let final_cost = if settings.currency == "CNY" {
            total_usd * settings.usd_to_cny_rate
        } else {
            total_usd
        };

        (final_cost, settings.currency.clone())
    }
}
