# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6 (6000.6.5f1) URP 3D game project — "Flame & Blame challenge". Greenfield: no gameplay
code yet; `Assets/_Core` is the home for it. A GDD and TDD will be provided by the user and
become the source of truth for gameplay scope — until then, don't invent game systems.

## Project documentation files (keep them current)

These live at repo root and are part of doing the work, not an afterthought:

- **DECISIONS.md** — architectural decision log (ADR-style). Add an entry whenever a
  non-trivial, hard-to-reverse choice is made; never silently edit old entries, supersede them.
- **GOTCHAS.md** — struggles, mistakes, hard restrictions. Append an entry the moment we get
  burned or discover a restriction, with a one-line **Rule**.
- **BUGS.md** — known bugs with status (`Open → In progress → Fixed → Verified`). Add on
  confirmation, update as status changes.
- **TODO.md** — task checklist. Add anything we must not forget; mark done items with date.
- **STYLE.md** — coding/asset/git conventions. Follow it; propose changes rather than drifting.

Workflow: before starting work, skim GOTCHAS.md and TODO.md. After finishing, update the files
that changed reality (decision made → DECISIONS, burned → GOTCHAS, bug fixed → BUGS, tasks → TODO).

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

Git: branch `main`, remote `origin` → `git@github.com:DiviZition/Flame-n-Blame-challenge.git`.

## Architecture & conventions

- **Game code root: `Assets/_Core`** (`Art/`, `Scenes/`, `Scripts/`). All gameplay code under
  `_Core/Scripts`; editor-only code in `_Core/Editor` with its own asmdef (see G-001).
- **Assembly:** `FlameNBlame.asmdef` references Unity.InputSystem, PrimeTween.Runtime,
  Unity.Cinemachine, UniTask. Add new vendor deps explicitly to the reference list.
- **Input:** Input System only (`activeInputHandler: 1`). Actions in
  `Assets/_Core/InputSystem_Actions.inputactions` (Player map: Move/Look/Attack/Interact/
  Crouch/Jump/Sprint; UI map). Never use legacy `Input.*`.
- **Rendering:** URP 17.6.0; pipeline assets in `Assets/Settings/` (`PC_RPAsset`, `Mobile_RPAsset`,
  `SampleSceneProfile`). Post-processing via com.unity.postprocessing.
- **Tweening/async/juice:** PrimeTween (tweens), UniTask (async), Feel 6.3 MMF_Player (feedback
  effects) + Nice Vibrations (haptics) — see DECISIONS.md D-004/D-005.
- **C# style, asset naming, git conventions:** STYLE.md.

## Vendor assets (never modify or add code inside)

`Assets/Feel`, `Assets/Plugins/*` (PrimeTween demo, Grabbit, Odin, TinyGiantStudio), `Assets/Pivot++`,
`Assets/vHierarchy`, `Assets/vFavorites`, `Assets/Wingman`, `Assets/Voxel Labs`,
`Assets/Runtime Transform Gizmos (Standard)`, `Assets/Cartoon FX Pack`, `Assets/LowPolyInterior`,
`Assets/TextMesh Pro`. Our code lives only in `Assets/_Core` (see G-006).

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
