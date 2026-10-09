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
}

