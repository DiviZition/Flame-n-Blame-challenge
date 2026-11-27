# DECISIONS.md — Decision Log

Two jobs: §Current structure (the state map, rewritten freely) and the log D-001…D-054 — the *why* of non-obvious calls, so they are not re-litigated.

**Rules:** one entry = `## D-xxx: title` + `**Date · Status · supersede**` + `Decision:` + `Why:`, **≤ 10 lines per entry, ≤ 400 for the file, ≤ 320 chars per line.** Log only the non-obvious: obvious, reversible, no system impact → not logged. **Entries are editable** — Decision and Why state the
rule as it stands today, history is in git (`git log --follow docs/DECISIONS.md`), a supersede is a name in the Status line, never a retelling. **Never in this file:** measured numbers, gate outputs, test counts, file lists, CLI commands, scene coordinates, probe values — they go to the commit body
and `TODO.md`.

---

## Current structure (state map, rewritten freely)

**Assemblies:** `Assets/_Core/FlameNBlame.asmdef` (runtime, `rootNamespace: Flame_n_Blame_Challenge`; its `name` field is the typo `FlameNBlame-asmdf` — TODO; carries the `CartoonFX` vendor-demo reference, G-001) and `Assets/_Core/Tests/FlameNBlame.Tests.asmdef` — **editor-only**, so the suite runs
as **EditMode** on code (D-026).

**Layers ↔ folders ↔ namespaces** (`Assets/_Core/Scripts/`): Composition `Flame_n_Blame_Challenge` at the root; UseCases `.UseCases`; Domain `.Domain` in `Domain/`; Adapters `.Adapters` in `InterfaceAdapters/` (D-014, D-012).
Inside `Domain/` and `InterfaceAdapters/`, entity subfolders `Player/`, `Burnables/`, `Push/` group the scripts; Adapters also has `Core/` (shared ports, settings, spawn). Namespaces unchanged.

**Domain:** `Player`, `PlayerMotion`, `PlayerMovement`, `PlayerLookRotator`, `ActivePush`, `PushType`, `IPushPreset`, `ImpulsePresetData`, `SustainedPresetData`, `PushPresetData`, `BurnableSystem` (`IBurnable`, `BurnableObject`, `IBurnablePreset`, `BurnablePreset`), `FireSpreadSystem`, `IHeatSink`.

