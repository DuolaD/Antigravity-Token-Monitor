use crate::models::{ModelPricing, PricingConfig, PricingSettings};
use std::collections::HashMap;
use std::fs;
use std::path::PathBuf;
use std::sync::RwLock;

pub struct PricingService {
    config_path: PathBuf,
    settings: RwLock<PricingSettings>,
    pricing: RwLock<HashMap<String, ModelPricing>>,
}

impl PricingService {
    pub fn new() -> Self {
        let config_dir = dirs::config_dir()
            .unwrap_or_else(|| PathBuf::from("."))
            .join("AntigravityTokens");
        let _ = fs::create_dir_all(&config_dir);
        let config_path = config_dir.join("pricing.json");

        let service = Self {
            config_path,
            settings: RwLock::new(PricingSettings::default()),
            pricing: RwLock::new(HashMap::new()),
        };

        service.load_defaults();
        service.load_from_file();
        service
    }

    fn load_defaults(&self) {
        let mut map = self.pricing.write().unwrap();
        map.clear();

        let defaults = vec![
            ("gemini-3.8-flash", "Gemini 3.8 Flash", 0.15, 0.60),
            ("gemini-3.7-flash", "Gemini 3.7 Flash", 0.15, 0.60),
            ("gemini-3.7-flash-exp-b", "Gemini 3.7 Flash Exp B", 0.15, 0.60),
            ("gemini-3.7-flash-exp-a", "Gemini 3.7 Flash Exp A", 0.15, 0.60),
            ("gemini-3.7-flash-control", "Gemini 3.7 Flash Control", 0.15, 0.60),
            ("gemini-3p7-flash-exp-d", "Gemini 3.7 Flash Exp D", 0.15, 0.60),
            ("gemini-3-flash-a", "Gemini 3 Flash A", 0.15, 0.60),
            ("gemini-3.6-flash", "Gemini 3.6 Flash", 0.15, 0.60),
            ("gemini-default", "Gemini Default", 0.15, 0.60),
            ("gemini-pro-default", "Gemini Pro Default", 1.25, 5.00),
            ("gemini-pro-a", "Gemini Pro A", 1.25, 5.00),
            ("claude-opus-4-6-thinking", "Claude Opus 4.6 (Thinking)", 15.00, 75.00),
            ("claude-3-5-sonnet", "Claude 3.5 Sonnet", 3.00, 15.00),
            ("default", "默认兜底 (Fallback)", 0.20, 0.80),
        ];

        for (id, name, prompt_p, out_p) in defaults {
            map.insert(
                id.to_string(),
                ModelPricing {
                    model_id: id.to_string(),
                    display_name: name.to_string(),
                    prompt_price_per_million: prompt_p,
                    output_price_per_million: out_p,
                },
            );
        }
    }

    pub fn load_from_file(&self) {
        if !self.config_path.exists() {
            return;
        }

        if let Ok(content) = fs::read_to_string(&self.config_path) {
            if let Ok(cfg) = serde_json::from_str::<PricingConfig>(&content) {
                *self.settings.write().unwrap() = cfg.settings;
                let mut map = self.pricing.write().unwrap();
                for item in cfg.pricing {
                    map.insert(item.model_id.clone(), item);
                }
            }
        }
    }

    pub fn save_to_file(&self) {
        let cfg = self.get_config();
        if let Ok(json) = serde_json::to_string_pretty(&cfg) {
            let _ = fs::write(&self.config_path, json);
        }
    }

    pub fn get_config(&self) -> PricingConfig {
        let settings = self.settings.read().unwrap().clone();
        let map = self.pricing.read().unwrap();
        let mut list: Vec<ModelPricing> = map.values().cloned().collect();
        list.sort_by(|a, b| a.model_id.cmp(&b.model_id));
        PricingConfig {
            settings,
            pricing: list,
        }
    }

    pub fn save_config(&self, config: PricingConfig) {
        *self.settings.write().unwrap() = config.settings;
        let mut map = self.pricing.write().unwrap();
        for item in config.pricing {
            map.insert(item.model_id.clone(), item);
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

        let rate = if let Some(exact) = map.get(model) {
            exact
        } else {
            let lower = model.to_lowercase();
            if lower.contains("flash") {
                map.get("gemini-3.8-flash")
                    .or_else(|| map.get("default"))
                    .unwrap()
            } else if lower.contains("pro") {
                map.get("gemini-pro-default")
                    .or_else(|| map.get("default"))
                    .unwrap()
            } else if lower.contains("claude") || lower.contains("opus") {
                map.get("claude-opus-4-6-thinking")
                    .or_else(|| map.get("default"))
                    .unwrap()
            } else {
                map.get("default").unwrap()
            }
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
