use crate::data_service::TokenDataService;
use crate::models::{
    DailyTokenStat, FilterOptions, OverviewStats, PagedLogsResult, PricingConfig,
    SystemSettingsInfo, TokenTrendPoint,
};
use crate::pricing::PricingService;
use crate::watcher::LiveSyncWatcher;
use std::sync::Arc;
use tauri::State;

pub struct AppState {
    pub data_service: Arc<TokenDataService>,
    pub pricing_service: Arc<PricingService>,
    pub watcher: Arc<LiveSyncWatcher>,
}

#[tauri::command]
pub fn get_overview_stats(state: State<'_, AppState>) -> OverviewStats {
    state.data_service.get_overview_stats()
}

#[tauri::command]
pub fn get_trend_data(
    state: State<'_, AppState>,
    preset: String,
    custom_start: Option<String>,
    custom_end: Option<String>,
) -> Vec<TokenTrendPoint> {
    state.data_service.get_trend_data(
        &preset,
        custom_start.as_deref(),
        custom_end.as_deref(),
    )
}

#[tauri::command]
pub fn get_paged_logs(
    state: State<'_, AppState>,
    page: usize,
    page_size: usize,
    model_filter: Option<String>,
    ws_filter: Option<String>,
    search_query: Option<String>,
    date_range: Option<String>,
) -> PagedLogsResult {
    state.data_service.get_paged_logs(
        page,
        page_size,
        model_filter.as_deref(),
        ws_filter.as_deref(),
        search_query.as_deref(),
        date_range.as_deref(),
    )
}

#[tauri::command]
pub fn get_daily_stats(state: State<'_, AppState>) -> Vec<DailyTokenStat> {
    state.data_service.get_daily_stats()
}

#[tauri::command]
pub fn get_filter_options(state: State<'_, AppState>) -> FilterOptions {
    state.data_service.get_filter_options()
}

#[tauri::command]
pub fn get_pricing_config(state: State<'_, AppState>) -> PricingConfig {
    state.pricing_service.get_config()
}

#[tauri::command]
pub fn save_pricing_config(state: State<'_, AppState>, config: PricingConfig) {
    state.pricing_service.save_config(config);
}

#[tauri::command]
pub fn reset_pricing_config(state: State<'_, AppState>) -> PricingConfig {
    state.pricing_service.reset_defaults();
    state.pricing_service.get_config()
}

#[tauri::command]
pub fn export_logs_csv(
    state: State<'_, AppState>,
    model_filter: Option<String>,
    ws_filter: Option<String>,
    search_query: Option<String>,
    date_range: Option<String>,
    lang: Option<String>,
) -> String {
    state.data_service.export_logs_csv(
        model_filter.as_deref(),
        ws_filter.as_deref(),
        search_query.as_deref(),
        date_range.as_deref(),
        lang.as_deref(),
    )
}

#[tauri::command]
pub fn toggle_currency(state: State<'_, AppState>) -> String {
    state.pricing_service.toggle_currency()
}

#[tauri::command]
pub fn set_currency(state: State<'_, AppState>, currency: String) -> String {
    state.pricing_service.set_currency(&currency)
}

#[tauri::command]
pub fn update_exchange_rates(
    state: State<'_, AppState>,
    rates: std::collections::HashMap<String, f64>,
    date: Option<String>,
) {
    state.pricing_service.update_exchange_rates(rates, date);
}

#[tauri::command]
pub fn refresh_data(state: State<'_, AppState>) -> OverviewStats {
    state.data_service.refresh_all(true);
    state.data_service.get_overview_stats()
}

#[tauri::command]
pub fn get_system_settings(state: State<'_, AppState>) -> SystemSettingsInfo {
    let monitored_paths: Vec<String> = state
        .data_service
        .monitored_directories
        .iter()
        .map(|p| p.to_string_lossy().to_string())
        .collect();

    let live_sync_enabled = state.watcher.is_enabled();
    let os_name = if cfg!(target_os = "macos") {
        "macOS"
    } else if cfg!(target_os = "windows") {
        "Windows"
    } else {
        "Linux"
    };

    SystemSettingsInfo {
        monitored_paths,
        live_sync_enabled,
        app_version: env!("CARGO_PKG_VERSION").to_string(),
        os_name: os_name.to_string(),
    }
}

#[tauri::command]
pub fn set_live_sync_enabled(state: State<'_, AppState>, enabled: bool) {
    state.watcher.set_enabled(enabled);
}
