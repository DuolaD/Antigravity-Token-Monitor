# Antigravity Tokens

<div align="center">

[English](README.md) | [简体中文](README.zh-CN.md) | 繁體中文

<p>
專為 Google Antigravity 與 Antigravity IDE 設計的輕量跨平台 Token 監控與費用統計工具。<br>
基於 Tauri v2、Rust 與 Vue 3 建置。單架構安裝包約 3.2MB，執行記憶體約 30 至 50MB，支援即時檔案監聽、思考 Token 拆解與多工作區統計。
</p>

[![Platform](https://img.shields.io/badge/Platform-macOS%20%7C%20Windows-blue.svg)](#平台與架構支援)
[![Architecture](https://img.shields.io/badge/Arch-x86__64%20%7C%20ARM64%20(Apple%20Silicon%20%2F%20WoA)-emerald.svg)](#平台與架構支援)
[![Bundle Size](https://img.shields.io/badge/Bundle%20Size-~3.2MB-success.svg)](#主要特點)
[![License](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE)

</div>

---

## 主要特點

- **體積小、開銷低**：單架構 Release 安裝包約 3.2MB（Universal 通用包約 6.4MB），毫秒級冷啟動，執行記憶體維持在 30 至 50MB。
- **原生跨平台支援**：
  - macOS：支援 Apple Silicon（M 系列晶片）與 Intel（x86_64）架構，自動讀取 `~/.gemini/antigravity` 本地對話資料。
  - Windows：原生支援 x64 與 Windows on ARM（WoA / 驍龍平台），無需轉譯執行。
- **雙軌即時監聽**：底層 Rust 結合檔案修改時間與 SQLite WAL（預寫日誌）變動，對話更新時統計指標與圖表自動同步。
- **思考 Tokens（Thinking）拆解**：直接解析本地 SQLite `gen_metadata` 中的二進位 Protobuf 資料，清楚區分輸入 Prompt、深度思考 Thinking 與輸出 Candidate Tokens。
- **趨勢與多維統計**：內建趨勢折線圖（支援週、月、季度、年及全部範圍）、十字準星資料卡、模型與工作區排行，以及按日彙總資料。
- **呼叫日誌與 CSV 匯出**：支援依對話標題、模型或 UUID 篩選記錄；可匯出帶 UTF-8 BOM 的 `.csv` 報表，便於在試算表軟體中檢視與對帳。
- **自訂費率與貨幣換算**：支援依每百萬 Token 自訂單價，並可在 USD（$）、CNY（¥）等貨幣間換算。

---

## 技術架構

- **宿主框架**：[Tauri v2](https://v2.tauri.app/)（使用系統原生 WebView 綁定，無內建 Chromium 執行階段開銷）
- **後端核心**：Rust（2021 Edition）
  - `rusqlite`（bundled，純靜態連結，無外部 C 動態程式庫依賴）
  - 二進位 Protobuf WireType 解碼器（零拷貝、極低記憶體分配）
  - `chrono` / `dirs` / `regex` / `urlencoding`
- **前端介面**：Vue 3 + TypeScript + Vite + Tailwind CSS + Lucide Icons

---

## 平台與架構支援

| 作業系統 | 目標架構 | 建置目標 Target | 產物格式 |
| :--- | :--- | :--- | :--- |
| **macOS** | Apple Silicon (M1/M2/M3/M4) | `aarch64-apple-darwin` | `.dmg` / `.app` (~3.2 MB) |
| **macOS** | Intel 架構 | `x86_64-apple-darwin` | `.dmg` / `.app` (~3.4 MB) |
| **macOS** | 通用二進位 (Universal) | `universal-apple-darwin` | `.dmg` / `.app` (~6.4 MB) |
| **Windows** | 傳統 PC (x64) | `x86_64-pc-windows-msvc` | `.exe` / `.zip` (~4.5 MB) |
| **Windows** | Windows on ARM (WoA) | `aarch64-pc-windows-msvc` | `.exe` / `.zip` (~4.2 MB) |

---

## 本地開發與建置

### 前置環境

1. [Rust](https://rustup.rs/) (>= 1.75.0)
2. [Bun](https://bun.sh/)（或 Node.js >= 18）

### 開發除錯

```bash
# 安裝前端相依套件
bun install

# 啟動桌面端熱重載開發環境
bun run tauri dev
```

### 生產打包

```bash
# 建置當前系統原生安裝包
bun run tauri build

# macOS 建置通用架構 (Universal Binary)
bun run tauri build --target universal-apple-darwin

# macOS 建置特定架構
bun run tauri build --target aarch64-apple-darwin
bun run tauri build --target x86_64-apple-darwin
```

打包產物位於 `src-tauri/target/release/bundle/` 或 `src-tauri/target/<target>/release/bundle/`。

---

## 自動化建置與發布

專案內建自動化多架構工作流程 [`.github/workflows/release.yml`](.github/workflows/release.yml)：

- **觸發條件**：推送版本標籤（例如 `git push origin v1.0.0`）或在 GitHub Actions 介面手動點擊 `Run workflow`。
- **建置矩陣**：在 Windows 與 macOS 虛擬機上平行完成 5 種架構與平台的二進位編譯與打包，並自動上傳至 GitHub Releases。

---

## 致謝

本專案基於 [CherySui@AntigravityTokens](https://github.com/CherySui/AntigravityTokens) 重構並開發，在此感謝原作者的貢獻。

---

## 開源授權

本專案採用 [MIT](LICENSE) 授權條款開源。
