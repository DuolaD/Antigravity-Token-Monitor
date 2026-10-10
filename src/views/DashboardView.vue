<template>
  <div class="space-y-6 pb-12">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-2">
          {{ t('dashboard.title') }}
        </h1>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">
          {{ t('dashboard.subtitle') }}
        </p>
      </div>

      <div class="flex items-center gap-3">
        <!-- Currency Selector Dropdown -->
        <div class="relative" ref="currencyMenuRef">
          <button
            @click="isCurrencyMenuOpen = !isCurrencyMenuOpen"
            class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800/80 hover:bg-gray-50 dark:hover:bg-gray-700/80 text-xs font-semibold text-gray-700 dark:text-gray-200 transition-colors shadow-xs cursor-pointer"
            :title="t('dashboard.selectCurrency')"
          >
            <Coins class="w-3.5 h-3.5 text-amber-500 dark:text-amber-400" />
            <span>{{ stats?.currency || 'USD' }}</span>
            <ChevronDown class="w-3 h-3 text-gray-400" />
          </button>

          <!-- Dropdown popover -->
          <div
            v-if="isCurrencyMenuOpen"
            class="absolute right-0 mt-1.5 w-48 rounded-xl bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 shadow-2xl z-50 py-1 overflow-hidden animate-fade-in"
          >
            <button
              v-for="cur in SUPPORTED_CURRENCIES"
              :key="cur.code"
              @click="onSelectCurrency(cur.code)"
              class="w-full px-3 py-1.5 text-left text-xs flex items-center justify-between transition-colors hover:bg-gray-100 dark:hover:bg-gray-800 cursor-pointer"
              :class="stats?.currency === cur.code ? 'text-blue-600 dark:text-blue-400 font-bold bg-blue-50 dark:bg-blue-500/10' : 'text-gray-700 dark:text-gray-300'"
            >
              <span>{{ cur.code }} ({{ cur.symbol }})</span>
              <span class="text-[10px] text-gray-400 dark:text-gray-500">
                {{ getLocalizedCurrencyName(cur.code, currentLocale) }}
              </span>
            </button>
          </div>
        </div>

        <!-- Refresh Button -->
        <button
          @click="fetchData(true)"
          :disabled="isRefreshing"
          class="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-xs font-semibold text-white transition-colors shadow-sm disabled:opacity-50 cursor-pointer"
        >
          <RotateCw class="w-3.5 h-3.5" :class="{ 'animate-spin': isRefreshing }" />
          <span>{{ t('dashboard.refreshNow') }}</span>
        </button>
      </div>
    </div>

    <!-- 4 Core Metric Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <!-- Card 1: Total Cost -->
      <div class="card-glass p-4 rounded-xl space-y-3">
        <div class="flex items-center gap-2 text-emerald-600 dark:text-emerald-400">
          <BadgeDollarSign class="w-4 h-4" />
          <span class="text-xs font-medium text-gray-600 dark:text-gray-300">{{ t('dashboard.cards.totalCost') }}</span>
        </div>
        <div class="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-baseline">
          <RollingNumber
            :value="stats?.total_cost || 0"
            :prefix="stats?.currency_symbol || '$'"
            :decimals="4"
          />
        </div>
        <div class="pt-2 border-t border-gray-100 dark:border-white/5 flex items-center justify-between text-xs text-gray-500 dark:text-gray-400">
          <span>{{ t('dashboard.cards.todayCost') }}</span>
          <span class="font-semibold text-emerald-600 dark:text-emerald-400 font-mono">
            {{ stats?.currency_symbol || '$' }}{{ (stats?.today_cost || 0).toFixed(4) }}
          </span>
        </div>
      </div>

      <!-- Card 2: Total Tokens -->
      <div class="card-glass p-4 rounded-xl space-y-3">
        <div class="flex items-center gap-2 text-blue-600 dark:text-blue-400">
          <Cpu class="w-4 h-4" />
          <span class="text-xs font-medium text-gray-600 dark:text-gray-300">{{ t('dashboard.cards.totalTokens') }}</span>
        </div>
        <div class="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-baseline">
          <RollingNumber :value="stats?.total_tokens || 0" :decimals="0" />
        </div>
        <div class="pt-2 border-t border-gray-100 dark:border-white/5 space-y-1 text-xs">
          <div class="flex items-center justify-between text-gray-500 dark:text-gray-400">
            <span>{{ t('dashboard.cards.inputPrompt') }}</span>
            <span class="font-mono text-gray-700 dark:text-gray-300">{{ (stats?.total_prompt_tokens || 0).toLocaleString() }}</span>
          </div>
          <div class="flex items-center justify-between text-amber-600 dark:text-amber-400">
            <span>{{ t('dashboard.cards.thinking') }}</span>
            <span class="font-mono font-semibold">
              {{ (stats?.total_thinking_tokens || 0).toLocaleString() }} ({{ stats?.thinking_ratio_text || '0%' }})
            </span>
          </div>
        </div>
      </div>

      <!-- Card 3: Total Calls -->
      <div class="card-glass p-4 rounded-xl space-y-3">
        <div class="flex items-center gap-2 text-purple-600 dark:text-purple-400">
          <MessageSquareCode class="w-4 h-4" />
          <span class="text-xs font-medium text-gray-600 dark:text-gray-300">{{ t('dashboard.cards.totalCalls') }}</span>
        </div>
        <div class="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-baseline">
          <RollingNumber :value="stats?.total_calls || 0" :decimals="0" />
        </div>
        <div class="pt-2 border-t border-gray-100 dark:border-white/5 flex items-center justify-between text-xs text-gray-500 dark:text-gray-400">
          <span>{{ t('dashboard.cards.todayCalls') }}</span>
          <span class="font-semibold text-purple-600 dark:text-purple-400 font-mono">
            {{ stats?.today_calls || 0 }} {{ t('dashboard.cards.roundsUnit') }}
          </span>
        </div>
      </div>

      <!-- Card 4: Avg Speed & Latency -->
      <div class="card-glass p-4 rounded-xl space-y-3">
        <div class="flex items-center gap-2 text-amber-500 dark:text-amber-400">
          <Zap class="w-4 h-4" />
          <span class="text-xs font-medium text-gray-600 dark:text-gray-300">{{ t('dashboard.cards.avgSpeed') }}</span>
        </div>
        <div class="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-baseline">
          <RollingNumber :value="stats?.avg_speed_tokens_per_sec || 0" suffix=" tok/s" :decimals="1" />
        </div>
        <div class="pt-2 border-t border-gray-100 dark:border-white/5 flex items-center justify-between text-xs text-gray-500 dark:text-gray-400">
          <span class="flex items-center gap-1">
            <span>{{ t('dashboard.cards.avgLatencyShort') }}:</span>
            <span class="font-mono text-gray-700 dark:text-gray-300 font-medium">{{ (stats?.avg_latency_seconds || 0).toFixed(2) }}s</span>
          </span>
          <span class="flex items-center gap-1 text-gray-700 dark:text-gray-300">
            <span>{{ t('dashboard.cards.peakSpeedShort') }}:</span>
            <span class="font-mono font-semibold text-amber-600 dark:text-amber-400">{{ (stats?.peak_speed_tokens_per_sec || 0).toFixed(1) }}</span>
          </span>
        </div>
      </div>
    </div>

    <!-- Trend Chart Card -->
    <div class="card-glass p-5 rounded-xl space-y-4">
      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
        <div class="flex items-center gap-2">
          <TrendingUp class="w-4 h-4 text-blue-600 dark:text-blue-400" />
          <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ t('dashboard.trend.title') }}</h2>
          <span class="text-xs text-gray-500 dark:text-gray-400">· {{ activePresetLabel }}</span>
        </div>

        <!-- Presets Buttons -->
        <div class="flex items-center flex-wrap gap-1.5 text-xs">
          <button
            v-for="p in presetOptions"
            :key="p.id"
            @click="setPreset(p.id)"
            :class="[
              'px-2.5 py-1 rounded-md transition-colors cursor-pointer',
              activePreset === p.id
                ? 'bg-blue-600 text-white font-semibold'
                : 'bg-gray-100 dark:bg-gray-800/80 hover:bg-gray-200/80 dark:hover:bg-gray-700/80 text-gray-700 dark:text-gray-300 border border-gray-200 dark:border-gray-700/60'
            ]"
          >
            {{ p.label }}
          </button>
        </div>
      </div>

      <!-- Native SVG Trend Chart -->
      <TrendChart :data="trendPoints" :currency-symbol="stats?.currency_symbol || '$'" />
    </div>

    <!-- Rankings: Top Models & Top Workspaces -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
      <!-- Top Models -->
      <div class="card-glass p-5 rounded-xl space-y-4">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <Cpu class="w-4 h-4 text-blue-600 dark:text-blue-400" />
            <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ t('dashboard.rankings.topModels') }}</h2>
          </div>
          <span class="text-xs text-gray-400 dark:text-gray-500 font-mono">
            {{ t('dashboard.rankings.modelsCount', { n: stats?.top_models.length || 0 }) }}
          </span>
        </div>

        <div class="space-y-3">
          <div
            v-for="m in stats?.top_models"
            :key="m.model"
            class="space-y-1.5"
          >
            <div class="flex items-center justify-between text-xs">
              <div class="flex items-center gap-2">
                <span class="font-medium text-gray-800 dark:text-gray-200 font-mono">{{ m.model }}</span>
                <span
                  v-if="m.avg_speed > 0"
                  class="text-[10px] px-1.5 py-0.5 rounded bg-amber-500/10 text-amber-600 dark:text-amber-400 font-mono font-medium"
                >
                  {{ m.avg_speed.toFixed(1) }} tok/s
                </span>
                <span class="text-[10px] text-gray-400 dark:text-gray-500">
                  {{ t('dashboard.rankings.callsCount', { n: m.calls }) }}
                </span>
              </div>
              <div class="flex items-center gap-2 font-mono">
                <span class="text-gray-600 dark:text-gray-300">{{ m.tokens.toLocaleString() }}</span>
                <span class="font-semibold text-blue-600 dark:text-blue-400">{{ m.percentage }}%</span>
              </div>
            </div>
            <!-- Progress Bar -->
            <div class="w-full h-1.5 bg-gray-100 dark:bg-gray-800 rounded-full overflow-hidden">
              <div
                class="h-full bg-blue-500 rounded-full transition-all duration-500"
                :style="{ width: `${m.percentage}%` }"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Top Workspaces -->
      <div class="card-glass p-5 rounded-xl space-y-4">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <FolderGit2 class="w-4 h-4 text-purple-600 dark:text-purple-400" />
            <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ t('dashboard.rankings.topWorkspaces') }}</h2>
          </div>
          <span class="text-xs text-gray-400 dark:text-gray-500 font-mono">
            {{ t('dashboard.rankings.workspacesCount', { n: stats?.top_workspaces.length || 0 }) }}
          </span>
        </div>

        <div class="space-y-3">
          <div
            v-for="w in stats?.top_workspaces"
            :key="w.workspace"
            class="space-y-1.5"
          >
            <div class="flex items-center justify-between text-xs">
              <div class="flex items-center gap-2">
                <span class="font-medium text-gray-800 dark:text-gray-200 truncate max-w-[200px]" :title="w.workspace">
                  {{ w.workspace === '默认工作区' ? t('common.defaultWorkspace') : w.workspace }}
                </span>
                <span class="text-[10px] text-gray-400 dark:text-gray-500">
                  {{ t('dashboard.rankings.callsCount', { n: w.calls }) }}
                </span>
              </div>
              <div class="flex items-center gap-2 font-mono">
                <span class="text-gray-600 dark:text-gray-300">{{ w.tokens.toLocaleString() }}</span>
                <span class="font-semibold text-purple-600 dark:text-purple-400">{{ w.percentage }}%</span>
              </div>
            </div>
            <!-- Progress Bar -->
            <div class="w-full h-1.5 bg-gray-100 dark:bg-gray-800 rounded-full overflow-hidden">
              <div
                class="h-full bg-purple-500 rounded-full transition-all duration-500"
                :style="{ width: `${w.percentage}%` }"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from "vue";
