import { openUrl } from "@tauri-apps/plugin-opener";

export const PROJECT_HOMEPAGE_URL = "https://github.com/DuolaD/Antigravity-Token-Monitor";

export async function openProjectHomepage(): Promise<void> {
  try {
    await openUrl(PROJECT_HOMEPAGE_URL);
  } catch (err) {
    console.warn("openUrl via tauri plugin failed, falling back to window.open", err);
    if (typeof window !== "undefined") {
      window.open(PROJECT_HOMEPAGE_URL, "_blank");
    }
  }
}
