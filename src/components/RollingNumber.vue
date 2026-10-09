<template>
  <span class="inline-block font-mono tracking-tight transition-colors duration-300">
    {{ prefix }}{{ formattedValue }}{{ suffix }}
  </span>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, computed } from "vue";

const props = withDefaults(
  defineProps<{
    value: number;
    decimals?: number;
    prefix?: string;
    suffix?: string;
    duration?: number;
  }>(),
  {
    decimals: 0,
    prefix: "",
    suffix: "",
    duration: 600,
  }
);

const displayValue = ref(props.value);
let animationFrameId: number | null = null;

const formatNumber = (num: number, dec: number) => {
  if (isNaN(num)) return "0";
  if (dec > 0) {
    return num.toLocaleString(undefined, {
      minimumFractionDigits: dec,
      maximumFractionDigits: dec,
    });
  }
  return Math.round(num).toLocaleString();
};

const formattedValue = computed(() => {
  return formatNumber(displayValue.value, props.decimals);
});

const animateTo = (target: number) => {
  const start = displayValue.value;
  const startTime = performance.now();
  const dur = props.duration;

  if (animationFrameId !== null) {
    cancelAnimationFrame(animationFrameId);
  }

  const update = (now: number) => {
    const elapsed = now - startTime;
    const progress = Math.min(elapsed / dur, 1);
    // Ease out cubic: 1 - pow(1 - x, 3)
    const ease = 1 - Math.pow(1 - progress, 3);
    displayValue.value = start + (target - start) * ease;

    if (progress < 1) {
      animationFrameId = requestAnimationFrame(update);
    } else {
      displayValue.value = target;
      animationFrameId = null;
    }
  };

  animationFrameId = requestAnimationFrame(update);
};

watch(
  () => props.value,
  (newVal) => {
    animateTo(newVal);
  }
);

onMounted(() => {
  displayValue.value = props.value;
});
</script>