import { invoke } from "@tauri-apps/api/core";
import { listen, type UnlistenFn } from "@tauri-apps/api/event";
import {
  Coins,
  RotateCw,
  BadgeDollarSign,
  Cpu,
  MessageSquareCode,
  Timer,
  Zap,
  TrendingUp,
  FolderGit2,
  ChevronDown,
} from "lucide-vue-next";
import type { OverviewStats, TokenTrendPoint } from "../types";
import RollingNumber from "../components/RollingNumber.vue";
import TrendChart from "../components/TrendChart.vue";
import { SUPPORTED_CURRENCIES, getLocalizedCurrencyName } from "../services/currency";
import { t, currentLocale } from "../services/i18n";

const stats = ref<OverviewStats | null>(null);
const trendPoints = ref<TokenTrendPoint[]>([]);
const isRefreshing = ref(false);

const isCurrencyMenuOpen = ref(false);
const currencyMenuRef = ref<HTMLElement | null>(null);

const activePreset = ref("all");

const presetOptions = computed(() => [
  { id: "week", label: t("dashboard.trend.presets.week") },
  { id: "month", label: t("dashboard.trend.presets.month") },
  { id: "quarter", label: t("dashboard.trend.presets.quarter") },
  { id: "halfYear", label: t("dashboard.trend.presets.halfYear") },
  { id: "year", label: t("dashboard.trend.presets.year") },
  { id: "all", label: t("dashboard.trend.presets.all") },
]);