**Adapters:** ports `IUpdatable`, `IPlayerInput`, `IGameSettings`, `IPhysicsSettings`, `IControlSettings`, `IPushPresets`; `PlayerInputMain`, `GameSettings`, `PhysicsSettings`, `PlayerControlSettings`, `PushPresets`, `ResourceSpawner`, `ResourcesProvider`, `PlayerMotor`, `HydrantPush`,
`BurnableShell`, `SmokePuffPool`, `ItemSpotsManipulator`, `GameplayUiShell`, `UiButtonFeel`. **UseCases:** `GamePlaySession`, `GameFlow` + `GameFlowState` (plain C#, no Unity types, D-053).

**Authored assets:** `Game Settings.asset` (two references, no numbers), `Physics Settings.asset` (mass, gravity, ground rate, drags, cap, jump carry), `Player Control Settings.asset` (speeds, accelerations, look), `Push Presets.asset` (`[SerializeReference]` list keyed by `PushType`),
`Resources.asset`.

**Who talks to what:** `Bootstrapper` — in the boot scene, `DontDestroyOnLoad`, the one `Update()` — loads the gameplay scene and builds the session when that scene reports ready, resolving `SpawnPoints` once there (D-052),
ticks `PlayerInputMain` every frame with no state check, gates the session graph on `GameFlow.State`, drives the flow from the ESC press fact, and flushes the staged removal at teardown (D-053, G-044) →
`Updater` → registered `IUpdatable` in registration order `Player → PlayerShell` — the shell forwards in order `Motor → ItemsHandler → ItemSpots`, so the pivots read the camera pose written this tick (D-058) — `→ BurnSystem`,
and inside the burn system `FireSpreadSystem → BurnableObject + BurnableShell pairs` (D-013, D-048, D-055). `Player` composes `PlayerMotion(PlayerMovement, IPhysicsSettings, IControlSettings, IPushPresets)`, forwards `ReceivePush(preset, direction)` and `ReportContact(grounded, normal)`,
exposes `Velocity`; `PlayerMotor` shells `Move(Velocity * dt)` and the contact normal from `OnControllerColliderHit`; `HydrantPush` shells a trigger → `ApplyForce(preset, PushDirection.forward)`. A source decides **whether** to fire, `PlayerMotion` what a fired push does (D-016). `BurnableShell` is the
sink `FireSpreadSystem` streams into (`IHeatSink`), copies `Charredness` → `_Burn` and `FireLevel` → `_Fire` into the shared material, puts `_BurnOrigin` on the mesh face the strongest burning neighbour stands on, writes
`_BurnExtent` once at the ignition, drives the fire and smoke particle chains (D-051), and reads the sign of `ApplyStream(amount, contact)` (D-046, D-055). The **one** `SmokePuffPool` and the `PlayerShell` arrive from
`Resources.asset` through `ResourceSpawner`, injected into the session and its shells (D-048, G-040).
`GameplayUiShell` reacts to `GameFlow.StateChanged` (window, restart button, open/close/hover/press tweens) and forwards button clicks to the flow's requests (D-054).

**Boot scene `Assets/_Core/Scenes/FnB_Boot.unity`:** the root `Bootstrapper`, with `Ui` (Canvas – Screen Space Overlay, CanvasScaler, GraphicRaycaster, `GameplayUiShell`) → `PaperPanel` (Image + CanvasGroup) → `TaskText`,
`ButtonStart`, `ButtonRestart` (each with `UiButtonFeel`), and `EventSystem` (`InputSystemUIInputModule`, G-043) — all children of the persistent root, so the UI survives every reload (D-054). No camera, no light.
It is build index 0, `FnB_Gameplay` index 1, the stale `SampleScene` entry is gone (G-003 closed).

**Scene `Assets/_Core/Scenes/FnB_Gameplay.unity`:** `=== System` → `BenchMark` + Main Camera (CinemachineBrain); `=== World` → light + `GrayBox Level` (Ground, `Hydrant` with `HydrantPush` and `PushDirection` pitched −45°, `Slope`, `Reference Cube`), `++ Spawn Points` (`SpawnPoints` +
`Player Spawn Point`); six root `Burnable` props carry `BurnableShell` with fire/smoke particle chains, and `BenchMark` sprays them. **The `PlayerMotor` clone is spawned at the prefab's own origin, not at the spawn point (B-004).**

**Strays:** `Player.cs` at repo root (untracked copy of `Domain/Player.cs`) and `X Bot@Falling.fbx` in the code folder — housekeeping (TODO).

---

## D-001: Unity 6 + URP
- **2026-11-15 · Accepted · the PC+mobile-profile premise retired by D-009**
- **Decision:** Unity 6000.6.5f1, URP 17.6.0.
- **Why:** Unity 6 LTS, VFX Graph available, URP covers the shipped profiles; HDRP overkill, Built-in stagnant.

## D-002: Input System package only
- **2026-11-15 · Accepted · `activeInputHandler: 1`**
- **Decision:** Unity Input System 1.20.1 only; actions in `InputSystem_Actions.inputactions`, regenerated, never hand-edited.
- **Why:** Device abstraction and action maps for free; legacy `Input.*` is a deprecated path — never call `Input.GetAxis/GetKey`.

## D-003: All game code under `Assets/_Core`, one asmdef
- **2026-11-15 · Accepted**
- **Decision:** `Assets/_Core/{Art,Scenes,Scripts}` is the only place game code goes, in one runtime assembly.
- **Why:** Vendor folders get overwritten by updates; one assembly keeps compile low and the dependency direction explicit. New vendor references are added explicitly, editor-only code gets its own asmdef (G-001).

## D-004: PrimeTween for tweens, UniTask for async
- **2026-11-15 · Accepted**
- **Decision:** PrimeTween for all tweening, UniTask for all async. No DOTween, no `StartCoroutine`.
- **Why:** Allocation-free, no coroutine spaghetti, integrates with the player loop.

## D-005: Feel 6.3 for juice
- **2026-11-15 · Accepted · the haptics half dormant since D-010**
- **Decision:** `MMF_Player` feedbacks from `Assets/Feel` for screenshake, particles, sound, time.
- **Why:** A battle-tested library beats hand-rolling each effect. Custom code stays out of `Assets/Feel/` (G-006), Feel's `SKILL.md` is read before Feel work (G-004).

## D-006: Root-level Markdown as project memory
- **2026-11-15 · Superseded by D-011**

## D-007: WebGL is the shipping target from day one
- **2026-11-16 · Accepted · the "web build every session" clause suspended by D-019**
- **Decision:** WebGL is the reference target for every system: a written perf budget **before** code, fire + particles + physics sharing one budget, no haptics at all.
- **Why:** Retrofitting web is the known failure mode for this scope — gameplay code is effectively single-threaded under wasm, so the budget is decided before the systems are authored. "Works in the editor" is not done.

## D-008: Scope locked to the four pillars
- **2026-11-16 · Accepted**
- **Decision:** (1) FPS controller with move/look/jump, `E`/LMB interaction, one carry slot; (2) object-based fire — burnables ignite, blacken, collapse, are extinguished, re-ignite neighbours after ~10 s of proximity; (3) an extinguisher that extinguishes **and** applies force; (4) the hydrant
  catapult. Win: the house is out and the player walks out.
- **Why:** The hydrant gives the biggest read-at-a-glance payoff for almost no animation cost, which is what judging rewards; a trampoline needs ~9 animations, ragdoll, IK. A fluid/grid fire is not web-viable (D-007): state-based fire is O(burnables), not O(cells). Deferred: scoring, co-op,
  destruction, ragdoll/IK, the dam. Outside the pillars → a new entry. **The design contract files are not in the repo** (README): cite pillars through this entry, never "TDD §n".

## D-009: One render pipeline asset, named for the shipping target
- **2026-11-16 · Accepted · renaming rebinds nothing: B-001, G-010**
- **Decision:** A single active pipeline named for the target we ship, not for a hardware class; quality levels re-cut for web (TODO).
- **Why:** One pipeline removes drift between what we author and what we ship; naming by target makes that drift visible the moment someone adds a second "just for testing" asset. Renaming bought a name, not a retune.

## D-010: Nice Vibrations kept, dormant
- **2026-11-16 · Accepted**
- **Decision:** `Assets/Feel/NiceVibrations` stays, unreferenced by our code; never called, never in an `MMF_Player`.
- **Why:** Removing an asset-store folder can dangle GUIDs inside Feel's prefabs and `.meta` files for zero gain (G-011).

## D-011: Project documentation lives in `docs/`
- **2026-11-16 · Accepted · supersedes D-006 · routing re-cut 2026-11-25 (see `docs/README.md`)**
- **Decision:** `docs/` holds the memory set; repo root keeps `README.md` (what the project is) and `CLAUDE.md` (what an agent loads).
- **Why:** One folder for "what we know", two root files for "what this is". `CLAUDE.md` **must** stay at root — it is auto-loaded from there only (G-009).

## D-012: Game logic as plain classes, Unity kept at the edge
- **2026-11-17 · Accepted**
- **Decision:** Layers `Domain` (state + rules), `UseCases` (the session), `Adapters` (ports + Unity-side implementations), one composition root (`Bootstrapper`) — the only scene object that knows concrete types. MonoBehaviours are the composition host and thin shells that forward what they observed
  and hold no rules; authored values are copied out of the Inspector into plain data objects passed to constructors, Unity math types cross the boundary freely.
- **Why:** Ordering, cost and testability become visible in code, not in scene wiring a diff cannot show. MonoBehaviour-per-feature makes execution order a script-execution-order setting, makes a half-built graph legal, and pays a per-object scheduling cost whether the body is empty. Rejected as
  overkill for 8 days: a Unity-free domain with our own math types, DI/ECS.

## D-013: One tick we own, registration order is the contract
- **2026-11-17 · Accepted · the fixed-step half superseded by D-018**
- **Decision:** Exactly one `Update()` — `Bootstrapper` → `GameUpdater.Update(time, dt)` → every registered `IUpdatable` **in registration order**; dedupe on register, removal through a staged queue with swap-remove.
- **Why:** One place to pause, step, measure, throttle a system. N `MonoBehaviour.Update` bodies pay a marshalling cost visible only in a web build; registration order is reviewable in a diff, unlike script execution order. Getting it wrong is a silent gameplay bug, not a compile error.

## D-014: Namespace root `Flame_n_Blame_Challenge`, namespaces by layer
- **2026-11-17 · Accepted · supersedes the naming in STYLE.md §Namespaces and the G-002 example**
- **Decision:** Root `Flame_n_Blame_Challenge` + `.Domain` / `.UseCases` / `.Adapters`, matching the asmdef `rootNamespace`; `FlameNBlame` is the assembly name, never a namespace; folder names may name the pattern (`InterfaceAdapters/` → `.Adapters`).
- **Why:** It compiles today and the editor auto-inserts it, so the convention holds itself. Namespaces by feature would let `…Fire` reference `…Player` invisibly; by layer the dependency direction is legible from a `using` line.

## D-015: `ARCHITECTURE.md` is the code map, DECISIONS stays the log
- **2026-11-17 · Accepted · extends D-011**
- **Decision:** ARCHITECTURE holds the shape (layers, dependency direction, tick ownership, the Unity boundary, the checklist) and no rationale; DECISIONS holds rationale only, and wins over ARCHITECTURE.
- **Why:** Append-only is right for decisions and wrong for a picture; a reader assembling the shape from five entries assembles it differently each time. The boundary is explicit so neither file sprawls.

## D-016: Player pushes go through the domain; contact, not the timer, absorbs momentum
- **2026-11-19 · Accepted · decay filled by D-020 · channels refined by D-027 · DTO surface by D-028**
- **Decision:** The domain owns velocity. Every push enters through `Player.ReceivePush(preset, sourceDirection)`, the direction from the **source**, never the player's facing. `Duration` ends the **adding**, never the velocity: momentum persists, decays by the authored rate, saturates at
  `MaxPushSpeed`, is absorbed by contact. Replace-per-`Type` keeps one entry, so a 0.1 s spray interval leaves one entry, not 30.
- **Why:** A rigid body never stops when a force's timer ends, and `CharacterController.Move` stores nothing — momentum must be ours. Rejected: the report-only variant (motion rules in an adapter, the exact violation being removed) and a single velocity vector (steering eats a launch back toward
  input intent).

