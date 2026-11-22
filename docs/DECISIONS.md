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

## Current structure (map, updated whenever reality moves)

Verified 2026-11-20 against the files and the live editor, not against old notes.

**Assemblies:** `Assets/_Core/FlameNBlame.asmdef` (its `name` field is the typo `FlameNBlame-asmdf`,
TODO; `rootNamespace: Flame_n_Blame_Challenge`; GUID references; includes the `CartoonFX` vendor-demo
reference, G-001) and `Assets/_Core/Tests/FlameNBlame.Tests.asmdef` — **editor-only**
(`includePlatforms: ["Editor"]`), so the 19 `[Test]` tests run as **EditMode**, on code with stub ports,
no scene load (D-026).

**Folders ↔ namespaces ↔ layers:**

| Folder (`Assets/_Core/Scripts/`) | Namespace | Layer | Contents |
|---|---|---|---|
| `` (root) | `Flame_n_Blame_Challenge` | Composition | `Bootstrapper.cs`, `GameUpdater.cs` |
| `UseCases/` | `.UseCases` | Application | `GamePlaySession.cs` |
| `Domain/` | `.Domain` | Domain | `Player`, `PlayerMotion`, `PlayerMovement`, `PlayerLookRotator`, `ActivePush`, `PushType`, `IPushPreset`, `ImpulsePresetData`, `SustainedPresetData` |
| `InterfaceAdapters/` | `.Adapters` | Adapters | ports `IUpdatable`, `IPlayerInput`, `IGameSettings`, `IPushPresets`; `PlayerInputMain`, `GameSettings` + `Game Settings.asset`, `PushPresets` + `Push Presets.asset`, `ResourceSpawner` + `ResourcesProvider` + `Resources.asset`, `PlayerMotor`, `HydrantPush` |

`Scripts/Unity/` and `Scripts/Application/` were deleted (2026-11-19, D-012). `HydrantPush` moved with
its `.meta` (GUID `75c4001556e8f7e47aca53aa50ef5170`) — that GUID is what the scene binds to.

**Who talks to what:** `Bootstrapper` (one `Update()`) → `GameUpdater` → registered `IUpdatable` in
registration order `PlayerInputMain` → `Player` → `PlayerMotor`. `Player` composes
`PlayerMotion(PlayerMovement, IGameSettings, IPushPresets)`, exposes `Velocity`, forwards
`ReceivePush(preset, direction)` (two overloads: Impulse, Sustained) and `ReportContact(grounded, normal)`.
Shells: `PlayerMotor` (`Move(Velocity*dt)`, reports `isGrounded` and the contact normal from Unity's
`OnControllerColliderHit`, forwards `ApplyForce(preset, direction)`), `HydrantPush` (trigger →
`ApplyForce(preset, PushDirection.forward)`). A source decides **whether** to fire; `PlayerMotion` decides
what a fired push does (D-016, D-021). `PlayerMotion`'s channels: **horizontal** `ControlVelocity` (input) +
`PushVelocity` (shove), and **vertical** `VerticalVelocity` (gravity + every launch); the ground absorbs
vertical **only when it points down**, so a launch that is strong enough to rise accumulates (D-028).
Presets are authored in `Push Presets.asset`, keyed by `PushType` (D-028).

**Scene `Assets/_Core/Scenes/FnB_Gameplay.unity`:** `=== System` → `Bootstrapper` (GUID
`40c42f9779f68674690ed56e87150acf`) + Main Camera (Camera + CinemachineBrain); `=== World` →
FX/Directional Light + `GrayBox Level` (Ground 10×10 plane, `Hydrant` with `CapsuleCollider` +
`HydrantPush` + `PushDirection`, `Burnable Box` at world (4.56, 0.34, 6.18), `Slope`, `Reference Cube`,
`Player Spawn Point` at (2.84, 0, 7.78)). **There is no `Player` GameObject, so `PlayerMotor` is
attached to nothing (B-004); no `SpineProxy`; the scene is not in Build Settings (G-003).**
`EditorBuildSettings.asset` still lists the deleted `SampleScene.unity`. The `House Blockade` test wall
(CLI scene-driving proof, D-023) was deleted on 2026-11-20 by the user's call.

**Strays:** `Player.cs` at repo root is an untracked copy of `_Core/Scripts/Domain/Player.cs`;
`X Bot@Falling.fbx` sits in the code folder. Both are housekeeping (TODO).

