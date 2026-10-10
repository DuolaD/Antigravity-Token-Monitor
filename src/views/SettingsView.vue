<template>
  <div class="space-y-6 max-w-4xl pb-12">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-2">
        系统设置
      </h1>
      <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">
        配置外观主题、数据探测源、高频实时同步与应用偏好
      </p>
    </div>

    <!-- Appearance Theme Card (macOS Style) -->
    <div class="card-glass p-5 rounded-xl space-y-4">
      <div class="flex items-center gap-2 text-blue-500 dark:text-blue-400">
        <SunMoon class="w-4 h-4" />
        <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">外观主题</h2>
      </div>
      <p class="text-[11px] text-gray-500 dark:text-gray-400">
        选择应用视觉主题模式。设置为跟随系统时，将自动响应操作系统深色/浅色外观切换。
      </p>

      <!-- 3 Theme Options mimicking macOS Settings Appearance -->
      <div class="grid grid-cols-3 gap-4 pt-1 max-w-md">
        <!-- Light Theme Option -->
        <button
          type="button"
          @click="onSelectTheme('light')"
          class="flex flex-col items-center gap-2 group cursor-pointer focus:outline-none"
        >
          <div
            :class="[
              'w-full aspect-[16/10] rounded-xl overflow-hidden border transition-all duration-200 relative shadow-sm',
              themeMode === 'light'
                ? 'ring-2 ring-blue-500 border-blue-500 shadow-md scale-[1.02]'
                : 'border-gray-200 dark:border-gray-700/80 opacity-80 group-hover:opacity-100 group-hover:border-gray-300 dark:group-hover:border-gray-600'
            ]"
          >
            <!-- Light Mock Window -->
            <div class="w-full h-full bg-[#f8fafc] flex flex-col">
              <div class="h-3.5 bg-gray-200/90 border-b border-gray-300/80 flex items-center px-1.5 gap-1">
                <span class="w-1.5 h-1.5 rounded-full bg-red-400"></span>
                <span class="w-1.5 h-1.5 rounded-full bg-amber-400"></span>
                <span class="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
              </div>
              <div class="flex-1 flex p-1.5 gap-1.5 bg-white">
                <div class="w-4 h-full bg-gray-100 rounded-sm"></div>
                <div class="flex-1 space-y-1 py-0.5">
                  <div class="w-10 h-1.5 bg-blue-100 rounded-sm"></div>
                  <div class="w-14 h-1 bg-gray-200 rounded-sm"></div>
                  <div class="w-8 h-1 bg-gray-200 rounded-sm"></div>
                </div>
              </div>
            </div>
            <!-- Check badge when selected -->
            <div
              v-if="themeMode === 'light'"
              class="absolute bottom-1.5 right-1.5 w-4 h-4 rounded-full bg-blue-500 text-white flex items-center justify-center shadow-xs"
            >
              <Check class="w-2.5 h-2.5 stroke-[3]" />
            </div>
          </div>
          <span
            :class="[
              'text-xs transition-colors',
              themeMode === 'light'
                ? 'font-bold text-blue-600 dark:text-blue-400'
                : 'text-gray-600 dark:text-gray-400 font-medium'
            ]"
          >
            浅色
          </span>
        </button>

        <!-- Dark Theme Option -->
        <button
          type="button"
          @click="onSelectTheme('dark')"
          class="flex flex-col items-center gap-2 group cursor-pointer focus:outline-none"
        >
          <div
            :class="[
              'w-full aspect-[16/10] rounded-xl overflow-hidden border transition-all duration-200 relative shadow-sm',
              themeMode === 'dark'
                ? 'ring-2 ring-blue-500 border-blue-500 shadow-md scale-[1.02]'
                : 'border-gray-200 dark:border-gray-700/80 opacity-80 group-hover:opacity-100 group-hover:border-gray-300 dark:group-hover:border-gray-600'
            ]"
          >
            <!-- Dark Mock Window -->
            <div class="w-full h-full bg-[#0d1117] flex flex-col">
              <div class="h-3.5 bg-gray-800 border-b border-gray-700/80 flex items-center px-1.5 gap-1">
                <span class="w-1.5 h-1.5 rounded-full bg-red-400"></span>
                <span class="w-1.5 h-1.5 rounded-full bg-amber-400"></span>
                <span class="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
              </div>
              <div class="flex-1 flex p-1.5 gap-1.5 bg-[#161b22]">
                <div class="w-4 h-full bg-gray-900 rounded-sm"></div>
                <div class="flex-1 space-y-1 py-0.5">
                  <div class="w-10 h-1.5 bg-blue-900/50 rounded-sm"></div>
                  <div class="w-14 h-1 bg-gray-700 rounded-sm"></div>
                  <div class="w-8 h-1 bg-gray-700 rounded-sm"></div>
                </div>
              </div>
            </div>
            <!-- Check badge when selected -->
            <div
              v-if="themeMode === 'dark'"
              class="absolute bottom-1.5 right-1.5 w-4 h-4 rounded-full bg-blue-500 text-white flex items-center justify-center shadow-xs"
            >
              <Check class="w-2.5 h-2.5 stroke-[3]" />
            </div>
          </div>
          <span
            :class="[
              'text-xs transition-colors',
              themeMode === 'dark'
                ? 'font-bold text-blue-600 dark:text-blue-400'
                : 'text-gray-600 dark:text-gray-400 font-medium'
            ]"
          >
            深色
          </span>
        </button>

        <!-- System Auto Option -->
        <button
          type="button"
          @click="onSelectTheme('system')"
          class="flex flex-col items-center gap-2 group cursor-pointer focus:outline-none"
        >
          <div
            :class="[
              'w-full aspect-[16/10] rounded-xl overflow-hidden border transition-all duration-200 relative shadow-sm',
              themeMode === 'system'
                ? 'ring-2 ring-blue-500 border-blue-500 shadow-md scale-[1.02]'
                : 'border-gray-200 dark:border-gray-700/80 opacity-80 group-hover:opacity-100 group-hover:border-gray-300 dark:group-hover:border-gray-600'
            ]"
          >
            <!-- Auto Split Mock Window (Left Light / Right Dark) -->
            <div class="w-full h-full flex">
              <!-- Left: Light half -->
              <div class="w-1/2 h-full bg-white flex flex-col border-r border-gray-300 dark:border-gray-700">
                <div class="h-3.5 bg-gray-200/90 border-b border-gray-300/80 flex items-center px-1 gap-0.5">
                  <span class="w-1.5 h-1.5 rounded-full bg-red-400"></span>
                  <span class="w-1.5 h-1.5 rounded-full bg-amber-400"></span>
                </div>
                <div class="flex-1 p-1 space-y-1">
                  <div class="w-6 h-1 bg-blue-200 rounded-sm"></div>
                  <div class="w-8 h-1 bg-gray-200 rounded-sm"></div>
                </div>
              </div>
              <!-- Right: Dark half -->
              <div class="w-1/2 h-full bg-[#161b22] flex flex-col">
                <div class="h-3.5 bg-gray-800 border-b border-gray-700/80 flex items-center px-1">
                  <span class="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
                </div>
                <div class="flex-1 p-1 space-y-1">
                  <div class="w-6 h-1 bg-blue-900/60 rounded-sm"></div>
                  <div class="w-8 h-1 bg-gray-700 rounded-sm"></div>
                </div>
              </div>
            </div>
            <!-- Check badge when selected -->
            <div
              v-if="themeMode === 'system'"
              class="absolute bottom-1.5 right-1.5 w-4 h-4 rounded-full bg-blue-500 text-white flex items-center justify-center shadow-xs"
            >
              <Check class="w-2.5 h-2.5 stroke-[3]" />
            </div>
          </div>
          <span
            :class="[
              'text-xs transition-colors',
              themeMode === 'system'
                ? 'font-bold text-blue-600 dark:text-blue-400'
                : 'text-gray-600 dark:text-gray-400 font-medium'
            ]"
          >
            自动 (跟随系统)
          </span>
        </button>
      </div>

      <!-- Theme Status Line -->
      <div class="pt-2 text-[11px] text-gray-500 dark:text-gray-400 border-t border-gray-100 dark:border-gray-800/60 flex items-center justify-between">
        <div class="flex items-center gap-1.5">
          <span class="w-1.5 h-1.5 rounded-full" :class="activeTheme === 'dark' ? 'bg-indigo-400' : 'bg-amber-400'"></span>
          <span>
            当前生效外观：<strong class="text-gray-700 dark:text-gray-200">{{ activeTheme === 'dark' ? '深色模式' : '浅色模式' }}</strong>
            <span v-if="themeMode === 'system'" class="text-gray-400">（已与操作系统同步）</span>
          </span>
        </div>
      </div>
    </div>

    <!-- Live Sync Setting Card -->
    <div class="card-glass p-5 rounded-xl space-y-4">
      <div class="flex items-center gap-2 text-blue-500 dark:text-blue-400">
        <Activity class="w-4 h-4" />
        <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">实时监听与心跳刷新</h2>
      </div>

      <div class="flex items-center justify-between pt-2">
        <div class="space-y-1">
          <div class="text-xs font-semibold text-gray-800 dark:text-gray-200">
            启用 1 秒级动态心跳探测
          </div>
          <div class="text-[11px] text-gray-500 dark:text-gray-400">
            开启后应用将在后台双轨检测 SQLite WAL 写入，对话产生时毫秒级跳动更新看板与统计
          </div>
        </div>

        <!-- Toggle Switch -->
        <button
          @click="toggleLiveSync"
          :class="[
            'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none',
            settings.live_sync_enabled ? 'bg-blue-600' : 'bg-gray-300 dark:bg-gray-700'
          ]"
        >
          <span
            :class="[
              'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out',
              settings.live_sync_enabled ? 'translate-x-5' : 'translate-x-0'
            ]"
          />
        </button>
      </div>
    </div>

    <!-- Monitored Paths Card -->
    <div class="card-glass p-5 rounded-xl space-y-4">
      <div class="flex items-center gap-2 text-emerald-600 dark:text-emerald-400">
        <FolderSearch class="w-4 h-4" />
        <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">已探测并绑定的 Antigravity 数据源</h2>
      </div>
      <p class="text-[11px] text-gray-500 dark:text-gray-400">
        系统根据操作系统环境自动识别并挂载以下会话存储路径中的 .db 数据库文件：
      </p>

      <div class="space-y-2 pt-1 font-mono text-xs">
        <div
          v-if="settings.monitored_paths.length === 0"
          class="p-3 rounded-lg bg-gray-100 dark:bg-gray-900/60 border border-gray-200 dark:border-gray-800 text-gray-500"
        >
          暂未发现默认 Antigravity 会话目录，请确认 Antigravity 已在本地生成对话记录。
        </div>

        <div
          v-for="p in settings.monitored_paths"
          :key="p"
          class="flex items-center gap-2.5 p-3 rounded-lg bg-gray-50 dark:bg-gray-900/80 border border-gray-200 dark:border-gray-800 text-gray-700 dark:text-gray-300"
        >
          <CheckCircle2 class="w-4 h-4 text-emerald-500 dark:text-emerald-400 flex-shrink-0" />
          <span class="truncate">{{ p }}</span>
        </div>
      </div>
    </div>

    <!-- About App Card -->
    <div class="card-glass p-5 rounded-xl space-y-3">
      <div class="flex items-center gap-2 text-purple-500 dark:text-purple-400">
        <Info class="w-4 h-4" />
        <h2 class="text-sm font-semibold text-gray-800 dark:text-gray-200">关于 Antigravity Token Monitor</h2>
      </div>

      <div class="space-y-1.5 text-xs text-gray-500 dark:text-gray-400">
        <p class="text-gray-800 dark:text-gray-300 font-medium">
          专为 Google Antigravity 设计的原生 Token 计量与费用监控客户端
        </p>
        <p>运行平台: {{ settings.os_name }} | 架构核心: Tauri v2 + Rust 高性能底层</p>
        <p class="text-[11px] text-gray-400 dark:text-gray-500 pt-1">
          版本: v{{ settings.app_version }} (轻量版 · 内存占用超低 · 跨平台通用)
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from "vue";
import { invoke } from "@tauri-apps/api/core";
import {
  SunMoon,
  Check,
  Activity,
  FolderSearch,
  CheckCircle2,
  Info,
} from "lucide-vue-next";
import type { SystemSettingsInfo } from "../types";
import { themeMode, activeTheme, setThemeMode, type ThemeMode } from "../services/theme";

const settings = reactive<SystemSettingsInfo>({
  monitored_paths: [],
  live_sync_enabled: true,
  app_version: "1.0.0",
  os_name: "macOS",
});

const onSelectTheme = (mode: ThemeMode) => {
  setThemeMode(mode);
};

const fetchSettings = async () => {
  try {
    const res = await invoke<SystemSettingsInfo>("get_system_settings");
    settings.monitored_paths = res.monitored_paths;
    settings.live_sync_enabled = res.live_sync_enabled;
    settings.app_version = res.app_version;
    settings.os_name = res.os_name;
  } catch (err) {
    console.error("fetchSettings error:", err);
  }
};

const toggleLiveSync = async () => {
  try {
    const nextVal = !settings.live_sync_enabled;
    await invoke("set_live_sync_enabled", { enabled: nextVal });
    settings.live_sync_enabled = nextVal;
  } catch (err) {
    console.error("toggleLiveSync error:", err);
  }
};

onMounted(() => {
  fetchSettings();
});
</script>