## D-017: Jump is one of the pushes
- **2026-11-19 · Accepted · gate's home moved by D-021 · press mechanism replaced by D-039**
- **Decision:** Jump is a `PushPresetData` applied through the same `ReceivePush` path as hydrant and spray.
- **Why:** One mechanism for jump, hydrant and spray shove = one place to tune, test, and find the bug. If jump cannot be expressed through the push entry point, the push model is wrong. Tests assert launch as `Force / Mass` — authored, not coded.

## D-018: No fixed step for now
- **2026-11-19 · Accepted · supersedes the `IFixedUpdatable` clause of D-013**
- **Decision:** Integration is dt-driven on the single `GameUpdater` tick, the fixed-step host is not built; frame-rate independence is asserted at `dt = 1/60, 1/30, 1/120` within 5%, permanently.
- **Why:** A fixed-step host is a second loop, ordering contract and measurement place for a cost the push model does not need — its only dt-sensitivity is integration error, and the tests bound it. Re-opens only as a new decision.

## D-019: Web builds deferred to a dedicated pass
- **2026-11-19 · Accepted · suspends D-007's per-session web build; budgets-before-code stands**
- **Decision:** Systems are built and verified in the editor; the web measurement happens in one dedicated pass after B-001 is fixed. The honest status line for any commit in this phase is **editor-verified, web-unverified**, written in the commit body.
- **Why:** A web build today measures a target with no render pipeline bound (B-001), so it buys no information every session. D-007 stays true as the *final* standard.

## D-020: Two decay rates, one per contact state
- **2026-11-19 · Accepted · the ground half superseded by D-038 · the air rate stands**
- **Decision:** The push channel decays at `AirDrag` airborne; ground friction acts in the ground plane only, never on the vertical, which contact and gravity own.
- **Why:** Ground contact is what absorbs momentum (G-012), the question was only *how fast* — authored, not derived. Not zeroing the shove on landing: a hard switch removes the skid, the feel stops being tunable, and a landing and a wall become indistinguishable.

## D-021: The jump gate lives in `PlayerMotion`, `Player` is composition and forwarding only
- **2026-11-19 · Accepted · supersedes the home of D-017's gate**
- **Decision:** The jump **rule** lives in `PlayerMotion`, next to the two facts it needs (contact, the push channel); the **request** is exposed by `PlayerMovement`; `Player` keeps the composition, `Velocity`, the two forwards.
- **Why:** One owner per rule; a rule split across two classes with a one-tick handshake is a bug magnet, and `PlayerMotion` already reads `PlayerMovement`, so the gate costs no new dependency. A `UseCases` source class is taken **where a source has scenario logic** (aim, hit test, interval), not
  for a gate.

## D-022: Unity CLI is the editor-automation interface, the MCP bridge stays unwired
- **2026-11-20 · Accepted · the skills-install clause superseded by D-023, D-025**
- **Decision:** The Unity CLI against the running editor is **the** way an agent drives this project (`unity status` first, then `unity cmd` / `unity test` / `unity build`); the MCP bridge is in no client.
- **Why:** 54 MCP tools on a path Unity deprecated duplicate what `unity cmd` does, and the relay is fragile by hand (G-013). The CLI reaches the warm editor, so the compile gate stops paying cold start, and reaches the Player runtime too.

