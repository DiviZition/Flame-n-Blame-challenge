# BUGS.md — Known Bugs

Tracks known bugs and their current status. One row per bug; update the status as work happens.

**Rules:**
- Add a row as soon as a bug is *confirmed* (with repro), not when merely suspected.
- Statuses: `Open` → `Repro` → `In progress` → `Fixed` (link the commit/PR) → `Verified`
  (tested in editor/playmode). If a "fix" didn't fix it, set back to `Open` and say why in Notes.
- Move truly dead entries (won't-fix, by-design) to the bottom under `Closed`.
- Bugs that are actually misunderstandings belong in GOTCHAS.md, not here.

**Row format:**

| ID | Status | Title | Repro / where | Notes |
|----|--------|-------|---------------|-------|
| B-001 | Open | … | … | … |

---

## Active

| ID | Status | Title | Repro / where | Notes |
|----|--------|-------|---------------|-------|
| B-001 | Open | WebGL build has no render pipeline assigned | Switch build target to WebGL and build/run | Confirmed 2026-11-16 from config, before any gameplay code. `m_PerPlatformDefaultQuality: WebGL: 0` → quality level 0 "Mobile", whose `customRenderPipeline` GUID `5e6cbd92db86f4b18aec3ed561671858` matches **no asset in `Assets/`** (`Mobile_RPAsset` was deleted). `GraphicsSettings.asset:40` `m_CustomRenderPipeline: {fileID: 0}` → no fallback. `Web_RPAsset` (GUID `4b83569d…`) is bound only to the "PC" level (`QualitySettings.asset:105`), and that level **excludes WebGL** (`:119`). Expect magenta/broken rendering in a web build while the editor looks correct (`m_CurrentQuality: 1`). Fix = give WebGL a level that points at `Web_RPAsset`; see G-010, D-009. |

## Closed

| ID | Status | Title | Repro / where | Notes |
|----|--------|-------|---------------|-------|
| —  | —      | — | — | — |
