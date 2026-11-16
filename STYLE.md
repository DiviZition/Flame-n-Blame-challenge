# STYLE.md — Style Guidelines

Consistent conventions for code and assets in this project. Follow them for all new work;
when touching old code, match the surrounding code. Deviations need a reason (put real ones
in DECISIONS.md).

## C# (game code in `Assets/_Core/Scripts`)

- **Namespaces:** `FlameNBlame` root, then by area: `FlameNBlame.Core`, `FlameNBlame.Player`,
  `FlameNBlame.Combat`, etc. One namespace per file; file path should mirror it loosely.
- **Naming:** PascalCase for types, methods, properties, public fields; `camelCase` for locals
  and parameters; `_camelCase` for private fields (including `[SerializeField]` ones);
  ALL_CAPS for constants.
- **Layout:** Allman braces (opening brace on its own line), one type per file,
  file name = type name. `using` directives outside the namespace, sorted.
- **Fields:** prefer `private` + `[SerializeField]` over `public` fields. No `public` fields
  except in data-only DTOs.
- **Async/tweening:** UniTask (`async UniTask`) instead of `StartCoroutine`; PrimeTween instead
  of DOTween/manual lerps (see D-004).
- **Unity messages:** keep `Awake/Start/Update/OnEnable/OnDisable` thin; delegate to named
  private methods when logic grows. Always unsubscribe events in `OnDisable`.
- **Avoid:** `GameObject.Find`/`FindObjectOfType` in `Update` (cache in `Awake`); `magic strings`
  for tags/layers (use constants); `SendMessage`.
- **Comments:** explain *why*, not *what*. No commented-out code in commits — delete it
  (git remembers).
- **No `var` when the type isn't obvious from the right-hand side.**

## Unity assets

- New folders/files under `Assets/_Core`: PascalCase, **no spaces** (vendor folders use spaces;
  ours don't). Scenes: `MainMenu`, `Level_01` style. Prefabs = their GameObject's name.
- Inspector-assigned references over runtime lookups where possible.
- Prefabs over scene-placed one-offs for anything that spawns at runtime.

## Git

- Branch `main`; small, single-purpose commits. Message: imperative mood, ≤ 72 chars subject
  (e.g. `Add player dash with PrimeTween`), details in body when needed.
- Don't commit generated files (`Library/`, `*.csproj`, `Temp/`…) — see GOTCHAS.md G-007.
- Reference issue/task IDs in the body when relevant.

## Docs

- DECISIONS.md for architecture calls with why; GOTCHAS.md the moment we get burned;
  BUGS.md for confirmed bugs; TODO.md for anything we must not forget. Keep them short and current.
