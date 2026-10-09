<template>
  <div class="space-y-6 pb-12">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-100 flex items-center gap-2">
          模型费率配置
        </h1>
        <p class="text-xs text-gray-400 mt-1">
          参考 NewAPI 模型倍率管理，自定义配置各模型每 100 万 (1M) Tokens 的结算价格
        </p>
      </div>

      <div class="flex items-center gap-2.5">
        <button
          @click="resetDefaults"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-gray-800 hover:bg-gray-700 text-xs font-semibold text-gray-300 transition-colors border border-gray-700"
        >
          <RotateCcw class="w-3.5 h-3.5" />
          <span>重置为默认</span>
        </button>

        <button
          @click="savePricing"
          :disabled="isSaving"
          class="flex items-center gap-1.5 px-4 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-xs font-semibold text-white transition-colors shadow-sm disabled:opacity-50"
        >
          <Save class="w-3.5 h-3.5" />
          <span>{{ isSaving ? '正在保存...' : '保存费率配置' }}</span>
        </button>
      </div>
    </div>

    <!-- Status Message Banner -->
    <div
      v-if="statusMessage"
      class="p-3 rounded-xl bg-emerald-950/60 border border-emerald-800/60 text-emerald-300 text-xs flex items-center justify-between animate-fade-in"
    >
      <div class="flex items-center gap-2">
        <CheckCircle2 class="w-4 h-4 text-emerald-400" />
        <span>{{ statusMessage }}</span>
      </div>
      <button @click="statusMessage = ''" class="text-emerald-400 hover:text-emerald-200">
        <X class="w-3.5 h-3.5" />
      </button>
    </div>

    <!-- Currency & Exchange Rate Card -->
    <div class="card-glass p-5 rounded-xl border border-white/5">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
        <div class="space-y-2">
          <label class="text-xs font-semibold text-gray-300">结算币种 (Currency)</label>
          <select
            v-model="config.settings.currency"
            class="w-full bg-gray-900/80 border border-gray-700/80 rounded-lg px-3 py-2 text-xs text-gray-200 focus:outline-none focus:border-blue-500"
          >
            <option value="USD">USD ($) 美元</option>
            <option value="CNY">CNY (¥) 人民币</option>
          </select>
        </div>

        <div class="space-y-2">
          <label class="text-xs font-semibold text-gray-300">USD / CNY 换算汇率</label>
          <input
            v-model.number="config.settings.usd_to_cny_rate"
            type="number"
            step="0.05"
            class="w-full bg-gray-900/80 border border-gray-700/80 rounded-lg px-3 py-2 text-xs text-gray-200 focus:outline-none focus:border-blue-500 font-mono"
          />
        </div>
      </div>
    </div>

    <!-- Pricing Table Card -->
    <div class="card-glass rounded-xl border border-white/5 overflow-hidden">
      <div class="p-4 bg-gray-900/60 border-b border-gray-800 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <Coins class="w-4 h-4 text-amber-400" />
          <h2 class="text-sm font-semibold text-gray-200">模型费率清单</h2>
        </div>
        <span class="text-xs text-gray-500 font-mono">共 {{ config.pricing.length }} 个模型规则</span>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-gray-900/90 border-b border-gray-800 text-gray-400 font-semibold select-none">
              <th class="py-3 px-4 w-56">模型标识</th>
              <th class="py-3 px-4">显示名称</th>
              <th class="py-3 px-4 w-48 text-right">输入单价 ($ / 1M Tokens)</th>
              <th class="py-3 px-4 w-48 text-right">输出单价 ($ / 1M Tokens)</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-800/60 font-mono">
            <tr
              v-for="item in config.pricing"
              :key="item.model_id"
              class="hover:bg-white/[0.02] transition-colors"
            >
              <!-- Model ID -->
              <td class="py-2.5 px-4 font-semibold text-sky-400">
                {{ item.model_id }}
              </td>

              <!-- Display Name -->
              <td class="py-2.5 px-4 font-sans">
                <input
                  v-model="item.display_name"
                  type="text"
                  class="w-full bg-gray-900/60 border border-gray-700/60 rounded px-2.5 py-1 text-xs text-gray-200 focus:outline-none focus:border-blue-500 font-sans"
                />
              </td>

              <!-- Prompt Price -->
              <td class="py-2.5 px-4 text-right">
                <input
                  v-model.number="item.prompt_price_per_million"
                  type="number"
                  step="0.01"
                  class="w-32 bg-gray-900/60 border border-gray-700/60 rounded px-2.5 py-1 text-xs text-right text-gray-200 focus:outline-none focus:border-blue-500 font-mono"
                />
              </td>

              <!-- Output Price -->
              <td class="py-2.5 px-4 text-right">
                <input
                  v-model.number="item.output_price_per_million"
                  type="number"
                  step="0.05"
                  class="w-32 bg-gray-900/60 border border-gray-700/60 rounded px-2.5 py-1 text-xs text-right text-gray-200 focus:outline-none focus:border-blue-500 font-mono"
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
import { ref, reactive, onMounted } from "vue";
import { invoke } from "@tauri-apps/api/core";
import { RotateCcw, Save, CheckCircle2, X, Coins } from "lucide-vue-next";
import type { PricingConfig } from "../types";

const config = reactive<PricingConfig>({
  settings: {
    currency: "USD",
    usd_to_cny_rate: 7.25,
  },
  pricing: [],
});

const isSaving = ref(false);
const statusMessage = ref("");

const fetchPricing = async () => {
  try {
    const res = await invoke<PricingConfig>("get_pricing_config");
    config.settings = res.settings;
    config.pricing = res.pricing;
  } catch (err) {
    console.error("fetchPricing error:", err);
  }
};

const savePricing = async () => {
  isSaving.value = true;
  try {
    await invoke("save_pricing_config", { config });
    statusMessage.value = "费率配置已成功保存！";
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
    statusMessage.value = "费率配置已重置为系统默认值！";
    setTimeout(() => {
      statusMessage.value = "";
    }, 4000);
  } catch (err) {
    console.error("resetDefaults error:", err);
  }
};

onMounted(() => {
  fetchPricing();
});
</script>
