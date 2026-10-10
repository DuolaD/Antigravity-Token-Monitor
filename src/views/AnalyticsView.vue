<template>
  <div class="space-y-6 pb-12">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-2">
          {{ t('analytics.title') }}
        </h1>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">
          {{ t('analytics.subtitle') }}
        </p>
      </div>

      <button
        @click="fetchData"
        class="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 text-xs font-semibold text-gray-700 dark:text-gray-200 transition-colors border border-gray-200 dark:border-gray-700 shadow-xs cursor-pointer"
      >
        <RotateCw class="w-3.5 h-3.5" :class="{ 'animate-spin': isRefreshing }" />
        <span>{{ t('analytics.refresh') }}</span>
      </button>
    </div>

    <!-- 3 Highlight Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <!-- Card 1: Active Days Span -->
      <div class="card-glass p-5 rounded-xl space-y-2">
        <span class="text-xs font-medium text-gray-600 dark:text-gray-400">{{ t('analytics.cards.activeDays') }}</span>
        <div class="text-2xl font-bold text-blue-600 dark:text-blue-400 font-mono">
          {{ dailyStats.length }} <span class="text-xs text-gray-500 dark:text-gray-400 font-sans">{{ t('analytics.cards.activeDaysUnit') }}</span>
        </div>
        <p class="text-[11px] text-gray-400 dark:text-gray-500">{{ t('analytics.cards.activeDaysDesc') }}</p>
      </div>

      <!-- Card 2: Avg Daily Tokens -->
      <div class="card-glass p-5 rounded-xl space-y-2">
        <span class="text-xs font-medium text-gray-600 dark:text-gray-400">{{ t('analytics.cards.avgDailyTokens') }}</span>
        <div class="text-2xl font-bold text-emerald-600 dark:text-emerald-400 font-mono">
          {{ avgDailyTokens.toLocaleString() }}
        </div>
        <p class="text-[11px] text-gray-400 dark:text-gray-500">{{ t('analytics.cards.avgDailyDesc') }}</p>
      </div>

      <!-- Card 3: Overall Thinking Ratio -->
      <div class="card-glass p-5 rounded-xl space-y-2">
        <span class="text-xs font-medium text-gray-600 dark:text-gray-400">{{ t('analytics.cards.thinkingRatio') }}</span>
        <div class="text-2xl font-bold text-amber-600 dark:text-amber-400 font-mono">
          {{ overallThinkingRatioText }}
        </div>
        <p class="text-[11px] text-gray-400 dark:text-gray-500">{{ t('analytics.cards.thinkingRatioDesc') }}</p>
      </div>
    </div>

    <!-- Daily History Table Card -->
    <div class="card-glass rounded-xl overflow-hidden">
      <div class="p-4 bg-gray-50/80 dark:bg-gray-900/60 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <CalendarDays class="w-4 h-4 text-blue-600 dark:text-blue-400" />
          <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ t('analytics.table.title') }}</h2>
        </div>
        <span class="text-xs text-gray-500 dark:text-gray-400 font-mono">
          {{ t('analytics.table.daysCount', { n: dailyStats.length }) }}
        </span>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-gray-100/70 dark:bg-gray-900/90 border-b border-gray-200 dark:border-gray-800 text-gray-600 dark:text-gray-400 font-semibold select-none">
              <th class="py-3 px-4 w-32">{{ t('analytics.table.date') }}</th>
              <th class="py-3 px-4 text-right">{{ t('analytics.table.inputTokens') }}</th>
              <th class="py-3 px-4 text-right">{{ t('analytics.table.completionTokens') }}</th>
              <th class="py-3 px-4 text-right text-amber-600 dark:text-amber-400">{{ t('analytics.table.thinkingTokens') }}</th>
              <th class="py-3 px-4 text-right text-amber-600 dark:text-amber-400">{{ t('analytics.table.thinkingRatio') }}</th>
              <th class="py-3 px-4 text-right font-bold text-gray-700 dark:text-gray-300">{{ t('analytics.table.dailyTotal') }}</th>
              <th class="py-3 px-4 text-right">{{ t('analytics.table.calls') }}</th>
              <th class="py-3 px-4 text-right text-emerald-600 dark:text-emerald-400">{{ t('analytics.table.cost') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-800/60 font-mono">
            <tr v-if="dailyStats.length === 0">
              <td colspan="8" class="py-12 text-center text-gray-400 dark:text-gray-500">
                {{ t('analytics.table.empty') }}
              </td>
            </tr>
            <tr
              v-for="d in dailyStats"
              :key="d.date"
              class="hover:bg-black/[0.02] dark:hover:bg-white/[0.02] transition-colors"
            >
              <td class="py-2.5 px-4 font-semibold text-gray-800 dark:text-gray-200">
                {{ d.date }}
              </td>
              <td class="py-2.5 px-4 text-right text-gray-600 dark:text-gray-400">
                {{ d.prompt_tokens.toLocaleString() }}
              </td>
              <td class="py-2.5 px-4 text-right text-gray-600 dark:text-gray-400">
                {{ d.output_tokens.toLocaleString() }}
              </td>
              <td class="py-2.5 px-4 text-right text-amber-600 dark:text-amber-400 font-semibold">
                {{ d.thinking_tokens.toLocaleString() }}
              </td>
              <td class="py-2.5 px-4 text-right text-amber-600 dark:text-amber-400 font-bold">
                {{ d.thinking_ratio }}%
              </td>
              <td class="py-2.5 px-4 text-right font-bold text-gray-900 dark:text-gray-200">
                {{ d.total_tokens.toLocaleString() }}
              </td>
              <td class="py-2.5 px-4 text-right text-gray-700 dark:text-gray-300">
                {{ d.calls }} {{ t('analytics.table.callsUnit') }}
              </td>
              <td class="py-2.5 px-4 text-right font-semibold text-emerald-600 dark:text-emerald-400">
                {{ currencySymbol }}{{ d.cost.toFixed(4) }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { invoke } from "@tauri-apps/api/core";
import { RotateCw, CalendarDays } from "lucide-vue-next";
import type { DailyTokenStat, OverviewStats } from "../types";
import { t } from "../services/i18n";

const dailyStats = ref<DailyTokenStat[]>([]);
const currencySymbol = ref("$");
const isRefreshing = ref(false);

const fetchData = async () => {
  isRefreshing.value = true;
  try {
    dailyStats.value = await invoke<DailyTokenStat[]>("get_daily_stats");
    const overview = await invoke<OverviewStats>("get_overview_stats");
    currencySymbol.value = overview.currency_symbol || "$";
  } catch (err) {
    console.error("fetchData error:", err);
  } finally {
    isRefreshing.value = false;
  }
};

const avgDailyTokens = computed(() => {
  if (dailyStats.value.length === 0) return 0;
  const sum = dailyStats.value.reduce((acc, cur) => acc + cur.total_tokens, 0);
  return Math.round(sum / dailyStats.value.length);
});

const overallThinkingRatioText = computed(() => {
  const totalOut = dailyStats.value.reduce((acc, cur) => acc + cur.output_tokens, 0);
  const totalThinking = dailyStats.value.reduce((acc, cur) => acc + cur.thinking_tokens, 0);
  if (totalOut === 0) return "0.0%";
  return `${((totalThinking / totalOut) * 100).toFixed(1)}%`;
});

onMounted(() => {
  fetchData();
});
</script>
