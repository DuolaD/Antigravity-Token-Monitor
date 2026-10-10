import { invoke } from "@tauri-apps/api/core";

// Primary API: Cloudflare Pages mirror (as requested by user)
export const PRIMARY_API_URL =
  "https://latest.currency-api.pages.dev/v1/currencies/usd.json";

// Fallback API: jsDelivr CDN mirror
export const FALLBACK_API_URL =
  "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/usd.json";

export interface CurrencyOption {
  code: string;
  name: string;
  symbol: string;
  label: string;
}

export const SUPPORTED_CURRENCIES: CurrencyOption[] = [
  { code: "USD", name: "美元 (USD)", symbol: "$", label: "USD ($)" },
  { code: "CNY", name: "人民币 (CNY)", symbol: "¥", label: "CNY (¥)" },
  { code: "EUR", name: "欧元 (EUR)", symbol: "€", label: "EUR (€)" },
  { code: "JPY", name: "日元 (JPY)", symbol: "¥", label: "JPY (¥)" },
  { code: "GBP", name: "英镑 (GBP)", symbol: "£", label: "GBP (£)" },
  { code: "HKD", name: "港币 (HKD)", symbol: "HK$", label: "HKD (HK$)" },
  { code: "TWD", name: "新台币 (TWD)", symbol: "NT$", label: "TWD (NT$)" },
  { code: "KRW", name: "韩元 (KRW)", symbol: "₩", label: "KRW (₩)" },
  { code: "SGD", name: "新加坡元 (SGD)", symbol: "S$", label: "SGD (S$)" },
  { code: "CAD", name: "加拿大元 (CAD)", symbol: "CA$", label: "CAD (CA$)" },
  { code: "AUD", name: "澳大利亚元 (AUD)", symbol: "AU$", label: "AUD (AU$)" },
];

export const DEFAULT_OFFLINE_RATES: Record<string, number> = {
  USD: 1.0,
  CNY: 7.25,
  EUR: 0.90,
  JPY: 155.0,
  GBP: 0.76,
  HKD: 7.80,
  TWD: 32.0,
  KRW: 1350.0,
  SGD: 1.28,
  CAD: 1.42,
  AUD: 1.43,
};

export function getCurrencySymbol(code: string): string {
  const c = code?.toUpperCase();
  const found = SUPPORTED_CURRENCIES.find((item) => item.code === c);
  if (found) return found.symbol;
  if (c === "RMB") return "¥";
  return "$";
}

export interface FetchRatesResult {
  success: boolean;
  date?: string;
  source?: "primary" | "fallback" | "offline";
  rates: Record<string, number>;
  error?: string;
}

/**
 * Fetch latest exchange rates with primary and fallback endpoints.
 */
export async function fetchLatestRates(): Promise<FetchRatesResult> {
  const timeoutMs = 5000;

  // 1. Try Primary endpoint (Cloudflare Pages)
  try {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), timeoutMs);

    const resp = await fetch(PRIMARY_API_URL, {
      signal: controller.signal,
      cache: "no-cache",
    });
    clearTimeout(timer);

    if (resp.ok) {
      const json = await resp.json();
      if (json && json.usd) {
        const extracted = parseRatesFromPayload(json.usd);
        return {
          success: true,
          date: json.date,
          source: "primary",
          rates: extracted,
        };
      }
    }
  } catch (err) {
    console.warn("Primary currency API failed, falling back to jsdelivr CDN:", err);
  }

  // 2. Try Fallback endpoint (jsDelivr CDN)
  try {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), timeoutMs);

    const resp = await fetch(FALLBACK_API_URL, {
      signal: controller.signal,
      cache: "no-cache",
    });
    clearTimeout(timer);

    if (resp.ok) {
      const json = await resp.json();
      if (json && json.usd) {
        const extracted = parseRatesFromPayload(json.usd);
        return {
          success: true,
          date: json.date,
          source: "fallback",
          rates: extracted,
        };
      }
    }
  } catch (err) {
    console.warn("Fallback currency API failed:", err);
  }

  // 3. Both failed, use offline defaults
  return {
    success: false,
    date: new Date().toISOString().slice(0, 10),
    source: "offline",
    rates: { ...DEFAULT_OFFLINE_RATES },
    error: "无法连接到汇率网络接口，已加载离线基准汇率",
  };
}

function parseRatesFromPayload(usdMap: Record<string, number>): Record<string, number> {
  const result: Record<string, number> = { USD: 1.0 };
  for (const item of SUPPORTED_CURRENCIES) {
    const key = item.code.toLowerCase();
    if (usdMap[key] !== undefined && typeof usdMap[key] === "number") {
      result[item.code] = parseFloat(usdMap[key].toFixed(4));
    } else if (DEFAULT_OFFLINE_RATES[item.code]) {
      result[item.code] = DEFAULT_OFFLINE_RATES[item.code];
    }
  }
  return result;
}

/**
 * Sync exchange rates to Tauri backend.
 */
export async function syncExchangeRates(force: boolean = false): Promise<FetchRatesResult> {
  const res = await fetchLatestRates();
  if (res.success || force) {
    try {
      await invoke("update_exchange_rates", {
        rates: res.rates,
        date: res.date || new Date().toISOString().slice(0, 10),
      });
    } catch (err) {
      console.error("Failed to update backend exchange rates:", err);
    }
  }
  return res;
}
