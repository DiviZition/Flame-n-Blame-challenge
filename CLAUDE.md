# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.
It stays at repo root (and thin) on purpose — everything else lives in [`docs/`](docs/), indexed by
[`docs/README.md`](docs/README.md). See G-009: Claude Code only auto-loads this file from the root.

## Project

Unity 6 (6000.6.5f1) URP 3D game — "Flame & Blame challenge". Firefighter mini-game: FPS feet, one
item in the hands, a burning house whose fire spreads and grows itself, an extinguisher that both
kills fire and shoves physics around, and a rotatable hydrant that launches you when you block its
water. Chaos that reads at a glance.

- **Scope is locked** to four pillars — FPS controller + carry slot, object-based fire, extinguisher,
  hydrant catapult — per [`docs/TDD.md`](docs/TDD.md), which is the binding source of truth
  ([`docs/Preproduction.md`](docs/Preproduction.md) is the reasoning behind it). Don't invent systems
  or pull in deferred ideas (co-op, scoring, destruction, ragdoll/IK, the dam) without a new
  DECISIONS entry. See D-008.
- **WebGL is the shipping target from day one**, not a late check (D-007). Judge every system
  against a web build; "works in the editor" is not done. Single shared budget for fire, particles
  and physics; no haptics at all (G-011).
- **Known blocker:** WebGL currently has no render pipeline assigned (B-001) — fix before treating
  any web build as a measurement.
- No gameplay code yet beyond scaffolding; `Assets/_Core` is the only home for it.

## Project documentation (keep it current)

Lives in [`docs/`](docs/) since 2026-11-16 (D-011) and is part of doing the work, not an afterthought:

- **docs/TDD.md** — the contract: scope, check-lists per feature, deferred list. Read before building.
- **docs/Preproduction.md** — brief, judging criteria, trailer observations, scored brainstorm.
- **docs/DECISIONS.md** — architectural decision log (ADR-style). Add an entry whenever a
  non-trivial, hard-to-reverse choice is made; never silently edit old entries, supersede them.
- **docs/GOTCHAS.md** — struggles, mistakes, hard restrictions. Append an entry the moment we get
  burned or discover a restriction, with a one-line **Rule**.
- **docs/BUGS.md** — known bugs with status (`Open → Repro → In progress → Fixed → Verified`).
- **docs/TODO.md** — task checklist incl. the TDD phase checklists. Mark done items with date.
- **docs/STYLE.md** — coding/asset/git/perf conventions. Follow it; propose changes, don't drift.

Workflow: before starting work, skim `docs/GOTCHAS.md` and `docs/TODO.md` (and `docs/TDD.md` for the
feature at hand). After finishing, update the files that changed reality — decision → DECISIONS,
burned → GOTCHAS, bug → BUGS, tasks → TODO. Design docs are the contract: never edit them.

## Commands

Editor binary (non-standard path — see G-005):
```
X:/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe
```

Open in editor:
```bash
"/x/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe" -projectPath "X:/Workspace/Projects/OwnProjects/Flame-n-Blame-challenge"
```

Headless compile check:
```bash
"/x/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe" -batchmode -quit -nographics \
  -projectPath "X:/Workspace/Projects/OwnProjects/Flame-n-Blame-challenge" -logFile -
```
(exit code 0 + no `error CS` lines in output = compiles)

Run tests (test framework present; make it a direct dependency when tests exist — G-008):
```bash
"/x/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "X:/Workspace/Projects/OwnProjects/Flame-n-Blame-challenge" \
  -runTests -testPlatform PlayMode -testFilter "FlameNBlame.SomeTests.SomeMethod" -testResults Results.xml
```
Note: `-quit` is incompatible with `-runTests`; tests need a test asmdef (see G-008).

No WebGL build command yet — the WebGL module/build profile isn't set up (TODO). Until it is, a
"web check" means switching build target in the editor and building manually, and B-001 applies.

Git: branch `main`, remote `origin` → `git@github.com:DiviZition/Flame-n-Blame-challenge.git`.

## Architecture & conventions

- **Game code root: `Assets/_Core`** (`Art/`, `Scenes/`, `Scripts/`). All gameplay code under
  `_Core/Scripts`; imported models/animations under `_Core/Art`; editor-only code in
  `_Core/Editor` with its own asmdef (see G-001).
- **Assembly:** `FlameNBlame.asmdef` references Unity.InputSystem, PrimeTween.Runtime,
  Unity.Cinemachine, UniTask. Add new vendor deps explicitly to the reference list.
- **Input:** Input System only (`activeInputHandler: 1`). Actions in
  `Assets/_Core/InputSystem_Actions.inputactions` (Player map: Move/Look/Attack/Interact/
  Crouch/Jump/Sprint; UI map). Never use legacy `Input.*`.
- **Rendering:** URP 17.6.0, single pipeline named for the target: `Assets/Settings/Web_RPAsset.asset`
  + `Web_Renderer.asset` (plus `DefaultVolumeProfile`, `SampleSceneProfile`,
  `UniversalRenderPipelineGlobalSettings`). `PC_RPAsset`/`Mobile_RPAsset` no longer exist; the web
  asset still carries PC-grade settings and is not yet bound to WebGL — see D-009, G-010, B-001.
  Post-processing via com.unity.postprocessing.
- **Tweening/async/juice:** PrimeTween (tweens), UniTask (async), Feel 6.3 `MMF_Player` for feedback.
  Nice Vibrations stays in the project but **dormant** — never call it, never add its modules to an
  `MMF_Player` (D-010, G-011). See D-004/D-005.
- **C# style, asset naming, perf rules, git conventions:** docs/STYLE.md.

## Vendor assets (never modify or add code inside)

`Assets/Feel`, `Assets/Plugins/*` (PrimeTween, Sirenix/Odin, TinyGiantStudio), `Assets/Pivot++`,
`Assets/vHierarchy`, `Assets/vFavorites`, `Assets/Wingman`, `Assets/Voxel Labs`,
`Assets/Runtime Transform Gizmos (Standard)`, `Assets/Cartoon FX Pack`, `Assets/LowPolyInterior`,
`Assets/TextMesh Pro`. Our code lives only in `Assets/_Core` (see G-006). Grabbit was removed
(outdated) — don't assume it's available.

## Tool availability

- **Unity CLI:** ✅ available at the path above (verified).
- **GitHub CLI (`gh`):** verify with `gh auth status`; not yet confirmed in this environment.
- **MCP servers:** `headroom` (context compression: `headroom_compress` / `headroom_retrieve` /
  `headroom_stats`). No project `.mcp.json`; the Unity AI Assistant package (2.21.0-pre.2, with
  its MCP editor assembly) is installed in-editor but not wired to Claude Code.
- **Skills:** built-in Claude Code skills only. Feel's AI skills at `Assets/Feel/Skills/` are NOT
  loaded — expose them via `.claude/skills` junction (G-004) before Feel work; meanwhile read the
  matching `SKILL.md` directly from that folder.

## Environment notes

- Windows 11, Git Bash. Paths with spaces need quoting (`Tiny Giant Studio`, `Cartoon FX Pack`).
- Generated dirs (`Library/`, `Temp/`, `Logs/`) exist on disk but are gitignored — never commit.
- EditorBuildSettings currently references a deleted scene (G-003); fix when the first scene lands.
- Quality levels are still the template `Mobile`/`PC` pair with `PC` active in-editor; that is the
  root of B-001 and will change as the web target is re-cut (D-009).
