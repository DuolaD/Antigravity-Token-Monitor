# Antigravity Tokens

<div align="center">

English | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md)

<p>
A lightweight, cross-platform token tracker and cost calculator for Google Antigravity and Antigravity IDE.<br>
Built with Tauri v2, Rust, and Vue 3. Single-architecture release packages are roughly 3.2 MB, runtime memory usage is around 30 to 50 MB, and the application monitors token usage across workspaces in real time.
</p>

[![Platform](https://img.shields.io/badge/Platform-macOS%20%7C%20Windows-blue.svg)](#platform-support)
[![Architecture](https://img.shields.io/badge/Arch-x86__64%20%7C%20ARM64%20(Apple%20Silicon%20%2F%20WoA)-emerald.svg)](#platform-support)
[![Bundle Size](https://img.shields.io/badge/Bundle%20Size-~3.2MB-success.svg)](#features)
[![License](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE)

</div>

---

## Features

- **Small footprint**: Single-architecture release packages are around 3.2 MB (universal binary is ~6.4 MB). Cold startup takes milliseconds, and runtime memory stays within 30 to 50 MB.
- **Native cross-platform support**:
  - macOS: Supports Apple Silicon (M series) and Intel (x86_64) architectures. Reads local conversation data from `~/.gemini/antigravity`.
  - Windows: Runs natively on Windows x64 and Windows on ARM (WoA / Snapdragon) without emulation.
- **Dual-track file monitoring**: The Rust backend watches file modification timestamps and SQLite WAL (write-ahead log) changes simultaneously, updating metrics and charts as conversations happen.
- **Thinking token breakdown**: Parses binary Protobuf fields in SQLite `gen_metadata` to separate input prompt, thinking, and candidate output tokens.
- **Trends and usage analytics**: Built-in gradient line charts with range presets (week, month, quarter, year, all time), crosshair inspection cards, model and workspace rankings, and daily summary tables.
- **Call logs and CSV export**: Search and filter records by conversation title, model, or session UUID. Exports standard `.csv` files with a UTF-8 BOM for spreadsheet applications and cost audits.
- **Custom pricing and currency switching**: Configure token rates per million tokens and toggle currency display between USD ($), CNY (¥), and other currencies.

---

## Tech stack

- **Host framework**: [Tauri v2](https://v2.tauri.app/) (uses system webviews, eliminating bundled Chromium overhead)
- **Backend**: Rust (2021 Edition)
  - `rusqlite` (bundled, statically linked without external C dependencies)
  - Handcrafted binary Protobuf WireType decoder (zero-copy, minimal memory allocation)
  - `chrono`, `dirs`, `regex`, `urlencoding`
- **Frontend**: Vue 3, TypeScript, Vite, Tailwind CSS, Lucide Icons

---

## Platform support

| Operating system | Target architecture | Build target | Package format |
| :--- | :--- | :--- | :--- |
| **macOS** | Apple Silicon (M1/M2/M3/M4) | `aarch64-apple-darwin` | `.dmg` / `.app` (~3.2 MB) |
| **macOS** | Intel (x86_64) | `x86_64-apple-darwin` | `.dmg` / `.app` (~3.4 MB) |
| **macOS** | Universal binary | `universal-apple-darwin` | `.dmg` / `.app` (~6.4 MB) |
| **Windows** | PC (x64) | `x86_64-pc-windows-msvc` | `.exe` / `.zip` (~4.5 MB) |
| **Windows** | Windows on ARM (WoA) | `aarch64-pc-windows-msvc` | `.exe` / `.zip` (~4.2 MB) |

---

## Development and build

### Prerequisites

1. [Rust](https://rustup.rs/) (>= 1.75.0)
2. [Bun](https://bun.sh/) (or Node.js >= 18)

### Local development

```bash
# Install frontend dependencies
bun install

# Start the desktop development environment with hot reload
bun run tauri dev
```

### Production build

```bash
# Build an installer for the current host system
bun run tauri build

# macOS: build a universal binary
bun run tauri build --target universal-apple-darwin

# macOS: build for a specific architecture
bun run tauri build --target aarch64-apple-darwin
bun run tauri build --target x86_64-apple-darwin
```

Build outputs are located in `src-tauri/target/release/bundle/` or `src-tauri/target/<target>/release/bundle/`.

---

## Automated CI/CD

The repository includes a multi-architecture workflow at [`.github/workflows/release.yml`](.github/workflows/release.yml):

- **Trigger**: Pushing a version tag (such as `v1.0.0`) or running the workflow manually from the GitHub Actions tab.
- **Build matrix**: Compiles and packages binaries for five platform targets in parallel across Windows and macOS runners, then uploads the release assets to GitHub Releases.

---

## FAQ

### macOS prompts that the app "is damaged and can't be opened"?

Because this open-source build is not signed with an Apple Developer ID certificate or notarized, macOS Gatekeeper may block the app and display an alert like `"Antigravity Tokens" is damaged and should be moved to the Trash` or `cannot be opened because the developer cannot be verified`.

**Solution**: Drag the app into your `/Applications` folder, open Terminal, and run the following command to remove the quarantine attribute:

```bash
sudo xattr -r -d com.apple.quarantine "/Applications/Antigravity Tokens.app"
```

---

## Acknowledgments

This project is refactored and developed based on [CherySui@AntigravityTokens](https://github.com/CherySui/AntigravityTokens). Thanks to the original author for their contributions.

---

## License

This project is licensed under the [MIT](LICENSE) license.
