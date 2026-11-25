# CLAUDE.md

Agent entry point: **how to behave, what to run, what not to touch.** Everything else — state,
decisions, traps, conventions — lives in [`docs/`](docs/), indexed by
[`docs/README.md`](docs/README.md). This file stays at repo root and thin: Claude Code only auto-loads
it from there, so a `docs/CLAUDE.md` is never read (G-009). Design docs are the contract: read, never
rewrite.

## Rules

- **Говори с пользователем по-русски** (D-023). Коротко и просто: 5–10 строк, без широких таблиц, без
  меню вариантов, без жаргона. Что сделано, что это значит, что осталось. Если нужен выбор — один
  короткий вопрос.
- Before taking any action — a short overview of what you are going to do, and ask the user to agree.
- **One task at a time.** A big prompt → break it into tasks and show the plan. Do one task → save →
  hand it to the user to play; **the user's verdict is the task done** (unless told to do all at once).
- **The suite is a regression net, not a per-task gate:** it runs **once**, at the end of a session, under
  90 seconds — never per task. Tests exist only for rules the user has approved (D-026), and **one rule
  gets one test**: a test that restates a rule from a second angle is deleted, not kept (D-050).
- **Tests run on code, not on the scene:** `unity cmd test run --mode editor` — EditMode, no Play Mode,
  no scene load. Play Mode is for feel checks with `capture game_view`, not for a test suite.
- **Write tests only for** Domain, UseCases (the session/orchestration layer) and **invariants**
  ("velocity never exceeds `MaxPushSpeed`", "a push outlives its `Duration`"). **Never for Adapters**:
  a shell has no rules to test, and a test on a shell tests Unity, not our logic.
- **Tests are written after the mechanic is approved by the user** — once it has been played and the user
  says it reads right. Writing tests before approval wastes them: mechanics get edited and bugs get
  fixed, and every test gets rewritten. Approved + stable → then assert.
- Do not touch files unrelated to the current task.
- Any important non-obvious decision is discussed with the user until fully clarified.
- The only source of truth about beauty is the user's eyes and feelings — show them what you made.

## Project

Unity 6 (6000.6.5f1), URP, 3D firefighter mini-game: FPS feet, one item in hand, a burning house whose
fire spreads itself, an extinguisher that kills fire **and** shoves physics, a rotatable hydrant that
launches you when you block its water. Chaos that reads at a glance.

- **Scope is locked** to four pillars — FPS controller + carry slot, object-based fire, extinguisher,
  hydrant catapult (D-008). Nothing outside them enters without a new DECISIONS entry. The design
  contract (`TDD.md`, `Preproduction.md`) is **not in the repo**: cite pillars through D-008, never as
  "TDD §n".
- **WebGL is the shipping target** (D-007): each system gets a written perf budget *before* code, fire +
  particles + physics share one budget, no haptics at all (G-011). "Works in the editor" is not done.
  Today's honest status line is *editor-verified, web-unverified* (D-019); web builds resume after
  B-001 is fixed.
- **State of the code is not in this file.** Read `docs/DECISIONS.md` §Current structure, and verify
  against the files: the docs are a map, not a guarantee.

## Commands

Unity is driven through **the Unity CLI against the running editor** — never batch mode first.

```bash
unity status                    # which editors are connected: port, pid, project, state (must be ready)
unity cmd script recompile      # the compile gate → status: up_to_date  ← run after EVERY edit
unity cmd editor status         # compiling: false, domainReloadInProgress: false
unity cmd console get           # compilationFailed: false, no `error CS` — read the console, no window
unity cmd test run --mode editor          # the test gate: runs the suite synchronously, on code
unity cmd test list --mode editor         # what is in FlameNBlame.Tests
unity cmd test run --mode playmode --async-tests   # only for a check that needs the running scene;
unity cmd test status --detail summary    #   returns at once, poll this until status: completed
unity cmd scene get_hierarchy   # scene structure; gameobject find / create / set_transform; component add
unity cmd script eval           # run C# inside the editor
unity cmd capture scene_view --output-path <png>   # verify a scene change with the user's eyes
unity test / unity build / unity run / unity recompile / unity watch
```

- `unity cmd` names are `group verb`; destructive commands need `confirm=true`; `unity cmd --query
  <term>` searches the list, `<group> <verb> --help` shows the accepted parameters.
