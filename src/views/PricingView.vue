<template>
  <div class="space-y-6 pb-12">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-2">
          {{ t('pricing.title') }}
        </h1>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">
          {{ t('pricing.subtitle') }}
        </p>
      </div>

      <div class="flex items-center gap-2.5">
        <button
          @click="resetDefaults"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 text-xs font-semibold text-gray-700 dark:text-gray-300 transition-colors border border-gray-200 dark:border-gray-700 shadow-xs cursor-pointer"
        >
          <RotateCcw class="w-3.5 h-3.5" />
          <span>{{ t('pricing.resetDefaults') }}</span>
        </button>

        <button
          @click="savePricing"
          :disabled="isSaving"
          class="flex items-center gap-1.5 px-4 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-xs font-semibold text-white transition-colors shadow-sm disabled:opacity-50 cursor-pointer"
        >
          <Save class="w-3.5 h-3.5" />
          <span>{{ isSaving ? t('pricing.saving') : t('pricing.savePricing') }}</span>
        </button>
      </div>
    </div>

    <!-- Status Message Banner -->
    <div
      v-if="statusMessage"
      class="p-3 rounded-xl bg-emerald-50 dark:bg-emerald-950/60 border border-emerald-200 dark:border-emerald-800/60 text-emerald-800 dark:text-emerald-300 text-xs flex items-center justify-between animate-fade-in"
    >
      <div class="flex items-center gap-2">
        <CheckCircle2 class="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
        <span>{{ statusMessage }}</span>
      </div>
      <button @click="statusMessage = ''" class="text-emerald-600 dark:text-emerald-400 hover:opacity-80 cursor-pointer">
        <X class="w-3.5 h-3.5" />
      </button>
    </div>

    <!-- Currency & Exchange Rate Card -->
    <div class="card-glass p-5 rounded-xl space-y-4">
      <div class="flex items-center justify-between pb-3 border-b border-gray-100 dark:border-gray-800/80">
        <div class="flex items-center gap-2">
          <Globe class="w-4 h-4 text-sky-600 dark:text-sky-400" />
          <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ t('pricing.currencyCard.title') }}</h2>
        </div>
        <button
          @click="onSyncRates"
          :disabled="isSyncingRates"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-sky-50 dark:bg-sky-950/80 hover:bg-sky-100 dark:hover:bg-sky-900/80 border border-sky-200 dark:border-sky-800/60 text-sky-700 dark:text-sky-300 hover:text-sky-900 dark:hover:text-sky-100 text-xs font-medium transition-colors disabled:opacity-50 shadow-xs cursor-pointer"
          :title="t('pricing.currencyCard.apiInfo')"
        >
          <RefreshCw class="w-3.5 h-3.5" :class="{ 'animate-spin': isSyncingRates }" />
          <span>{{ isSyncingRates ? t('pricing.currencyCard.syncingRates') : t('pricing.currencyCard.syncRates') }}</span>
        </button>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
        <div class="space-y-2">
          <label class="text-xs font-semibold text-gray-700 dark:text-gray-300">{{ t('pricing.currencyCard.targetCurrency') }}</label>
          <select
            v-model="config.settings.currency"
            @change="onCurrencyChange"
            class="w-full bg-white dark:bg-gray-900/80 border border-gray-200 dark:border-gray-700/80 rounded-lg px-3 py-2 text-xs text-gray-800 dark:text-gray-200 focus:outline-none focus:border-blue-500 font-sans"
          >
            <option
              v-for="cur in SUPPORTED_CURRENCIES"
              :key="cur.code"
              :value="cur.code"
            >
              {{ cur.code }} ({{ cur.symbol }}) - {{ getLocalizedCurrencyName(cur.code, currentLocale) }}
            </option>
          </select>
        </div>

        <div class="space-y-2">
          <div class="flex items-center justify-between">
            <label class="text-xs font-semibold text-gray-700 dark:text-gray-300">
              {{ t('pricing.currencyCard.exchangeRateLabel', { cur: config.settings.currency }) }}
            </label>
            <span class="text-[10px] text-gray-400 dark:text-gray-500 font-mono">
              {{ config.settings.currency === 'USD' ? t('pricing.currencyCard.baseCurrency') : t('pricing.currencyCard.manualOverride') }}
            </span>
          </div>

          <div class="relative">
            <input
              v-if="config.settings.currency === 'USD'"
              value="1.0000"
              disabled
              type="text"
              class="w-full bg-gray-100 dark:bg-gray-900/40 border border-gray-200 dark:border-gray-800 rounded-lg px-3 py-2 text-xs text-gray-400 dark:text-gray-500 font-mono cursor-not-allowed"
            />
            <input
              v-else
              :value="currentExchangeRate"
              @input="onExchangeRateInput"
              type="number"
              step="0.0001"
              class="w-full bg-white dark:bg-gray-900/80 border border-gray-200 dark:border-gray-700/80 rounded-lg px-3 py-2 text-xs text-gray-800 dark:text-gray-200 focus:outline-none focus:border-blue-500 font-mono"
            />
          </div>
        </div>
      </div>

      <!-- Sync Status & Info -->
      <div class="pt-2 flex flex-wrap items-center justify-between text-[11px] text-gray-400 dark:text-gray-500 border-t border-gray-100 dark:border-gray-800/40">
        <div class="flex items-center gap-1.5">
          <span class="inline-block w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
          <span>{{ t('pricing.currencyCard.apiInfo') }}</span>
        </div>
        <div v-if="config.settings.last_rates_update" class="font-mono text-gray-500 dark:text-gray-400">
          {{ t('pricing.currencyCard.baseDate') }}{{ config.settings.last_rates_update }}
        </div>
      </div>
    </div>

    <!-- Pricing Table Card -->
    <div class="card-glass rounded-xl overflow-hidden">
      <div class="p-4 bg-gray-50/80 dark:bg-gray-900/60 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <Coins class="w-4 h-4 text-amber-500 dark:text-amber-400" />
          <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ t('pricing.table.title') }}</h2>
        </div>
        <span class="text-xs text-gray-400 dark:text-gray-500 font-mono">
          {{ t('pricing.table.rulesCount', { n: config.pricing.length }) }}
        </span>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-gray-100/70 dark:bg-gray-900/90 border-b border-gray-200 dark:border-gray-800 text-gray-600 dark:text-gray-400 font-semibold select-none">
              <th class="py-3 px-4 w-56">{{ t('pricing.table.modelId') }}</th>
              <th class="py-3 px-4">{{ t('pricing.table.displayName') }}</th>
              <th class="py-3 px-4 w-48 text-right">{{ t('pricing.table.promptPrice') }}</th>
              <th class="py-3 px-4 w-48 text-right">{{ t('pricing.table.outputPrice') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-800/60 font-mono">
            <tr
              v-for="item in config.pricing"
              :key="item.model_id"
              class="hover:bg-black/[0.02] dark:hover:bg-white/[0.02] transition-colors"
            >
              <!-- Model ID -->
              <td class="py-2.5 px-4 font-semibold text-sky-600 dark:text-sky-400">
                {{ item.model_id }}
              </td>

              <!-- Display Name -->
              <td class="py-2.5 px-4 font-sans">
                <input
                  v-model="item.display_name"
                  type="text"
                  class="w-full bg-white dark:bg-gray-900/60 border border-gray-200 dark:border-gray-700/60 rounded px-2.5 py-1 text-xs text-gray-800 dark:text-gray-200 focus:outline-none focus:border-blue-500 font-sans"
                />
              </td>

              <!-- Prompt Price -->
              <td class="py-2.5 px-4 text-right">
                <input
                  v-model.number="item.prompt_price_per_million"
                  type="number"
                  step="0.01"
                  class="w-32 bg-white dark:bg-gray-900/60 border border-gray-200 dark:border-gray-700/60 rounded px-2.5 py-1 text-xs text-right text-gray-800 dark:text-gray-200 focus:outline-none focus:border-blue-500 font-mono"
                />
              </td>

              <!-- Output Price -->
              <td class="py-2.5 px-4 text-right">
                <input
                  v-model.number="item.output_price_per_million"
                  type="number"
                  step="0.05"
                  class="w-32 bg-white dark:bg-gray-900/60 border border-gray-200 dark:border-gray-700/60 rounded px-2.5 py-1 text-xs text-right text-gray-800 dark:text-gray-200 focus:outline-none focus:border-blue-500 font-mono"
                />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from "vue";
import { invoke } from "@tauri-apps/api/core";
import {
  RotateCcw,
  Save,
  CheckCircle2,
  X,
  Coins,
  Globe,
  RefreshCw,
} from "lucide-vue-next";
import type { PricingConfig } from "../types";
import {
  SUPPORTED_CURRENCIES,
  DEFAULT_OFFLINE_RATES,
  syncExchangeRates,
  getLocalizedCurrencyName,
} from "../services/currency";
import { t, currentLocale } from "../services/i18n";

const config = reactive<PricingConfig>({
  settings: {
    currency: "USD",
    usd_to_cny_rate: 7.25,
    exchange_rates: { ...DEFAULT_OFFLINE_RATES },
    last_rates_update: null,
  },
  pricing: [],
});

const isSaving = ref(false);
const isSyncingRates = ref(false);
const statusMessage = ref("");

const currentExchangeRate = computed(() => {
  const cur = config.settings.currency?.toUpperCase() || "USD";
  if (cur === "USD") return 1.0;
  if (cur === "CNY") return config.settings.usd_to_cny_rate || 7.25;
  if (config.settings.exchange_rates && config.settings.exchange_rates[cur]) {
    return config.settings.exchange_rates[cur];
  }
  return DEFAULT_OFFLINE_RATES[cur] || 1.0;
});

const onExchangeRateInput = (e: Event) => {
  const val = parseFloat((e.target as HTMLInputElement).value);
  if (isNaN(val) || val <= 0) return;
  const cur = config.settings.currency?.toUpperCase() || "USD";
  if (!config.settings.exchange_rates) {
    config.settings.exchange_rates = { ...DEFAULT_OFFLINE_RATES };
  }
  config.settings.exchange_rates[cur] = val;
  if (cur === "CNY") {
    config.settings.usd_to_cny_rate = val;
  }
};

const onCurrencyChange = () => {
  const cur = config.settings.currency?.toUpperCase() || "USD";
  if (cur === "CNY") {
    if (config.settings.exchange_rates && config.settings.exchange_rates["CNY"]) {
      config.settings.usd_to_cny_rate = config.settings.exchange_rates["CNY"];
    }
  }
};

const fetchPricing = async () => {
  try {
    const res = await invoke<PricingConfig>("get_pricing_config");
    config.settings = res.settings;
    if (!config.settings.exchange_rates || Object.keys(config.settings.exchange_rates).length === 0) {
      config.settings.exchange_rates = { ...DEFAULT_OFFLINE_RATES };
    }
    config.pricing = res.pricing;
  } catch (err) {
    console.error("fetchPricing error:", err);
  }
};

const onSyncRates = async () => {
  isSyncingRates.value = true;
  try {
    const res = await syncExchangeRates(true);
    await fetchPricing();
    if (res.success) {
      const srcText = res.source === "primary" ? t("pricing.currencyCard.sourcePrimary") : t("pricing.currencyCard.sourceFallback");
      statusMessage.value = t("pricing.currencyCard.syncSuccess", {
        date: res.date || "today",
        source: srcText,
      });
    } else {
      statusMessage.value = res.error || t("pricing.currencyCard.syncFail");
    }
    setTimeout(() => {
      statusMessage.value = "";
    }, 5000);
  } catch (err) {
    console.error("onSyncRates error:", err);
  } finally {
    isSyncingRates.value = false;
  }
};

const savePricing = async () => {
  isSaving.value = true;
  try {
    await invoke("save_pricing_config", { config });
    statusMessage.value = t("pricing.savedSuccess");
    setTimeout(() => {
      statusMessage.value = "";
    }, 4000);
  } catch (err) {
    console.error("savePricing error:", err);
  } finally {
    isSaving.value = false;
  }
};

const resetDefaults = async () => {
  try {
    const res = await invoke<PricingConfig>("reset_pricing_config");
    config.settings = res.settings;
    config.pricing = res.pricing;
    statusMessage.value = t("pricing.resetSuccess");
    setTimeout(() => {
      statusMessage.value = "";
    }, 4000);
  } catch (err) {
    console.error("resetDefaults error:", err);
  }
};

onMounted(async () => {
  await fetchPricing();
  const today = new Date().toISOString().slice(0, 10);
  if (!config.settings.last_rates_update || config.settings.last_rates_update !== today) {
    syncExchangeRates(false).then((res) => {
      if (res.success) {
        fetchPricing();
      }
    });
  }
});
</script>
