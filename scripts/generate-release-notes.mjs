import { execSync } from "node:child_process";
import fs from "node:fs";
import process from "node:process";

function run(cmd) {
  try {
    return execSync(cmd, { encoding: "utf-8", stdio: ["pipe", "pipe", "ignore"] }).trim();
  } catch {
    return "";
  }
}

/**
 * Resolve repository in owner/repo format
 */
function getRepository() {
  if (process.env.GITHUB_REPOSITORY) {
    return process.env.GITHUB_REPOSITORY;
  }
  const remoteUrl = run("git remote get-url origin");
  const match = remoteUrl.match(/[:/]([^/]+\/[^/.]+)(\.git)?$/);
  return match ? match[1] : "DuolaD/AntigravityTokens-DuolaD";
}

/**
 * Find previous release tag before currentTag
 */
function getPreviousTag(currentTag) {
  // 1. Try gh release list if available
  const ghReleases = run("gh release list --exclude-drafts --limit 20 --json tagName --jq '.[].tagName'");
  if (ghReleases) {
    const tags = ghReleases.split("\n").map((t) => t.trim()).filter(Boolean);
    const prev = tags.find((t) => t !== currentTag);
    if (prev) return prev;
  }

  // 2. Try git describe on parent commit
  if (currentTag) {
    const describeTag = run(`git describe --tags --abbrev=0 "${currentTag}^" 2>/dev/null`);
    if (describeTag && describeTag !== currentTag) {
      return describeTag;
    }
  }

  // 3. Try git tag sorted by version
  const allTags = run("git tag --sort=-v:refname");
  if (allTags) {
    const tags = allTags.split("\n").map((t) => t.trim()).filter(Boolean);
    const prev = tags.find((t) => t !== currentTag);
    if (prev) return prev;
  }

  return "";
}

/**
 * Extract commit list as markdown bullet list ("- %s")
 */
function getCommitLog(prevTag, currentTag) {
  let log = "";
  if (prevTag && currentTag) {
    log = run(`git log "${prevTag}..${currentTag}" --pretty=format:"- %s"`);
    if (!log) {
      log = run(`git log "${prevTag}..HEAD" --pretty=format:"- %s"`);
    }
  } else if (currentTag) {
    log = run(`git log "${currentTag}" --pretty=format:"- %s"`);
    if (!log) {
      log = run('git log "HEAD" --pretty=format:"- %s"');
    }
  } else {
    log = run('git log -n 10 --pretty=format:"- %s"');
  }

  if (!log) {
    log = run('git log -n 1 --pretty=format:"- %s"');
  }

  return log ? log.trim() : "- Update application";
}

/**
 * Fetch existing body from GitHub Release if already published
 */
function getExistingReleaseBody(currentTag) {
  if (!currentTag) return "";
  const body = run(`gh release view "${currentTag}" --json body --jq '.body'`);
  return body ? body.trim() : "";
}

// Main execution
const currentTag = process.argv[2] || process.env.TAG_NAME || "";
const outputFile = process.argv[3] || "release-notes.md";

const serverUrl = process.env.GITHUB_SERVER_URL || "https://github.com";
const repo = getRepository();

console.log(`[generate-release-notes] Current Tag: "${currentTag}"`);
console.log(`[generate-release-notes] Repository: "${repo}"`);

const prevTag = getPreviousTag(currentTag);
console.log(`[generate-release-notes] Previous Tag: "${prevTag || '(None)'}"`);

// Determine Changelog compare link
let compareUrl = "";
if (prevTag && currentTag) {
  compareUrl = `${serverUrl}/${repo}/compare/${prevTag}...${currentTag}`;
} else if (currentTag) {
  compareUrl = `${serverUrl}/${repo}/commits/${currentTag}`;
} else {
  compareUrl = `${serverUrl}/${repo}/commits/main`;
}

// Check existing release body
const existingBody = getExistingReleaseBody(currentTag);
let finalBody = "";

if (existingBody) {
  console.log("[generate-release-notes] Existing release description detected.");
  finalBody = existingBody;
} else {
  console.log("[generate-release-notes] No existing description found. Generating from commit log...");
  finalBody = getCommitLog(prevTag, currentTag);
}

// Ensure finalBody is cleanly trimmed before appending changelog
finalBody = finalBody.trim();

// Append Full Changelog link if not already present
const changelogLine = `**Full Changelog**: ${compareUrl}`;
if (!finalBody.includes("Full Changelog:")) {
  if (finalBody) {
    finalBody = `${finalBody}\n\n${changelogLine}`;
  } else {
    finalBody = changelogLine;
  }
}

fs.writeFileSync(outputFile, finalBody + "\n", "utf-8");
console.log(`[generate-release-notes] Successfully wrote notes to ${outputFile}:`);
console.log("-----------------------------------------");
console.log(finalBody);
console.log("-----------------------------------------");
