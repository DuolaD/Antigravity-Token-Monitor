<template>
  <div class="flex h-screen w-screen overflow-hidden bg-[#0d1117] text-gray-200 select-none">
    <!-- Sidebar Navigation -->
    <aside class="w-56 flex-shrink-0 bg-gray-950/70 border-r border-white/5 flex flex-col justify-between p-3.5 backdrop-blur-xl">
      <div class="space-y-6">
        <!-- Logo / Title -->
        <div class="flex items-center gap-2.5 px-2 pt-1">
          <div class="w-7 h-7 rounded-lg bg-gradient-to-tr from-emerald-500 to-blue-500 flex items-center justify-center shadow-lg shadow-emerald-500/20">
            <Sparkles class="w-4 h-4 text-white" />
          </div>
          <div>
            <div class="text-xs font-bold tracking-tight text-white">Antigravity Tokens</div>
            <div class="text-[10px] text-gray-400">Token 消耗监控</div>
          </div>
        </div>

        <!-- Navigation Menu -->
        <nav class="space-y-1">
          <button
            v-for="item in navItems"
            :key="item.id"
            @click="currentTab = item.id"
            :class="[
              'w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-medium transition-all duration-150',
              currentTab === item.id
                ? 'bg-blue-600/15 text-blue-400 border border-blue-500/20 font-semibold shadow-sm'
                : 'text-gray-400 hover:text-gray-200 hover:bg-white/[0.04]'
            ]"
          >
            <component :is="item.icon" class="w-4 h-4" />
            <span>{{ item.label }}</span>
          </button>
        </nav>
      </div>

      <!-- Footer: Real-time Live Badge -->
      <div class="p-2.5 rounded-xl bg-gray-900/60 border border-white/5 space-y-1.5">
        <div class="flex items-center gap-2">
          <span class="relative flex h-2 w-2">
            <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
          </span>
          <span class="text-[11px] font-semibold text-emerald-400">实时探测监听中</span>
        </div>
        <p class="text-[10px] text-gray-500 leading-tight">
          毫秒级捕获 SQLite WAL 变更
        </p>
      </div>
    </aside>

    <!-- Main Content View Area -->
    <main class="flex-1 overflow-y-auto px-7 py-6">
      <KeepAlive>
        <component :is="activeComponent" />
      </KeepAlive>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from "vue";
import {
  Sparkles,
  LayoutDashboard,
  ScrollText,
  BarChart3,
  Coins,
  Settings,
} from "lucide-vue-next";

import DashboardView from "./views/DashboardView.vue";
import LogsView from "./views/LogsView.vue";
import AnalyticsView from "./views/AnalyticsView.vue";
import PricingView from "./views/PricingView.vue";
import SettingsView from "./views/SettingsView.vue";

const currentTab = ref("dashboard");

const navItems = [
  { id: "dashboard", label: "总览看板", icon: LayoutDashboard },
  { id: "logs", label: "日志明细", icon: ScrollText },
  { id: "analytics", label: "统计分析", icon: BarChart3 },
  { id: "pricing", label: "模型费率", icon: Coins },
  { id: "settings", label: "系统设置", icon: Settings },
];

const activeComponent = computed(() => {
  switch (currentTab.value) {
    case "dashboard":
      return DashboardView;
    case "logs":
      return LogsView;
    case "analytics":
      return AnalyticsView;
    case "pricing":
      return PricingView;
    case "settings":
      return SettingsView;
    default:
      return DashboardView;
  }
});
</script>