# DECISIONS.md — Architectural Decision Log

Records important, non-trivial decisions made during development, so we don't relitigate them
later or "quickly improve" something and spend hours fixing what we broke.

**Rules:**
- Every entry gets a sequential ID (`D-001`), a date, and a status.
- Write entries *when* a decision is made, not after.
- A decision can only be changed by adding a new entry that supersedes the old one — never
  silently edit or delete an old decision (mark the old one `Superseded by D-XXX`).
- "Trivial" (obvious, easily reversible, no system-wide impact) → don't log it here.
- One decision per entry. Keep the *why* — that's the part we'll forget.

**Entry format:**
```
## D-XXX: <Short title>
- **Date:** YYYY-MM-DD
- **Status:** Accepted | Superseded by D-YYY | Reverted
- **Context:** What problem or constraint forced a choice.
- **Decision:** What we chose.
- **Why:** The reasoning, including the alternatives we rejected.
- **Consequences:** What this locks us into / what we now owe.
```

---

## D-001: Unity 6000.6.5f1 with URP
- **Date:** 2026-11-15
- **Status:** Accepted (made at project creation)
- **Context:** New 3D game project; needed a modern, supported pipeline with VFX Graph, Shader Graph and scalable quality (PC + mobile profiles exist in `Assets/Settings/`).
- **Decision:** Unity 6 6000.6.5f1, Universal Render Pipeline 17.6.0.
- **Why:** Unity 6 LTS line; URP covers both PC and mobile perf targets; VFX Graph available for fire/effects. HDRP rejected as overkill for the scope; Built-in rejected for tooling stagnation.
- **Consequences:** Must use URP-compatible shaders/assets; some HDRP-only assets are out.
- *(Note 2026-11-16: the "PC + mobile profiles" premise in the Context no longer holds — see D-009.)*

## D-002: Input System package only (no legacy Input)
- **Date:** 2026-11-15
- **Status:** Accepted (project setting `activeInputHandler: 1`)
- **Context:** Project must handle keyboard/gamepad cleanly and possibly rebinding.
- **Decision:** Unity Input System 1.20.1 exclusively. Actions live in `Assets/_Core/InputSystem_Actions.inputactions` (Player + UI maps).
- **Why:** Legacy `Input.*` is deprecated-path; Input System gives device abstraction and action maps for free.
- **Consequences:** Never call legacy `Input.GetAxis/GetKey`; any new action must be added to the `.inputactions` asset. PlayerInput or direct `InputAction` usage both OK.

