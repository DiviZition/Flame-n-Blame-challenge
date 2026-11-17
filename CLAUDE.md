# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.
It stays at repo root (and thin) on purpose — everything else lives in [`docs/`](docs/), indexed by
[`docs/README.md`](docs/README.md). Claude Code only auto-loads this file from the root (G-009), so
pointers belong here and detail belongs in `docs/`. Design docs are the contract: read, never rewrite.

## Project

Unity 6 (6000.6.5f1) URP 3D game — "Flame & Blame challenge". Firefighter mini-game: FPS feet, one
item in the hands, a burning house whose fire spreads and grows itself, an extinguisher that both
kills fire and shoves physics around, and a rotatable hydrant that launches you when you block its
water. Chaos that reads at a glance.

- **Scope is locked** to four pillars — FPS controller + carry slot, object-based fire, extinguisher,
  hydrant catapult — per [`docs/TDD.md`](docs/TDD.md), the binding source of truth
  ([`docs/Preproduction.md`](docs/Preproduction.md) is the reasoning behind it). Don't invent systems
  or pull in deferred ideas (co-op, scoring, destruction, ragdoll/IK, the dam) without a new
  DECISIONS entry. See D-008.
  ⚠️ **Neither design doc is in the repo.** They were never copied in and the originals are gone, so
  the contract exists only as the summaries in DECISIONS D-008 and `README.md`. Cite the pillars
  through D-008, never as "TDD §3"; nothing may quote a TDD section number as if the file existed.
  Recovering them is the top item in `docs/TODO.md`.
- **WebGL is the shipping target from day one**, not a late check (D-007). Judge every system
  against a web build; "works in the editor" is not done. Each system gets a written perf budget
  *before* code, sharing one budget for fire, particles and physics; no haptics at all (G-011).
- **Known blocker:** WebGL currently has no render pipeline assigned (B-001) — fix before treating
  any web build as a measurement.

## Current state of the code (as of 2026-11-17 — verify against the files, this list is a map, not a guarantee)

- **The working tree does not compile.** `Assets/_Core/Scripts/Bootstrapper.cs:113` is a dangling
  `_resources` statement (no `;`) followed by a comment, inside `StartGamePlay()`. Fix that first,
  then run the headless compile check.
- **Nothing runs yet.** `Bootstrapper`'s script GUID appears nowhere in
  `Assets/_Core/Scenes/FnB_Gameplay.unity` → the composition root is not in the scene, so no tick is
  driven. `PlayerMotor` *is* attached on the `Player` object. The scene is placeholder cubes/cylinders
  + Cinemachine camera/brain + a `SpineProxy` (Kevin Iglesias) — no authored level.
- **What exists today:** `Bootstrapper.cs` (composition host, `GameSettings` + `ResourcesProvider`
  ScriptableObjects and their interfaces, `ResourceSpawner`, and `GamePlaySession` under
  `namespace Flame_n_Blame_Challenge.UseCases` — **7 types in one file, across 2 namespaces**, which
  breaks STYLE.md's one-type-per-file: they must move to `Scripts/UseCases/` etc. per D-012),
  `GameUpdater.cs`, `InterfaceAdapters/{IUpdatable, IPlayerInput, PlayerInputMain, PlayerMotor}`,
  `Domain/Player.cs` (`Player`, `PlayerLookRotator`, `PlayerMovement`). Move/look logic is real and
  non-trivial; `StartGamePlay()` is unfinished (player prefab spawn + `PlayerMotor.Construct` are
  TODO), so `PlayerMotor` currently has no `Player` to read.
- **`Player.cs` at repo root is a stray copy** of `Assets/_Core/Scripts/Domain/Player.cs` (untracked,
  no `using` directives, outside `_Core` — it does not compile on its own). Read the `_Core` file;
  never edit the root one (D-003, G-006).
- `Scripts/Application/` exists and is empty while ARCHITECTURE's table names `Scripts/UseCases/` —
  pick one in the same change that creates the folder, don't leave both.
- **`IFixedUpdatable` + the fixed-step host is decided and not built** (D-013); the first physics
  pillar (extinguisher spray forces) implements it, not re-decides it.
- ARCHITECTURE's §"Current reality vs decided" table is the authoritative snapshot. When the shape
  moves, update ARCHITECTURE.md in the same change (D-015).

## Commands

Editor binary (non-standard path — see G-005):
```
X:/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe
```

Open in editor:
```bash
"/x/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe" -projectPath "X:/Workspace/Projects/OwnProjects/Flame-n-Blame-challenge"
```

Headless compile check (the standard "is it done yet" gate):
```bash
"/x/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe" -batchmode -quit -nographics \
  -projectPath "X:/Workspace/Projects/OwnProjects/Flame-n-Blame-challenge" -logFile -
```
(exit code 0 + no `error CS` lines in output = compiles). `-logFile -` prints the log to stdout. A
RTK hook rewrites shell commands and filters output, so for Unity runs use `rtk proxy <cmd>` if the
filtered result looks short — compile errors are exactly what it drops.

Run tests (framework is present only as a transitive dep — G-008; no tests exist yet):
```bash
"/x/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "X:/Workspace/Projects/OwnProjects/Flame-n-Blame-challenge" \
  -runTests -testPlatform PlayMode -testFilter "Flame_n_Blame_Challenge.Tests.SomeTests.SomeMethod" -testResults Results.xml
```
Note: `-quit` is incompatible with `-runTests`; tests need a test asmdef (see G-008).

No WebGL build command yet — the WebGL module/build profile isn't set up (TODO). Until it is, a
"web check" means switching build target in the editor and building manually, and B-001 applies.

Git: branch `main`, remote `origin` → `git@github.com:DiviZition/Flame-n-Blame-challenge.git`.

## Architecture & conventions

- **Shape of the code: read [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) before adding a system.**
  Its rules section is READ-ONLY; only the §Project-Specific Structure may be edited.
  Game logic is plain C# classes in `.Domain` / `.UseCases` / `.Adapters` (namespaces by **layer**,
  root `Flame_n_Blame_Challenge` — D-014), wired once by a `Bootstrapper` composition root.
  Dependencies point inward only; Domain never touches Unity, the updater, or physics queries.
  MonoBehaviours are only the host and thin shells Unity must call (D-012) — no rules inside them,
  and "just put it on the prefab" is not a legal move.
