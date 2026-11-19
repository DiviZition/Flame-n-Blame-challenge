# Architecture Rules

This document defines the architectural conventions for the project.  
Follow these rules strictly when adding or changing code.

---

## READ-ONLY RULES

**The entire section below (until the Project-Specific Structure) is READ-ONLY.**  
Do not edit, rewrite, or remove any of these rules.  
Only the "Project-Specific Structure" section at the bottom may be updated as the architecture evolves.

---

### Goal

Keep core game logic pure C#, testable, and independent from Unity.  
Unity-specific code lives only in the outer layers.

### Layers

| Layer            | Responsibility                                      | Allowed dependencies          |
|------------------|-----------------------------------------------------|-------------------------------|
| **Domain**       | Entities, value objects, pure game rules            | Nothing outside Domain        |
| **Application**  | Use cases, session/orchestration management         | Domain                        |
| **Adapters**     | Translation between Domain/Application and outside  | Application + Domain          |
| **Drivers**      | Unity, input, physics, rendering, third-party SDKs  | Everything (outermost layer)  |

**Dependency rule:**  
Dependencies may only point **inward**.  
Domain must never reference Application, Adapters, or Drivers.

### Core Principles

1. **Composition Root**  
   There is a single entry point (Bootstrap) that creates long-lived infrastructure and manages the lifetime of higher-level application objects (such as a game session). It contains no game logic.

2. **Session / Application Ownership**  
   A dedicated Application-layer object owns the lifetime of all gameplay entities that exist only during an active session.  
   It is responsible for creating them, registering them for updates if needed, and cleaning them up when the session ends.

3. **Domain Purity**  
   Domain classes contain only state and business rules.  
   They must not:
   - Know about the update loop or register/unregister themselves
   - Hold references to session managers, updaters, or Unity objects
   - Perform physics queries, raycasts, scene lookups, or any engine-specific work

4. **Update Loop**  
   A pure C# updater manages a list of updatable objects and calls their update method.  
   It supports safe removal during iteration.  
   Domain objects never talk to the updater directly — registration is performed by the Application layer.

5. **Intent over Simulation (when using engine physics)**  
   Domain expresses *intent* (desired velocity, state flags, actions).  
   Actual position integration, collision response, and physics simulation stay in Adapters/Drivers.

6. **What Goes Where**

   | Concern                              | Layer                  |
   |--------------------------------------|------------------------|
   | Pure entities & game rules           | Domain                 |
   | Session lifetime & orchestration     | Application            |
   | Update loop management               | Application / Infra    |
   | Unity MonoBehaviours, input, physics | Drivers / Adapters     |
   | Interfaces (ports) for external needs| Domain or Application  |

### Anti-Patterns (Forbidden)

- Domain class registering itself in the update system
- Business rules living inside MonoBehaviours
- Domain or Application depending on concrete Unity types
- God objects that mix session management + entity logic + registration
- Domain owning authoritative world position when engine physics is used
- Creating session-scoped entities directly from the Composition Root instead of from the session object

### Practical Guidance

- When in doubt, ask: “Does this code need to know about Unity or the current session lifetime?”  
  - Yes → outer layer  
  - No → Domain or Application
- Prefer small, focused classes over large managers.
- It is acceptable to be slightly pragmatic under time pressure, but **never** violate the dependency direction (Domain must stay pure).

---

## Project-Specific Structure

> This section is **mutable**.  
> Update it as the concrete architecture takes shape.  
> Describe current major classes, their layers, ownership, and important dependencies here.

### Layers & Namespaces (current direction)

| Layer          | Namespace                              | Folder                        | Role |
|----------------|----------------------------------------|-------------------------------|------|
| Composition    | `Flame_n_Blame_Challenge`              | `Assets/_Core/Scripts/`       | `Bootstrapper`, `GameUpdater` — build the graph, own the tick |
| Use Cases      | `Flame_n_Blame_Challenge.UseCases`     | `Scripts/UseCases/`           | Session / run orchestration (`GamePlaySession`) |
| Domain         | `Flame_n_Blame_Challenge.Domain`       | `Scripts/Domain/`             | Pure game state & rules |
| Adapters       | `Flame_n_Blame_Challenge.Adapters`     | `Scripts/InterfaceAdapters/`  | Ports (`IPlayerInput`, `IUpdatable`…) + Unity implementations |