## D-023: CLI-only toolchain, the agent speaks Russian
- **2026-11-20 · Accepted · the "exactly 2 skills" clause superseded by D-025; CLI-first and Russian stand**
- **Decision:** The CLI is the toolchain, skill manuals are deleted rather than maintained; the agent answers in Russian, 5–10 lines, no wide tables, no option menus.
- **Why:** Adverting 36 manuals spends context in every session and 35 describe systems outside the locked pillars (D-008).

## D-024: Docs re-scoped — one job per file, bugs live in TODO
- **2026-11-20 · Accepted · supersedes the routing rules of D-011 and D-015; routing re-cut 2026-11-25**
- **Decision:** One responsibility per file; `BUGS.md` deleted, its rows into TODO as `- [ ] B-xxx`, done when the test passes; state moved out of `CLAUDE.md` into DECISIONS §Current structure.
- **Why:** A doc mixing state with behaviour is read every session and spends context on facts that change daily; a doc retelling decisions makes two files argue. The user does not want to track the docs himself, so each file answers exactly one question.

## D-025: Zero Unity skills — the CLI documents itself
- **2026-11-20 · Accepted · supersedes D-023's "exactly 2 skills"**
- **Decision:** No Unity skills in the repo or in the global folder; Feel's 8 folders stay in the vendor folder, unadvertised, read directly (G-004). `unity skill install` is a per-task exception, never a habit.
- **Why:** `unity cmd` prints every command with its description, `--query` searches, `--help` prints parameters, and the non-obvious facts are already in CLAUDE.md §Commands and G-014/G-015. A manual duplicating help output goes stale against the CLI binary.

## D-026: Tests — on code, on Domain/UseCases/invariants, after the mechanic is approved
- **2026-11-20 · Accepted · rewrites the test process in CLAUDE.md and STYLE.md · the suite's frequency changed by D-050**
- **Decision:** Suite runs **EditMode on code** (`unity cmd test run --mode editor`), the test asmdef editor-only. Tested: Domain, UseCases, **invariants**; **never Adapters**. Written **after** the user has played and approved the mechanic. A test runs once, under 90 s.
- **Why:** Feel is judged by the user's eyes, so a test cannot say a launch feels right; asserting an unapproved mechanic is writing a specification the user is about to change. A test on a shell asserts Unity, Play Mode pays a scene load for code that touches no Unity, re-running buys nothing.

## D-027: Simulation split — horizontal shove channel + vertical launch channel, contact by normal
- **2026-11-20 · Accepted · launch-state rule refined by D-030 · the UseCase split is the unbuilt half**
- **Decision:** The push channel is **horizontal only**; all vertical momentum (gravity, terminal velocity, the vertical part of every launch) lives in its own channel, so a timed force can accumulate. The shell reports the **contact normal**; a wall absorbs **along the normal**, keeps the parallel
  component; `y < 0.5` is a wall. Test expectations become analytic: the late window on the force/drag equilibrium, the series monotone, the plateau inside 3τ, plus a scenario with the equilibrium **above** the cap.
- **Why:** A model where a timer, a channel and the ground fight for the same vector cannot express "shove me for 2 seconds". `CollisionFlags.Sides` is a boolean with no axis, so any rule built on it is guesswork; an absolute convergence threshold is unpassable at any τ.

## D-028: Push DTO split by force shape, direction as a Vector3, presets in one asset
- **2026-11-20 · Accepted · point 1 superseded by D-029, restored by D-031; the rest stands**
- **Decision:** Two DTOs, one per force shape: `ImpulsePresetData {Type, Force (N·s)}` — no Duration, no curve; `SustainedPresetData {Type, Force (N), Curve, Duration}`. Direction is a **Vector3 handed by the source**, never a Quaternion; the envelope is one scalar curve over progress.
- **Why:** `Quaternion × AnimationCurve` with an empty curve evaluates `(1,1,1)`, so the launch ran along the nozzle's diagonal — a direction nobody authored (G-018). `Duration` is meaningless for an impulse, so fields a shape cannot read must not be authorable.

## D-029: One preset list, keyed by `PushType`
- **2026-11-20 · Accepted · the single DTO and the drawer superseded by D-031, `Name` by D-032; the one list stands**
- **Decision:** `PushPresets` holds one `_presets` array read as `IPushPresets.PresetOf(PushType)` — one lookup, one name, one preset. Field visibility is an Inspector concern, not a runtime flag.
- **Why:** Two lists keyed by the same key are two sources of truth for one launch, with nothing stopping the designer from picking the wrong one; a flag whose job is to hide fields a shape cannot read is the crutch D-028 had already named.

## D-030: The ground is a normal force, reactive facts stay reactive
- **2026-11-20 · Accepted · supersedes the sign-only vertical rule; recorded in ARCHITECTURE §Reactive first and §Fixing a bug**
- **Decision:** The ground sets vertical to `GroundedGravity` when grounded, vertical is downward, **and** the live vertical force ≤ `Mass · Gravity`; above weight the contact breaks and the launch accumulates, below weight the contact cancels the push — honest, a weak jet cannot lift you.
  `LiveVerticalForce()` is derived, never stored, **private**. Prefer reactive facts (Unity events); a poll needs a stated reason.
- **Why:** The sign is not the rule; the force behind the sign is. "Absorb vertical when it points down" kills every timed launch because the grounded rest value is negative, and "no absorption while any push points up" is force-blind: a weak jet is never absorbed and the body accelerates
  **through** the ground. A launch that lifts needs `Force > Mass·Gravity` (80 kg × 19.62 = 1569 N, a 45° nozzle ≈ 2220 N), so a 1000 N hydrant is a shove by design.