- **One tick we own** (D-013): the single `Update()` is on `Bootstrapper` →
  `GameUpdater.Update(Time.time, Time.deltaTime)` → every registered `IUpdatable` in **registration
  order**, which *is* the ordering contract (never Script Execution Order). Removal is staged
  (queue + swap-remove), so an updatable can remove itself mid-loop. Physics impulses/queries go on a
  parallel `IFixedUpdatable` (not built).
- **Intent, not simulation:** Domain exposes desired velocity/state; the shell integrates it with
  Unity (today: `PlayerMotor` reads `Movement.DesiredVelocity` and calls `CharacterController.Move`,
  and feeds `SetIsGrounded` back into Domain).
- **Authored numbers** live on shells/ScriptableObjects (`GameSettings`, `ResourcesProvider`) and get
  copied into plain data objects passed to constructors; Domain never reads the Inspector.
- **Game code root: `Assets/_Core`** (`Art/`, `Scenes/`, `Scripts/`). All gameplay code under
  `_Core/Scripts`; imported models/animations under `_Core/Art`; editor-only code in `_Core/Editor`
  with its own asmdef (see G-001).
- **Assembly:** `Assets/_Core/FlameNBlame.asmdef`. Its `name` field is the typo `FlameNBlame-asmdf`
  (in TODO); `rootNamespace` is `Flame_n_Blame_Challenge` (D-014) and `autoReferenced: true`.
  References are GUID-based (renaming is safe) and currently: Unity.InputSystem, PrimeTween.Runtime,
  Unity.Cinemachine, UniTask, **CartoonFX** — the last is a vendor demo assembly, so an asset-store
  update can break our compile; dropping it (with the reference list checked against the code, not the
  docs) is the job. The editor-only reference G-001 describes is **already gone**; G-001's status is
  stale. Add each new vendor dep explicitly to the reference list.
- **Input:** Input System only (`activeInputHandler: 1`). Actions in
  `Assets/_Core/InputSystem_Actions.inputactions` — Player map: Move, Look, Use, Throw, Interact,
  Crouch, Jump, Sprint, Previous, Next; UI map. There is **no** `Attack` action; the grab/throw path
  is Use + Throw. Never use legacy `Input.*`.
  `Assets/_Core/InputSystem_Actions.cs` is the editor-generated wrapper — regenerate it from the
  `.inputactions` asset after changing actions, never hand-edit; `PlayerInputMain` subscribes to it
  and is `IDisposable` (unsubscribe + `Disable()` on teardown).
- **Rendering:** URP 17.6.0, single pipeline named for the target: `Assets/Settings/Web_RPAsset.asset`
  + `Web_Renderer.asset` (plus `DefaultVolumeProfile`, `SampleSceneProfile`,
  `UniversalRenderPipelineGlobalSettings`). `PC_RPAsset`/`Mobile_RPAsset` no longer exist; the web
  asset still carries PC-grade settings and is not yet bound to WebGL — see D-009, G-010, B-001.
  Post-processing via com.unity.postprocessing.
