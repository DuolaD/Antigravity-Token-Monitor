export interface ModelPricing {
  model_id: string;
  display_name: string;
  prompt_price_per_million: number;
  output_price_per_million: number;
}

export interface PricingSettings {
  currency: string;
  usd_to_cny_rate: number;
  exchange_rates?: Record<string, number>;
  last_rates_update?: string | null;
}

export interface PricingConfig {
  settings: PricingSettings;
  pricing: ModelPricing[];
}

export interface TokenCallItem {
  id: string;
  conversation_id: string;
  title: string;
  workspace: string;
  workspace_full: string;
  timestamp: string;
  timestamp_epoch: number;
  model: string;
  prompt_tokens: number;
  output_tokens: number;
  thinking_tokens: number;
  candidate_tokens: number;
  latency_ms: number;
  speed_tokens_per_sec: number;
  cost: number;
  currency: string;
  step_index: number | null;
}

export interface ModelUsageStat {
  model: string;
  tokens: number;
  calls: number;
  cost: number;
  percentage: number;
  avg_speed: number;
}

export interface WorkspaceUsageStat {
  workspace: string;
  tokens: number;
  calls: number;
  cost: number;
  percentage: number;
}

export interface OverviewStats {
  total_tokens: number;
  total_prompt_tokens: number;
  total_output_tokens: number;
  total_thinking_tokens: number;
  total_candidate_tokens: number;
  thinking_ratio_text: string;
  total_calls: number;
  total_conversations: number;
  total_cost: number;
  avg_latency_seconds: number;
  avg_speed_tokens_per_sec: number;
  peak_speed_tokens_per_sec: number;
  currency: string;
  currency_symbol: string;

  today_tokens: number;
  today_prompt_tokens: number;
  today_output_tokens: number;
  today_thinking_tokens: number;
  today_calls: number;
  today_cost: number;

  top_models: ModelUsageStat[];
  top_workspaces: WorkspaceUsageStat[];
}

export interface TokenTrendPoint {
  date: string;
  date_label: string;
  full_date_label: string;
  prompt_tokens: number;
  output_tokens: number;
  thinking_tokens: number;
  total_tokens: number;
  calls: number;
  cost: number;
  avg_speed: number;
}

export interface DailyTokenStat {
  date: string;
  prompt_tokens: number;
  output_tokens: number;
  thinking_tokens: number;
  thinking_ratio: number;
  total_tokens: number;
  calls: number;
  cost: number;
  avg_speed: number;
}

export interface PagedLogsResult {
  items: TokenCallItem[];
  total_count: number;
  total_pages: number;
  page: number;
  page_size: number;
}

export interface FilterOptions {
  models: string[];
  workspaces: string[];
}

export interface SystemSettingsInfo {
  monitored_paths: string[];
  live_sync_enabled: boolean;
  app_version: string;
  os_name: string;
}