- **Vectors are JSON arrays**: `--position "[x,y,z]"`, `--scale "[x,y,z]"`. Scene objects are addressed
  by `--hierarchy-path "/Root/Child"`; `gameobject find` has no `--path` (G-014).
- `autoRefresh` is **disabled** in the editor, so the recompile gate is your habit, not the editor's.
- Batch-mode `Unity.exe` (`X:/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe`) is the **fallback**
  when no editor is connected — it pays a cold start every call (G-005), and `-quit` is incompatible
  with `-runTests`.
- Git: branch `main`, remote `origin` → `git@github.com:DiviZition/Flame-n-Blame-challenge.git`.

**No WebGL build command exists yet** (no WebGL module/build profile, B-001): until then a "web check"
means switching the build target in the editor and building manually.

## Skills & MCP

- **No Unity skills are installed** (2026-11-20, D-025). The CLI documents itself: `unity cmd` lists the
  ~196 commands with descriptions, `unity cmd --query <term>` searches them, and
  `unity cmd <group> <verb> --help` prints the accepted parameters. Do not install the Unity skill set:
  `unity skill install/sync/refresh` re-create all 36 skill folders (G-015). If a niche manual is truly
  needed for one task: `unity skill install opencode --local --skill <name>`, and delete its folder when
  the task is done. `unity skills` tracking is stale after manual deletes — trust the folders on disk.
- **Unity's MCP server is deprecated and is not wired into any client** (D-022, G-013). Nothing may add
  it to `mcp.servers` without a new decision.
- **Feel skills are not advertised** (G-004): read `Assets/Feel/Skills/<id>/SKILL.md` directly before any
  Feel work — guessing Feel's component names is the failure mode. The 8 Feel skill folders stay in the
  vendor folder, untouched.

## Architecture & conventions

- **Read [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) before adding a system.** Its rules section is
  READ-ONLY; only the §Project-Specific Structure may be edited. Update it in the same change that moves
  a layer.
- **Hard rules that are not negotiable:** game rules are plain C# classes, never MonoBehaviour bodies;
  MonoBehaviours are the composition host and thin shells that forward data inward; exactly one
  `Update()` exists — on `Bootstrapper`; the registration order in `GamePlaySession.StartGamePlay()`
  **is** the execution-order contract (Script Execution Order is not a legal move); dependencies point
  inward only, Domain never touches Unity, the updater, or physics queries; authored numbers come from
  shells/ScriptableObjects into plain data objects passed to constructors.
- **Namespaces by layer, not by feature:** root `Flame_n_Blame_Challenge` + `.Domain` / `.UseCases` /
  `.Adapters` (D-014). `FlameNBlame` is the assembly name, never a namespace.
- **Perf budget before code, every system** (D-007). C#, asset, git and perf conventions:
  [`docs/STYLE.md`](docs/STYLE.md).

## Vendor assets (never modify, never put our code inside)

`Assets/Feel`, `Assets/Plugins/*` (PrimeTween, Sirenix/Odin), `Assets/Tiny Giant Studio`,
`Assets/Pivot++`, `Assets/vHierarchy`, `Assets/vFavorites`, `Assets/Wingman`, `Assets/Voxel Labs`,
`Assets/Runtime Transform Gizmos (Standard)`, `Assets/Cartoon FX Pack`, `Assets/LowPolyInterior`,
`Assets/TextMesh Pro`, `Assets/Kevin Iglesias`, `Assets/HotReload`. **Ours:** `Assets/_Core`
(`Art/`, `Scenes/`, `Scripts/`, `Editor/`), `Assets/Settings`. If a vendor file must change, copy it
into `_Core` and edit the copy (G-006). Nice Vibrations stays, **dormant**: never call it, never add its
modules to an `MMF_Player` (D-010). Grabbit was removed — don't assume it exists.

## Docs (part of doing the work, not an afterthought)

Routing, budgets and the writing rules are defined **once**, in
[`docs/README.md`](docs/README.md). `DocBudgetTests` is the gate: a file over its budget is compacted
**before** code. Measured numbers, gate outputs, test counts, CLI transcripts and probe values go to the
commit body and to `TODO.md` status lines — never into a doc.

Workflow: skim `GOTCHAS.md` + `TODO.md` before starting, `ARCHITECTURE.md` before building anything,
`DECISIONS.md` §Current structure when a choice looks strange; after finishing, update the file that
changed reality. There is no `BUGS.md`: bugs are `- [ ] B-xxx` lines in `TODO.md`, done when their test
passes.