## D-031: The preset list is `[SerializeReference]` — the DTO's type is its shape
- **2026-11-21 · Accepted · supersedes D-029's single DTO and drawer, restores D-028's split; `Name` deleted by D-032**
- **Decision:** `PushPresetData` is abstract with `Type`, abstract `Force` and `IsImpulse`; the list is `[SerializeField, SerializeReference]`, so the Inspector draws a concrete-type dropdown, creates the instance and shows exactly that type's fields. `PushKind` and the drawer are deleted, units
  move to `[Tooltip]`, the runtime branch is `IsImpulse`.
- **Why:** Adding an element to a `[Serializable]` **class** array creates a null slot no drawer can instantiate — the entry draws as an empty box and every typed number was lost (G-021). A flag whose only job is hiding fields a shape cannot read is a crutch; a comment promising an attribute not in
  the file is a claim, not a feature.

## D-032: A preset has no Name — `Type` is the whole authored identity
- **2026-11-21 · Accepted · supersedes the `Name` field of D-029, D-031**
- **Decision:** `PushPresetData` holds `Type` only; the asset is re-authored through Unity so `_name` keys leave the YAML, numbers unchanged.
- **Why:** `PresetOf(PushType)` is the only lookup, nothing prints a preset's name, sources store `PushType` — the label was a second identity authored by hand and read by nobody, and in a keyed list it invites believing two entries share a name.

## D-033: Dead surface is deleted — one push entry point, `IPushPreset` with its facts, 4 `PushType`
- **2026-11-21 · Accepted · the user's rule: "Clean all the dead fields if they present"**
- **Decision:** `PlayerMotion.ApplyPush(preset, worldDirection)` is the single entry point; the two shape-named methods, `LiveVerticalForce` as public, the empty `IPushPreset`, four `PushType` values nothing fires, and the unread `Burnable` body are deleted. `IPushPreset` gains `Force` and
  `IsImpulse` — the three facts the ticking and the ground's rule read; kept enum indices unchanged, so authored `_type` ints survive.
- **Why:** A field with no reader tells the designer it is read, and a second name for one behaviour is a second rule. Deleting them costs nothing and removes the claim.

## D-034: A jump carries the player's speed as momentum, not as a tilt of the launch
- **2026-11-22 · Accepted · the user's rule: a running jump must not be lower than a standing one**
- **Decision:** The preset launches straight up, untouched; the player's speed is carried into the push channel as inertia in the same tick, `ControlVelocity * JumpCarry` (authored, default 1) — a velocity, not a force, so `MaxPushSpeed` bounds it, `AirDrag` decays it in flight, the ground absorbs
  it on landing.
- **Why:** A tilt trades height for reach, adds a knob the user cannot author, and punishes the run-up: the direction is normalized inside the entry point, so tilting at full speed splits the launch 45°/45° and the vertical drops by √2. The drafted ratio was `0/0` = NaN standing, and the epsilon
  guard accepted NaN as "no direction" (G-023). Multiplying by zero is not a division, so the degenerate case needs no guard.

## D-035: `GameSettings` grouped with Unity headers, not Odin
- **2026-11-22 · Accepted · the user's call: "for now, no Odin"**
- **Decision:** Groups in the file, in the order a designer tunes them, each number with a `[Tooltip]` stating its unit and what it decides; Unity's own `[Header]`/`[Tooltip]`, no Odin.
- **Why:** Unity's attributes cost nothing in a WebGL build; the Odin runtime chain is ~720 KB and its shipping size after IL2CPP stripping cannot be measured today (no web build, D-019). A header is a fact of the Inspector, so grouping lives in code. Odin is not banned: a `CustomEditor` in
  `_Core/Editor` is the zero-weight path.

## D-036: Contact decides who has authority over momentum
- **2026-11-22 · Accepted · the ground half superseded by D-038; the air clamp stands**
- **Decision:** Airborne, control has no authority over momentum: the component of `ControlVelocity` **along** the shove is removed every tick, so the speed in the shove's direction is the shove's, while steering **across** keeps `PlayerSpeedInAir` (a projection, not a suppression). Grounded, the
  ground's reaction is a real force, so the running legs act on the total momentum.
- **Why:** The same fact the vertical rules use: a force needs something to push against. Airborne the player's effort has no reaction, so it cannot be a force on the body; input and inertia coexisting is what ice feels like (G-012).

## D-037: The authored config is split by subject, `GameSettings` binds them
- **2026-11-22 · Accepted · the user's ask: physics and speeds into separate configs**
- **Decision:** `IPhysicsSettings` + `Physics Settings.asset` — mass, gravity, the grounded rest value, terminal velocity, the ground rate, air drag, the push cap, jump carry. `IControlSettings` + `Player Control Settings.asset` — the three speeds, accelerations, look. `IGameSettings` is the facade
  with the two references and no numbers of its own; a session rule joins it only with the system that reads its number.
- **Why:** A port holds what its reader reads: `PlayerLookRotator` has no business knowing a Mass, and every consumer of one flat asset read the whole surface. The physics port is not player-prefixed — debris, a wave, a burnable body read the same numbers.

## D-038: The ground has ONE authored force — `GroundFriction`
- **2026-11-22 · Accepted · the user's rule: one parameter for surface friction · supersedes D-020's two rates and D-036's ground half**
- **Decision:** Grounded, one number does three jobs: ramps the run toward `DesiredVelocity`, brings the control to rest on input release, eats the push-channel momentum **in every direction**. `GroundDrag`, `GroundAcceleration` and the counter-run sign rule are deleted; D-036's air clamp untouched.
- **Why:** μN/Mass is **one force**; the channels are bookkeeping, not physics. `GroundAcceleration` 20 > μ 12 is unphysical — the legs cannot push harder than the ground's reaction — so the effective rate was 12 all along, two numbers were tuned against each other, and the sign rule made decay 16/s
  one way, 4/s the other. Excess speed is slip, friction eats slip, and a ground with three numbers is not tunable.

