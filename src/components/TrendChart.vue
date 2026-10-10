<template>
  <div ref="containerRef" class="relative w-full h-[280px] select-none" @mousemove="onMouseMove" @mouseleave="onMouseLeave">
    <!-- Empty State -->
    <div v-if="!hasData" class="absolute inset-0 flex flex-col items-center justify-center text-gray-400 dark:text-gray-500 text-sm">
      <span>{{ t('chart.empty') }}</span>
    </div>

    <!-- Chart SVG -->
    <svg v-else class="w-full h-full overflow-visible">
      <defs>
        <!-- Area Gradient -->
        <linearGradient id="chartGradient" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stop-color="#3b82f6" :stop-opacity="activeTheme === 'dark' ? 0.35 : 0.22" />
          <stop offset="100%" stop-color="#3b82f6" stop-opacity="0.0" />
        </linearGradient>
      </defs>

      <!-- Horizontal Grid Lines and Y-Axis Labels -->
      <g v-for="grid in gridLines" :key="grid.y">
        <line
          :x1="paddingLeft"
          :y1="grid.y"
          :x2="chartWidth - paddingRight"
          :y2="grid.y"
          :stroke="activeTheme === 'dark' ? 'rgba(255, 255, 255, 0.08)' : 'rgba(0, 0, 0, 0.06)'"
          stroke-dasharray="3,3"
        />
        <text
          :x="paddingLeft - 10"
          :y="grid.y + 4"
          text-anchor="end"
          :class="['text-[10px] font-mono', activeTheme === 'dark' ? 'fill-gray-400' : 'fill-gray-500']"
        >
          {{ grid.label }}
        </text>
      </g>

      <!-- Gradient Area -->
      <polygon v-if="areaPath" :points="areaPath" fill="url(#chartGradient)" />

      <!-- Main Line -->
      <polyline
        v-if="linePath"
        :points="linePath"
        fill="none"
        stroke="#3b82f6"
        stroke-width="2.5"
        stroke-linecap="round"
        stroke-linejoin="round"
      />

      <!-- X-Axis Date Labels -->
      <g v-for="lbl in xLabels" :key="lbl.x">
        <text
          :x="lbl.x"
          :y="chartHeight - 8"
          text-anchor="middle"
          :class="['text-[10px] font-mono', activeTheme === 'dark' ? 'fill-gray-400' : 'fill-gray-500']"
        >
          {{ lbl.text }}
        </text>
      </g>

      <!-- Crosshair & Active Dot -->
      <g v-if="hoverIndex !== null && activePoint">
        <!-- Vertical Crosshair Line -->
        <line
          :x1="activePoint.x"
          :y1="paddingTop"
          :x2="activePoint.x"
          :y2="chartHeight - paddingBottom"
          stroke="rgba(59, 130, 246, 0.5)"
          stroke-dasharray="4,4"
          stroke-width="1.5"
        />
        <!-- Outer glow dot -->
        <circle :cx="activePoint.x" :cy="activePoint.y" r="6" fill="#3b82f6" fill-opacity="0.4" />
        <!-- Inner white dot -->
        <circle :cx="activePoint.x" :cy="activePoint.y" r="3.5" fill="#ffffff" stroke="#3b82f6" stroke-width="2" />
      </g>
    </svg>

    <!-- Floating Tooltip Card -->
    <div
      v-if="hoverIndex !== null && activeData && activePoint"
      class="absolute z-20 pointer-events-none p-3 rounded-xl bg-white/95 dark:bg-gray-900/95 border border-gray-200 dark:border-gray-700/80 shadow-2xl backdrop-blur-md text-xs min-w-[190px]"
      :style="tooltipStyle"
    >
      <div class="font-semibold text-gray-800 dark:text-gray-200 border-b border-gray-100 dark:border-gray-800 pb-1.5 mb-2 flex items-center justify-between">
        <span>{{ formatFullDate(activeData.date || activeData.full_date_label) }}</span>
        <span class="text-[10px] px-1.5 py-0.5 rounded bg-blue-500/10 text-blue-600 dark:text-blue-400 font-mono">
          {{ t('chart.calls', { calls: activeData.calls }) }}
        </span>
      </div>

      <div class="space-y-1 font-mono">
        <div class="flex justify-between items-center text-gray-500 dark:text-gray-400">
          <span>{{ t('chart.totalTokens') }}</span>
          <span class="text-blue-600 dark:text-blue-400 font-bold">{{ activeData.total_tokens.toLocaleString() }}</span>
        </div>
        <div class="flex justify-between items-center text-gray-500 dark:text-gray-400">
          <span>{{ t('chart.inputPrompt') }}</span>
          <span class="text-gray-700 dark:text-gray-300">{{ activeData.prompt_tokens.toLocaleString() }}</span>
        </div>
        <div class="flex justify-between items-center text-gray-500 dark:text-gray-400">
          <span>{{ t('chart.output') }}</span>
          <span class="text-gray-700 dark:text-gray-300">{{ activeData.output_tokens.toLocaleString() }}</span>
        </div>
        <div class="flex justify-between items-center text-amber-500 dark:text-amber-400">
          <span>{{ t('chart.thinking') }}</span>
          <span>{{ activeData.thinking_tokens.toLocaleString() }}</span>
        </div>
        <div v-if="activeData.avg_speed > 0" class="flex justify-between items-center text-purple-600 dark:text-purple-400">
          <span>{{ t('chart.avgSpeed') }}</span>
          <span>{{ activeData.avg_speed.toFixed(1) }} tok/s</span>
        </div>
        <div class="flex justify-between items-center text-emerald-600 dark:text-emerald-400 pt-1 border-t border-gray-100 dark:border-gray-800/80 font-semibold">
          <span>{{ t('chart.estimatedCost') }}</span>
          <span>{{ currencySymbol }}{{ activeData.cost.toFixed(4) }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from "vue";
import type { TokenTrendPoint } from "../types";
import { activeTheme } from "../services/theme";
import { t, currentLocale } from "../services/i18n";

const props = withDefaults(
  defineProps<{
    data: TokenTrendPoint[];
    currencySymbol?: string;
  }>(),
  {
    currencySymbol: "$",
  }
);

const containerRef = ref<HTMLDivElement | null>(null);
const chartWidth = ref(800);
const chartHeight = ref(280);

const paddingTop = 20;
const paddingBottom = 32;
const paddingLeft = 55;
const paddingRight = 24;

const hoverIndex = ref<number | null>(null);

const hasData = computed(() => {
  return props.data && props.data.length > 0 && props.data.some((p) => p.total_tokens > 0);
});

const updateDimensions = () => {
  if (containerRef.value) {
    chartWidth.value = containerRef.value.clientWidth || 800;
    chartHeight.value = containerRef.value.clientHeight || 280;
  }
};

let resizeObserver: ResizeObserver | null = null;
onMounted(() => {
  updateDimensions();
  if (containerRef.value && typeof ResizeObserver !== "undefined") {
    resizeObserver = new ResizeObserver(() => {
      updateDimensions();
    });
    resizeObserver.observe(containerRef.value);
  }
});

onUnmounted(() => {
  if (resizeObserver) {
    resizeObserver.disconnect();
  }
});

// Calculate Max Value rounded nicely
const maxVal = computed(() => {
  if (!props.data || props.data.length === 0) return 1000;
  const m = Math.max(...props.data.map((p) => p.total_tokens), 0);
  if (m <= 0) return 1000;

  const exp = Math.floor(Math.log10(m));
  const factor = Math.pow(10, exp);
  const normalized = m / factor;
  let nice = 10;
  if (normalized <= 1.0) nice = 1.0;
  else if (normalized <= 2.0) nice = 2.0;
  else if (normalized <= 2.5) nice = 2.5;
  else if (normalized <= 5.0) nice = 5.0;
  return Math.ceil(nice * factor);
});

const formatShortNumber = (num: number) => {
  if (num >= 1_000_000_000) return `${(num / 1_000_000_000).toFixed(1)}B`;
  if (num >= 1_000_000) return `${(num / 1_000_000).toFixed(1)}M`;
  if (num >= 1_000) return `${(num / 1_000).toFixed(1)}K`;
  return num.toString();
};

const formatFullDate = (raw: string) => {
  if (!raw) return "";
  // Check if raw is YYYY-MM-DD
  const m = raw.match(/^(\d{4})-(\d{2})-(\d{2})$/);
  if (m) {
    const [, year, month, day] = m;
    if (currentLocale.value === "en") {
      const monthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
      const mIdx = parseInt(month, 10) - 1;
      return `${monthNames[mIdx] || month} ${parseInt(day, 10)}, ${year}`;
    }
    return `${year}年${parseInt(month, 10)}月${parseInt(day, 10)}日`;
  }
  return raw;
};

const gridLines = computed(() => {
  const lines = [];
  const count = 4;
  const plotH = chartHeight.value - paddingTop - paddingBottom;
  for (let i = 0; i <= count; i++) {
    const fraction = i / count;
    const y = paddingTop + plotH - fraction * plotH;
    const val = fraction * maxVal.value;
    lines.push({
      y,
      label: formatShortNumber(val),
    });
  }
  return lines;
});

const pointsCoords = computed(() => {
  if (!props.data || props.data.length === 0) return [];
  const plotW = chartWidth.value - paddingLeft - paddingRight;
  const plotH = chartHeight.value - paddingTop - paddingBottom;
  const n = props.data.length;
  const stepX = n > 1 ? plotW / (n - 1) : plotW;

  return props.data.map((p, i) => {
    const x = n > 1 ? paddingLeft + i * stepX : paddingLeft + plotW / 2;
    const ratio = maxVal.value > 0 ? p.total_tokens / maxVal.value : 0;
    const y = paddingTop + plotH - ratio * plotH;
    return { x, y };
  });
});

const linePath = computed(() => {
  const coords = pointsCoords.value;
  if (coords.length === 0) return "";
  return coords.map((pt) => `${pt.x},${pt.y}`).join(" ");
});

const areaPath = computed(() => {
  const coords = pointsCoords.value;
  if (coords.length < 2) return "";
  const plotH = chartHeight.value - paddingTop - paddingBottom;
  const bottomY = paddingTop + plotH;
  const first = coords[0];
  const last = coords[coords.length - 1];

  let path = `${first.x},${bottomY} `;
  path += coords.map((pt) => `${pt.x},${pt.y}`).join(" ");
  path += ` ${last.x},${bottomY}`;
  return path;
});

const xLabels = computed(() => {
  if (!props.data || props.data.length === 0) return [];
  const coords = pointsCoords.value;
  const n = props.data.length;
  const count = Math.min(6, n);
  if (count <= 1) {
    return [{ x: coords[0].x, text: props.data[0].date_label }];
  }

  const step = Math.ceil((n - 1) / (count - 1));
  const list = [];
  for (let i = 0; i < n; i += step) {
    list.push({ x: coords[i].x, text: props.data[i].date_label });
  }
  if ((n - 1) % step !== 0) {
    list.push({ x: coords[n - 1].x, text: props.data[n - 1].date_label });
  }
  return list;
});

const onMouseMove = (e: MouseEvent) => {
  if (!containerRef.value || !props.data || props.data.length === 0) return;
  const rect = containerRef.value.getBoundingClientRect();
  const mouseX = e.clientX - rect.left;

  const coords = pointsCoords.value;
  let closestIdx = 0;
  let minDist = Infinity;

  coords.forEach((pt, idx) => {
    const dist = Math.abs(pt.x - mouseX);
    if (dist < minDist) {
      minDist = dist;
      closestIdx = idx;
    }
  });

  hoverIndex.value = closestIdx;
};

const onMouseLeave = () => {
  hoverIndex.value = null;
};

const activePoint = computed(() => {
  if (hoverIndex.value === null) return null;
  return pointsCoords.value[hoverIndex.value] || null;
});

const activeData = computed(() => {
  if (hoverIndex.value === null) return null;
  return props.data[hoverIndex.value] || null;
});

const tooltipStyle = computed(() => {
  if (!activePoint.value) return {};
  const pt = activePoint.value;
  const cardW = 210;
  let left = pt.x + 16;
  if (left + cardW > chartWidth.value - 10) {
    left = pt.x - cardW - 16;
  }
  let top = pt.y - 70;
  if (top < 10) top = 10;
  if (top > chartHeight.value - 150) top = chartHeight.value - 150;

  return {
    left: `${left}px`,
    top: `${top}px`,
  };
});
</script>
