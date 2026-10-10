<template>
  <div class="space-y-6 pb-12">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-100 flex items-center gap-2">
          总览看板
        </h1>
        <p class="text-xs text-gray-400 mt-1">
          实时监控 Google Antigravity 本地模型 Token 消耗与成本
        </p>
      </div>

      <div class="flex items-center gap-3">
        <!-- Currency Selector Dropdown -->
        <div class="relative" ref="currencyMenuRef">
          <button
            @click="isCurrencyMenuOpen = !isCurrencyMenuOpen"
            class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-700 bg-gray-800/80 hover:bg-gray-700/80 text-xs font-semibold text-gray-200 transition-colors shadow-sm"
            title="选择结算货币"
          >
            <Coins class="w-3.5 h-3.5 text-amber-400" />
            <span>{{ stats?.currency || 'USD' }}</span>
            <ChevronDown class="w-3 h-3 text-gray-400" />
          </button>

          <!-- Dropdown popover -->
          <div
            v-if="isCurrencyMenuOpen"
            class="absolute right-0 mt-1.5 w-44 rounded-xl bg-gray-900 border border-gray-700 shadow-2xl z-50 py-1 overflow-hidden animate-fade-in"
          >
            <button
              v-for="cur in SUPPORTED_CURRENCIES"
              :key="cur.code"
              @click="onSelectCurrency(cur.code)"
              class="w-full px-3 py-1.5 text-left text-xs flex items-center justify-between transition-colors hover:bg-gray-800"
              :class="stats?.currency === cur.code ? 'text-blue-400 font-bold bg-blue-500/10' : 'text-gray-300'"
            >
              <span>{{ cur.code }} ({{ cur.symbol }})</span>
              <span class="text-[10px] text-gray-500">{{ cur.name.split(' ')[0] }}</span>
            </button>
          </div>
        </div>

        <!-- Refresh Button -->
        <button
          @click="fetchData(true)"
          :disabled="isRefreshing"
          class="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-xs font-semibold text-white transition-colors shadow-sm disabled:opacity-50"
        >
          <RotateCw class="w-3.5 h-3.5" :class="{ 'animate-spin': isRefreshing }" />
          <span>立即刷新</span>
        </button>
      </div>
    </div>

    <!-- 4 Core Metric Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <!-- Card 1: Total Cost -->
      <div class="card-glass p-4 rounded-xl border border-white/5 space-y-3">
        <div class="flex items-center gap-2 text-emerald-400">
          <BadgeDollarSign class="w-4 h-4" />
          <span class="text-xs font-medium text-gray-300">累计预估花费</span>
        </div>
        <div class="text-2xl font-bold text-gray-100 flex items-baseline">
          <RollingNumber
            :value="stats?.total_cost || 0"
            :prefix="stats?.currency_symbol || '$'"
            :decimals="4"
          />
        </div>
        <div class="pt-2 border-t border-white/5 flex items-center justify-between text-xs text-gray-400">
          <span>今日消耗</span>
          <span class="font-semibold text-emerald-400 font-mono">
            {{ stats?.currency_symbol || '$' }}{{ (stats?.today_cost || 0).toFixed(4) }}
          </span>
        </div>
      </div>

      <!-- Card 2: Total Tokens -->
      <div class="card-glass p-4 rounded-xl border border-white/5 space-y-3">
        <div class="flex items-center gap-2 text-blue-400">
          <Cpu class="w-4 h-4" />
          <span class="text-xs font-medium text-gray-300">累计总 Tokens</span>
        </div>
        <div class="text-2xl font-bold text-gray-100 flex items-baseline">
          <RollingNumber :value="stats?.total_tokens || 0" :decimals="0" />
        </div>
        <div class="pt-2 border-t border-white/5 space-y-1 text-xs">
          <div class="flex items-center justify-between text-gray-400">
            <span>输入 Prompt:</span>
            <span class="font-mono text-gray-300">{{ (stats?.total_prompt_tokens || 0).toLocaleString() }}</span>
          </div>
          <div class="flex items-center justify-between text-amber-400">
            <span>思考 Thinking:</span>
            <span class="font-mono font-semibold">
              {{ (stats?.total_thinking_tokens || 0).toLocaleString() }} ({{ stats?.thinking_ratio_text || '0%' }})
            </span>
          </div>
        </div>
      </div>

      <!-- Card 3: Total Calls -->
      <div class="card-glass p-4 rounded-xl border border-white/5 space-y-3">
        <div class="flex items-center gap-2 text-purple-400">
          <MessageSquareCode class="w-4 h-4" />
          <span class="text-xs font-medium text-gray-300">累计请求轮次</span>
        </div>
        <div class="text-2xl font-bold text-gray-100 flex items-baseline">
          <RollingNumber :value="stats?.total_calls || 0" :decimals="0" />
        </div>
        <div class="pt-2 border-t border-white/5 flex items-center justify-between text-xs text-gray-400">
          <span>今日请求</span>
          <span class="font-semibold text-purple-400 font-mono">
            {{ stats?.today_calls || 0 }} 次
          </span>
        </div>
      </div>

      <!-- Card 4: Avg Latency -->
      <div class="card-glass p-4 rounded-xl border border-white/5 space-y-3">
        <div class="flex items-center gap-2 text-pink-400">
          <Timer class="w-4 h-4" />
          <span class="text-xs font-medium text-gray-300">平均响应耗时</span>
        </div>
        <div class="text-2xl font-bold text-gray-100 flex items-baseline">
          <RollingNumber :value="stats?.avg_latency_seconds || 0" suffix=" s" :decimals="2" />
        </div>
        <div class="pt-2 border-t border-white/5 flex items-center justify-between text-xs text-gray-400">
          <span>单轮交互耗时</span>
          <span class="text-gray-300">秒级平均响应</span>
        </div>
      </div>
    </div>

    <!-- Trend Chart Card -->
    <div class="card-glass p-5 rounded-xl border border-white/5 space-y-4">
      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
        <div class="flex items-center gap-2">
          <TrendingUp class="w-4 h-4 text-blue-400" />
          <h2 class="text-sm font-semibold text-gray-200">Token 消耗趋势折线图</h2>
          <span class="text-xs text-gray-400">· {{ activePreset }}</span>
        </div>

        <!-- Presets and Custom Range -->
        <div class="flex items-center flex-wrap gap-1.5 text-xs">
          <button
            v-for="p in presets"
            :key="p"
            @click="setPreset(p)"
            :class="[
              'px-2.5 py-1 rounded-md transition-colors',
              activePreset === p
                ? 'bg-blue-600 text-white font-semibold'
                : 'bg-gray-800/80 hover:bg-gray-700/80 text-gray-300 border border-gray-700/60'
            ]"
          >
            {{ p }}
          </button>
        </div>
      </div>

      <!-- Native SVG Trend Chart -->
      <TrendChart :data="trendPoints" :currency-symbol="stats?.currency_symbol || '$'" />
    </div>

    <!-- Rankings: Top Models & Top Workspaces -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
      <!-- Top Models -->
      <div class="card-glass p-5 rounded-xl border border-white/5 space-y-4">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <Cpu class="w-4 h-4 text-blue-400" />
            <h2 class="text-sm font-semibold text-gray-200">模型消耗排行榜</h2>
          </div>
          <span class="text-xs text-gray-500 font-mono">共 {{ stats?.top_models.length || 0 }} 个模型</span>
        </div>

        <div class="space-y-3">
          <div
            v-for="m in stats?.top_models"
            :key="m.model"
            class="space-y-1.5"
          >
            <div class="flex items-center justify-between text-xs">
              <div class="flex items-center gap-2">
                <span class="font-medium text-gray-200 font-mono">{{ m.model }}</span>
                <span class="text-[10px] text-gray-500">({{ m.calls }} 次)</span>
              </div>
              <div class="flex items-center gap-2 font-mono">
                <span class="text-gray-300">{{ m.tokens.toLocaleString() }}</span>
                <span class="font-semibold text-blue-400">{{ m.percentage }}%</span>
              </div>
            </div>
            <!-- Progress Bar -->
            <div class="w-full h-1.5 bg-gray-800 rounded-full overflow-hidden">
              <div
                class="h-full bg-blue-500 rounded-full transition-all duration-500"
                :style="{ width: `${m.percentage}%` }"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Top Workspaces -->
      <div class="card-glass p-5 rounded-xl border border-white/5 space-y-4">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <FolderGit2 class="w-4 h-4 text-purple-400" />
            <h2 class="text-sm font-semibold text-gray-200">项目/工作区消耗排行</h2>
          </div>
          <span class="text-xs text-gray-500 font-mono">共 {{ stats?.top_workspaces.length || 0 }} 个项目</span>
        </div>

        <div class="space-y-3">
          <div
            v-for="w in stats?.top_workspaces"
            :key="w.workspace"
            class="space-y-1.5"
          >
            <div class="flex items-center justify-between text-xs">
              <div class="flex items-center gap-2">
                <span class="font-medium text-gray-200 truncate max-w-[200px]" :title="w.workspace">
                  {{ w.workspace }}
                </span>
                <span class="text-[10px] text-gray-500">({{ w.calls }} 次)</span>
              </div>
              <div class="flex items-center gap-2 font-mono">
                <span class="text-gray-300">{{ w.tokens.toLocaleString() }}</span>
                <span class="font-semibold text-purple-400">{{ w.percentage }}%</span>
              </div>
            </div>
            <!-- Progress Bar -->
            <div class="w-full h-1.5 bg-gray-800 rounded-full overflow-hidden">
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
import { ref, onMounted, onUnmounted } from "vue";
import { invoke } from "@tauri-apps/api/core";
import { listen, type UnlistenFn } from "@tauri-apps/api/event";
import {
  Coins,
  RotateCw,
  BadgeDollarSign,
  Cpu,
  MessageSquareCode,
  Timer,
  TrendingUp,
  FolderGit2,
  ChevronDown,
} from "lucide-vue-next";
import type { OverviewStats, TokenTrendPoint } from "../types";
import RollingNumber from "../components/RollingNumber.vue";
import TrendChart from "../components/TrendChart.vue";
import { SUPPORTED_CURRENCIES } from "../services/currency";

const stats = ref<OverviewStats | null>(null);
const trendPoints = ref<TokenTrendPoint[]>([]);
const isRefreshing = ref(false);

const isCurrencyMenuOpen = ref(false);
const currencyMenuRef = ref<HTMLElement | null>(null);

const presets = ["周", "月", "季度", "半年", "-1年", "全部"];
const activePreset = ref("全部");

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
