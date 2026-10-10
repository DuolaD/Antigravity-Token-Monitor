<template>
  <div class="space-y-5 pb-12">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-2">
          {{ t('logs.title') }}
        </h1>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">
          {{ t('logs.subtitle') }}
        </p>
      </div>

      <button
        @click="exportCsv"
        :disabled="isExporting"
        class="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-xs font-semibold text-white transition-colors shadow-sm disabled:opacity-50 cursor-pointer"
      >
        <FileSpreadsheet class="w-3.5 h-3.5" />
        <span>{{ isExporting ? t('logs.exporting') : t('logs.exportCsv') }}</span>
      </button>
    </div>

    <!-- Filter Toolbar -->
    <div class="card-glass p-3.5 rounded-xl">
      <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-2.5">
        <!-- Search Query -->
        <div class="lg:col-span-2 relative">
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="t('logs.searchPlaceholder')"
            class="w-full bg-white dark:bg-gray-900/80 border border-gray-200 dark:border-gray-700/80 rounded-lg px-3 py-1.5 text-xs text-gray-800 dark:text-gray-200 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:border-blue-500"
            @keyup.enter="applyFilter"
          />
        </div>

        <!-- Date Range -->
        <div>
          <select
            v-model="selectedDateRange"
            class="w-full bg-white dark:bg-gray-900/80 border border-gray-200 dark:border-gray-700/80 rounded-lg px-2.5 py-1.5 text-xs text-gray-800 dark:text-gray-200 focus:outline-none focus:border-blue-500"
            @change="applyFilter"
          >
            <option value="all">{{ t('logs.filters.allTime') }}</option>
            <option value="today">{{ t('logs.filters.today') }}</option>
            <option value="7d">{{ t('logs.filters.last7Days') }}</option>
            <option value="30d">{{ t('logs.filters.last30Days') }}</option>
          </select>
        </div>

        <!-- Model Filter -->
        <div>
          <select
            v-model="selectedModel"
            class="w-full bg-white dark:bg-gray-900/80 border border-gray-200 dark:border-gray-700/80 rounded-lg px-2.5 py-1.5 text-xs text-gray-800 dark:text-gray-200 focus:outline-none focus:border-blue-500"
            @change="applyFilter"
          >
            <option value="all">{{ t('logs.filters.allModels') }}</option>
            <option v-for="m in filterOptions.models" :key="m" :value="m">
              {{ m }}
            </option>
          </select>
        </div>

        <!-- Workspace Filter -->
        <div>
          <select
            v-model="selectedWorkspace"
            class="w-full bg-white dark:bg-gray-900/80 border border-gray-200 dark:border-gray-700/80 rounded-lg px-2.5 py-1.5 text-xs text-gray-800 dark:text-gray-200 focus:outline-none focus:border-blue-500"
            @change="applyFilter"
          >
            <option value="all">{{ t('logs.filters.allWorkspaces') }}</option>
            <option v-for="w in filterOptions.workspaces" :key="w" :value="w">
              {{ w === '默认工作区' ? t('common.defaultWorkspace') : w }}
            </option>
          </select>
        </div>

        <!-- Filter & Reset Buttons -->
        <div class="flex items-center gap-1.5">
          <button
            @click="applyFilter"
            class="flex-1 flex items-center justify-center gap-1 px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-xs font-semibold text-white transition-colors cursor-pointer"
          >
            <Filter class="w-3.5 h-3.5" />
            <span>{{ t('logs.filters.filterBtn') }}</span>
          </button>
          <button
            @click="resetFilter"
            class="px-2.5 py-1.5 rounded-lg bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 border border-gray-200 dark:border-gray-700 transition-colors cursor-pointer"
            :title="t('logs.filters.resetBtnTooltip')"
          >
            <RotateCcw class="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>

    <!-- Data Table Card -->
    <div class="card-glass rounded-xl overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-gray-100/70 dark:bg-gray-900/90 border-b border-gray-200 dark:border-gray-800 text-gray-600 dark:text-gray-400 font-semibold select-none">
              <th class="py-3 px-3.5 w-36">{{ t('logs.table.time') }}</th>
              <th class="py-3 px-3.5 min-w-[200px]">{{ t('logs.table.dialogWorkspace') }}</th>
              <th class="py-3 px-3.5 w-40">{{ t('logs.table.model') }}</th>
              <th class="py-3 px-3 w-24 text-right">{{ t('logs.table.promptTokens') }}</th>
              <th class="py-3 px-3 w-24 text-right">{{ t('logs.table.completionTokens') }}</th>
              <th class="py-3 px-3 w-24 text-right text-amber-600 dark:text-amber-400">{{ t('logs.table.thinkingTokens') }}</th>
              <th class="py-3 px-3 w-28 text-right font-bold text-gray-700 dark:text-gray-300">{{ t('logs.table.totalTokens') }}</th>
              <th class="py-3 px-3 w-20 text-right">{{ t('logs.table.latency') }}</th>
              <th class="py-3 px-3.5 w-24 text-right">{{ t('logs.table.cost') }}</th>
              <th class="py-3 px-2 w-12 text-center">{{ t('logs.table.details') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-800/60 font-mono">
            <tr v-if="logs.items.length === 0">
              <td colspan="10" class="py-12 text-center text-gray-400 dark:text-gray-500">
                {{ t('logs.table.empty') }}
              </td>
            </tr>
            <tr
              v-for="item in logs.items"
              :key="item.id"
              class="hover:bg-black/[0.02] dark:hover:bg-white/[0.02] transition-colors"
            >
              <!-- Time -->
              <td class="py-2.5 px-3.5 text-gray-500 dark:text-gray-400 text-[11px] whitespace-nowrap">
                {{ item.timestamp }}
              </td>

              <!-- Title & Workspace -->
              <td class="py-2.5 px-3.5 font-sans">
                <div class="font-semibold text-gray-800 dark:text-gray-200 truncate max-w-[280px]" :title="item.title">
                  {{ item.title }}
                </div>
                <div class="text-[11px] text-gray-400 dark:text-gray-500 truncate max-w-[280px]" :title="item.workspace_full">
                  {{ item.workspace === '默认工作区' ? t('common.defaultWorkspace') : item.workspace }}
                </div>
              </td>

              <!-- Model Badge -->
              <td class="py-2.5 px-3.5">
                <span class="inline-block px-2 py-0.5 rounded bg-sky-50 dark:bg-sky-950/60 border border-sky-200 dark:border-sky-800/40 text-sky-700 dark:text-sky-400 text-[10px] font-semibold truncate max-w-[150px]" :title="item.model">
                  {{ item.model }}
                </span>
              </td>

              <!-- Prompt -->
              <td class="py-2.5 px-3 text-right text-gray-600 dark:text-gray-400">
                {{ item.prompt_tokens.toLocaleString() }}
              </td>

              <!-- Candidate -->
              <td class="py-2.5 px-3 text-right text-gray-600 dark:text-gray-400">
                {{ item.candidate_tokens.toLocaleString() }}
              </td>

              <!-- Thinking -->
              <td class="py-2.5 px-3 text-right text-amber-600 dark:text-amber-400 font-semibold">
                {{ item.thinking_tokens.toLocaleString() }}
              </td>

              <!-- Total -->
              <td class="py-2.5 px-3 text-right font-bold text-gray-900 dark:text-gray-200">
                {{ (item.prompt_tokens + item.output_tokens).toLocaleString() }}
              </td>

              <!-- Latency -->
              <td class="py-2.5 px-3 text-right text-gray-500 dark:text-gray-400">
                {{ (item.latency_ms / 1000).toFixed(1) }}s
              </td>

              <!-- Cost -->
              <td class="py-2.5 px-3.5 text-right font-semibold text-emerald-600 dark:text-emerald-400">
                {{ getCurrencySymbol(item.currency) }}{{ item.cost.toFixed(5) }}
              </td>

              <!-- Detail Modal Button -->
              <td class="py-2.5 px-2 text-center">
                <button
                  @click="openDetail(item)"
                  class="p-1 rounded text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors cursor-pointer"
                  :title="t('logs.table.viewDetailsTooltip')"
                >
                  <Eye class="w-3.5 h-3.5" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination Footer -->
      <div class="p-3.5 bg-gray-50/80 dark:bg-gray-900/60 border-t border-gray-200 dark:border-gray-800 flex items-center justify-between text-xs text-gray-600 dark:text-gray-400 select-none">
        <div>
          {{ t('logs.pagination.pageInfo', { page: logs.page, pages: logs.total_pages, total: logs.total_count }) }}
        </div>

        <div class="flex items-center gap-2">
          <button
            @click="changePage(logs.page - 1)"
            :disabled="logs.page <= 1"
            class="px-2.5 py-1 rounded bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-40 disabled:hover:bg-white dark:disabled:hover:bg-gray-800 border border-gray-200 dark:border-transparent text-gray-700 dark:text-gray-200 transition-colors flex items-center gap-1 cursor-pointer disabled:cursor-not-allowed shadow-2xs"
          >
            <ChevronLeft class="w-3.5 h-3.5" />
            <span>{{ t('logs.pagination.prev') }}</span>
          </button>
          <button
            @click="changePage(logs.page + 1)"
            :disabled="logs.page >= logs.total_pages"
            class="px-2.5 py-1 rounded bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-40 disabled:hover:bg-white dark:disabled:hover:bg-gray-800 border border-gray-200 dark:border-transparent text-gray-700 dark:text-gray-200 transition-colors flex items-center gap-1 cursor-pointer disabled:cursor-not-allowed shadow-2xs"
          >
            <span>{{ t('logs.pagination.next') }}</span>
            <ChevronRight class="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>

    <!-- Detail Dialog Modal -->
    <div
      v-if="activeDetailItem"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 dark:bg-black/60 backdrop-blur-xs"
      @click.self="activeDetailItem = null"
    >
      <div class="w-full max-w-lg card-glass p-5 rounded-2xl border border-gray-200 dark:border-gray-700 shadow-2xl space-y-4">
        <div class="flex items-center justify-between border-b border-gray-100 dark:border-gray-800 pb-3">
          <div class="flex items-center gap-2 text-blue-600 dark:text-blue-400">
            <Info class="w-4 h-4" />
            <h3 class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ t('logs.modal.title') }}</h3>
          </div>
          <button
            @click="activeDetailItem = null"
            class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 p-1 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors cursor-pointer"
          >
            <X class="w-4 h-4" />
          </button>
        </div>

        <div class="space-y-2 text-xs font-mono">
          <div class="flex justify-between py-1 border-b border-gray-100 dark:border-gray-800/60">
            <span class="text-gray-500 dark:text-gray-400">{{ t('logs.modal.callId') }}</span>
            <span class="text-gray-800 dark:text-gray-200">{{ activeDetailItem.id }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-100 dark:border-gray-800/60">
            <span class="text-gray-500 dark:text-gray-400">{{ t('logs.modal.convUuid') }}</span>
            <span class="text-gray-800 dark:text-gray-200">{{ activeDetailItem.conversation_id }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-100 dark:border-gray-800/60">
            <span class="text-gray-500 dark:text-gray-400">{{ t('logs.modal.callTime') }}</span>
            <span class="text-gray-800 dark:text-gray-200">{{ activeDetailItem.timestamp }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-100 dark:border-gray-800/60">
            <span class="text-gray-500 dark:text-gray-400">{{ t('logs.modal.modelName') }}</span>
            <span class="text-sky-600 dark:text-sky-400 font-semibold">{{ activeDetailItem.model }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-100 dark:border-gray-800/60">
            <span class="text-gray-500 dark:text-gray-400">{{ t('logs.modal.workspace') }}</span>
            <span class="text-gray-800 dark:text-gray-200 truncate max-w-[280px]" :title="activeDetailItem.workspace_full">
              {{ activeDetailItem.workspace_full || (activeDetailItem.workspace === '默认工作区' ? t('common.defaultWorkspace') : activeDetailItem.workspace) }}
            </span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-100 dark:border-gray-800/60">
            <span class="text-gray-500 dark:text-gray-400">{{ t('logs.modal.promptTokens') }}</span>
            <span class="text-gray-800 dark:text-gray-200">{{ activeDetailItem.prompt_tokens.toLocaleString() }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-100 dark:border-gray-800/60">
            <span class="text-gray-500 dark:text-gray-400">{{ t('logs.modal.candidateTokens') }}</span>
            <span class="text-gray-800 dark:text-gray-200">{{ activeDetailItem.candidate_tokens.toLocaleString() }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-100 dark:border-gray-800/60">
            <span class="text-amber-600 dark:text-amber-400">{{ t('logs.modal.thinkingTokens') }}</span>
            <span class="text-amber-600 dark:text-amber-400 font-bold">{{ activeDetailItem.thinking_tokens.toLocaleString() }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-100 dark:border-gray-800/60">
            <span class="text-gray-500 dark:text-gray-400">{{ t('logs.modal.latency') }}</span>
            <span class="text-gray-800 dark:text-gray-200">{{ activeDetailItem.latency_ms }} ms</span>
          </div>
          <div class="flex justify-between py-1">
            <span class="text-emerald-600 dark:text-emerald-400">{{ t('logs.modal.cost') }}</span>
            <span class="text-emerald-600 dark:text-emerald-400 font-bold">
              {{ getCurrencySymbol(activeDetailItem.currency) }}{{ activeDetailItem.cost }} {{ activeDetailItem.currency }}
            </span>
          </div>
        </div>

        <div class="pt-2 flex justify-end">
          <button
            @click="activeDetailItem = null"
            class="px-4 py-1.5 rounded-lg bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-xs font-semibold text-gray-700 dark:text-gray-200 transition-colors cursor-pointer"
          >
            {{ t('logs.modal.close') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from "vue";
import { invoke } from "@tauri-apps/api/core";
import {
  FileSpreadsheet,
  Filter,
  RotateCcw,
  Eye,
  ChevronLeft,
  ChevronRight,
  Info,
  X,
} from "lucide-vue-next";
import type { FilterOptions, PagedLogsResult, TokenCallItem } from "../types";
import { getCurrencySymbol } from "../services/currency";
import { t, currentLocale } from "../services/i18n";

const searchQuery = ref("");
const selectedDateRange = ref("all");
const selectedModel = ref("all");
const selectedWorkspace = ref("all");

const filterOptions = reactive<FilterOptions>({
  models: [],
  workspaces: [],
});

const logs = reactive<PagedLogsResult>({
  items: [],
  total_count: 0,
  total_pages: 1,
  page: 1,
  page_size: 15,
});

const isExporting = ref(false);
const activeDetailItem = ref<TokenCallItem | null>(null);

const fetchFilterOptions = async () => {
  try {
    const opts = await invoke<FilterOptions>("get_filter_options");
    filterOptions.models = opts.models;
    filterOptions.workspaces = opts.workspaces;
  } catch (err) {
    console.error("fetchFilterOptions error:", err);
  }
};

const fetchLogs = async (page = 1) => {
  try {
    const res = await invoke<PagedLogsResult>("get_paged_logs", {
      page,
      pageSize: 15,
      modelFilter: selectedModel.value === "all" || selectedModel.value === "全部模型" ? null : selectedModel.value,
      wsFilter: selectedWorkspace.value === "all" || selectedWorkspace.value === "全部工作区" ? null : selectedWorkspace.value,
      searchQuery: searchQuery.value ? searchQuery.value : null,
      dateRange: selectedDateRange.value === "all" || selectedDateRange.value === "全部" ? null : selectedDateRange.value,
    });
    logs.items = res.items;
    logs.total_count = res.total_count;
    logs.total_pages = res.total_pages;
    logs.page = res.page;
    logs.page_size = res.page_size;
  } catch (err) {
    console.error("fetchLogs error:", err);
  }
};

const applyFilter = () => {
  fetchLogs(1);
};

const resetFilter = () => {
  searchQuery.value = "";
  selectedDateRange.value = "all";
  selectedModel.value = "all";
  selectedWorkspace.value = "all";
  fetchLogs(1);
};

const changePage = (p: number) => {
  if (p >= 1 && p <= logs.total_pages) {
    fetchLogs(p);
  }
};

const openDetail = (item: TokenCallItem) => {
  activeDetailItem.value = item;
};

const exportCsv = async () => {
  isExporting.value = true;
  try {
    const csvContent = await invoke<string>("export_logs_csv", {
      modelFilter: selectedModel.value === "all" || selectedModel.value === "全部模型" ? null : selectedModel.value,
      wsFilter: selectedWorkspace.value === "all" || selectedWorkspace.value === "全部工作区" ? null : selectedWorkspace.value,
      searchQuery: searchQuery.value ? searchQuery.value : null,
      dateRange: selectedDateRange.value === "all" || selectedDateRange.value === "全部" ? null : selectedDateRange.value,
      lang: currentLocale.value,
    });

    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.setAttribute("href", url);
    link.setAttribute(
      "download",
      `antigravity_tokens_${new Date().toISOString().slice(0, 10)}.csv`
    );
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  } catch (err) {
    console.error("exportCsv error:", err);
  } finally {
    isExporting.value = false;
  }
};

onMounted(async () => {
  await fetchFilterOptions();
  await fetchLogs(1);
});
</script>
