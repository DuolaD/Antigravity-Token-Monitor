import { ref, computed } from "vue";

export type ThemeMode = "system" | "light" | "dark";

const STORAGE_KEY = "antigravity_tokens_theme_mode";

// Get initial mode from localStorage, defaulting to 'system' (follow OS)
const storedMode = (localStorage.getItem(STORAGE_KEY) as ThemeMode) || "system";
export const themeMode = ref<ThemeMode>(
  storedMode === "light" || storedMode === "dark" || storedMode === "system"
    ? storedMode
    : "system"
);

// MatchMedia listener for OS color scheme
const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
export const systemIsDark = ref(mediaQuery.matches);

mediaQuery.addEventListener("change", (e) => {
  systemIsDark.value = e.matches;
  if (themeMode.value === "system") {
    applyTheme();
  }
});

// Computed active resolved theme: 'light' | 'dark'
export const activeTheme = computed<"light" | "dark">(() => {
  if (themeMode.value === "light") return "light";
  if (themeMode.value === "dark") return "dark";
  return systemIsDark.value ? "dark" : "light";
});

export function setThemeMode(mode: ThemeMode) {
  themeMode.value = mode;
  localStorage.setItem(STORAGE_KEY, mode);
  applyTheme();
}

export function applyTheme() {
  const isDark = activeTheme.value === "dark";
  const root = document.documentElement;

  if (isDark) {
    root.classList.add("dark");
    root.classList.remove("light");
    root.setAttribute("data-theme", "dark");
    root.style.colorScheme = "dark";
  } else {
    root.classList.remove("dark");
    root.classList.add("light");
    root.setAttribute("data-theme", "light");
    root.style.colorScheme = "light";
  }
}

// Initialize theme on app boot
export function initTheme() {
  applyTheme();
}
