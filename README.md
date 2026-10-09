# Antigravity Tokens

<div align="center">

专为 **Google Antigravity**（以及 Antigravity IDE）深度设计的轻量级跨平台 Token 消耗监控与费用核算系统。  
基于 **Tauri v2 + Rust + Vue 3** 构建，**极致轻量（安装包仅 ~3MB，内存占用仅 ~30MB）**，支持毫秒级实时监听、细粒度思考 Token 拆解及多工作区聚合分析。

[![Platform](https://img.shields.io/badge/Platform-macOS%20%7C%20Windows-blue.svg)](#平台支持)
[![Architecture](https://img.shields.io/badge/Arch-x86__64%20%7C%20ARM64%20(Apple%20Silicon%20%2F%20WoA)-emerald.svg)](#平台支持)
[![Bundle Size](https://img.shields.io/badge/Bundle%20Size-~3.2MB-success.svg)](#项目特点)
[![License](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE)

</div>

---

## 🌟 项目特点与重构革新

- 🪶 **极致小而美**：告别原先动辄 100MB+ 的臃肿体积，单架构 Release 安装包仅 **~3.2MB**（Universal 全架构通用包仅 **~6.4MB**），冷启动毫秒级，运行内存低至 30~50MB。
- 🍏 **原生跨平台兼容**：
  - **macOS**：完美适配 Apple Silicon (M系列芯片) 与 Intel (x86_64)，自动识别读取 `~/.gemini/antigravity` 本地会话数据。
  - **Windows**：原生支持 Windows x64 以及 **Windows on ARM (WoA / 骁龙平台)**，无需转译无损耗运行。
- ⚡ **毫秒级双轨实时监听**：底层 Rust 采用文件修改时间与 SQLite WAL 预写日志双轨监听，对话产生时指标与图表自动跳动递增。
- 🧠 **显式思考 Tokens (Thinking) 深度解析**：直接逆向解析本地 SQLite `gen_metadata` 中的二进制 Protobuf 数据，清晰区分输入 Prompt、深度思考 Thinking 与输出 Candidate Tokens 占比。
- 📊 **交互式趋势与多维分析**：内置自绘渐变趋势折线图（周/月/季/年/自定义跨度）、十字准星浮动数据卡、模型/工作区排行进度条、按日归集报表。
- 📝 **明细流水与一键 CSV 导出**：支持按对话标题/模型/UUID检索与筛选，支持一键导出带 UTF-8 BOM 的标准 `.csv` 报表供成本审计。
- 💰 **自由模型倍率与币种换算**：支持自定义每百万 Token 单价，一键切换 USD ($) 与 CNY (¥) 汇率换算。

---

## 🛠️ 技术架构

- **宿主框架**: [Tauri v2](https://v2.tauri.app/) (轻量系统级 WebView 绑定，零内置 Chromium 开销)
- **后端核心**: Rust (2021 Edition)
  - `rusqlite` (bundled，零外部 C 库依赖，跨平台纯净静态链接)
  - 轻量级二进制 Protobuf WireType 纯手写解码器（零拷贝、超低内存分配）
  - `chrono` / `dirs` / `regex` / `urlencoding`
- **前端界面**: Vue 3 + TypeScript + Vite + Tailwind CSS + Lucide Icons

---

## 🖥️ 平台与架构支持矩阵

| 操作系统 | 目标架构 | 构建目标 Target | 产物格式 |
| :--- | :--- | :--- | :--- |
| **macOS** | Apple Silicon (M1/M2/M3/M4) | `aarch64-apple-darwin` | `.dmg` / `.app` (~3.2 MB) |
| **macOS** | Intel 架构 | `x86_64-apple-darwin` | `.dmg` / `.app` (~3.4 MB) |
| **macOS** | 通用二进制 (Universal) | `universal-apple-darwin` | `.dmg` / `.app` (~6.4 MB) |
| **Windows** | 传统 PC (x64) | `x86_64-pc-windows-msvc` | `.exe` / `.zip` (~4.5 MB) |
| **Windows** | Windows on ARM (WoA) | `aarch64-pc-windows-msvc` | `.exe` / `.zip` (~4.2 MB) |

---

## 🚀 本地开发与编译

### 前置环境

1. [Rust](https://rustup.rs/) (>= 1.75.0)
2. [Bun](https://bun.sh/) (或 Node.js >= 18)

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

## 🤖 自动化 CI/CD (GitHub Actions)

项目内置了自动化多架构流水线 [`.github/workflows/release.yml`](.github/workflows/release.yml)：

- **触发条件**：推送版本 tag（例如 `git push origin v1.0.0`）或在 GitHub Actions 界面手动点击 `Run workflow`。
- **构建矩阵**：并行在 Windows 和 macOS 虚拟机上完成 5 种架构与平台的二进制打包编译，自动上传至 GitHub Release。

---

## 📄 开源协议

本项目采用 [MIT](LICENSE) 协议开源。