**Dependency direction:**  
Composition → UseCases → Domain  
All of them may depend on Adapter **ports** (interfaces). Only Composition instantiates concrete Adapter implementations.

`Scripts/UseCases/` is the chosen folder (D-012, D-014); `Scripts/Application/` is **deleted** — the
READ-ONLY table's "Application" layer is what `UseCases` *is*, so one folder, not two.

### The Player, as built (D-016, D-017)

| Type | Layer | What it owns |
|------|-------|--------------|
| `Player` | Domain | **Composition and forwarding only** — the three parts, `Velocity` for the shell, `ReceivePush` and `ReportContact` forwards. No rules in it: the jump gate moved to `PlayerMotion` (D-021), so no `_input`/`_settings` field is needed at all |
| `PlayerMotion` | Domain | **Velocity.** Three summed channels — `ControlVelocity` (input, never touched by a push), `PushVelocity` (push momentum), `VerticalVelocity` (gravity + vertical launch) — plus the active push list (capacity 8). The rule order: contact → jump → gravity → pushes → input control → drag → cap → sum. It also owns the jump gate (D-021) and **two authored decay rates** — `GroundDrag` while grounded, `AirDrag` while airborne (D-020) |
| `PlayerMovement` | Domain | Input + look → `DesiredVelocity`; air control is the reduced `PlayerSpeedInAir`; exposes the jump *request* (`IsJumpRequested`) and nothing else about the jump (D-021) |
| `PlayerLookRotator` | Domain | Yaw/pitch from the look delta; exposes `Forward`/`Right` |
| `PushPresetData` + `PushType`/`PushKind`/`PushApplication`/`Vector3CurveData` | Domain | Authored push data as a plain DTO: `Type` (replace-per-type key), `Kind` (Impulse N·s / Sustained N), `Applies` (Always / GroundedOnly), `Force`, `Curve` (flat = constant, decay = one puff), `Duration` (ends the **adding**, never the velocity) |
| `PlayerMotor` | Adapters (shell) | `Move(Velocity * dt)`, write yaw/pitch onto the transforms, report `CollisionFlags.Sides` + `isGrounded` into the domain, and forward `ApplyForce` to `Player.ReceivePush`. **No rules in it** — no force list, no curve evaluation, no gravity, no velocity |
| `HydrantPush` | Adapters (shell) | Trigger → `playerMotor.ApplyForce(preset, _pushDirection.rotation)`. Direction comes from the **source's** rotation, never the player's facing |

**The push rule (D-016, D-020):** a force's `Duration` ends the *adding*; **contact** is what absorbs
momentum — grounded zeroes vertical and push-Y, `CollisionFlags.Sides` zeroes the pushed X/Z and leaves
`ControlVelocity` so the player can still walk. Momentum then decays at **authored `GroundDrag` while
grounded** (in the ground plane only — never on the vertical, so a jump fired on that tick survives)
**and authored `AirDrag` while airborne**; one rate for both is what makes the floor read as ice
(G-012), and it saturates at authored `MaxPushSpeed`. External pushes enter through
`Player.ReceivePush(preset, sourceOrientation)`; the player's own push (the jump) is fired inside
`PlayerMotion`'s step (D-021). Every future shove (spray, debris, hydrant burst) uses the same one
method: *the source calls the shell, the shell forwards, the domain decides what velocity does.* A
shell that evaluates curves, sums forces, or zeroes velocity when a timer expires has business rules
in it (G-012). A source's **conditions to fire** (is the aim on target, is the button held, did I just
land) are never in `PlayerMotion` — the source decides whether to fire; the domain decides what a fired
push does. Whether `PushPresetData.Applies` should exist at all is an open question, not a decision
(TODO): the only preset that used `GroundedOnly` was the jump, and D-021's gate makes it redundant.

### Who owns the tick

