<template>
  <div class="flex h-screen w-screen overflow-hidden bg-[#f6f8fa] dark:bg-[#0d1117] text-gray-800 dark:text-gray-200 select-none transition-colors duration-200">
    <!-- Sidebar Navigation -->
    <aside class="w-56 flex-shrink-0 bg-white/85 dark:bg-gray-950/70 border-r border-gray-200/80 dark:border-white/5 flex flex-col justify-between p-3.5 backdrop-blur-xl transition-colors duration-200">
      <div class="space-y-6">
        <!-- Logo / Title -->
        <div class="flex items-center gap-2.5 px-2 pt-1">
          <div class="w-7 h-7 rounded-lg bg-gradient-to-tr from-emerald-500 to-blue-500 flex items-center justify-center shadow-lg shadow-emerald-500/20">
            <Sparkles class="w-4 h-4 text-white" />
          </div>
          <div>
            <div class="text-xs font-bold tracking-tight text-gray-900 dark:text-white">{{ t('app.title') }}</div>
            <div class="text-[10px] text-gray-500 dark:text-gray-400">{{ t('app.subtitle') }}</div>
          </div>
        </div>

        <!-- Navigation Menu -->
        <nav class="space-y-1">
          <button
            v-for="item in navItems"
            :key="item.id"
            @click="currentTab = item.id"
            :class="[
              'w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-xs font-medium transition-all duration-150 cursor-pointer',
              currentTab === item.id
                ? 'bg-blue-600/10 dark:bg-blue-600/15 text-blue-600 dark:text-blue-400 border border-blue-500/30 dark:border-blue-500/20 font-semibold shadow-xs'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 hover:bg-black/[0.04] dark:hover:bg-white/[0.04]'
            ]"
          >
            <component :is="item.icon" class="w-4 h-4" />
            <span>{{ item.label }}</span>
          </button>
        </nav>
      </div>

      <!-- Footer: Quick Language, Theme Toggle & Real-time Live Badge -->
      <div class="space-y-2">
        <!-- Quick Language Switcher Button -->
        <button
          type="button"
          @click="cycleLanguage"
          class="w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg border border-gray-200/80 dark:border-white/5 bg-gray-50/80 dark:bg-gray-900/40 hover:bg-gray-100 dark:hover:bg-gray-800/60 text-xs text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 transition-colors cursor-pointer"
          :title="t('app.language.tooltip')"
        >
          <div class="flex items-center gap-1.5">
            <Globe class="w-3.5 h-3.5 text-sky-500 dark:text-sky-400" />
            <span class="text-[11px] font-medium">{{ languageModeLabel }}</span>
          </div>
          <span class="text-[10px] text-gray-400 font-mono">{{ activeLangBadge }}</span>
        </button>

        <!-- Quick Theme Toggle -->
        <button
          type="button"
          @click="cycleTheme"
          class="w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg border border-gray-200/80 dark:border-white/5 bg-gray-50/80 dark:bg-gray-900/40 hover:bg-gray-100 dark:hover:bg-gray-800/60 text-xs text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 transition-colors cursor-pointer"
          :title="t('app.theme.tooltip')"
        >
          <div class="flex items-center gap-1.5">
            <component :is="themeIcon" class="w-3.5 h-3.5 text-blue-500 dark:text-blue-400" />
            <span class="text-[11px] font-medium">{{ themeLabel }}</span>
          </div>
          <span class="text-[10px] text-gray-400 font-mono">{{ activeTheme === 'dark' ? t('app.theme.darkShort') : t('app.theme.lightShort') }}</span>
        </button>

        <!-- Live Status Badge -->
        <div class="p-2.5 rounded-xl bg-gray-100/80 dark:bg-gray-900/60 border border-gray-200/80 dark:border-white/5 space-y-1.5 transition-colors duration-200">
          <div class="flex items-center gap-2">
            <span class="relative flex h-2 w-2">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
              <span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
            </span>
            <span class="text-[11px] font-semibold text-emerald-600 dark:text-emerald-400">{{ t('app.liveStatus.active') }}</span>
          </div>
          <p class="text-[10px] text-gray-500 leading-tight">
            {{ t('app.liveStatus.desc') }}
          </p>
        </div>
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
  Sun,
  Moon,
  Laptop,
  Globe,
} from "lucide-vue-next";

import DashboardView from "./views/DashboardView.vue";
import LogsView from "./views/LogsView.vue";
import AnalyticsView from "./views/AnalyticsView.vue";
import PricingView from "./views/PricingView.vue";
import SettingsView from "./views/SettingsView.vue";
import { themeMode, activeTheme, setThemeMode } from "./services/theme";
import { t, languageMode, currentLocale, setLanguageMode, type LanguageMode } from "./services/i18n";

const currentTab = ref("dashboard");

const navItems = computed(() => [
  { id: "dashboard", label: t("app.nav.dashboard"), icon: LayoutDashboard },
  { id: "logs", label: t("app.nav.logs"), icon: ScrollText },
  { id: "analytics", label: t("app.nav.analytics"), icon: BarChart3 },
  { id: "pricing", label: t("app.nav.pricing"), icon: Coins },
  { id: "settings", label: t("app.nav.settings"), icon: Settings },
]);

const themeIcon = computed(() => {
  if (themeMode.value === "system") return Laptop;
  if (themeMode.value === "light") return Sun;
  return Moon;
});

const themeLabel = computed(() => {
  if (themeMode.value === "system") return t("app.theme.system");
  if (themeMode.value === "light") return t("app.theme.light");
  return t("app.theme.dark");
});

const cycleTheme = () => {
  if (themeMode.value === "system") {
    setThemeMode("light");
  } else if (themeMode.value === "light") {
    setThemeMode("dark");
  } else {
    setThemeMode("system");
  }
};

const languageModeLabel = computed(() => {
  if (languageMode.value === "system") return t("app.language.system");
  if (languageMode.value === "zh-CN") return "简体中文";
  if (languageMode.value === "zh-TW") return "繁體中文";
  return "English";
});

const activeLangBadge = computed(() => {
  if (currentLocale.value === "zh-CN") return "简";
  if (currentLocale.value === "zh-TW") return "繁";
  return "EN";
});

const cycleLanguage = () => {
  const modes: LanguageMode[] = ["system", "zh-CN", "zh-TW", "en"];
  const curIdx = modes.indexOf(languageMode.value);
  const nextMode = modes[(curIdx + 1) % modes.length];
  setLanguageMode(nextMode);
};

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