- **Tweening/async/juice:** PrimeTween (tweens), UniTask (async), Feel 6.3 `MMF_Player` for feedback.
  Nice Vibrations stays in the project but **dormant** — never call it, never add its modules to an
  `MMF_Player` (D-010, G-011). See D-004/D-005.
- **C# style, asset naming, perf rules, git conventions:** docs/STYLE.md.

## Project documentation (keep it current)

Lives in [`docs/`](docs/) since 2026-11-16 (D-011) and is part of doing the work, not an afterthought.
`docs/ARCHITECTURE.md` is untracked, and the root-level copies of DECISIONS/GOTCHAS/BUGS/TODO/STYLE
are deleted-but-unstaged — the docs move is not committed yet (TODO).

- **docs/TDD.md / docs/Preproduction.md** — the contract (both missing, see the warning above).
- **docs/ARCHITECTURE.md** — the code map: layers, dependency direction, who owns the tick, the Unity
  boundary, how to add a system. Read before adding a system; update in the same change (D-015).
  Precedence: DECISIONS > ARCHITECTURE; ARCHITECTURE states intent, its reality table says what exists.
- **docs/DECISIONS.md** — ADR log. Add an entry whenever a non-trivial, hard-to-reverse choice is
  made; never silently edit old entries, supersede them.
- **docs/GOTCHAS.md** — struggles/mistakes/hard restrictions, with a one-line **Rule**. Append the
  moment we get burned.
- **docs/BUGS.md** — known bugs, `Open → Repro → In progress → Fixed → Verified`.
- **docs/TODO.md** — task checklist incl. the TDD phase checklists. Mark done items with date.
- **docs/STYLE.md** — coding/asset/git/perf conventions. Follow it; propose changes, don't drift.

Workflow: before starting work, skim `docs/GOTCHAS.md` and `docs/TODO.md` (and `docs/TDD.md` for the
feature at hand; `docs/ARCHITECTURE.md` before building anything). After finishing, update the files
that changed reality — decision → DECISIONS, shape → ARCHITECTURE, burned → GOTCHAS, bug → BUGS,
tasks → TODO. Shape → ARCHITECTURE, call → DECISIONS, convention → STYLE.

## Vendor assets (never modify or add code inside)

`Assets/Feel`, `Assets/Plugins/*` (PrimeTween, Sirenix/Odin), `Assets/Tiny Giant Studio`,
`Assets/Pivot++`, `Assets/vHierarchy`, `Assets/vFavorites`, `Assets/Wingman`, `Assets/Voxel Labs`,
`Assets/Runtime Transform Gizmos (Standard)`, `Assets/Cartoon FX Pack`, `Assets/LowPolyInterior`,
`Assets/TextMesh Pro`, `Assets/Kevin Iglesias` (Human Animations — in use via a SpineProxy in the
gameplay scene), `Assets/HotReload`. Ours: `Assets/_Core`, `Assets/Settings`.
Our code lives only in `Assets/_Core` (see G-006); if a vendor file must change, copy it into `_Core`
and modify the copy. Grabbit was removed (outdated) — don't assume it's available.

## Tool availability

- **Unity CLI:** ✅ available at the path above (verified).
- **GitHub CLI (`gh`):** verify with `gh auth status`; not yet confirmed in this environment.
- **MCP servers:** `headroom` (context compression: `headroom_compress` / `headroom_retrieve` /
  `headroom_stats`). No project `.mcp.json`; the Unity AI Assistant package (2.21.0-pre.2, with
  its MCP editor assembly) is installed in-editor but not wired to Claude Code.
- **Skills:** built-in Claude Code skills only. `.claude/` holds only `settings.local.json` — the
  `.claude/skills` junction to `Assets/Feel/Skills` (G-004) has **not** been created, so Feel's AI
  skills are NOT loaded. Create the junction before Feel work; meanwhile read the matching `SKILL.md`
  directly from `Assets/Feel/Skills/`.

## Environment notes

- Windows 11; PowerShell is the primary shell, Git Bash also available. Paths with spaces need
  quoting (`Tiny Giant Studio`, `Cartoon FX Pack`).
- Generated dirs (`Library/`, `Temp/`, `Logs/`) exist on disk but are gitignored — never commit
  (G-007). Two `.slnx` files are tracked by accident.
- EditorBuildSettings still lists the deleted `Assets/Scenes/SampleScene.unity` (G-003). Add
  `Assets/_Core/Scenes/FnB_Gameplay.unity` when it is the real scene — on WebGL an empty scene list
  ships a page that loads and shows nothing.
- Quality levels are still the template `Mobile`/`PC` pair with `PC` active in-editor; that is the
  root of B-001 and will change as the web target is re-cut (D-009).
- `companyName` is `DefaultCompany`; it becomes the web build folder/product path, so set it before
  the first web build (TODO).