- Exactly one Unity `Update()` exists — on `Bootstrapper`.
- It calls `GameUpdater.Update(time, deltaTime)`.
- `GameUpdater` calls `Update` on every registered `IUpdatable` **in registration order**.
- Registration order **is** the execution order contract. Do not rely on Script Execution Order.
- Safe removal during iteration is supported (staged queue + swap-remove).
- **No fixed step for now** (D-018, supersedes the `IFixedUpdatable` clause of D-013): integration is
  dt-driven on the single `GameUpdater` tick, and frame-rate independence is asserted by tests at
  `dt = 1/60`, `1/30`, `1/120` (within 5%). Re-opens only as a new decision for a system that needs
  deterministic stepping.
- Registration order today, in `GamePlaySession.StartGamePlay()`: `PlayerInputMain` → `Player` →
  `PlayerMotor`. The shell runs **after** the domain so it consumes the velocity the domain decided this
  tick. `PlayerMotor`'s own Unity `Update()` was deleted to keep the first bullet true (D-016).

### Unity boundary

- **Default:** plain C# classes.
- **MonoBehaviours are allowed only when Unity requires them** (physics/trigger callbacks, Animator, authored prefab references, etc.).
- These MonoBehaviours must stay **thin shells**: they forward events/data into plain classes. No game rules inside shells.
- Authored numbers live on shells or ScriptableObjects, then are copied into **plain data objects** that are passed to constructors. Domain never reads from the Inspector or does `GetComponent` in hot paths.

### Current reality vs decided

| Status              | Items |
|---------------------|-------|
| **Exists today**    | `Bootstrapper` (composition root, one type per file), `GameUpdater`, `IUpdatable`, `IPlayerInput`, `PlayerInputMain`, `GamePlaySession` in `Scripts/UseCases/`, `Scripts/Domain/{Player, PlayerMovement, PlayerLookRotator, PlayerMotion, ActivePush, PushPresetData, PushType, PushKind, PushApplication, Vector3CurveData}`, `IGameSettings` (port, in `Scripts/InterfaceAdapters/`) + `GameSettings` (authored `Mass`, `Gravity`, `GroundedGravity`, `TerminalVelocity`, accelerations, `GroundFriction`, **`GroundDrag`**, `AirDrag`, `MaxPushSpeed`, the jump preset), `PlayerMotor` gutted to a thin shell, `HydrantPush` as a shell, `FlameNBlame.Tests` asmdef + the push-model tests (**T1–T13**, plus the jump suite with its auto-rebound pin), first perf budget written (D-016) |
| **Written, not verified** | **The whole push model.** The headless compile gate and the PlayMode test run have **not** been executed (the Bash safety classifier refused destructive commands on 2026-11-19 — read-only shell worked, so the refusal is classification, not absence), so the tree's compile status is unconfirmed — B-002 is `Fixed` on paper only, not `Verified`. No web build was made (D-019); no editor feel check was run. Treat this row as the top priority on `TODO.md`. |
| **Decided, not built yet** | Push-source registry routing (deferred by D-016), spray shove with the 0.1 s flat-curve refresh (phase 2 — the shape a `UseCases` source class would host, D-021's reasoning), hydrant burst tuning, control-loss lockout (**cut** by D-016 — re-deciding needs a new entry), fire/burnable systems, WebGL budgets for fire/particles/physics, anisotropic ground friction (needs a contact normal, so it changes what the shell reports — D-020), `Bootstrapper` attached in the scene |
| **Still scaffolding** | `Bootstrapper`'s GUID is absent from `FnB_Gameplay.unity`, so **no tick runs at all**; `Player.prefab` carries a non-kinematic Rigidbody beside the `CharacterController`; the scene is not in Build Settings (G-003) |

### Adding a new system — checklist

1. Put state & rules in **Domain**.
2. If it needs engine services (input, physics queries, audio…), declare a **port** and implement it in Adapters.
3. Register it for the tick at composition time, in the required order (there is no fixed tick, D-018).
4. Author tunable numbers on a shell/ScriptableObject → pass as plain data.
5. Keep MonoBehaviours thin; put no rules in them.

### Notes

- Prefer updating this section when the shape changes.
- Never edit the READ-ONLY RULES above.
