<template>
  <div class="space-y-5 pb-12">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-100 flex items-center gap-2">
          日志明细
        </h1>
        <p class="text-xs text-gray-400 mt-1">
          查看每一次 LLM 调用的精确输入、输出、思考 Token 与耗时记录
        </p>
      </div>

      <button
        @click="exportCsv"
        :disabled="isExporting"
        class="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-xs font-semibold text-white transition-colors shadow-sm disabled:opacity-50"
      >
        <FileSpreadsheet class="w-3.5 h-3.5" />
        <span>{{ isExporting ? '正在导出...' : '导出 CSV 报表' }}</span>
      </button>
    </div>

    <!-- Filter Toolbar -->
    <div class="card-glass p-3.5 rounded-xl border border-white/5">
      <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-2.5">
        <!-- Search Query -->
        <div class="lg:col-span-2 relative">
          <input
            v-model="searchQuery"
            type="text"
            placeholder="搜索标题 / 模型 / UUID..."
            class="w-full bg-gray-900/80 border border-gray-700/80 rounded-lg px-3 py-1.5 text-xs text-gray-200 placeholder-gray-500 focus:outline-none focus:border-blue-500"
            @keyup.enter="applyFilter"
          />
        </div>

        <!-- Date Range -->
        <div>
          <select
            v-model="selectedDateRange"
            class="w-full bg-gray-900/80 border border-gray-700/80 rounded-lg px-2.5 py-1.5 text-xs text-gray-200 focus:outline-none focus:border-blue-500"
            @change="applyFilter"
          >
            <option value="全部">全部时间</option>
            <option value="今天">今天</option>
            <option value="近7天">近 7 天</option>
            <option value="近30天">近 30 天</option>
          </select>
        </div>

        <!-- Model Filter -->
        <div>
          <select
            v-model="selectedModel"
            class="w-full bg-gray-900/80 border border-gray-700/80 rounded-lg px-2.5 py-1.5 text-xs text-gray-200 focus:outline-none focus:border-blue-500"
            @change="applyFilter"
          >
            <option value="全部模型">全部模型</option>
            <option v-for="m in filterOptions.models" :key="m" :value="m">
              {{ m }}
            </option>
          </select>
        </div>

        <!-- Workspace Filter -->
        <div>
          <select
            v-model="selectedWorkspace"
            class="w-full bg-gray-900/80 border border-gray-700/80 rounded-lg px-2.5 py-1.5 text-xs text-gray-200 focus:outline-none focus:border-blue-500"
            @change="applyFilter"
          >
            <option value="全部工作区">全部工作区</option>
            <option v-for="w in filterOptions.workspaces" :key="w" :value="w">
              {{ w }}
            </option>
          </select>
        </div>

        <!-- Filter & Reset Buttons -->
        <div class="flex items-center gap-1.5">
          <button
            @click="applyFilter"
            class="flex-1 flex items-center justify-center gap-1 px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-xs font-semibold text-white transition-colors"
          >
            <Filter class="w-3.5 h-3.5" />
            <span>筛选</span>
          </button>
          <button
            @click="resetFilter"
            class="px-2.5 py-1.5 rounded-lg bg-gray-800 hover:bg-gray-700 text-gray-400 hover:text-gray-200 border border-gray-700 transition-colors"
            title="重置筛选条件"
          >
            <RotateCcw class="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>

    <!-- Data Table Card -->
    <div class="card-glass rounded-xl border border-white/5 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-gray-900/90 border-b border-gray-800 text-gray-400 font-semibold select-none">
              <th class="py-3 px-3.5 w-36">时间</th>
              <th class="py-3 px-3.5 min-w-[200px]">对话 / 项目工作区</th>
              <th class="py-3 px-3.5 w-40">模型</th>
              <th class="py-3 px-3 w-24 text-right">提示 Tokens</th>
              <th class="py-3 px-3 w-24 text-right">补全 Tokens</th>
              <th class="py-3 px-3 w-24 text-right text-amber-400">思考 Tokens</th>
              <th class="py-3 px-3 w-28 text-right font-bold text-gray-300">总计 Tokens</th>
              <th class="py-3 px-3 w-20 text-right">耗时</th>
              <th class="py-3 px-3.5 w-24 text-right">费用</th>
              <th class="py-3 px-2 w-12 text-center">详情</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-800/60 font-mono">
            <tr v-if="logs.items.length === 0">
              <td colspan="10" class="py-12 text-center text-gray-500">
                暂无符合条件的调用日志记录
              </td>
            </tr>
            <tr
              v-for="item in logs.items"
              :key="item.id"
              class="hover:bg-white/[0.02] transition-colors"
            >
              <!-- Time -->
              <td class="py-2.5 px-3.5 text-gray-400 text-[11px] whitespace-nowrap">
                {{ item.timestamp }}
              </td>

              <!-- Title & Workspace -->
              <td class="py-2.5 px-3.5 font-sans">
                <div class="font-semibold text-gray-200 truncate max-w-[280px]" :title="item.title">
                  {{ item.title }}
                </div>
                <div class="text-[11px] text-gray-500 truncate max-w-[280px]" :title="item.workspace_full">
                  {{ item.workspace }}
                </div>
              </td>

              <!-- Model Badge -->
              <td class="py-2.5 px-3.5">
                <span class="inline-block px-2 py-0.5 rounded bg-sky-950/60 border border-sky-800/40 text-sky-400 text-[10px] font-semibold truncate max-w-[150px]" :title="item.model">
                  {{ item.model }}
                </span>
              </td>

              <!-- Prompt -->
              <td class="py-2.5 px-3 text-right text-gray-400">
                {{ item.prompt_tokens.toLocaleString() }}
              </td>

              <!-- Candidate -->
              <td class="py-2.5 px-3 text-right text-gray-400">
                {{ item.candidate_tokens.toLocaleString() }}
              </td>

              <!-- Thinking -->
              <td class="py-2.5 px-3 text-right text-amber-400 font-semibold">
                {{ item.thinking_tokens.toLocaleString() }}
              </td>

              <!-- Total -->
              <td class="py-2.5 px-3 text-right font-bold text-gray-200">
                {{ (item.prompt_tokens + item.output_tokens).toLocaleString() }}
              </td>

              <!-- Latency -->
              <td class="py-2.5 px-3 text-right text-gray-400">
                {{ (item.latency_ms / 1000).toFixed(1) }}s
              </td>

              <!-- Cost -->
              <td class="py-2.5 px-3.5 text-right font-semibold text-emerald-400">
                {{ getCurrencySymbol(item.currency) }}{{ item.cost.toFixed(5) }}
              </td>

              <!-- Detail Modal Button -->
              <td class="py-2.5 px-2 text-center">
                <button
                  @click="openDetail(item)"
                  class="p-1 rounded text-gray-400 hover:text-blue-400 hover:bg-gray-800 transition-colors"
                  title="查看完整调用参数"
                >
                  <Eye class="w-3.5 h-3.5" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination Footer -->
      <div class="p-3.5 bg-gray-900/60 border-t border-gray-800 flex items-center justify-between text-xs text-gray-400 select-none">
        <div>
          第 {{ logs.page }} 页 / 共 {{ logs.total_pages }} 页 (共 {{ logs.total_count }} 条记录)
        </div>

        <div class="flex items-center gap-2">
          <button
            @click="changePage(logs.page - 1)"
            :disabled="logs.page <= 1"
            class="px-2.5 py-1 rounded bg-gray-800 hover:bg-gray-700 disabled:opacity-40 disabled:hover:bg-gray-800 text-gray-200 transition-colors flex items-center gap-1"
          >
            <ChevronLeft class="w-3.5 h-3.5" />
            <span>上一页</span>
          </button>
          <button
            @click="changePage(logs.page + 1)"
            :disabled="logs.page >= logs.total_pages"
            class="px-2.5 py-1 rounded bg-gray-800 hover:bg-gray-700 disabled:opacity-40 disabled:hover:bg-gray-800 text-gray-200 transition-colors flex items-center gap-1"
          >
            <span>下一页</span>
            <ChevronRight class="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>

    <!-- Detail Dialog Modal -->
    <div
      v-if="activeDetailItem"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      @click.self="activeDetailItem = null"
    >
      <div class="w-full max-w-lg card-glass p-5 rounded-2xl border border-gray-700 shadow-2xl space-y-4">
        <div class="flex items-center justify-between border-b border-gray-800 pb-3">
          <div class="flex items-center gap-2 text-blue-400">
            <Info class="w-4 h-4" />
            <h3 class="text-sm font-bold text-gray-100">单次调用元数据详情</h3>
          </div>
          <button
            @click="activeDetailItem = null"
            class="text-gray-400 hover:text-gray-200 p-1 rounded-md hover:bg-gray-800 transition-colors"
          >
            <X class="w-4 h-4" />
          </button>
        </div>

        <div class="space-y-2 text-xs font-mono">
          <div class="flex justify-between py-1 border-b border-gray-800/60">
            <span class="text-gray-400">调用标识 (ID):</span>
            <span class="text-gray-200">{{ activeDetailItem.id }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-800/60">
            <span class="text-gray-400">会话 UUID:</span>
            <span class="text-gray-200">{{ activeDetailItem.conversation_id }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-800/60">
            <span class="text-gray-400">调用时间:</span>
            <span class="text-gray-200">{{ activeDetailItem.timestamp }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-800/60">
            <span class="text-gray-400">模型名称:</span>
            <span class="text-sky-400 font-semibold">{{ activeDetailItem.model }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-800/60">
            <span class="text-gray-400">所属工作区:</span>
            <span class="text-gray-200 truncate max-w-[280px]" :title="activeDetailItem.workspace_full">
              {{ activeDetailItem.workspace_full || activeDetailItem.workspace }}
            </span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-800/60">
            <span class="text-gray-400">Prompt Tokens:</span>
            <span class="text-gray-200">{{ activeDetailItem.prompt_tokens.toLocaleString() }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-800/60">
            <span class="text-gray-400">Candidate Tokens:</span>
            <span class="text-gray-200">{{ activeDetailItem.candidate_tokens.toLocaleString() }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-800/60">
            <span class="text-amber-400">Thinking Tokens:</span>
            <span class="text-amber-400 font-bold">{{ activeDetailItem.thinking_tokens.toLocaleString() }}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-gray-800/60">
            <span class="text-gray-400">耗时 (Latency):</span>
            <span class="text-gray-200">{{ activeDetailItem.latency_ms }} ms</span>
          </div>
          <div class="flex justify-between py-1">
            <span class="text-emerald-400">预估结算费用:</span>
            <span class="text-emerald-400 font-bold">
              {{ getCurrencySymbol(activeDetailItem.currency) }}{{ activeDetailItem.cost }} {{ activeDetailItem.currency }}
            </span>
          </div>
        </div>

        <div class="pt-2 flex justify-end">
          <button
            @click="activeDetailItem = null"
            class="px-4 py-1.5 rounded-lg bg-gray-800 hover:bg-gray-700 text-xs font-semibold text-gray-200 transition-colors"
          >
            关闭
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

const searchQuery = ref("");
const selectedDateRange = ref("全部");
const selectedModel = ref("全部模型");
const selectedWorkspace = ref("全部工作区");

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
      modelFilter: selectedModel.value === "全部模型" ? null : selectedModel.value,
      wsFilter: selectedWorkspace.value === "全部工作区" ? null : selectedWorkspace.value,
      searchQuery: searchQuery.value ? searchQuery.value : null,
      dateRange: selectedDateRange.value === "全部" ? null : selectedDateRange.value,
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
  selectedDateRange.value = "全部";
  selectedModel.value = "全部模型";
  selectedWorkspace.value = "全部工作区";
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
      modelFilter: selectedModel.value === "全部模型" ? null : selectedModel.value,
      wsFilter: selectedWorkspace.value === "全部工作区" ? null : selectedWorkspace.value,
      searchQuery: searchQuery.value ? searchQuery.value : null,
      dateRange: selectedDateRange.value === "全部" ? null : selectedDateRange.value,
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