const activePresetLabel = computed(() => {
  const opt = presetOptions.value.find((o) => o.id === activePreset.value);
  return opt ? opt.label : t("dashboard.trend.presets.all");
});

let unlistenUpdate: UnlistenFn | null = null;

const fetchData = async (force = false) => {
  isRefreshing.value = true;
  try {
    if (force) {
      stats.value = await invoke<OverviewStats>("refresh_data");
    } else {
      stats.value = await invoke<OverviewStats>("get_overview_stats");
    }
    await fetchTrend();
  } catch (err) {
    console.error("fetchData error:", err);
  } finally {
    isRefreshing.value = false;
  }
};

const fetchTrend = async () => {
  try {
    trendPoints.value = await invoke<TokenTrendPoint[]>("get_trend_data", {
      preset: activePreset.value,
      customStart: null,
      customEnd: null,
    });
  } catch (err) {
    console.error("fetchTrend error:", err);
  }
};

const setPreset = (p: string) => {
  activePreset.value = p;
  fetchTrend();
};

const onSelectCurrency = async (code: string) => {
  isCurrencyMenuOpen.value = false;
  try {
    await invoke("set_currency", { currency: code });
    await fetchData(false);
  } catch (err) {
    console.error("set_currency error:", err);
  }
};

const handleClickOutside = (e: MouseEvent) => {
  if (currencyMenuRef.value && !currencyMenuRef.value.contains(e.target as Node)) {
    isCurrencyMenuOpen.value = false;
  }
};

onMounted(async () => {
  await fetchData(false);
  window.addEventListener("click", handleClickOutside);

  // Listen to background live sync event
  try {
    unlistenUpdate = await listen("token_data_updated", () => {
      fetchData(false);
    });
  } catch (err) {
    console.error("listen error:", err);
  }
});

onUnmounted(() => {
  window.removeEventListener("click", handleClickOutside);
  if (unlistenUpdate) {
    unlistenUpdate();
  }
});
</script>
