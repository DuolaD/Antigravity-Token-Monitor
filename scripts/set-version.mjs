import fs from "node:fs";
import path from "node:path";
import process from "node:process";

/**
 * Parse any raw tag string into:
 * - tag: the original or display tag string
 * - version: a strictly valid SemVer string required by Cargo and Tauri
 */
export function parseTagAndVersion(rawInput, fallbackVersion = "1.0.0") {
  const raw = (rawInput || "").trim();
  if (!raw) {
    return { tag: `v${fallbackVersion}`, version: fallbackVersion };
  }

  const tag = raw;
  let clean = raw.replace(/^[vV]/, "");

  // 1. Standard SemVer (e.g., 1.2.3, 1.2.3-beta.1, 1.2.3+20261010)
  const semverRegex = /^(\d+)\.(\d+)\.(\d+)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$/;
  if (semverRegex.test(clean)) {
    return { tag, version: clean };
  }

  // 2. Two-part version (e.g., 1.2 -> 1.2.0)
  const twoPartRegex = /^(\d+)\.(\d+)(-[0-9A-Za-z.-]+)?$/;
  if (twoPartRegex.test(clean)) {
    const match = clean.match(twoPartRegex);
    return { tag, version: `${match[1]}.${match[2]}.0${match[3] || ""}` };
  }

  // 3. Single integer version (e.g., 2 -> 2.0.0)
  const onePartRegex = /^(\d+)(-[0-9A-Za-z.-]+)?$/;
  if (onePartRegex.test(clean)) {
    const match = clean.match(onePartRegex);
    return { tag, version: `${match[1]}.0.0${match[2] || ""}` };
  }

  // 4. Embedded version pattern (e.g., release-1.2.3, v2026.10.10)
  const extractRegex = /(\d+\.\d+(\.\d+)?(-[0-9A-Za-z.-]+)?)/;
  const match = clean.match(extractRegex);
  if (match) {
    let extracted = match[1];
    if (extracted.split(".").length === 2) {
      extracted += ".0";
    }
    return { tag, version: extracted };
  }

  // 5. Completely non-numeric tags (e.g., nightly, canary, my-tag)
  // Cargo and Tauri require a valid SemVer format, so create a valid pre-release semver
  const safePrerelease = clean.replace(/[^0-9A-Za-z.-]/g, "-").replace(/^-+|-+$/g, "") || "build";
  return { tag, version: `${fallbackVersion}-${safePrerelease}` };
}

/**
 * Apply the version to package.json, src-tauri/tauri.conf.json, and src-tauri/Cargo.toml
 */
export function applyVersion(rootDir, newVersion) {
  const packageJsonPath = path.join(rootDir, "package.json");
  const tauriConfPath = path.join(rootDir, "src-tauri", "tauri.conf.json");
  const cargoTomlPath = path.join(rootDir, "src-tauri", "Cargo.toml");

  // 1. package.json
  if (fs.existsSync(packageJsonPath)) {
    const pkg = JSON.parse(fs.readFileSync(packageJsonPath, "utf-8"));
    pkg.version = newVersion;
    fs.writeFileSync(packageJsonPath, JSON.stringify(pkg, null, 2) + "\n", "utf-8");
    console.log(`[set-version] Updated package.json -> ${newVersion}`);
  }

  // 2. src-tauri/tauri.conf.json
  if (fs.existsSync(tauriConfPath)) {
    const tauriConf = JSON.parse(fs.readFileSync(tauriConfPath, "utf-8"));
    tauriConf.version = newVersion;
    fs.writeFileSync(tauriConfPath, JSON.stringify(tauriConf, null, 2) + "\n", "utf-8");
    console.log(`[set-version] Updated tauri.conf.json -> ${newVersion}`);
  }

  // 3. src-tauri/Cargo.toml (only replace version inside [package] table)
  if (fs.existsSync(cargoTomlPath)) {
    const cargo = fs.readFileSync(cargoTomlPath, "utf-8");
    const updated = cargo.replace(
      /(^\[package\][\s\S]*?^version\s*=\s*)"[^"]+"/m,
      `$1"${newVersion}"`
    );
    fs.writeFileSync(cargoTomlPath, updated, "utf-8");
    console.log(`[set-version] Updated Cargo.toml -> ${newVersion}`);
  }
}

// CLI Execution
const args = process.argv.slice(2);
const rootDir = process.cwd();

// Find default version from package.json if available
let defaultVersion = "1.0.0";
try {
  const pkg = JSON.parse(fs.readFileSync(path.join(rootDir, "package.json"), "utf-8"));
  if (pkg.version) defaultVersion = pkg.version;
} catch {
  // ignore
}

if (args.includes("--parse")) {
  const parseIndex = args.indexOf("--parse");
  const rawInput = args[parseIndex + 1] || "";
  const result = parseTagAndVersion(rawInput, defaultVersion);
  console.log(JSON.stringify(result));

  if (args.includes("--github-output") && process.env.GITHUB_OUTPUT) {
    fs.appendFileSync(
      process.env.GITHUB_OUTPUT,
      `tag=${result.tag}\nversion=${result.version}\n`,
      "utf-8"
    );
  }
  process.exit(0);
}

const inputTagOrVersion = args[0] || "";
const { tag, version } = parseTagAndVersion(inputTagOrVersion, defaultVersion);
console.log(`[set-version] Resolved Tag: "${tag}", SemVer Version: "${version}"`);
applyVersion(rootDir, version);
