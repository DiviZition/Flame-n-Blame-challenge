# AGENTS.md

**`CLAUDE.md` at the repo root is the source of truth for agents — read it first and treat it as this
file.** It is thin by design (behaviour rules, commands, vendor boundaries, pointers into `docs/`);
the current-state map lives in `docs/DECISIONS.md` §Current structure and is a map, not a guarantee:
verify against the files before acting on it.

## Start of every session

1. `CLAUDE.md` — commands, assembly layout, vendor boundaries, and the workflow rules (one task at a
   time, propose before acting, a task is done only when its test passes).
2. `docs/README.md` — reading order for the doc set.
3. `docs/GOTCHAS.md` + `docs/TODO.md` — what will bite you, and what is next.
4. `docs/ARCHITECTURE.md` before adding a system; `docs/DECISIONS.md` when a choice looks strange.
5. `docs/STYLE.md` for C#, asset, perf and git conventions.

## The facts most often guessed wrong

- **Drive Unity with the CLI, not batch mode.** `unity` is
  `C:\Users\DiviZition Limited\AppData\Local\Unity\bin\unity.exe`. Check `unity status` first — a warm
  editor is usually connected (port 7800). Compile gate: `unity cmd script recompile`, run after every
  edit (`autoRefresh` is disabled in the editor). Tests: `unity cmd test run --mode editor` — the suite
  is **EditMode** (the test asmdef is editor-only) and answers synchronously, on code, no scene load.
  `unity cmd console get` reads the console without opening it. The batch-mode `Unity.exe` invocation in
  CLAUDE.md is the fallback: it pays a cold start on every call.
- **Our code lives only in `Assets/_Core`.** `Assets/Feel`, `Assets/Plugins/*`, `Assets/Cartoon FX Pack`,
  `Assets/Kevin Iglesias`, `Assets/Tiny Giant Studio`, `Assets/Pivot++`, `Assets/TextMesh Pro` and every
  other vendor folder is overwritten by asset-store updates. If a vendor file must change, copy it into
  `_Core` and edit the copy.
- **Game rules are plain C# classes, never MonoBehaviour bodies.** Domain touches no Unity, no updater,
  no physics queries; MonoBehaviours are the composition host and thin shells that forward data inward.
  Exactly one `Update()` exists — on `Bootstrapper` — and the registration order in
  `GamePlaySession.StartGamePlay()` **is** the execution-order contract. Script Execution Order is not
  a legal move.
- **Namespaces are by layer, not by feature:** root `Flame_n_Blame_Challenge` + `.Domain` / `.UseCases`
  / `.Adapters`, matching the asmdef `rootNamespace`. `FlameNBlame` is the assembly name, never a namespace.
- **Perf budget before code, every system** — WebGL is the shipping target on one shared budget for
  fire, particles and physics; editor profiler numbers are a hint, not evidence.

## Do not

- Add Unity's MCP server to any client config — Unity deprecated it, `unity cmd` covers all of it (D-022, G-013).
- Create the `.claude/skills` junction to `Assets/Feel/Skills`; read `Assets/Feel/Skills/<id>/SKILL.md`
  directly before Feel work (G-004).
- Add a `NiceVibrations*` module to an `MMF_Player`, or gate any feel on haptics — WebGL has none (G-011, D-010).
- Hand-edit `Assets/_Core/InputSystem_Actions.cs`; regenerate it from the `.inputactions` asset.
- Use legacy `Input.*`, `StartCoroutine`, DOTween, or `SendMessage`/`GameObject.Find` in hot paths.
- Commit `Library/`, `Temp/`, `Logs/`, root `*.csproj`/`*.slnx`, or WebGL build output (G-007).
- Edit the READ-ONLY section of `docs/ARCHITECTURE.md`, the design docs, or an existing DECISIONS/GOTCHAS
  entry — supersede with a new entry, never rewrite history.
- Touch any file outside the current task's scope.