## D-039: The jump fires on a fresh press fact, not on a held key
- **2026-11-23 · Accepted · the user's call: no bunny-hop · supersedes D-017's contact lock · play check passed**
- **Decision:** `IPlayerInput.JumpPressed` is a **one-tick fact** (`WasPressedThisFrame`), never the held state; the gate is two conditions — press fact + grounded — with **zero stored jump state**: the contact flag deleted, a press seen in the air not remembered, each hop needing a new press.
  `AirDrag` and `AirAcceleration` stay split.
- **Why:** An event that lasts one tick cannot be consumed twice, so the flag existed only because the press was a state. With `JumpCarry` and `AirDrag` 0.5, held Space was the classic bunny-hop entry: each hop carried the previous speed back through the push channel. In flight there is no reaction
  force, so the two air numbers are not one force.

## D-040: Fire VFX — object-space shader mask, particle layers, one shared material
- **2026-11-23 · Accepted for the shader and particles · the domain half superseded by D-043, the lab half by D-048**
- **Decision:** Char and flame are computed from `positionOS` around `_BurnOrigin` in hand-written ShaderLab — no texture, no UV, no per-prop mask. The material is **shared** across burnables: per-object scalars travel in a `MaterialPropertyBlock`, so 24 objects are **one shader variant**. Particle
  layers use URP `Particles/Unlit` with **no** soft particles, sorted smoke → fire → puffs.
- **Why:** A shared material plus object-space noise is the only combination giving whole-geometry coverage for 24 props at one variant and zero texture memory; a mask pays memory and a UV layout per prop, a material instance pays a variant per prop. A Particle System is the web-safe emitter (VFX
  Graph has no WebGL support), a camera-depth sample is what the web budget cannot pay.

## D-041: Polygonal noise — facets with hard edges, grouping, base texture
- **2026-11-23 · Accepted · the user's eyes: smooth noise read as soap · the shader is accepted and not touched**
- **Decision:** `FacetOf(p, size, shape)` returns the cell index and centre in object space; every facet carries **one flat quantized value**, so a transition is one hard line. Grouping is a parameter pair per mask (`_FacetShape`, `_FacetSize`), every mask quantized (`_CharLevels`, `_FlameLevels`,
  `_ShellLevels`), each boundary a hard `step` line (`_EdgeWidth`, `_EdgeDarken`); the flicker scrolls the **grid** so flames pop between facets; `_BaseMap` is the prop's albedo, sampled in the surface pass only; facet parameters are **material-level** (shared), the shell writes only its per-object
  scalars.
- **Why:** Quantization is the only way to get straight edges out of a procedural mask — the smoothness is in the interpolation itself and no scalar removes it. Cost: two integer `FacetOf` calls per fragment, no branches, no new keyword variants, no extra passes, so the D-040 web budget holds.

## D-042: KISS is a standing rule
- **2026-11-23 · Accepted · the user's rule, verbatim: "это очень перемудреный шейдер"**
- **Decision:** The **shortest** mechanism that gives the feel is the one to write; a knob, branch, parameter or pass is added only when **the feel or the budget asks for it by name**, not because it is tunable. Work order: simple version → play → complicate **only the exact spot the eyes complained
  about**. Several variants are the budget for a first draft, not for every part of it; an accepted complex thing stays (the D-041 shader) and the rule applies to the **next** system.
- **Why:** Every knob is something the project carries forever. A design that needs a paragraph to explain is the version that grows; the version that ships is the one the eyes accept with the fewest parts.

## D-043: Burnable state on one normalized axis; the visuals unbound from the domain numbers
- **2026-11-24 · Accepted · self-feed and douse floor superseded by D-045, origin by D-046, lab by D-048; supersedes the domain half of D-040**
- **Decision:** One axis — `IgnitionStrength` 0..1 + `CurrentDurability` (HP) — and **one heat door** `AffectIgnitionStrength(amount)`: the source integrates the amount, `dt` never enters the domain, `FireResistance` (0..1) applies inside, so a wardrobe is hard to ignite **and** hard to douse with
  one number. Visuals are derived reads (`FireLevel`, `SmokeLevel`, `Charredness = 1 − HP/MaxDurability` — char has no scale, so an extinguished object stays black); the DTO is **5 numbers** (`IgnitionThreshold`, `FireResistance`, `MaxBurnDurability`, `MaxBurnIntensity`, `MaxFirePopulationRange`),
  the last two reaching only the domain. Spread is a **cursor, not a sweep**: `n = ceil(count/4)` objects per frame, each fed `Σ influence × elapsed_of_that_object`, so the result is `n`-independent.
- **Why:** Four accumulators needed seven authored numbers to keep them in step, and every one is a knob carried forever (D-042). Keeping `dt` out of the domain keeps its language as "a source gave me this much heat", which is what a rule can be tested on; particle rates, emission, shell height and
  outline are the **shell's own** numbers — a domain number never sets a particle speed.

## D-044: A hand-test script for the stand
- **2026-11-24 · Superseded by D-048 · the shell's door and the sign rule untouched**
- **Decision:** `SprayTest` — one click, one stream, no registration, no timer, no cadence, no domain arithmetic; press, not hold. Deleted with the lab.
- **Why:** A check that is easy to run is the check that gets run; a rig with a pulse accumulator and registration is a system, and the user needs a click. The shell's door was already the right entry.

## D-045: The ramp is driven by `MaxBurnIntensity`; the char front fixed at the first ignition
- **2026-11-24 · Accepted · supersedes D-043 on the self-feed, the douse floor and the origin; the cycle and guard superseded by D-048**
- **Decision:** A burning body feeds itself by **its own intensity**: `I += FireLevel · MaxBurnIntensity · dt`, so `MaxBurnIntensity` is one number with three jobs — the ramp (`(1 − threshold)/MaxBurnIntensity` s), neighbour heat, the rate HP is eaten (burn length `Durability/MaxBurnIntensity` s). A
  stream wins only when `stream × FireResistance > MaxBurnIntensity` (G-034). The char front is written **once, on the first ignition** (`_originLocked`), `Construct()` unlocks it with the char; no spot-per-douse list, no shader change, no auto restart in a hand test.
