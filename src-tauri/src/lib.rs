pub mod commands;
pub mod data_service;
pub mod db;
pub mod models;
pub mod pricing;
pub mod protobuf;
pub mod summaries;
pub mod watcher;

use commands::*;
use data_service::TokenDataService;
use pricing::PricingService;
use std::sync::Arc;
use watcher::LiveSyncWatcher;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let pricing_service = Arc::new(PricingService::new());
    let data_service = Arc::new(TokenDataService::new(pricing_service.clone()));
    let watcher = Arc::new(LiveSyncWatcher::new(data_service.clone()));

    // Initial load
    data_service.refresh_all(true);

    let app_state = AppState {
        data_service,
        pricing_service,
        watcher: watcher.clone(),
    };

    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .manage(app_state)
        .setup(move |app| {
            watcher.start(app.handle().clone());
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            get_overview_stats,
            get_trend_data,
            get_paged_logs,
            get_daily_stats,
            get_filter_options,
            get_pricing_config,
            save_pricing_config,
            reset_pricing_config,
            export_logs_csv,
            toggle_currency,
            set_currency,
            update_exchange_rates,
            refresh_data,
            get_system_settings,
            set_live_sync_enabled,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_local_data_discovery() {
        let pricing = Arc::new(PricingService::new());
        let data = TokenDataService::new(pricing.clone());
        println!("Monitored directories: {:?}", data.monitored_directories);
        data.refresh_all(true);
        let stats = data.get_overview_stats();
        println!("=== REAL LOCAL ANTIGRAVITY STATS ===");
        println!("Total tokens: {}", stats.total_tokens);
        println!("Prompt tokens: {}", stats.total_prompt_tokens);
        println!("Output tokens: {}", stats.total_output_tokens);
        println!("Thinking tokens: {} ({})", stats.total_thinking_tokens, stats.thinking_ratio_text);
        println!("Total calls: {}", stats.total_calls);
        println!("Total conversations: {}", stats.total_conversations);
        println!("Total cost: {}{}", stats.currency_symbol, stats.total_cost);
        println!("Top models count: {}", stats.top_models.len());
        for m in &stats.top_models {
            println!("  - {}: {} tokens ({} calls, {}%)", m.model, m.tokens, m.calls, m.percentage);
        }
        println!("Top workspaces count: {}", stats.top_workspaces.len());
        for w in &stats.top_workspaces {
            println!("  - {}: {} tokens ({} calls, {}%)", w.workspace, w.tokens, w.calls, w.percentage);
        }
        assert!(!data.monitored_directories.is_empty(), "Should discover local antigravity directories");
        assert!(stats.total_calls > 0, "Should successfully parse local conversation calls");
    }

    #[test]
    fn test_pricing_defaults_and_matching() {
        let pricing = PricingService::new_defaults_only();

        // 1. Exact match with lowercase
        let (cost_flash, cur) = pricing.calculate_cost(1_000_000, 1_000_000, "gemini-3.8-flash");
        assert_eq!(cur, "USD");
        assert!((cost_flash - 4.50).abs() < 1e-4, "Prompt $0.75 + Output $3.75 should be $4.50");

        // 2. Case-insensitive exact match
        let (cost_flash_mixed, _) = pricing.calculate_cost(1_000_000, 1_000_000, "Gemini-3.8-Flash");
        assert!((cost_flash_mixed - 4.50).abs() < 1e-4, "Mixed case should match exact model rate");

        // 3. Fallback heuristic for unlisted flash variant
        let (cost_flash_n, _) = pricing.calculate_cost(1_000_000, 1_000_000, "gemini-3.8-flash-n");
        assert!((cost_flash_n - 4.50).abs() < 1e-4, "Unlisted flash variant should match flash rate");

        // 4. Claude 3.5 Sonnet mixed case
        let (cost_sonnet, _) = pricing.calculate_cost(1_000_000, 1_000_000, "Claude-3-5-Sonnet");
        assert!((cost_sonnet - 18.00).abs() < 1e-4, "Prompt $3.00 + Output $15.00 should be $18.00");

        // 5. Claude Opus thinking
        let (cost_opus, _) = pricing.calculate_cost(1_000_000, 1_000_000, "claude-opus-4-6-thinking");
        assert!((cost_opus - 30.00).abs() < 1e-4, "Prompt $5.00 + Output $25.00 should be $30.00");
    }

    #[test]
    fn test_csharp_pascal_case_json_compatibility() {
        let csharp_json = r#"{
            "Settings": {
                "Currency": "CNY",
                "UsdToCnyRate": 6.711
            },
            "Pricing": [
                {
                    "ModelId": "gemini-3.8-flash",
                    "DisplayName": "Gemini 3.8 Flash",
                    "PromptPricePerMillion": 0.75,
                    "OutputPricePerMillion": 3.75
                }
            ]
        }"#;

        let parsed: Result<crate::models::PricingConfig, _> = serde_json::from_str(csharp_json);
        assert!(parsed.is_ok(), "Should parse C# PascalCase JSON successfully");
        let cfg = parsed.unwrap();
        assert_eq!(cfg.settings.currency, "CNY");
        assert!((cfg.settings.usd_to_cny_rate - 6.711).abs() < 1e-4);
        assert_eq!(cfg.pricing.len(), 1);
        assert_eq!(cfg.pricing[0].model_id, "gemini-3.8-flash");
        assert!((cfg.pricing[0].prompt_price_per_million - 0.75).abs() < 1e-4);
        assert!((cfg.pricing[0].output_price_per_million - 3.75).abs() < 1e-4);
    }

    #[test]
    fn test_multi_currency_and_rate_updates() {
        let pricing = PricingService::new_defaults_only();

        // 1. Check USD default cost
        let (cost_usd, cur_usd) = pricing.calculate_cost(1_000_000, 1_000_000, "gemini-3.8-flash");
        assert_eq!(cur_usd, "USD");
        assert!((cost_usd - 4.50).abs() < 1e-4);
        assert_eq!(PricingService::get_currency_symbol("USD"), "$");

        // 2. Set to EUR
        pricing.set_currency("EUR");
        let (cost_eur, cur_eur) = pricing.calculate_cost(1_000_000, 1_000_000, "gemini-3.8-flash");
        assert_eq!(cur_eur, "EUR");
        // default EUR rate is 0.90, so 4.50 * 0.90 = 4.05
        assert!((cost_eur - 4.05).abs() < 1e-4);
        assert_eq!(PricingService::get_currency_symbol("EUR"), "€");

        // 3. Update dynamic exchange rates from API payload
        let mut rates = std::collections::HashMap::new();
        rates.insert("EUR".to_string(), 0.8927);
        rates.insert("JPY".to_string(), 158.32);
        rates.insert("CNY".to_string(), 6.6968);
        pricing.update_exchange_rates(rates, Some("2026-10-10".to_string()));

        let (cost_eur_updated, _) = pricing.calculate_cost(1_000_000, 1_000_000, "gemini-3.8-flash");
        assert!((cost_eur_updated - (4.50 * 0.8927)).abs() < 1e-4);

        // 4. Set to JPY
        pricing.set_currency("JPY");
        let (cost_jpy, cur_jpy) = pricing.calculate_cost(1_000_000, 1_000_000, "gemini-3.8-flash");
        assert_eq!(cur_jpy, "JPY");
        assert!((cost_jpy - (4.50 * 158.32)).abs() < 1e-3);
        assert_eq!(PricingService::get_currency_symbol("JPY"), "¥");
    }

    #[test]
    fn test_multilingual_csv_headers() {
        let pricing = Arc::new(PricingService::new_defaults_only());
        let data = TokenDataService::new(pricing);

        let csv_zh_cn = data.export_logs_csv(None, None, None, None, Some("zh-CN"));
        assert!(csv_zh_cn.starts_with("\u{FEFF}时间,对话标题,项目工作区,模型"));

        let csv_zh_tw = data.export_logs_csv(None, None, None, None, Some("zh-TW"));
        assert!(csv_zh_tw.starts_with("\u{FEFF}時間,對話標題,專案工作區,模型"));

        let csv_en = data.export_logs_csv(None, None, None, None, Some("en"));
        assert!(csv_en.starts_with("\u{FEFF}Time,Conversation Title,Workspace,Model"));
    }

    #[test]
    fn test_multilingual_trend_and_filter_presets() {
        let pricing = Arc::new(PricingService::new_defaults_only());
        let data = TokenDataService::new(pricing);

        let t_week = data.get_trend_data("week", None, None);
        let t_zh_week = data.get_trend_data("周", None, None);
        assert_eq!(t_week.len(), t_zh_week.len());
        assert_eq!(t_week.len(), 7);

        let t_month = data.get_trend_data("month", None, None);
        let t_zh_month = data.get_trend_data("月", None, None);
        assert_eq!(t_month.len(), t_zh_month.len());
        assert_eq!(t_month.len(), 30);
    }
}

