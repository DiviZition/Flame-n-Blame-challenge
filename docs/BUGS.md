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
| B-002 | Fixed (compile gate **not run yet**) | The working tree does not compile: duplicate `ForcePreset` definitions + `using NUnit.Framework` in the runtime asmdef | `Domain/Player.cs` had `class ForcePreset` + `PushForceType` + `PushForceInteraptionType`; `InterfaceAdapters/PlayerMotor.cs` had `struct ForcePreset` + `Vector3Curve`, and `using Flame_n_Blame_Challenge.Domain;` → ambiguous reference (expect `error CS0104`). `using NUnit.Framework;` at `PlayerMotor.cs:2` sits in the runtime assembly. `Bootstrapper.cs:113` is a dangling `_resources` statement with no `;`. | Confirmed 2026-11-19 by reading the tree, not by compiling. Fixed by D-016: the domain DTOs (`PushPresetData`, `PushType`, `PushKind`, `PushApplication`, `Vector3CurveData`) replace both drafts, `PlayerMotor` is gutted to a shell (no `ForceInstance`, no `TestApplyForce`, no NUnit using), and `Bootstrapper` was rewritten. **Not `Verified`** — the headless compile gate has not been run (Bash unavailable this session); run `-batchmode -quit -nographics -logFile -` and check for `error CS` before calling this fixed. **Still not run 2026-11-19 later in the same session:** the safety classifier refused every destructive and Unity command while read-only calls worked, so the refusal is classification, not a missing tool. The same session then added `GroundDrag` to `IGameSettings`/`GameSettings`/`StubSettings`/`Game Settings.asset` and moved the jump gate into `PlayerMotion` — more surface for that one gate to check, so the gate's first job is both B-002's original errors **and** the new field being present in the interface, the class, the stub and the asset YAML in the same shape. |

## Closed

| ID | Status | Title | Repro / where | Notes |
|----|--------|-------|---------------|-------|
| —  | —      | — | — | — |