- **Why:** `MaxBurnIntensity` already *is* "how hard this fire works", so it should also be what makes the body grow, and `MaxBurnDurability` becomes readable as seconds. The char mask is measured from the origin, so a new contact re-centres the pattern and dark plateaus recede (G-035) — fixing the
  origin is the shell copying a fact, not a rule.

## D-046: The sign of the stream picks the cosmetic fact
- **2026-11-24 · Accepted · supersedes D-045 on *when* the origin is written; the `SprayTest` half by D-048**
- **Decision:** `ApplyStream(amount, Vector3? contact)` reads the **sign**, and the sign is the source's (G-031): `amount > 0` (fire) writes `_BurnOrigin` at the contact once, on the first ignition, no puff; `amount < 0` (water) plays the splash at the contact and leaves the origin where the fire
  put it. No point → the centre is the contact.
- **Why:** One door, one sign, no new state, no flags, no enums: the sign is the only fact distinguishing fire from water, so it is the cheapest selector. The char front is the fire's mark, the splash water's; mapping a contact's cosmetic is the shell's job, not a fire rule.

## D-047: The fire code swept for dead fields, duplicated numbers, inert tooling
- **2026-11-24 · Accepted · items 3–4 (the builder's numbers, the ContextMenu) deleted with the lab by D-048; no rule and no number changed**
- **Decision:** Delete fields written and never read (a cached mesh array, a pool list the session never needed); make `TickBurn` private — a public second entry to a rule lets a caller burn the body twice in a frame; delete the numbers the defaults already carry, the material values the shell
  overwrites every tick, and the `[ContextMenu]` on a parameterised method; fix stale citations.
- **Why:** The house rules are the test: a field written and never read lies about what the system uses; the same number in a builder and a default is two numbers to tune; a dev rig that logs a warning **per object** is a rig that does not do what it says (G-039).

## D-048: The lab is deleted; the pool is one for the scene, injected at composition
- **2026-11-24 · Accepted · supersedes D-044, D-045's cycle, D-046's hand-spray, D-047's items 3–4**
- **Decision:** The dev rig, driver, hand-spray and editor builder are deleted, the session knows nothing about a demo cadence; the accepted VFX assets stay (shader, shared materials, prefabs) as authored content, not scaffolding. **One** `SmokePuffPool` for the whole scene, handed to the session by
  `Bootstrapper` as a serialized field, referenced by every shell, never scanned for. Nothing ignites today: no prop carries a shell. The visual verdict moves from the lab stand to the game — made by playing props that burn.
- **Why:** A dev rig inside the session is a rule the game carries for no game reason; the puff budget (32 alive) is a **scene** number, so N pools is N × the budget; a `FindObjectsByType` scan for a service the composition can hand in is the same shape STYLE.md bans for the tick (G-040).

## D-049: Doc budgets are a test, not a rule
- **2026-11-25 · Accepted · the user's ask: make the docs stop growing · suite runs green**
- **Decision:** Every doc has a line budget, a per-entry budget and a max line length, written once in docs/README.md and asserted by `DocBudgetTests` (the editor suite reads the docs as plain text). A red test is compacted **before** code, never by raising the number. Routing lives in one file and
  every other doc points at it; `AGENTS.md` is a pure pointer to `CLAUDE.md`. Numbers, gate outputs, test counts, CLI transcripts, scene coordinates and probe values are banned from every doc — they belong to the commit body and to `TODO.md` status lines.
- **Why:** A length rule on paper failed: the log grew ~250 lines a day for a week under "keep them short", because an append-only entry cannot be compressed, so writing more was the only legal move. Making entries editable (history lives in git) removes the reason for a long entry; a test that
  reads
  the files makes the budget impossible to skip, and the ban on run-output text removes the bulk a session produces every day.

## D-050: Tests are a regression net; the user's play is the task gate
- **2026-11-25 · Accepted · the user's rule: the per-task test was an experiment and is retired · suite runs green**
- **Decision:** A task is delivered to the user and **their play verdict is the task done** — the suite is not a per-task gate, it runs once at the end of a session. A rule gets exactly one test, and a test that restates the same rule from a second angle is deleted, not kept: the jump and motion
  suites were cut to one test per rule, keeping the analytic launch, the press fact, the no-bounce gate, the analytic carry, contact
  absorption by normal, the ground/air rates, the drag equilibrium, the cap, direction from the source, frame-rate independence, the no-allocation budget, the degenerate-data case.
- **Why:** Almost half the code was test code, and the deleted tests asserted rules that had not moved for days — the cost was maintaining them, not running them: the suite takes a fraction of a second. Play is the gate that judges feel, which no test can, so a suite that blocks delivery spends the
  time it was written to save. One rule per test is the shape that stays small enough to keep.

## D-051: The burnable look runs on the ignition, not on heat
- **2026-11-25 · Accepted · the user's play verdict · D-045 and D-046 confirmed, the fire axis and the particle rule added**
- **Decision:** Every frame the shell copies `Charredness` → `_Burn` and `FireLevel` → `_Fire`. `_BurnOrigin` and `_BurnExtent` — the farthest mesh corner from the origin, so `Burn` 1 covers the mesh at any object size, and the mesh lives in the shell, not in the shader — travel **once**, on the
  first frame `FireLevel` is above 0, from the last fire contact. Fire particles and the shader fire run on `FireLevel`, the band above the threshold; smoke runs on the full `IgnitionStrength` axis and drops to 0 when the body
  burns out. The particle roots sit at the burn point, scale from `_particleMinScale` to `_particleMaxScale` and move to the mesh centre on the `Charredness` axis, whose 0 is the ignition; every transform under a root is
  written, so a nested sub-emitter follows its parent, and a chain authored with a disabled root is switched on.
- **Why:** Flame, fire particles and the char front must all begin the moment the body ignites, not the moment it gets warm, and smoke is the warm-up: the fire axis is the band above the threshold, so one read gives the
  remap. A doused object keeps its char and its place — the douse stops the burn, it does not undo it, so nothing moves back. A nested sub-emitter is authored as a child, so writing the whole chain is what keeps it attached.

## D-052: The composition root launches the game from a boot scene that survives the load
- **2026-11-26 · Accepted**
- **Decision:** `Bootstrapper` lives in an empty boot scene and is `DontDestroyOnLoad`; it requests the gameplay scene load itself and builds the session on `sceneLoaded`, resolving `SpawnPoints` there once. The gameplay scene
  name is authored on the shell, and the load door is one method, so a restart is dispose + load again.
- **Why:** A persistent object cannot reference objects of the scene a single-mode load destroys, so a serialized spawn-point reference is null exactly when the session needs it: assets survive the load, scene objects do not.
  Resolving the world once at the load keeps the shell thin and gives Phase 2 a restart with no added state. The boot scene fires `sceneLoaded` for itself, so the handler needs a requested-flag check — an unrequested load
  must not build a session.

## D-053: The pause gate is the composition root, and the input tick moves to it
- **2026-11-26 · Accepted · the user's call: the input tick moves to the composition root**
- **Decision:** `GameFlow` (UseCases) holds three states — Briefing, Playing, Paused — and the four requests that move between them; a request that does not match the state is a no-op, and restart returns to Briefing.
  `Bootstrapper` ticks the input **every frame, with no state check**, so the ESC press fact stays fresh while the world is frozen, and gates the session graph on the state. The input is never registered: the root
  ticks it ahead of the global list, so the order stays input → player → motor → burn. Restart is the same load door as launch: dispose, **flush the staged removal**, load — and the new load lands on Briefing, so no
  restart state is kept (G-044).
- **Why:** A pause must freeze the whole graph, and exactly one object owns that graph, so the gate is a state check on the root, not a second loop. A session that paused itself would have to know the UI; the flow needs
  no scene, no timer, no flags, no counters (D-042), and it is plain C# — the rules are testable the day the user approves the feel.

## D-054: The launch UI lives under the persistent composition root
- **2026-11-26 · Accepted**
- **Decision:** The canvas, the paper panel, the buttons and the EventSystem are children of `Bootstrapper`, so they survive every reload with it, and the shell's references all live inside that subtree. The window
  shows in Briefing and Paused only; the buttons forward requests to the flow and the shell tweens what the state decided.
- **Why:** A single-mode load destroys the boot scene's roots, so a UI that is a root dies with the scene it is supposed to restart. Under the root that survives, the UI and its EventSystem persist with no second
  DontDestroyOnLoad call and no scene lookup.
## D-055: The spread streams heat through the shell's door and names the strongest neighbour
- **2026-11-27 · Accepted · extends D-046; the spread clause of D-043 rewritten**
- **Decision:** Spread heat goes out through `IHeatSink.ApplyStream(amount, contactPoint)`, the **same door the
  spray uses**, and a visit names the **strongest** in-range neighbour — who heats me is a fire fact, so the
  char front goes on the mesh face turned to him. Every contact is clamped to the shell's mesh box, and that
  surface point is the origin and the splash spawn together: no puff floats off the prop. `IBurnable` gains
  `MaxFirePopulationRange`, the pair test the adjacency table is built with. Order: spread → body → shell.
- **Why:** A list of affectors in `IBurnable` is domain data the domain never reads (D-047), and a shell that
  picks the strongest neighbour is a shell with rules (D-012); mapping a direction to a point is geometry, D-046.

## D-056: The flame lives only on unburnt surface
- **2026-11-27 · Accepted · extends D-041 · the look verdict is the user's play check**
- **Decision:** The char paint covers the flame: the surface `fireMask` and the shell's tongue are both cut by
  `1 - burnt`, so fire shows only where the front has not eaten, a fully burnt prop carries fire at the front
  line only, and no flame ever draws over black. Particle chains are not masked — they are the shell's
  business, scaled on `Charredness` — the next lever if the eye still tangles.
- **Why:** Flame over char tangles the eye: the front line already tells where the fire eats, so a flame drawn
  on dead paint adds a fact the fire does not have. One multiply per pass, no new uniforms, one variant.

## D-057: Smoke peaks at mid heat, and the two chains own their ramps
- **2026-11-27 · Accepted · extends D-051 · the look verdict is the user's play check**
- **Decision:** The smoke chain keeps its travel on `Charredness`, and its scale ramp moves to the heat axis:
  `_smokeMinScale` at heat 0, `_smokeMaxScale` at heat 0.5, midpoint at heat 1. The fire chain stays as
  accepted — its scale ramps with `Charredness`, its emission with `FireLevel`. Each chain owns its min and
  max as its own Vector3s, so fire and smoke are authored apart, axis by axis.
- **Why:** The thickest plume is a mid-hot body eating; a monotonic ramp makes a burnt-out prop exhale the
  biggest column, and the eye reads the fire as dying. Fire motion and the fire ramp are the user's keep,
  and one shared scalar cannot author a plume that differs from the flame.

## D-058: Spot pivots follow the camera by two weights, position rides authored extremes
- **2026-11-27 · Accepted · extends D-012, D-013 · the feel verdict is the user's play check**
- **Decision:** One pivot per hand, authored as the parent of the hand spot and the item spot under the bone.
  Position blends the authored spot on the body toward the camera anchor: camera position plus one authored
  constant distance, the direction from the authored spot shifted by two extreme deltas lerped on camera pitch
  — no offset orbit, the pivot rides a fixed-distance sphere so the IK reach never stretches. 0 is bone-owned
  with noise only, 1 is the hard camera lock; rotation blends the root's yaw toward the camera rotation.
- **Why:** A rigid camera-frame offset orbits: looking up threw the hands out of IK reach, looking down rose
  them above the camera. The bone's limb animation once drove the hands under the floor, so the anchor uses
  bone translation and root yaw only. Pivot children keep item parenting and IK untouched.

