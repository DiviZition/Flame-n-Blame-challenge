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

- Everything lives in `docs/` (D-011): **ARCHITECTURE.md for the shape of the code** (layers, tick,
  Unity boundary — edit it in the same change that moves a layer); DECISIONS.md for architecture calls
  with why; GOTCHAS.md the moment we get burned; BUGS.md for confirmed bugs; TODO.md for anything we
  must not forget; `docs/README.md` is the index. Shape → ARCHITECTURE, call → DECISIONS, convention →
  this file. `CLAUDE.md` stays at repo root and only points here (G-009). Keep them short and current.
- Design docs (`TDD.md`, `Preproduction.md`) are the contract: read them, don't rewrite them.
  A change of scope is a new DECISIONS entry, never an edit to the TDD.
