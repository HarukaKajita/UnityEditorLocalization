#!/usr/bin/env node
// パッケージ同梱 skills/ を正本として、リポジトリ直下の .claude/skills / .agents/skills へ
// ミラー複製する（GOLD_STANDARD.md §2.6-4）。
//
// 使い方（リポジトリルートで実行）:
//   node scripts/sync-agent-skills.mjs           # 正本から生成ミラーへ複製
//   node scripts/sync-agent-skills.mjs --check   # バイト比較で drift 検査（不一致なら exit 1）
//
// 仕様:
// - 正本: Packages/*/skills/<skill>/ （embedded package 同梱スキル。複数パッケージ対応）
// - 生成先: .claude/skills/<skill>/ と .agents/skills/<skill>/
// - *.meta は複製しない（ミラーはエージェント用であり Unity にインポートされないため）
// - ミラーは「正本にあるスキル名のディレクトリ」だけを対象に完全一致へ揃える
//   （正本に無いファイルはミラー側から削除する。正本に無い別スキルのディレクトリには触れない）
// - symlink は使わない（Windows で壊れるため。MySite scripts/sync-skills.mjs と同方針）
//
// 生成先を直接編集しないこと。編集は必ず正本（Packages/*/skills/）側で行う。

import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const repoRoot = process.cwd();
const checkMode = process.argv.includes("--check");
const mirrorRoots = [".claude/skills", ".agents/skills"];

// --- 正本のスキルディレクトリを列挙する ---
function findSourceSkillDirs() {
  const packagesDir = path.join(repoRoot, "Packages");
  const result = []; // { skillName, srcDir }
  if (!fs.existsSync(packagesDir)) return result;
  for (const pkg of fs.readdirSync(packagesDir, { withFileTypes: true })) {
    if (!pkg.isDirectory()) continue;
    const skillsDir = path.join(packagesDir, pkg.name, "skills");
    if (!fs.existsSync(skillsDir)) continue;
    for (const entry of fs.readdirSync(skillsDir, { withFileTypes: true })) {
      if (!entry.isDirectory()) continue;
      result.push({ skillName: entry.name, srcDir: path.join(skillsDir, entry.name) });
    }
  }
  return result;
}

// --- ディレクトリ配下の相対ファイルパス一覧 ---
// 正本側は *.meta を除外（ミラーはエージェント用のため）。
// ミラー側は *.meta も列挙し、余分なファイルとして削除対象にする。
function listFiles(dir, { excludeMeta }) {
  const files = [];
  const walk = (current) => {
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const full = path.join(current, entry.name);
      if (entry.isDirectory()) {
        walk(full);
      } else if (!(excludeMeta && entry.name.endsWith(".meta"))) {
        files.push(path.relative(dir, full));
      }
    }
  };
  walk(dir);
  return files.sort();
}

function filesEqual(a, b) {
  if (!fs.existsSync(a) || !fs.existsSync(b)) return false;
  const bufA = fs.readFileSync(a);
  const bufB = fs.readFileSync(b);
  return bufA.equals(bufB);
}

// --- 1 スキルぶんを 1 つのミラー先に同期（または検査）する ---
function syncSkill(skillName, srcDir, mirrorRoot) {
  const destDir = path.join(repoRoot, mirrorRoot, skillName);

  // 重要: ミラー先が symlink の場合（パッケージ同梱のスキルインストーラが張ったリンク等）、
  // リンク越しに書き込み・削除すると正本側を破壊する。リンク自体を外して実体コピーへ置き換える。
  if (fs.existsSync(path.dirname(destDir)) && fs.lstatSync(destDir, { throwIfNoEntry: false })?.isSymbolicLink()) {
    if (checkMode) {
      return [`${path.join(mirrorRoot, skillName)} が symlink（実体コピーへの置き換えが必要）`];
    }
    fs.unlinkSync(destDir); // リンク先には触れずリンクだけを外す
  }
  const srcFiles = listFiles(srcDir, { excludeMeta: true });
  const destFiles = fs.existsSync(destDir) ? listFiles(destDir, { excludeMeta: false }) : [];
  const drifts = [];

  for (const rel of srcFiles) {
    const src = path.join(srcDir, rel);
    const dest = path.join(destDir, rel);
    if (!filesEqual(src, dest)) {
      drifts.push(`${path.join(mirrorRoot, skillName, rel)} が正本と不一致（または欠落）`);
      if (!checkMode) {
        fs.mkdirSync(path.dirname(dest), { recursive: true });
        fs.copyFileSync(src, dest);
      }
    }
  }
  for (const rel of destFiles) {
    if (!srcFiles.includes(rel)) {
      drifts.push(`${path.join(mirrorRoot, skillName, rel)} は正本に存在しない`);
      if (!checkMode) fs.rmSync(path.join(destDir, rel));
    }
  }
  return drifts;
}

const sources = findSourceSkillDirs();
if (sources.length === 0) {
  console.log("Packages/*/skills/ にスキルが見つかりません。何もしません。");
  process.exit(0);
}

let allDrifts = [];
for (const { skillName, srcDir } of sources) {
  for (const mirrorRoot of mirrorRoots) {
    allDrifts = allDrifts.concat(syncSkill(skillName, srcDir, mirrorRoot));
  }
}

if (checkMode) {
  if (allDrifts.length > 0) {
    console.error("スキルミラーの drift を検出しました。`node scripts/sync-agent-skills.mjs` で再生成してください:");
    for (const d of allDrifts) console.error("  - " + d);
    process.exit(1);
  }
  console.log(`OK: ${sources.length} スキル × ${mirrorRoots.length} ミラーが正本と一致しています。`);
} else {
  if (allDrifts.length > 0) {
    console.log(`${allDrifts.length} 件を同期しました:`);
    for (const d of allDrifts) console.log("  - " + d);
  } else {
    console.log("すべて一致済み。変更はありません。");
  }
}
