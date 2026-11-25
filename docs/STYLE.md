# STYLE.md — Style Guidelines

Consistent conventions for code and assets in this project. Follow them for all new work;
when touching old code, match the surrounding code. Deviations need a reason (put real ones
in DECISIONS.md).

## C# (game code in `Assets/_Core/Scripts`)

- **Namespaces:** `Flame_n_Blame_Challenge` root, then **by layer**: `.Domain`, `.UseCases`,
  `.Adapters` (see D-014 and the layer table in [ARCHITECTURE.md](ARCHITECTURE.md) §2). Matches the
  asmdef `rootNamespace`, so the editor auto-inserts it correctly. Not by feature — a feature
  namespace would let `…Fire` reference `…Player` with nothing to see. `FlameNBlame` is the
  assembly/file name, never a namespace. Folder names may state the pattern rather than mirror the
  namespace (`Scripts/InterfaceAdapters/` → `.Adapters`); that is deliberate (D-014). One namespace
  per file; file path should mirror it.
- **Naming:** PascalCase for types, methods, properties, public fields; `camelCase` for locals
  and parameters; `_camelCase` for private fields (including `[SerializeField]` ones);
  ALL_CAPS for constants.
- **Layout:** Allman braces (opening brace on its own line), one type per file,
  file name = type name. `using` directives outside the namespace, sorted.
- **Fields:** prefer `private` + `[SerializeField]` over `public` fields. No `public` fields
  except in data-only DTOs.
- **Async/tweening:** UniTask (`async UniTask`) instead of `StartCoroutine`; PrimeTween instead
  of DOTween/manual lerps (see D-004).
- **Unity messages:** only the composition host and the thin shells are allowed to define
  `Update`/`FixedUpdate`; gameplay classes implement `IUpdatable` instead (D-013). Keep
  `Awake/Start/OnEnable/OnDisable` thin and delegate to named private methods when logic grows;
  a shell holds no rules (D-012, ARCHITECTURE.md §4). Always unsubscribe events in `OnDisable`,
  and dispose adapters that subscribed to anything external.
- **Avoid:** `GameObject.Find`/`FindObjectOfType` in `Update` (cache in `Awake`); `magic strings`
  for tags/layers (use constants); `SendMessage`.
- **Comments:** explain *why*, not *what*. No commented-out code in commits — delete it
  (git remembers).
- **No `var` when the type isn't obvious from the right-hand side.**

## Performance (WebGL is the shipping target — D-007)

- **Budget first, then build:** each system gets a number (live particles, awake rigidbodies,
  simultaneous burning objects) written down before it gets code — see TODO.md.
- No per-frame allocation in `Update`/`FixedUpdate` hot paths: no LINQ, no closures capturing
  per frame, no `new List<>`/string concat per frame, no `GetComponent`/`Find` per frame.
- Fire is object-state based, never a per-cell simulation (D-008); propagation is polled on a
  schedule (tick or timer), not tested every frame by every burnable.
- Cache component references in `Awake`; prefer `ParticleSystem` over `VFX Graph` for anything on
  the spray/fire hot path until a web measurement says otherwise.
- Pool anything that spawns repeatedly (foam puffs, ignite/collapse events, audio sources).
- Physics layers must be set up early and used for query masks — broad per-object collision
  checking is the cheapest thing to lose on web.
- If a measurement is needed, it happens in a **web build**; profiler numbers from the editor are
  a hint, not evidence (G-010).

## Tests (D-026)

- **EditMode, on code:** `[Test]` plain NUnit on plain C# classes, run with
  `unity cmd test run --mode editor`. No scene load, no Play Mode, no `[UnityTest]`, no
  `GameObject`/`Instantiate` in a test. The test asmdef is editor-only
  (`includePlatforms: ["Editor"]`), which is what makes the suite EditMode.
- **What gets tests:** Domain rules, UseCases (session orchestration), and **invariants** — the
  always-true properties of the domain state (`velocity ≤ MaxPushSpeed`, a push outlives its
  `Duration`, a jump fired on the landing tick survives). **Adapters get none:** a shell has no rules,
  so a test on a shell asserts Unity's behaviour.
- **Stubs, not fakes of Unity:** domain classes take ports (`IPlayerInput`, `IPhysicsSettings`,
  `IControlSettings`, `IPushPresets`) through constructors, so a test builds them with
  `StubInput`/`StubPhysics`/`StubControl`/`StubPresets` and the authored DTOs (`Presets`). Authored numbers are
  set **before** construction (the domain reads them at construction, D-012) — mutating a settings object
  afterwards is not a test input (D-037: one port per reader, so a test sets the port its subject lives in).
- **One rule, one test:** a rule gets exactly one test, and a test that restates the same rule from a
  second angle is deleted, not kept (D-050). A suite that asserts six rules twenty times is the shape that
  made tests feel expensive.
- **When, and how often:** a test is written after the user has played the mechanic and approved it. The
  suite is a **regression net run once at the end of a session**, never per task — the acceptance gate for
  a task is the user playing the mechanic, and their verdict is the task done (D-050).
- **Assert analytic expectations:** author a value, compute what must follow (`Force / Mass`), assert
  that. A test that asserts an observed number from a run is a snapshot, not a rule.
- **Permanent assertions:** frame-rate independence at `dt = 1/60, 1/30, 1/120` within 5% (T8) and the
  no-per-tick-allocation budget (T12, `GC.GetTotalMemory` delta). A rewrite that breaks either fails.
- Naming: `XTests.Method_Condition_Expectation` (e.g. `ContinuousSpray_Saturates_WithoutStackingPushes`).

## Unity assets

- New folders/files under `Assets/_Core`: PascalCase, **no spaces** (vendor folders use spaces;
  ours don't). Scenes: `MainMenu`, `Level_01` style. Prefabs = their GameObject's name.
- Imported models/animations belong in `Assets/_Core/Art/…`, never in `Scripts/` — a model in the
  code folder is both confusing and a reminder that naming rules got skipped.
- Inspector-assigned references over runtime lookups where possible.
- Prefabs over scene-placed one-offs for anything that spawns at runtime.

## Git

- Branch `main`; small, single-purpose commits. Message: imperative mood, ≤ 72 chars subject
  (e.g. `Add player dash with PrimeTween`), details in body when needed.
- Don't commit generated files (`Library/`, `*.csproj`, `Temp/`…) — see GOTCHAS.md G-007.
- Don't commit WebGL build output directories.
- Reference issue/task IDs in the body when relevant.

## Docs

Routing, budgets and the writing rules live in **[`docs/README.md`](README.md)** — the single place that
defines them. Updating the right file is part of the work, not an afterthought, and the doc-budget test
is the gate that says a file has grown past its budget. Design docs are the contract: read, don't rewrite.

