use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ModelPricing {
    #[serde(alias = "ModelId")]
    pub model_id: String,
    #[serde(alias = "DisplayName")]
    pub display_name: String,
    #[serde(alias = "PromptPricePerMillion")]
    pub prompt_price_per_million: f64,
    #[serde(alias = "OutputPricePerMillion")]
    pub output_price_per_million: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PricingSettings {
    #[serde(alias = "Currency")]
    pub currency: String, // "USD" or "CNY"
    #[serde(alias = "UsdToCnyRate")]
    pub usd_to_cny_rate: f64,
}

impl Default for PricingSettings {
    fn default() -> Self {
        Self {
            currency: "USD".to_string(),
            usd_to_cny_rate: 7.25,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PricingConfig {
    #[serde(alias = "Settings")]
    pub settings: PricingSettings,
    #[serde(alias = "Pricing")]
    pub pricing: Vec<ModelPricing>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TokenCallItem {
    pub id: String,
    pub conversation_id: String,
    pub title: String,
    pub workspace: String,
    pub workspace_full: String,
    pub timestamp: String, // formatted ISO or local string
    pub timestamp_epoch: i64,
    pub model: String,
    pub prompt_tokens: i64,
    pub output_tokens: i64,
    pub thinking_tokens: i64,
    pub candidate_tokens: i64,
    pub latency_ms: i64,
    pub cost: f64,
    pub currency: String,
    pub step_index: Option<i32>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ConversationSummary {
    pub id: String,
    pub title: String,
    pub workspace: String,
    pub created_at: i64,
    pub updated_at: i64,
    pub db_path: String,
    pub total_calls: usize,
    pub total_prompt_tokens: i64,
    pub total_output_tokens: i64,
    pub total_thinking_tokens: i64,
    pub total_candidate_tokens: i64,
    pub total_latency_ms: i64,
    pub total_cost: f64,
    pub models: Vec<String>,
    pub calls: Vec<TokenCallItem>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ModelUsageStat {
    pub model: String,
    pub tokens: i64,
    pub calls: usize,
    pub cost: f64,
    pub percentage: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WorkspaceUsageStat {
    pub workspace: String,
    pub tokens: i64,
    pub calls: usize,
    pub cost: f64,
    pub percentage: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct OverviewStats {
    pub total_tokens: i64,
    pub total_prompt_tokens: i64,
    pub total_output_tokens: i64,
    pub total_thinking_tokens: i64,
    pub total_candidate_tokens: i64,
    pub thinking_ratio_text: String,
    pub total_calls: usize,
    pub total_conversations: usize,
    pub total_cost: f64,
    pub avg_latency_seconds: f64,
    pub currency: String,
    pub currency_symbol: String,

    pub today_tokens: i64,
    pub today_prompt_tokens: i64,
    pub today_output_tokens: i64,
    pub today_thinking_tokens: i64,
    pub today_calls: usize,
    pub today_cost: f64,

    pub top_models: Vec<ModelUsageStat>,
    pub top_workspaces: Vec<WorkspaceUsageStat>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TokenTrendPoint {
    pub date: String,
    pub date_label: String,
    pub full_date_label: String,
    pub prompt_tokens: i64,
    pub output_tokens: i64,
    pub thinking_tokens: i64,
    pub total_tokens: i64,
    pub calls: usize,
    pub cost: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DailyTokenStat {
    pub date: String,
    pub prompt_tokens: i64,
    pub output_tokens: i64,
    pub thinking_tokens: i64,
    pub thinking_ratio: f64,
    pub total_tokens: i64,
    pub calls: usize,
    pub cost: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PagedLogsResult {
    pub items: Vec<TokenCallItem>,
    pub total_count: usize,
    pub total_pages: usize,
    pub page: usize,
    pub page_size: usize,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FilterOptions {
    pub models: Vec<String>,
    pub workspaces: Vec<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SystemSettingsInfo {
    pub monitored_paths: Vec<String>,
    pub live_sync_enabled: bool,
    pub app_version: String,
    pub os_name: String,
}
