<template>
  <div class="space-y-6 max-w-4xl pb-12">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-100 flex items-center gap-2">
        系统设置
      </h1>
      <p class="text-xs text-gray-400 mt-1">
        配置数据探测源、高频实时同步与应用偏好
      </p>
    </div>

    <!-- Live Sync Setting Card -->
    <div class="card-glass p-5 rounded-xl border border-white/5 space-y-4">
      <div class="flex items-center gap-2 text-blue-400">
        <Activity class="w-4 h-4" />
        <h2 class="text-sm font-semibold text-gray-200">实时监听与心跳刷新</h2>
      </div>

      <div class="flex items-center justify-between pt-2">
        <div class="space-y-1">
          <div class="text-xs font-semibold text-gray-200">
            启用 1 秒级动态心跳探测
          </div>
          <div class="text-[11px] text-gray-400">
            开启后应用将在后台双轨检测 SQLite WAL 写入，对话产生时毫秒级跳动更新看板与统计
          </div>
        </div>

        <!-- Toggle Switch -->
        <button
          @click="toggleLiveSync"
          :class="[
            'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none',
            settings.live_sync_enabled ? 'bg-blue-600' : 'bg-gray-700'
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
    <div class="card-glass p-5 rounded-xl border border-white/5 space-y-4">
      <div class="flex items-center gap-2 text-emerald-400">
        <FolderSearch class="w-4 h-4" />
        <h2 class="text-sm font-semibold text-gray-200">已探测并绑定的 Antigravity 数据源</h2>
      </div>
      <p class="text-[11px] text-gray-400">
        系统根据操作系统环境自动识别并挂载以下会话存储路径中的 .db 数据库文件：
      </p>

      <div class="space-y-2 pt-1 font-mono text-xs">
        <div
          v-if="settings.monitored_paths.length === 0"
          class="p-3 rounded-lg bg-gray-900/60 border border-gray-800 text-gray-500"
        >
          暂未发现默认 Antigravity 会话目录，请确认 Antigravity 已在本地生成对话记录。
        </div>

        <div
          v-for="p in settings.monitored_paths"
          :key="p"
          class="flex items-center gap-2.5 p-3 rounded-lg bg-gray-900/80 border border-gray-800 text-gray-300"
        >
          <CheckCircle2 class="w-4 h-4 text-emerald-400 flex-shrink-0" />
          <span class="truncate">{{ p }}</span>
        </div>
      </div>
    </div>

    <!-- About App Card -->
    <div class="card-glass p-5 rounded-xl border border-white/5 space-y-3">
      <div class="flex items-center gap-2 text-purple-400">
        <Info class="w-4 h-4" />
        <h2 class="text-sm font-semibold text-gray-200">关于 Antigravity Token Monitor</h2>
      </div>

      <div class="space-y-1.5 text-xs text-gray-400">
        <p class="text-gray-300 font-medium">
          专为 Google Antigravity 设计的原生 Token 计量与费用监控客户端
        </p>
        <p>运行平台: {{ settings.os_name }} | 架构核心: Tauri v2 + Rust 高性能底层</p>
        <p class="text-[11px] text-gray-500 pt-1">
          版本: v{{ settings.app_version }} (轻量版 · 内存占用超低 · 跨平台通用)
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from "vue";
import { invoke } from "@tauri-apps/api/core";
import { Activity, FolderSearch, CheckCircle2, Info } from "lucide-vue-next";
import type { SystemSettingsInfo } from "../types";

const settings = reactive<SystemSettingsInfo>({
  monitored_paths: [],
  live_sync_enabled: true,
  app_version: "1.0.0",
  os_name: "macOS",
});

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