## D-003: All game code lives under `Assets/_Core` behind one asmdef
- **Date:** 2026-11-15
- **Status:** Accepted
- **Context:** Mixed workspace of store assets; need a hard boundary between "our code" and "vendor code".
- **Decision:** `Assets/_Core/{Art,Scenes,Scripts}` is the only place game code goes. Runtime assembly: `FlameNBlame.asmdef` referencing InputSystem, PrimeTween, Cinemachine, UniTask.
- **Why:** Vendor folders get overwritten by asset-store updates; a single game assembly keeps compile times low and dependency direction explicit.
- **Consequences:** New vendor dependency (e.g., Feel's `MoreMountains.Tools`) must be added to the asmdef reference list explicitly. Editor-only code goes in a separate `_Core/Editor` asmdef, never in the runtime one.

## D-004: PrimeTween for tweens, UniTask for async
- **Date:** 2026-11-15
- **Status:** Accepted (made at project setup)
- **Context:** Need tweening and async without allocation churn or coroutine spaghetti.
- **Decision:** PrimeTween (local tgz package) for all tweening; UniTask for all async/await.
- **Why:** PrimeTween is allocation-free and simple; UniTask integrates with Unity's player loop and replaces `StartCoroutine` entirely.
- **Consequences:** Don't use DOTween (not installed) or `StartCoroutine` in new code; both libs are referenced by the game asmdef already.

## D-005: Feel 6.3 for game juice and feedback
- **Date:** 2026-11-15
- **Status:** Accepted (made at project setup)
- **Context:** Game needs impactful feedback (screenshake, haptics, particles, sound) without hand-rolling each effect.
- **Decision:** Use MMF_Player feedbacks from `Assets/Feel` for juice; Nice Vibrations (bundled) for haptics.
- **Why:** Battle-tested component library; AI skills for it ship in `Assets/Feel/Skills/`.
- **Consequences:** Custom code must stay out of `Assets/Feel/` (updates overwrite it). Feel-specific workflows follow the skills in `Assets/Feel/Skills/`.
- *(Note 2026-11-16: the haptics half of this decision is now dormant — see D-010.)*

## D-006: Root-level Markdown files as project memory
- **Date:** 2026-11-15
- **Status:** Superseded by D-011
- **Context:** Decisions, mistakes, bugs and tasks need to survive session boundaries for both humans and AI agents.
- **Decision:** `DECISIONS.md`, `GOTCHAS.md`, `BUGS.md`, `TODO.md`, `STYLE.md` at repo root; `CLAUDE.md` points to them and stays under 200 lines.
- **Why:** Plain files are readable by everyone and versioned with the code; CLAUDE.md is re-read by every agent session, the others are consulted on demand.
- **Consequences:** Updating these files is part of doing the work, not an afterthought.

## D-007: WebGL is the shipping target from day one
- **Date:** 2026-11-16
- **Status:** Accepted
- **Context:** The TDD (§4) lists "Перевірка на WebGL" as a polish-phase task, i.e. a check at the end. The user overrode that: web is "must must must have", so it becomes a constraint we design under rather than a surprise we discover. Our whole scope — fire propagation, an extinguisher particle spray, physics impulses and a player launch — is precisely the mix that fails a late web port.
- **Decision:** WebGL is the reference target for every system. The editor/PC is where we author; a WebGL build must exist and run from the first week, and each new system is judged against a web build, not against the editor.
- **Why:** Retrofitting web is the known failure mode for this kind of demo: gameplay code is effectively single-threaded under wasm, so physics + particles + fire propagation must share one budget that is decided *before* those systems are authored, not balanced after. Download size caps asset imports, memory is capped by the browser tab, and haptics simply do not exist there. Porting at the end would mean rewriting the very systems that are the entry to the challenge.
- **Consequences:** Every system gets a perf budget before it gets implemented (tracked in TODO.md). A web build runs every session, so "compiles and looks right in the editor" is explicitly not done. No thread-dependent APIs; IL2CPP stripping and shader variants can only be trusted in a real web build. Quality levels and the pipeline asset must actually route WebGL to a working pipeline (currently they don't — B-001). Makes URP 17's VFX Graph heavier to use than on PC, so prefer Particle System for the spray (the TDD suggests exactly that).

## D-008: Scope locked to the four TDD pillars
- **Date:** 2026-11-16
- **Status:** Accepted
- **Context:** Two documents disagreed about what to build. The brainstorm in `Preproduction.md` scored "catch a person on a trampoline" 8/10 and never listed the extinguisher; the TDD commits to extinguisher + hydrant catapult. Confirmed with the user: **the TDD is the final output.**
- **Decision:** Four pillars, and nothing else: (1) FPS controller with movement/camera/jump, `E`/LMB interaction and a single carry slot; (2) an object-based fire system where burnables ignite, blacken, collapse, can be extinguished, and re-ignite nearby burnables after ~10s of proximity so the fire propagates itself; (3) the extinguisher as a grabbable that extinguishes *and* applies force; (4) the hydrant catapult. Win condition: the house is extinguished and the player walks out.
- **Why:** The hydrant gives the biggest read-at-a-glance payoff for almost no animation cost (rotate + impulse + feedback), which is the exact axis the judging criteria reward. The trampoline needs ~9 animations, ragdoll tuning and IK experience the preproduction notes already flagged as hard. A fluid/grid fire simulation is not web-viable under D-007, and the TDD says so too ("об'єкти горіння, не симуляція рідини") — state-based fire costs O(burnables) instead of O(cells).
- **Consequences:** The TDD's deferred list is now official: meta/scoring, full co-op, building destruction, ragdoll/IK, blowing the dam. Anything outside the four pillars needs a decision entry before it enters scope. Fire tuning starts from the TDD's numbers (10s spread proximity, burn-then-collapse). The player-pushback coupling in pillar 3 is still an open design question the TDD itself raises — it needs its own decision before implementation.

## D-009: One render pipeline asset, named for its shipping target
- **Date:** 2026-11-16
- **Status:** Accepted
- **Context:** With web as the target (D-007), keeping a PC-tuned pipeline as the active one means testing one thing and shipping another. The user renamed the template `PC_RPAsset`/`PC_Renderer` to `Web_RPAsset`/`Web_Renderer` and deleted the mobile ones, planning to re-tune later.
- **Decision:** A single active pipeline, `Assets/Settings/Web_RPAsset.asset` + `Web_Renderer.asset`, named for the target we ship rather than for a hardware class. Quality levels will be re-cut for web (TODO).
- **Why:** One pipeline removes drift between what we author and what we ship; naming by target makes that drift visible the moment someone adds a second "just for testing" asset.
- **Consequences:** Partially supersedes D-001's PC+mobile premise. The renamed asset still carries PC-grade settings (HDR, shadow distance, post-processing) — re-tuning is an open task, not a completed one. **Renaming did not rebind anything**: `Web_RPAsset` is still attached only to the "PC" quality level, that level excludes WebGL, and the deleted mobile asset left level 0 pointing at a dead GUID, so a web build currently has no pipeline at all (B-001, G-010). `Mobile_RPAsset` is gone and must not be assumed available as a quality-tier fallback.

## D-010: Feel for juice; Nice Vibrations kept but dormant
- **Date:** 2026-11-16
- **Status:** Accepted
- **Context:** Feel's value here is the whole feedback toolkit (screenshake, particles, sound, time), not the haptics — and haptics don't exist on our shipping target (D-007). Deleting Nice Vibrations was considered.
- **Decision:** Keep `Assets/Feel/NiceVibrations` in the project, unreferenced by our code, and use Feel's `MMF_Player` for juice. Deleting it was rejected as not worth the risk.
- **Why:** The user's call: NV is dormant weight, not a hazard, and an asset-store folder removal can dangle GUIDs inside Feel's own prefabs and meta files for zero gain. Feel is genuinely useful far beyond vibration.
- **Consequences:** Never call NiceVibrations from our code, and never add a `NiceVibrations*` module to an `MMF_Player`'s inspector list — that would recreate the hard dependency and re-add runtime init cost. Every effect has to read as "felt" without haptics, because on web there is nothing to feel (G-011). Haptics may be revisited only as a PC-only nicety if it ever becomes free.

## D-011: Project documentation lives in `docs/`
- **Date:** 2026-11-16
- **Status:** Accepted (supersedes D-006)
- **Context:** The memory docs were scattered at repo root, which reads as clutter next to `Assets/`, `Packages/` and the solution files — and the design documents (TDD + preproduction) now have to be versioned with the project rather than living in `D:\Downloads\`.
- **Decision:** `docs/` holds everything: the design contract (`TDD.md`, `Preproduction.md`) and the memory set (`DECISIONS.md`, `GOTCHAS.md`, `BUGS.md`, `TODO.md`, `STYLE.md`), indexed by `docs/README.md`. Repo root keeps only `README.md` (what the project is) and `CLAUDE.md`.
- **Why:** One folder for "what we know", two root files for "what this is" and "what an agent must load". The design docs are the contract, so they belong under version control beside the memory they generate.
- **Consequences:** All path references point at `docs/…`. **`CLAUDE.md` stays at repo root** — Claude Code only auto-loads it from there, and moving it would silently drop every project instruction from future sessions (G-009). Root `CLAUDE.md` therefore stays a thin entry point that points into `docs/` instead of duplicating it, and keeping the two in sync is now a thing we owe.

## D-012: Game logic as plain classes; Unity kept at the edge
- **Date:** 2026-11-17
- **Status:** Accepted
- **Context:** The first code appeared before the shape was written down: `Bootstrapper` news up a session, `GamePlaySession`/`PlayerState`/`PlayerMovement` are plain classes, `PlayerInputMain` wraps the generated Input System actions behind `IPlayerInput`. Nothing in the docs said this was the approach, so the next system (or the next agent session) would have invented its own — and the natural Unity default, one MonoBehaviour per gameplay feature, is what D-007's shared web budget is worst at.
- **Decision:** Four layers with one inward dependency direction — `Domain` (state and rules), `UseCases` (a session), `Adapters` (ports + their Unity-side implementations), and a single **composition root** (`Bootstrapper`) that is the only scene object knowing concrete types exist. Gameplay rules live in plain C# classes; MonoBehaviours are only (a) the composition host and (b) thin **shells** Unity must call — physics/trigger callbacks, `Animator` behaviours, `MMF_Player` hosts, prefab holders — which forward what they observed into a plain class and hold no rules. Authored values are copied out of the Inspector/`ScriptableObject` into plain data objects (`MovementData`, `BurnableData`) passed to constructors. Unity math and query types (`Vector3`, `RaycastHit`) cross the boundary freely.
- **Why:** Ordering, cost and testability become visible in code instead of in scene wiring that a diff can't show. It lets the four pillars be authored in any order without fighting the scene file, lets the fire rules be asserted on without loading a scene (G-008's test plan), and lets a system that measures badly on web be re-written without touching the object holding it. The alternative — MonoBehaviour-per-feature with self-registration in `Awake` — makes execution order a script-execution-order setting, makes a half-built graph legal, and pays a per-object Unity scheduling cost whether or not the body is empty. Rejected as overkill for 8 days: a full Unity-free domain with our own math types, and DI/ECS frameworks.
- **Consequences:** `Scripts/Domain/` and `Scripts/UseCases/` folders have to appear; the types currently inside `Bootstrapper.cs` must move out of it (also a STYLE.md one-type-per-file break). Every physics/trigger touchpoint needs a shell, so "just put it on the prefab" is no longer a legal move — and the `ARCHITECTURE.md` §4 rules are what an implementer follows. Every adapter that subscribes externally becomes `IDisposable`, and the scene-reload path (menu/pause, phase 1) must be able to rebuild the graph from the root. Decided **now**, before pillars 2 and 3 exist, precisely because the extinguisher and hydrant are the two systems most tempted to skip it.

## D-013: One tick we own — `GameUpdater` for the update step, a fixed-step owner for physics
- **Date:** 2026-11-17
- **Status:** Accepted
- **Context:** With gameplay classes no longer MonoBehaviours (D-012), something has to call them, and pillars 2 and 3 are impulse-driven physics (spray shoves objects *and the player*, hydrant launches the player) which cannot be driven off a variable frame step.
- **Decision:** Exactly one `Update()` in the scene — `Bootstrapper` → `GameUpdater.Update(time, deltaTime)` → every registered `IUpdatable`, in **registration order**. `GameUpdater` dedupes on register and removes through a staged queue with swap-remove. A fixed-step counterpart (`IFixedUpdatable`, driven from one `FixedUpdate()` on the same host with `Time.fixedDeltaTime`) is part of this design; physics queries and impulses belong to it, visuals and camera read state on the update step.
- **Why:** One place to pause, step, measure, or throttle a system — fire propagation is polled on a schedule rather than run per frame (STYLE.md), and a system we drive can simply run every 3rd tick. Registration order as the ordering contract is reviewable in a diff, unlike script execution order. N `MonoBehaviour.Update` bodies cost per-object marshalling that only shows up in a web build (G-010's pattern); a list of plain objects does not. The swap-remove + queued-removal shape means an updatable can stop itself mid-loop safely and neither path allocates per frame.
- **Consequences:** `IFixedUpdatable` and its host are **not implemented yet** — tracked in TODO.md so the first physics pillar builds it instead of re-deciding it. Registration order is a real responsibility: getting it wrong is a silent gameplay bug, not a compile error. Nothing else may define Unity's `Update`/`FixedUpdate` except the host and the shells that *must* be MonoBehaviours (D-012). Per-system cost is now attributable, so the budgets TODO.md asks for become enforceable per system.

## D-014: Namespace root is `Flame_n_Blame_Challenge`; folders and namespaces are allowed to differ
- **Date:** 2026-11-17
- **Status:** Accepted (supersedes the naming in STYLE.md §Namespaces and the example in G-002)
- **Context:** Two names for the same thing were live at once: the code and the asmdef `rootNamespace` say `Flame_n_Blame_Challenge`, while STYLE.md §Namespaces, the G-002 rule and TODO all said `FlameNBlame`, by area (`FlameNBlame.Player`, `FlameNBlame.Combat`). Writing the architecture map forced the choice, because the map would otherwise freeze whichever one it happened to mention.
- **Decision:** Root namespace **`Flame_n_Blame_Challenge`**, sub-namespaces by **layer** (`.Domain`, `.UseCases`, `.Adapters`), matching the asmdef `rootNamespace` that is already set. `FlameNBlame` stays the assembly/file name. The folder is `InterfaceAdapters/` while the namespace is `.Adapters` — deliberate, not drift: the folder names the pattern, the namespace stays short. Namespaces are by layer, not by feature.
- **Why:** The user's call: keep what already compiles, and it is what the editor's `rootNamespace` will auto-insert for new files, so the convention holds itself instead of relying on everyone remembering to type it. Namespacing by layer keeps the dependency direction of D-012 legible from a `using` line; namespacing by feature would let `FlameNBlame.Fire` reference `FlameNBlame.Player` without anyone noticing. Renaming five files now is cheaper than renaming fifty later, but it buys nothing we need.
- **Consequences:** STYLE.md §Namespaces and the G-002 rule are updated to match — this is the one place where editing an existing entry is correct, since the decision changed reality rather than correcting a mistake. The hyphenated-namespace bug itself (G-002) is resolved. Expect `using Flame_n_Blame_Challenge.Adapters;` in most files; the verbosity is accepted. The assembly's own `name` field is currently the typo `FlameNBlame-asmdf`; references to it are GUID-based, so fixing it is safe but cosmetic and belongs to the asmdef housekeeping task (G-001), not to this decision.

## D-015: `docs/ARCHITECTURE.md` is the code map; DECISIONS.md stays the log
- **Date:** 2026-11-17
- **Status:** Accepted (extends D-011)
- **Context:** The doc set routes by content type — one decision per DECISIONS entry, per-file conventions in STYLE.md, restrictions in GOTCHAS, tasks in TODO — and no file could hold "what the code looks like as a whole". A reader who wants the shape has to assemble it from D-003, D-012, D-013, D-014 and the code, and two sessions would assemble it differently.
- **Decision:** `docs/ARCHITECTURE.md` is added: a living map (layers, dependency direction, tick ownership, Unity boundary, composition root, how to add a system), with the *why* and the alternatives kept in DECISIONS entries. Precedence is stated in the file: DECISIONS wins over ARCHITECTURE, ARCHITECTURE describes intent, the §real-vs-decided table says what today's code actually does. `docs/README.md` and `CLAUDE.md` gain pointers; STYLE.md §Docs gains the routing rule.
- **Why:** Append-only one-decision-per-entry is the right shape for decisions and the wrong shape for a picture — that is not a flaw to fix by making DECISIONS looser, since D-006/D-011 show what happens when decisions get quietly re-litigated. A separate map risks doc sprawl, so the boundary is explicit: ARCHITECTURE may change freely as the shape changes and holds no rationale; DECISIONS is append-only and holds nothing but rationale.
- **Consequences:** A new file to keep current — the map goes stale the moment a layer moves, so an agent that changes the shape edits ARCHITECTURE.md in the same change. "Where does new content go" now has one more branch: shape → ARCHITECTURE, call → DECISIONS, convention → STYLE. Kept short and code-referenced on purpose (174 lines at writing); if it grows past ~200 or starts arguing with itself, that is a sign content belongs in DECISIONS. It also has to stay honest about unbuilt decisions — the §real-vs-decided table is the part that stops the map reading as a spec the code failed to meet.
