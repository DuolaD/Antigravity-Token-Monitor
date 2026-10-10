# Antigravity Tokens

<div align="center">

[English](README.md) | 简体中文 | [繁體中文](README.zh-TW.md)

<p>
面向 Google Antigravity 与 Antigravity IDE 的轻量跨平台 Token 监控与费用统计工具。<br>
基于 Tauri v2、Rust 与 Vue 3 构建。单架构安装包约 3.2MB，运行内存约 30 至 50MB，支持实时文件监听、思考 Token 拆解与多工作区统计。
</p>

[![Platform](https://img.shields.io/badge/Platform-macOS%20%7C%20Windows-blue.svg)](#平台与架构支持)
[![Architecture](https://img.shields.io/badge/Arch-x86__64%20%7C%20ARM64%20(Apple%20Silicon%20%2F%20WoA)-emerald.svg)](#平台与架构支持)
[![Bundle Size](https://img.shields.io/badge/Bundle%20Size-~3.2MB-success.svg)](#主要特性)
[![License](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE)

</div>

---

## 主要特性

- **体积小、资源占用低**：单架构 Release 安装包约 3.2MB（Universal 通用包约 6.4MB），毫秒级冷启动，运行内存维持在 30 至 50MB。
- **原生跨平台支持**：
  - macOS：支持 Apple Silicon（M 系列芯片）与 Intel（x86_64）架构，自动读取 `~/.gemini/antigravity` 本地会话数据。
  - Windows：原生支持 x64 与 Windows on ARM（WoA / 骁龙平台），无需转译运行。
- **双轨实时监听**：底层 Rust 结合文件修改时间与 SQLite WAL（预写日志）变动，对话更新时统计指标与图表自动同步。
- **思考 Tokens（Thinking）拆解**：直接解析本地 SQLite `gen_metadata` 中的二进制 Protobuf 数据，清晰区分输入 Prompt、深度思考 Thinking 与输出 Candidate Tokens。
- **趋势与多维统计**：内置趋势折线图（支持周、月、季度、年及全部范围）、十字准星数据卡、模型与工作区排行，以及按日汇总数据。
- **调用日志与 CSV 导出**：支持按对话标题、模型或 UUID 筛选记录；可导出带 UTF-8 BOM 的 `.csv` 报表，方便在电子表格中查看与核算。
- **自定义费率与货币转换**：支持按每百万 Token 自定义单价，并可在 USD（$）、CNY（¥）等货币间换算。

---

## 技术架构

- **宿主框架**：[Tauri v2](https://v2.tauri.app/)（使用系统原生 WebView 绑定，无内置 Chromium 运行时开销）
- **后端核心**：Rust（2021 Edition）
  - `rusqlite`（bundled，纯静态链接，无外部 C 动态库依赖）
  - 二进制 Protobuf WireType 解码器（零拷贝、极低内存分配）
  - `chrono` / `dirs` / `regex` / `urlencoding`
- **前端界面**：Vue 3 + TypeScript + Vite + Tailwind CSS + Lucide Icons

---

## 平台与架构支持

| 操作系统 | 目标架构 | 构建目标 Target | 产物格式 |
| :--- | :--- | :--- | :--- |
| **macOS** | Apple Silicon (M1/M2/M3/M4) | `aarch64-apple-darwin` | `.dmg` / `.app` (~3.2 MB) |
| **macOS** | Intel 架构 | `x86_64-apple-darwin` | `.dmg` / `.app` (~3.4 MB) |
| **macOS** | 通用二进制 (Universal) | `universal-apple-darwin` | `.dmg` / `.app` (~6.4 MB) |
| **Windows** | 传统 PC (x64) | `x86_64-pc-windows-msvc` | `.exe` / `.zip` (~4.5 MB) |
| **Windows** | Windows on ARM (WoA) | `aarch64-pc-windows-msvc` | `.exe` / `.zip` (~4.2 MB) |

---

## 本地开发与构建

### 前置环境

1. [Rust](https://rustup.rs/) (>= 1.75.0)
2. [Bun](https://bun.sh/)（或 Node.js >= 18）

### 开发调试

```bash
# 安装前端依赖
bun install

# 启动桌面端热重载开发环境
bun run tauri dev
```

### 生产打包

```bash
# 构建当前系统原生安装包
bun run tauri build

# macOS 构建通用架构 (Universal Binary)
bun run tauri build --target universal-apple-darwin

# macOS 构建单一指定架构
bun run tauri build --target aarch64-apple-darwin
bun run tauri build --target x86_64-apple-darwin
```

打包产物位于 `src-tauri/target/release/bundle/` 或 `src-tauri/target/<target>/release/bundle/`。

---

## 自动化构建与发布

项目内置了自动化多架构工作流 [`.github/workflows/release.yml`](.github/workflows/release.yml)：

- **触发条件**：推送版本标签（例如 `git push origin v1.0.0`）或在 GitHub Actions 界面手动点击 `Run workflow`。
- **构建矩阵**：在 Windows 与 macOS 虚拟机上并行完成 5 种架构与平台的二进制编译与打包，并自动上传至 GitHub Releases。

---

## 开源协议

本项目采用 [MIT](LICENSE) 协议开源。