---

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
- *(Note 2026-11-19: the "a web build runs every session" clause is suspended by D-019. The budget-before-code rule, the "editor is not done" standard and the no-haptics rule are untouched.)*

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
- *(Note 2026-11-19: the `IFixedUpdatable` half of this decision is superseded by D-018 — the fixed-step host is not built. The single-`Update` host, registration order as the ordering contract, and staged removal stand, and `PlayerMotor`'s own `Update()` was deleted to restore the single-`Update` rule (D-016).)*

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

## D-016: Player pushes go through the domain; contact, not the timer, absorbs momentum
- **Date:** 2026-11-19
- **Status:** Accepted (closes the open coupling question D-008 left for pillar 3)
- **Context:** D-008 explicitly left pillar 3's coupling open ("how to adequately bind the player's own pushback? It's an external system"). The first implementation put the active-force list, the force tick, the curve evaluation, gravity and vertical velocity inside `PlayerMotor` — a shell, which D-012 forbids — and the tree did not compile (B-002). The symptom drove the design: a 2-second hydrant preset expired while the player was still flying and velocity dropped to zero in mid-air. That is the feel of no inertia, and it is structural: `CharacterController.Move` teleports by the vector handed to it and keeps nothing (G-012), so momentum must be ours to own.
- **Decision:** The domain owns velocity. `PlayerMotion` (Domain) holds three summed channels — `ControlVelocity` (input-driven, never touched by a push), `PushVelocity` (push momentum), `VerticalVelocity` (gravity + the vertical part of a launch) — plus the active push list (capacity 8, index swap-remove). Every push enters through **`Player.ReceivePush(PushPresetData, Quaternion worldOrientation)`**, and the direction comes from the **source's** rotation, never the player's facing. A preset is `Type / Kind / Applies / Force / Curve / Duration`; `Kind` is `Impulse` (Force in N·s, fires once) or `Sustained` (Force in newtons, acceleration per tick for `Duration`). **The force's `Duration` ends the *adding*, never the velocity**: momentum persists in flight, decays by authored `AirDrag`, saturates at authored `MaxPushSpeed`, and is absorbed by **contact** — grounded zeroes vertical and push-Y, side contact zeroes the pushed X/Z and leaves `ControlVelocity` intact so the player can still walk away. `PlayerMotor` is a thin shell: `Move(Velocity * dt)` + report `CollisionFlags` (Sides) and `isGrounded` back into the domain.
- **Why:** A rigid body never stops when a force's timer ends; contact is what absorbs momentum, so making the timer authoritative is precisely what produced the mid-air stop. Rejected: the report-only variant (domain reports input/jump/push list, the shell integrates it) — it puts the motion rules in an adapter, the exact violation being removed, and it is *more* work for the shell, not less; a single velocity vector — the input steering step would eat a launch back toward the input intent, so the three channels make "pushes add, never replace" a structural fact instead of a comment. Replace-per-`Type` bounds the entry count (a 0.1 s spray interval leaves **one** entry, not 30); authored `MaxPushSpeed` bounds the velocity (drag alone gives an equilibrium far too high to feel right); `max(Duration, 0.001f)` and a guarded inverse mass are the no-NaN guards. List overflow drops the oldest entry and logs, never grows silently.
- **Consequences:** Every future push source (spray, falling debris, hydrant burst) uses the same one method, so the pattern is: a system that wants to shove the player calls the shell, the shell forwards, the domain decides what velocity does. Routing stays as-is (`HydrantPush → PlayerMotor.ApplyForce → Player.ReceivePush`); a domain-side push-source registry is a later change. Contact reporting **must** include Sides, not just `isGrounded` (G-012). `PlayerMotor`'s own `Update()` is deleted, restoring D-013's single-`Update` rule, and it registers in `GamePlaySession` **after** the player, so input → player → motor is one list in code. All feel is tunable in one place (GameSettings + presets). The perf budget for this system is written *before* code, per D-007.
- *(Note 2026-11-19, same session: this entry named one decay rate, `AirDrag`, and nothing for the ground. A push in any direction but straight up left the player sliding for seconds. Filled by D-020 — two authored rates, `GroundDrag` grounded and `AirDrag` airborne.)*

## D-017: Jump is one of the pushes
- **Date:** 2026-11-19
- **Status:** Accepted (supersedes `PlayerJumpForce` and `PlayerMotorConfig` as jump authoring)
- **Context:** Jump existed three times at once: `GameSettings.PlayerJumpForce`, `PlayerMotorConfig.JumpCurve/JumpDuration`, and an empty `JumpPreset` slot on the prefab, with the implementation in the shell. If jump could not be expressed through the push entry point, the push model would be wrong.
- **Decision:** Jump is a `PushPresetData` (`Type: PlayerJump`, `Kind: Impulse`, `Applies: GroundedOnly`) authored on `GameSettings`. `Player` applies it to itself through the same `ReceivePush` path the hydrant and the spray use, gated on grounded, one press per contact, re-armed on the next contact.
- **Why:** One mechanism for all three (jump, hydrant launch, spray shove) means one place to tune, one place to test, and one place a future bug can live. It can be expressed through the same entry point, so there is no reason to keep a second one.
- **Consequences:** `PlayerJumpForce` and `PlayerMotorConfig` are deleted; the prefab's orphaned `JumpPreset` block is housekeeping. The jump lives in the Y component of `PushVelocity`, so a grounded tick zeroes it on landing — which is the same rule as any other push, not a jump special case. Jump tests assert launch magnitude as `Force / Mass`, i.e. authored, not coded.
- *(Note 2026-11-19, same session: "`Player` applies it to itself" — the preset, the entry point and the one-press-per-contact rule all still hold, but the gate's home moved into `PlayerMotion` and the flag out of `Player`. See D-021.)*

## D-018: No fixed step for now
- **Date:** 2026-11-19
- **Status:** Accepted (supersedes the `IFixedUpdatable` clause of D-013)
- **Context:** D-013 planned a fixed-step host for physics impulses and queries, on the reasoning that impulse-driven pillars "cannot be driven off a variable frame step". For this phase the user's call was to use the general `IUpdatable` and forget the fixed step.
- **Decision:** Integration is dt-driven on the single `GameUpdater` tick; `IFixedUpdatable` and its host are **not built**. Frame-rate independence is asserted by test T8 at `dt = 1/60`, `1/30`, `1/120`, with final velocity within 5%.
- **Why:** A fixed-step host is a second loop, a second ordering contract, and a second place to measure, for a cost the push model does not need: its only dt-sensitivity is integration error, and the tests bound it. `PlayerMotion`'s steps are all `x += rate * dt` or `x *= 1 - k * dt`, which is stable across the frame rates the game runs at.
- **Consequences:** The fixed-step TODO entry is struck, and D-013's "physics impulses go on the fixed step" clause is dead. If a future system needs deterministic stepping (fire-spread schedule, rigidbody queries), it re-opens as a new decision, not a resurrection of this clause. The frame-rate-independence tests become permanent: a rewrite that only works at one tick rate fails them.

## D-019: Web builds deferred to a dedicated pass
- **Date:** 2026-11-19
- **Status:** Accepted (suspends D-007's "a web build runs every session" clause; D-007's budget-before-code rule and the no-haptics rule stay in force)
- **Context:** The construction phase needs fast iteration, and a WebGL build costs minutes of the user's machine. Any web measurement today is untrustworthy: B-001 means the web target has no render pipeline bound at all, so a web build measures a broken configuration.
- **Decision:** Systems are built and verified in the editor — headless compile gate, PlayMode tests, editor feel checks — and **no web build is made in this change**. Per-system perf budgets are still written *before* code, and nothing is re-scoped. The web measurement happens in one dedicated pass, after B-001 is fixed.
- **Why:** Building against a pipeline that is not assigned measures nothing, and paying for that measurement every session buys no information. The deferral is a known price, not an accident: the systems in scope (fire, particles, impulses) are exactly the ones D-007 says must be judged on web.
- **Consequences:** The honest status line for any commit in this phase is **editor-verified, web-unverified**, and it must be written as such in the commit body. The risk we accept, stated plainly: a system tuned in the editor may measure badly on web, and we choose to find that out in one pass. B-001 remains the blocker for that pass, and D-007's "works in the editor is not done" stays true as a *final* standard.

## D-020: The push channel has two authored decay rates, one per contact state
- **Date:** 2026-11-19
- **Status:** Accepted (fills a gap D-016 left open: it named `AirDrag`, and nothing for the ground)
- **Context:** The launch and the jump read correctly in the editor, but a push in any direction except straight up leaves the player **sliding for seconds** — "the ground feels like ice". The cause is a rule gap, not a bad number: the push channel decayed at `AirDrag` (0.5/s) whether or not the player was grounded, so a 10 m/s launch still carried 2.5 m/s two seconds later, while the player's own control channel stops at `GroundFriction` (12/s). One rate for both states is what produced the slide.
- **Decision:** The push channel decays at **authored `GroundDrag` while grounded, `AirDrag` while airborne**, and ground friction acts **in the ground plane only** (`PushVelocity.x/z`) — never on its vertical component, which contact and gravity already own. Authored `GroundDrag` = 4 (units/s): a 10 m/s shove reaches walking speed in about an eighth of a second and comes to rest inside a second. Tuning range is stated with the field: ~1 for a long skid, past 8 for a hard stop.
- **Why:** Ground contact is what absorbs momentum (G-012); the question was only *how fast*, and that is authored, not derived. It is authored separately from `GroundFriction` because the two channels must be tuned independently — share the rate and a snappy stop-press drags the shove to a halt too, which deletes the "a shove outlives a stop" feel. Not zeroing the shove on landing: a hard switch removes the skid entirely, so the feel stops being tunable and a landing and a wall become indistinguishable. Not anisotropic (along-slide kept, across-slide absorbed) as it would be on a real capsule: it needs a contact normal, which `CharacterController` does not give the shell, so it would need a raycast — logged as the next refinement, not this one. Restricting ground friction to the ground plane is what keeps a jump fired on that tick from being eaten by the friction meant to absorb the *slide*.
- **Consequences:** A new authored field on `IGameSettings`/`GameSettings` and in `Game Settings.asset`, and in `StubSettings`. New test T13 (`GroundContact_AbsorbsAShove_TheAirDoesNot`) asserts the asymmetry with analytic expectations, and it fails on a one-rate model — it is the gate for this rule. Tests that want exact push-channel math now set `GroundDrag = 0f` explicitly next to `AirDrag = 0f`; the stub default mirrors the authored 4, so forgetting is a test of the real rule, not a free pass. Re-opening for anisotropic friction is a new decision that also changes what the shell reports (a normal, not a `Sides` bool).

## D-021: The jump gate lives in `PlayerMotion`; `Player` is composition and forwarding only
- **Date:** 2026-11-19
- **Status:** Accepted (supersedes the home of the jump gate in D-017; the preset, the entry point and the grounded/one-per-contact rule are unchanged)
- **Context:** `Player` held `TryJump()` and `_jumpFiredThisContact`. The user's objection: `Player` is a wrapper on the domain logic — it should hold no rules — and jump is a feature of movement, so the movement class should manage it. Both halves of that objection are right, and the second one cannot be taken literally: `PlayerMotion` depends on `PlayerMovement` (it reads `DesiredVelocity`), so a jump applied *by* `PlayerMovement` would need a reference back and the composition would be a construction cycle.
- **Decision:** The jump **rule** (may a request become a launch this tick, one per contact, re-armed on the next) lives in `PlayerMotion`, next to the only two facts it needs — contact and the push channel — and it fires the authored `_settings.JumpPreset` through the same `ApplyPush` path. The jump **request** is exposed by `PlayerMovement` (`IsJumpRequested`, reading `IPlayerInput.IsJumping`) — request in, rule out. `Player` keeps exactly three things: the composition, `Velocity` for the shell, and the two forwards (`ReceivePush`, `ReportContact`). `TryJump()` and the flag are deleted from it.
- **Why:** One owner per rule; a rule split across two classes with a one-tick handshake between them is a bug magnet, and the cycle rules the naive reading out. `PlayerMotion` is a domain class that already reads `PlayerMovement`, so the gate costs no new dependency, no new field on `Player`, and no setter-injected wiring (which the layering forbids: wiring once in a constructor is the rule). A per-feature `UseCases` class was considered for this and for the other push sources and is not taken now: a jump use case would own a three-condition gate, which is smaller than the class that hosts it; the same shape is a clear win for the extinguisher spray, where the start condition is a raycast, the direction is the aim, and the interval is a re-call — that gets its own entry when the spray exists.
- **Consequences:** `Player.UpdateLogic` is three calls; the jump fires inside `PlayerMotion`'s step, after `ApplyContact()` and before gravity, so a launch on the tick it lands survives the ground it left (the ordering contract D-013 already states). The gate now uses `PlayerMotion`'s own `IsGrounded` (the report Unity gave) instead of `PlayerMovement`'s copy, which are fed by the same call — so there is one fewer place for grounded to disagree. `Player` no longer needs the `IPlayerInput`/`IGameSettings` references at all, only the parameters. `PlayerJumpTests` drive `_input.IsJumping` and are unaffected: they assert behaviour through `Player`, never the gate. The jump preset's `Applies: GroundedOnly` is now redundant with this gate — that axis is a separate open question (TODO), and if it is deleted this entry's decision does not change.

## D-022: Unity CLI is the editor-automation interface; the MCP bridge stays unwired
- **Date:** 2026-11-20
- **Status:** Accepted
- **Context:** Verified the toolchain end to end. `unity.exe` (Unity CLI, v1.0.1) is installed at
  `%LOCALAPPDATA%\Unity\bin`; `com.unity.pipeline` 1.0.0 is already in `Packages/manifest.json`, and
  `unity status` reports our running editor on port 7800 with `state: ready` — so a live editor
  answers 196 Pipeline commands (`script recompile`, `test run`, `console get`, `scene get_hierarchy`,
  `gameobject find`, `build run`, `diagnostics profiler_record`, `runtime simulate_key`). Unity's own
  docs (`Documentation~/snippets/mcp-deprecation-notice.md` in AI Assistant 2.21.0-pre.2) say the MCP
  server is deprecated and the CLI replaces it. The bridge does work — 54 tools + 1 prompt over the
  named pipe `\\.\pipe\unity-mcp-<hash>-<pid>`, relay `~/.unity/relay/relay_win.exe` — but see G-013
  for how fragile it is to drive by hand, and every MCP tool is covered by `unity cmd`.
- **Decision:** The Unity CLI is **the** way an agent drives this editor: `unity status` first, then
  `unity cmd`, `unity test`, `unity build`, `unity run`, `unity recompile`. The MCP bridge is **not**
  registered in any client's MCP config. Skills come from the CLI binary: 37 Unity skills installed
  project-local (`.opencode/skills/`, `unity skill install opencode --local`) and user-global for
  Claude Code (`~/.claude/skills/`). The G-004 `.claude/skills` junction to `Assets/Feel/Skills` is
  **not** created — Feel's `SKILL.md` files are read directly, on demand.
- **Why:** Registering 54 MCP tools on a deprecated path spends context to duplicate what `unity cmd`
  already does, and a stdio MCP proxy that shuts down at stdin EOF (G-013) is a bad dependency to
  build on. The CLI talks to the *warm* editor, so the compile gate stops paying batch-mode cold-start
  cost, and it reaches the Player runtime too (`--runtime`), which a batch run never does. Installing
  skills project-local keeps them pinned to this repo's Unity version and lets `unity skill sync`
  re-render them against the CLI binary; the Claude Code copy is user-global so it covers every project
  on this machine. OpenCode discovers both sources; Claude Code needed a global install to see them.
- **Consequences:** `CLAUDE.md` §"Unity toolchain" is the pointer agents read; the compile gate for
  B-002 is `unity cmd script recompile`, not the batch-mode invocation. After any `unity self-update`,
  `unity skill sync` re-renders the installed skills — a stale skill copy is a known failure mode.
  Nothing may add Unity MCP to `mcp.servers` without a new decision. Whether `.opencode/skills/` is
  committed or gitignored is **open** (TODO). The Feel-skills path is unchanged in substance: agents
  read `Assets/Feel/Skills/<id>/SKILL.md` before Feel work (G-004 stays Open with a revised rule).

## D-023: CLI-only toolchain, exactly 2 Unity skills, agent speaks Russian
- **Date:** 2026-11-20
- **Status:** Accepted (supersedes the skills-install clause of D-022; D-022's MCP-unwired clause stands)
- **Context:** 37 Unity skills were installed project-local + user-global earlier the same day, and the
  user's review was that the list is too complex. Skills are manuals for the agent, not runtime
  capability: the CLI's 196 commands do the work, and the MCP bridge (54 tools) is deprecated by Unity.
  The CLI was proven able to drive the scene end to end with no MCP: `gameobject create` →
  `set_transform` → `component add` → `component get_properties` → `capture scene_view/game_view`,
  with the change written into `FnB_Gameplay.unity` (the `House Blockade` test wall).
- **Decision:** The Unity CLI is the toolchain and the MCP bridge stays unwired (D-022). **Exactly two
  skill manuals** remain: `unity-cli` and `unity-pipeline`, project-local `.opencode/skills/` plus the
  Claude Code global copy; the other 35 were deleted from both locations. There is no
  `unity skill uninstall`, so deleting folders is the undo — and `unity skill sync` / `unity skill
  refresh` are banned, because they re-render and re-create the whole 36-skill tree (G-015).
  **The agent answers in Russian**, short and plain, 5–10 lines, no wide tables, no option menus
  (CLAUDE.md §Rules).
- **Why:** Adverting 36 manuals spends context in every session, and 35 of them describe systems
  outside the four locked pillars (D-008). The 2 that stay are the ones needed to drive the CLI
  correctly. Deleting folders, not disabling, keeps the repo clean and makes the state visible on disk.
- **Consequences:** Getting a niche skill back for one task =
  `unity skill install opencode --local --skill <name>`, then delete it again. The CLI's tracking list
  (`unity skills`) stays **stale** after manual deletes — trust the folders on disk, not the list.
  Whether the 2 remaining skill dirs (754 KB, generated by the CLI binary) are committed or gitignored
  is **open** (TODO). After `unity self-update`, do **not** run `sync`: skills stay as they are.

## D-026: Tests — on code, on Domain/UseCases/invariants, after the user approves the mechanic
- **Date:** 2026-11-20
- **Status:** Accepted (changes the test process in CLAUDE.md §Rules and STYLE.md §Tests; D-016's test
  list stays, its tests are on borrowed time until the feel pass)
- **Context:** The 19 tests are `[Test]` plain NUnit on plain C# classes with stub ports, but the test
  asmdef is platform-agnostic, so Unity runs them as **PlayMode** — paying a scene load and driving
  Play Mode for code that touches no Unity, in a scene that has no `Player` (B-004), so the run proves
  nothing about gameplay. The user's review: writing tests before a mechanic is approved means writing
  them 5 times, because the mechanic gets edited and bugs get fixed; a test on an adapter asserts
  Unity, not our rules; and re-running a passing test 5 times buys no information.
- **Decision:**
  1. **Layers tested:** Domain, UseCases, and **invariants** (always-true properties of domain state).
     **Never Adapters** — shells have no rules.
  2. **When:** tests are written **after** the user has played the mechanic and approved it.
  3. **Where:** the suite runs **on code** through `unity cmd test run --mode editor`;
     `FlameNBlame.Tests.asmdef` becomes editor-only (`includePlatforms: ["Editor"]`), which is what
     makes it EditMode. Play Mode is for feel checks with `capture game_view`, not for the suite.
  4. **The 5-run rule is removed:** a test runs once, under 90 s; the gate is re-run whenever the code
     changes.
- **Why:** Feel is judged by the user's eyes, so a test cannot say a launch feels right — asserting an
  unapproved mechanic is writing a specification the user is about to change. Adapters are thin
  forwarders, so a test there costs the same as a test of `CharacterController`. EditMode removes the
  scene from the measurement: the same 19 assertions, no scene load, no Play Mode. One run is enough
  because NUnit is deterministic and the gate is not a one-off — every edit re-runs it.
- **Consequences:** The CLI gate for tests is `unity cmd test run --mode editor` (synchronous, no
  `--async-tests`, results in the run output). The existing suite needs no code change, only the asmdef
  platform. The push-model tests are written for a mechanic that is **not** user-approved yet (the 5
  editor feel checks are open), so expect them to be edited during the feel pass; the permanent
  assertions are frame-rate independence (T8) and the no-per-tick-allocation budget (T12).

## D-025: Zero Unity skills — the CLI documents itself
- **Date:** 2026-11-20
- **Status:** Accepted (supersedes the "exactly 2 skills" clause of D-023; D-023's MCP-unwired clause,
  the CLI-first clause and the Russian-language clause stand)
- **Context:** After the D-023 prune, 2 manuals stayed: `unity-cli` + `unity-pipeline`, 754 KB,
  untracked, in `.opencode/skills/` and in the user's global `~/.claude/skills/`. Reviewing what the
  agent actually needs: `unity cmd` prints every command **with its description**, `unity cmd --query
  <term>` searches the list, `unity cmd <group> <verb> --help` prints the accepted parameters, and the
  non-obvious facts (JSON-array vectors, `--hierarchy-path`, the compile/test gates, `confirm=true` for
  destructive commands) are recorded in CLAUDE.md §Commands and G-014/G-015.
- **Decision:** **No Unity skills in the repo and none in the global skills folder**: `unity-cli` and
  `unity-pipeline` deleted from both. `gepeto` + `pinokio` (the user's non-Unity global skills) stay.
  **Feel's 8 skill folders stay in `Assets/Feel/Skills/` where they were** — vendor folder, unadvertised,
  read directly (G-004).
- **Why:** A manual that duplicates the CLI's own help output spends context in every session and goes
  stale against the CLI binary; a folder of generated markdown in the repo has to be committed,
  gitignored, or deleted, and it is regenerated by commands we are not allowed to run. Self-documenting
  tools beat manuals.
- **Consequences:** An agent driving Unity reads CLAUDE.md §Commands and asks the CLI (`--query`,
  `--help`). `unity skill install` is now a **per-task exception**, never a habit: install one niche
  skill, use it, delete its folder. Nothing in the repo is generated by the Unity CLI, so `.opencode/`
  is empty and the git-tracking question is closed. The tracking store (`unity skills`) lists 36 entries
  that do not exist on disk — trust the folders.

## D-024: Docs re-scoped — one job per file, BUGS.md merged into TODO.md
- **Date:** 2026-11-20
- **Status:** Accepted (supersedes the routing rules of D-011 and D-015; the entries they recorded stand)
- **Context:** The doc set had drifted from what it is for. `CLAUDE.md` (306 lines) carried scene
  archaeology, MCP pipe internals and a tool inventory — state, not behaviour; `TODO.md` (241 lines)
  had duplicate lines, stale "not run yet" items and paragraph-long narratives; `ARCHITECTURE.md`'s
  mutable section retold the rationale of D-016/D-020, which DECISIONS already owns; `BUGS.md` did the
  same job as TODO. The user asked for exactly one responsibility per file, and said he does not want
  to keep track of the docs himself.
- **Decision:** The split, one line each:
  - `CLAUDE.md` — agent behaviour only: rules, commands, toolchain, vendor boundaries, doc routing.
    Thin (~110 lines). No state, no environment inventory.
  - `TODO.md` — the work list, **including bugs** (the `B-` ids stay, as `- [ ] B-001` lines with
    repro + status). Done / Now / Phases / Housekeeping / Done sections.
  - `ARCHITECTURE.md` — how to build: the READ-ONLY rules, the layer map, who owns the tick, the
    checklist for a new system. No rationale, no history.
  - `DECISIONS.md` — **§Current structure** (folders, namespaces, assemblies, who talks to what, scene
    contents) kept current, plus the append-only decision log with the *why*.
  - `GOTCHAS.md` — traps we fell into, each ≤ 5 lines: Status / What / Rule.
  - `STYLE.md` — conventions. `docs/README.md` — the index.
  **BUGS.md is deleted**; its three rows move into TODO.md's bug section. Editing existing
  DECISIONS/GOTCHAS entries is allowed **only** to compress verbosity, never to change an ID, a status
  or a Rule; new content is always a new entry.
- **Why:** A doc that mixes state with behaviour is read every session (CLAUDE.md) and spends context
  on facts that change daily; a doc that retells decisions makes two files argue. The user's stated
  need is to not have to know what is in each file — so each file answers exactly one question, and
  the answer to "what's the state" is in one place (DECISIONS §Current structure) rather than three.
- **Consequences:** `CLAUDE.md`'s "Current state of the code" section is gone: state moves to
  DECISIONS. Bugs are tracked by the same checklist mechanism as tasks, so a bug is done when its test
  passes, not when it is written. `README.md`, `STYLE.md §Docs` and `CLAUDE.md §Docs` routing lines are
  rewritten to match. `docs/` stays untracked (TODO) - the doc cleanup is one commit.

## D-027: Simulation split — horizontal shove channel + vertical launch channel, contact by normal
- **Date:** 2026-11-20
- **Status:** Accepted (refines D-016's channel model and D-020's absorption rules; the step order of D-016
  stands; supersedes the "`CollisionFlags.Sides` zeroes the pushed axes" clause; D-021's jump gate stays
  in `PlayerMotion` for now — moving it to a UseCase is the second half of this decision, not done yet)
- **Context:** An audit of the simulation found two structural defects and one test-design defect.
  **Vertical launches lived in the push channel**, which the ground zeroes every grounded tick, so a
  *timed* launch (a 2 s hydrant curve, a 0.5 s jump curve) accumulates nothing — the ground eats it tick
  by tick; and the B-003 test was unpassable by construction: `meanLate - meanEarly < 0.1` compares the
  first second of an exponential ramp (τ = 1/AirDrag = 2 s) against the asymptote, which always differs,
  while the cap (12) sat above the force/drag equilibrium (8), so the clamp that **was** implemented was
  never exercised. **`CollisionFlags.Sides` carries no axis**, so wall absorption zeroed both X and Z:
  sliding along a wall stopped the player dead.
- **Decision:**
  1. The push channel is **horizontal only**. All vertical momentum — gravity, terminal velocity, and the
     vertical part of every launch — lives in the vertical channel. A push's delta is split on entry.
  2. A **launch state**: any upward delta arms `IsLaunched`; the ground may reset vertical momentum only
     when grounded **and not launched**; the landing that absorbs a launch ends it. This is what makes a
     timed force physically meaningful.
  3. The shell reports **the contact normal** (`ReportContact(grounded, Vector3 normal)`), taken from
     Unity's `OnControllerColliderHit`, which `Move` fires synchronously inside itself. A wall absorbs the
     push component **along the normal**; the component parallel to the surface is kept. A normal with
     y < 0.5 is a wall, so a walkable slope is not treated as one.
  4. **Test expectations become analytic**: convergence is asserted as "the late window sits on the
     force/drag equilibrium, the series is monotone, the plateau is reached inside 3τ", and saturation is
     asserted by a second scenario where the equilibrium is **above** the cap.
- **Why:** A model where a timer, a channel and the ground all fight for the same vector cannot express
  "shove me for 2 seconds" — the accumulation is the point, and the ground cancels it. `Sides` is a
  boolean, so any rule built on it is guesswork about which axis stopped you. An absolute convergence
  threshold is unpassable at any τ: exponential ramps approach asymptotically by definition, so the
  honest assertion is about the asymptote, not about a difference of two windows.
- **Consequences:** The suite is **20 tests, all passing** (`unity cmd test run --mode editor`, 0.15 s).
  The jump tests read `VerticalVelocity`, not `PushVelocity.y`, and run with gravity off so the launch is
  a clean `Force/Mass`. `PlayerMotor` gains `OnControllerColliderHit` (a Unity callback on the shell, not
  a physics query, so the Domain boundary holds). B-003 is `Verified`. The authoring surface for
  Impulse presets (`Duration`, curve past t=0, `Applies`, `FiredImpulse`) stays in the code, dead, until
  the feel pass - the second half of this decision (`PushDirector` + the four UseCases, the jump gate
  leaving `PlayerMotion`) is the next task.

## D-028: Push DTO split by force shape, direction as a Vector3, presets in one asset
- **Date:** 2026-11-20
- **Status:** Accepted (deletes the push DTO surface of D-016/D-020: `PushPresetData`, `PushKind`,
  `PushApplication`, `Vector3CurveData`; supersedes D-027's stored launch state; the step order, the
  channels, the contact normal, the analytic tests and D-021's jump gate in `PlayerMotion` all stand)
- **Context:** The hydrant launched the player in a direction nobody had authored. Two independent causes:
  **the API takes a Quaternion** (`ApplyForce(preset, _pushDirection.rotation)`) and the envelope
  multiplies it, so with the authored empty X/Y/Z curves the envelope read as `(1,1,1)` and the launch ran
  along the **diagonal** of the nozzle's space; and the authored preset was `Kind = Impulse`, whose
  `Duration` and curve past t=0 are ignored, so the 2 s decay the author typed had no effect. Two Inspector
  fields did nothing at all: `Applies` (dead since D-021's jump gate) and `ActivePush.FiredImpulse` (never
  set). The user's UseCase examples (hydrant 1000 N decaying to 0 over 1 s, jump plateau 0.2 s then fade,
  kick/spray as plain impulses) need per-shape parameter sets, and `Duration` is meaningless for an impulse.
- **Decision:**
  1. **Two DTOs, one per force shape:** `ImpulsePresetData {Type, Force (N·s)}` — no Duration, no curve,
     because an impulse has nothing to time; `SustainedPresetData {Type, Force (N), Curve, Duration}`.
     `PushKind` and `PushApplication` are deleted: the DTO's shape **is** the kind.
  2. **Direction is a Vector3 handed over by the source** (`PushDirection.forward`), never a Quaternion.
     **The envelope is one scalar `AnimationCurve`** (0..1 across the duration), never per-axis.
     `Vector3CurveData` is deleted.
  3. **Two entry points:** `PlayerMotion.ApplyImpulse(preset, direction)` and
     `ApplySustained(preset, direction)`, forwarded by the `Player.ReceivePush` and `PlayerMotor.ApplyForce`
     overloads. The direction is normalized on entry.
  4. **All presets in one authored asset:** `Assets/_Core/Push Presets.asset` (`PushPresets :
     ScriptableObject`), keyed by `PushType`, read through the `IPushPresets` port at the source's **firing**
     moment, never per tick. `IGameSettings.JumpPreset` is gone; the jump preset lives in the asset like
     every other push.
  5. **Vertical absorption is directional, not a state:** the ground sets vertical to `GroundedGravity`
     only when vertical is **downward**. A launch that rises is never eaten by the ground, so a timed force
     accumulates; a force too weak to overcome gravity is absorbed, which is the physically honest case.
     `IsLaunched => VerticalVelocity > 0` — a derived fact, no stored flag.
- **Why:** A rotation times a curve is a direction the author cannot see, and a per-axis "flat" envelope is
  a diagonal. Fields that mean nothing for their kind invite authoring numbers that do nothing — the hydrant
  preset had a decay curve, a duration and a `GroundedOnly` flag, none of which an Impulse reads. Presets in
  one asset keep the shape of one launch (Force, curve, Duration) in one place, so a source stores only the
  key and the direction, and a designer tunes without opening the scene.
- **Consequences:** `HydrantPush` stores `PushPresets` + `PushType` + the child Transform and forwards
  `PushDirection.forward`. DTO fields are private `[SerializeField]` with read-only properties (a field
  cannot implement an interface property), so tests build presets through constructors. The jump is now a
  timed preset, so a jump that lifts needs `Force > Mass * Gravity` (80 · 19.62 = 1569 N) — the feel pass
  tunes the asset, not the code. `GroundedOnly` is gone with its rule, so test T9 is deleted. The suite is
  `unity cmd test run --mode editor` → **19 tests, 19 passed** (T9 deleted with its rule, the jump suite is
  7 timed-launch tests). The suite is re-signed for the two entry points and `StubPresets`. G-018 records the
  diagonal launch, G-019 the CLI limit that a serialized curve must be authored as YAML.

## D-029: One preset list, `PushKind` as the authoring switch
- **Date:** 2026-11-20
- **Status:** Accepted (supersedes D-028's point 1 — the two DTOs; everything else of D-028 stands: the
  Vector3 direction, the scalar envelope, the two entry points, the one asset, `IPushPresets`)
- **Context:** The user's complaint about the authoring surface: "I can make a HydrantPush type version in
  both: impulse, and sustained lists" — two lists keyed by `PushType` let one name be authored twice, with
  nothing stopping the designer from picking the wrong list in the source. The requested shape: one list,
  pick a name, then a type, and the Inspector shows the settings that type can read.
- **Decision:**
  1. **One DTO:** `PushPresetData {Type, Name, Kind, Force, Curve, Duration}`. `PushKind` returns as the
     **authoring switch** — the designer picks Impulse or Sustained, the code never infers it.
  2. **One list:** `PushPresets` holds a single `_presets` array keyed by `PushType` (the enum stays
     load-bearing, 8 values), read as `IPushPresets.PresetOf(PushType)` — one lookup, one name, one preset.
  3. **The Inspector hides what a kind cannot read:** `PushPresetDrawer`
     (`[CustomPropertyDrawer(typeof(PushPresetData))]`, an editor-only adapter) draws Curve/Duration only
     for Sustained and labels Force `N·s` for an Impulse, `N` for a Sustained. Field visibility is an
     editor concern; the runtime branch lives in `TickPushes`, driven by the authored `Kind`.
- **Why:** Two lists keyed by the same key is two sources of truth for one launch; a name chosen twice is a
  bug that shows up as "the hydrant fired the wrong shape". Authoring the kind once, in the preset, makes
  the runtime branch a read of an authored fact, and the drawer makes an unreadable field invisible instead
  of a trap (G-018's lesson: fields a kind cannot read must not be authorable).
- **Consequences:** `ImpulsePresetData` and `SustainedPresetData` are deleted; `Presets.Impulse`/
  `Presets.Sustained` builders and `StubPresets.PresetOf` are re-signed to the single DTO. The asset is
  re-authored as one list (`Push Presets.asset`, GUID unchanged, so the scene references survive):
  HydrantPush Sustained 1000 N curve 1→0 dur 1 s, ExtinguisherShove Sustained 300 N flat dur 0.1 s,
  PlayerJump Sustained 2000 N keys 1@0/1@0.2/0.5@0.7/0@1 dur 1 s, ExtinguisherKick Impulse 300 N·s. The
  drawer is a shell in `InterfaceAdapters` — no domain file is editor-only.

## D-030: The ground is a normal force; reactive facts stay reactive
- **Date:** 2026-11-20
- **Status:** Accepted (supersedes D-028's point 5 — the sign-only vertical rule; `IsLaunched` as a derived
  fact, the wall rule, the channels, the step order and the analytic tests all stand)
- **Context:** After D-029 the jump was still invisible and the hydrant pushed only X/Z. The measured facts:
  the jump request reaches the domain (`jump=True`), the push enters the active list (`pushes=1`), and the
  ground resets vertical to `GroundedGravity` every tick — because the launch starts from the **negative**
  grounded rest value, so the sign rule ate the launch on its first ticks (G-020). The first fix — "no
  absorption while any push points up" — is a crutch: force-blind, so a 300 N jet is never absorbed and the
  body accelerates down through the ground. The user's rule on the same session: reactivity is preferred,
  polling only for a stated reason; and a fix must be judged from the outside for crutch-ness.
- **Decision:**
  1. **Vertical absorption is the normal force:** the ground sets vertical to `GroundedGravity` when
     grounded, vertical is downward, **and** the live vertical force of the Sustained pushes is
     ≤ `Mass * Gravity`. Above weight the contact is broken and the launch accumulates; below weight the
     contact cancels the push, which is the honest case (a weak jet cannot lift you). Impulse pushes are
     not in the force sum — an impulse lands as velocity, and the sign rule covers velocity.
     `LiveVerticalForce()` is derived from the active list, never stored.
  2. **Reactive-first** and **the crutch rule** are recorded in ARCHITECTURE.md §Reactive first and
     §Fixing a bug: events over polls (a poll needs a stated reason), and a domain crutch is allowed only
     in sketches with a TODO line.
- **Why:** The sign is not the rule; the force behind the sign is. A ground that can push only up absorbs
  downward momentum when it is not being exceeded — that is textbook normal force, and it makes both the
  weak-jet case and the launch case fall out of one rule instead of two guards. Polling a sphere around a
  sensor would have replaced a working `OnTriggerEnter` with a per-tick query: the user is right that it is
  over-engineering, and the push shell stays reactive.
- **Consequences:** `RunningLaunch` is replaced by `LiveVerticalForce()`; `_mass` is read once in the
  constructor (the inverse mass was already read there). Authoring consequence: a launch that lifts needs
  `Force > Mass·Gravity` (80 · 19.62 = 1569 N; a nozzle at 45° needs ≈ 2220 N), so the hydrant at 1000 N is
  a shove by design, not a launch. The suite stays **19/19 green** (`unity cmd test run --mode editor`); the
  launch-survives-ground invariant is a test to write after the feel pass, together with the rest.

## D-031: The preset list is `[SerializeReference]` - the DTO's type is its shape
- **Date:** 2026-11-21
- **Status:** Accepted (supersedes D-029's points 1 and 3 - the single DTO and `PushPresetDrawer`; the one
  list, `IPushPresets`, `PresetOf`, the Vector3 direction, the scalar envelope, the two entry points and the
  measured numbers all stand, and D-028's DTO split is restored)
- **Context:** The authoring surface D-029 built does not work in the Inspector: `PushPresetData` is a
  `[Serializable]` **class**, and adding an element to a class array gives a **null slot** that no drawer can
  fill - the entry draws as an empty box and every number the designer typed was lost (G-021). The user asked
  for `[SerializeReference]` ("a list where the entries are objects of different types") and offered a
  drawer-with-flags approach as the fallback. A flag whose only job is to hide fields a shape cannot read is
  the crutch D-028 had already named.
- **Decision:**
  1. **The base is abstract:** `PushPresetData {Type, Name}` with abstract `Force` and abstract
     `IsImpulse`; `ImpulsePresetData {Force}` (N*s, **no** Duration and **no** curve fields at all) and
     `SustainedPresetData {Force, Curve, Duration}` (N, envelope, duration). The shape is the **type** of the
     DTO, never an authored flag.
  2. **The list is `[SerializeField, SerializeReference] PushPresetData[]`** in `PushPresets`: Unity's
     Inspector draws a concrete-type dropdown per entry, instantiates the instance itself and shows exactly
     the fields the chosen type has. `PushKind` and `PushPresetDrawer` are deleted. The drawer's unit labels
     (`N·s` vs `N`) move to `[Tooltip]` on each authored field, so the Inspector states the unit without a
     drawer. The attribute must be **in the file** — a doc-comment that promises it is not the feature
     (G-021).
  3. **The runtime branch is a type check:** `TickPushes` asks `IsImpulse` and casts to
     `SustainedPresetData`; `EnvelopeAt` moves from `PlayerMotion` to the DTO that owns the curve.
  4. **The asset is authored through Unity** (`script eval` writing `managedReferenceValue`, then
     `ApplyModifiedProperties` + `SetDirty`), never as hand-written YAML: a SerializeReference block needs
     `typeInfo` and `fileID`s that Unity generates. Numbers stay as measured: HydrantPush Sustained **4500 N**
     curve 1->0 dur 1 s, ExtinguisherShove Sustained 300 N flat dur 0.1 s, PlayerJump Sustained **2600 N**
     keys 1@0/1@0.2/0.5@0.7/0@1 dur 1 s, ExtinguisherKick Impulse 300 N*s.
- **Why:** A list of variants is data, and the Inspector is the author's interface: data that cannot be
  created there makes the design unusable, however correct the code is. SerializeReference makes Unity
  instantiate what the author picks, so field visibility stops being a rule in code and becomes a fact of the
  type - an Impulse has no Duration to hide, because it has no Duration. The builders the tests use
  (`Presets.Impulse` / `Presets.Sustained`) return these same classes, so the asset and the tests author one
  shape.
- **Consequences:** `PushKind.cs` and `PushPresetDrawer.cs` are deleted with
  `unity cmd asset delete --asset <path> --confirm true`; `Presets.cs`, `StubPresets.cs`, `PlayerMotionTests`
  and `PlayerJumpTests` are re-typed to the concrete DTOs. New `.cs` files must be written **through Unity**
  (`asset write_text`): with `autoRefresh: disabled` an external file is never imported as a script and the
  compile reports `CS0246` (G-022). The asset's GUID is unchanged, so the scene references survive; the gate
  after the change is `script recompile` → `up_to_date` and `test run --mode editor` → **19/19**.

## D-032: A preset has no Name - `Type` is the whole authored identity
- **Date:** 2026-11-21
- **Status:** Accepted (supersedes D-031's `Name` field and D-029's `Name` label; the rest of both entries
  stands: `[SerializeReference]`, the two DTOs, `PresetOf`, the unit tooltips, the measured numbers)
- **Context:** The user's read of the Inspector: "There is an unnecessary field: Name. Let's use only Type and
  remove the name." The check that makes it true: `PresetOf(PushType)` is the only lookup in the code, nothing
  prints a preset's `Name`, and the scene's sources store `PushType` - so the label was a second identity for
  one entry, authored by hand and read by nobody.
- **Decision:**
  1. **`PushPresetData` holds `Type` only.** `Name` and its tooltip are deleted; the constructors take
     `(PushType type)` and the DTOs take their own fields.
  2. **The asset is re-authored through Unity** with the new constructors, so the `_name` keys leave the YAML.
     Numbers unchanged: HydrantPush 4500 N curve 1->0 dur 1 s, ExtinguisherShove 300 N flat dur 0.1 s,
     PlayerJump 2600 N keys 1@0/1@0.2/0.5@0.7/0@1 dur 1 s, ExtinguisherKick Impulse 300 N�s.
  3. **No value tuning by test sweeps.** The gate is the one standing run (`script recompile` ->
     `up_to_date`, `test run --mode editor` -> 19/19). Preset numbers move from the **user's play** (the
     `[v]` log is read afterwards), never from a search over parameters: the user's measurement is that
     "running many tests to find the best preset values is very time consuming".
- **Why:** A field that no code reads is authoring overhead, and in a keyed list it invites the designer to
  believe two entries can share a name while the lookup uses the key. The name of a push is already the
  `PushType` value the source stores.
- **Consequences:** `Presets.Impulse` / `Presets.Sustained` builders lose the `type.ToString()` name argument;
  the asset's YAML shrinks one line per entry. The suite stays **19/19** (`unity cmd test run --mode editor`)
  on the same day, and the feel numbers keep coming from the user playing, not from parameter search.

## D-033: Dead surface is deleted - one push entry point, `IPushPreset` with its facts, 4 `PushType` values
- **Date:** 2026-11-21
- **Status:** Accepted (the user's rule: "Clean all the dead fields if they present". Retires D-029's two
  shape-named entry points, the empty `IPushPreset`, four `PushType` values with no source, the `Burnable`
  body, and the temporary `[v]` diagnostic log. The DTO shapes, `PresetOf`, `[SerializeReference]` and the
  authored numbers of D-031/D-032 stand.)
- **Context:** After the preset authoring worked, the whole surface was read through once: every
  `[SerializeField]`, every private field and every public accessor in `_Core` was checked for a reader. The
  GameSettings numbers all have one. What did not: `ApplyImpulse` and `ApplySustained` (one body, two names),
  `LiveVerticalForce` (public, read only by `ApplyContact`), `IPushPreset` (declared `Type` only, and no code
  was ever typed as it), four enum values nothing fires, `BurnableSystem` (an empty class) with `Burnable`
  (a private list nothing touched and fields with no readers), and the `[v]` log in `PlayerMotor`, whose job
  was the feel measurements.
- **Decision:**
  1. **`PlayerMotion.ApplyPush(preset, worldDirection)` is the single entry point.** The shape is the DTO's
     type (D-031), so an entry point cannot branch: two methods named by shape are a second name for one
     behavior. `Player.ReceivePush` forwards to it; `PlayerMotor.ApplyForce` forwards to that.
  2. **`LiveVerticalForce()` is private.** Its only reader is the ground's normal-force rule; the `[v]` log
     that read it for the feel pass is removed, and the numbers it gave are already in `TODO.md`.
  3. **`IPushPreset` gains `Force` and `IsImpulse`.** The interface previously promised only `Type` and no
     consumer used it, so it is now exactly the three facts `TickPushes` and the ground's rule read.
  4. **`PushType` keeps `HydrantPush`, `ExtinguisherShove`, `PlayerJump`, `ExtinguisherKick`.**
     `DebrisShove`, `FireBlast`, `HydrantBurst`, `SprayPuff` are deleted: no source fires them and no asset
     entry is keyed by them. The indices of the four kept values are unchanged, so the `_type` ints already
     authored in `Push Presets.asset` survive, and the cap tests, which use `(PushType)i` casts for 9 pushes,
     are unaffected.
  5. **`BurnableSystem` and `Burnable` are deleted; `IBurnable` stays** as the authored contract of D-008
     pillar 1 (the implementation is the Phase 1 work in `TODO.md`). A class whose fields are read by nobody
     is authored overhead.
- **Why:** A field with no reader tells the designer it is read, and a second name for one behavior is a
  second rule. Deleting them costs nothing and removes the claim.
- **Consequences:** `PlayerMotionTests` call `ApplyPush` (11 sites), the suite stays **19/19**
  (`unity cmd test run --mode editor`), `PlayerMotor` is log-free, and the enum's named values are the sources
  that exist. The asset's authored jump is the **user's** tuning (an Impulse 700 N�s in the Inspector), not a
  number I re-wrote.

## D-034: A jump carries the player's speed as momentum, not as a tilt of the launch
- **Date:** 2026-11-22
- **Status:** Accepted (the user's rule: keep the speed influence, do not spoil the vertical launch — a
  running jump must not be lower than a standing one, and the slower you run the less it carries you).
  Supersedes nothing: the jump gate (D-017, D-021), the authored preset (D-029, D-031, D-033) and the three
  channels (D-016) all stand.
- **Context:** The user wanted the jump to carry the player in the direction of movement with the force of
  their speed, and drafted `direction = Vector3.up + _controlVelocity.normalized *
  (_controlVelocity.magnitude / _movement.DesiredVelocity.magnitude)`. Reading it gave three facts:
  (1) standing still is `0/0` = **NaN**, and `AddActivePush`'s `direction.sqrMagnitude > 0.0001f` guard
  compares **false** against NaN, so the direction became `Vector3.zero` — the entry was created, the launch
  had no direction, **the jump did not fire, and nothing reported it** (G-023);
  (2) the direction is normalized inside the entry point and the impulse magnitude is authored, so tilting it
  at full speed splits the launch 45°/45°: the vertical launch drops by √2 — **a running jump jumps lower
  than a standing one**;
  (3) the ratio is *proximity to the authored target speed*, not speed: walking (5) and sprinting (8) both
  reach 1, so a run-up buys nothing.
- **Decision:**
  1. **The preset launches straight up, untouched:** `ApplyPush(PresetOf(PlayerJump), Vector3.up)`. The
     authored `Force` is the launch, so height stays the number the user tunes in the Inspector and no speed
     rule touches it.
  2. **The player's own speed is carried into the push channel as inertia** in the same tick:
     `AddPushDelta(_controlVelocity * JumpCarry)`. `JumpCarry` is authored in `IGameSettings`/`GameSettings`,
     default **1**: a sprint jump carries the whole 8 m/s, a walk jump carries its 5, standing carries exactly
     zero. The direction of the carry is the direction of movement, so it needs no facing.
  3. **The carry is a velocity in the push channel, not a force:** `MaxPushSpeed` bounds it, `AirDrag` decays
     it through the flight, `GroundDrag` absorbs it on landing — the rules every shove already obeys (D-016,
     G-012). No new list entry and no `Carry` field on the DTO: an authored preset cannot hold a magnitude
     derived from a live velocity, and a field 3 of 4 presets cannot read is authored overhead (D-028, D-031).
- **Why:** A tilt trades height for reach, adds a knob the user cannot author, and **punishes the run-up** —
  the opposite of what the mechanic is for. Carrying the velocity is the physical fact: nothing opposes
  horizontal motion at the moment of launch, so the model's own channel rules decide what survives it.
  Multiplying by zero is not a division, so the degenerate case (standing) needs no guard.
- **Consequences:** `TryJump` is two statements and reads no ratio; `_movement.DesiredVelocity` plays no part
  in the jump. One authored number (`JumpCarry`) is tuned by the user playing, not by a parameter sweep
  (D-032). The jump tests drive `_input.InputDirection = 0`, so the carry is zero there and the suite is
  unchanged. After the user played and approved the feel (2026-11-22), **T14a-T14e** assert it: standing
  carries zero, `PushVelocity == control * JumpCarry` on the jump tick, walk 5 vs sprint 8, a carry above
  `MaxPushSpeed` saturates at the cap, and the carry outlives the jump tick decaying only by `AirDrag` —
  `unity cmd test run --mode editor` → **24 tests, 24 passed**.
