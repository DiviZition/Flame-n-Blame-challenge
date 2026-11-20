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

### Layers & Namespaces

| Layer | Namespace | Folder | Role |
|----------------|----------------------------------------|-------------------------------|------|
| Composition | `Flame_n_Blame_Challenge` | `Assets/_Core/Scripts/` | `Bootstrapper`, `GameUpdater` — build the graph, own the tick |
| Use Cases | `Flame_n_Blame_Challenge.UseCases` | `Scripts/UseCases/` | session orchestration (`GamePlaySession`) |
| Domain | `Flame_n_Blame_Challenge.Domain` | `Scripts/Domain/` | pure game state & rules |
| Adapters | `Flame_n_Blame_Challenge.Adapters` | `Scripts/InterfaceAdapters/` | ports (`IUpdatable`, `IPlayerInput`, `IGameSettings`) + Unity implementations |

**Dependency direction:** Composition → UseCases → Domain; all three may depend on Adapter **ports**,
and only Composition instantiates concrete Adapter implementations. `Scripts/UseCases/` is the chosen
folder (D-012, D-014); `Scripts/Application/` is deleted — one folder for one layer.
The current file-by-file map is `docs/DECISIONS.md` §Current structure.

### The Player, as built

| Type | Layer | What it owns |
|------|-------|--------------|
| `Player` | Domain | composition + forwarding only: the three parts, `Velocity` for the shell, `ReceivePush`, `ReportContact`. **No rules, no `_input`/`_settings`** (D-021) |
| `PlayerMotion` | Domain | **velocity**: `ControlVelocity` (input, never touched by a push), `PushVelocity` (momentum), `VerticalVelocity` (gravity + vertical launch), the active push list (capacity 8), the jump gate (D-021), `GroundDrag`/`AirDrag` (D-020). Step order: contact → jump → gravity → pushes → control → drag → cap → sum |
| `PlayerMovement` | Domain | input + look → `DesiredVelocity`; reduced `PlayerSpeedInAir`; exposes the jump **request** only (D-021) |
| `PlayerLookRotator` | Domain | yaw/pitch from the look delta → `Forward`/`Right` |
| `PushPresetData` + `PushType`/`PushKind`/`PushApplication`/`Vector3CurveData` | Domain | authored push DTO: `Type` (replace-per-type key), `Kind` (Impulse N·s / Sustained N), `Applies`, `Force`, `Curve`, `Duration` (ends the **adding**, never the velocity). `Applies` is an open question (TODO) |
| `PlayerMotor` | Adapters (shell) | `Move(Velocity * dt)`, yaw/pitch onto transforms, report `CollisionFlags.Sides` + `isGrounded`, forward `ApplyForce` → `Player.ReceivePush`. **No rules** |
| `HydrantPush` | Adapters (shell) | trigger → `ApplyForce(preset, nozzle rotation)` — direction from the **source**, never the player's facing |

**The push rule, structurally** (the *why* is D-016, D-020, D-021; the trap is G-012): a push enters
only through `Player.ReceivePush(preset, sourceOrientation)`; `Duration` ends the adding, **contact**
absorbs momentum, decay is authored per contact state, the channel saturates at `MaxPushSpeed`. A shell
that evaluates curves, sums forces, or zeroes velocity on a timer has business rules in it. A source
decides **whether** to fire; `PlayerMotion` decides what a fired push does.

### Who owns the tick

- Exactly one Unity `Update()` — on `Bootstrapper` → `GameUpdater.Update(time, deltaTime)` → every
  registered `IUpdatable` **in registration order**, which **is** the execution-order contract (never
  Script Execution Order). Safe removal during iteration (staged queue + swap-remove).
- Registration order today, in `GamePlaySession.StartGamePlay()`: `PlayerInputMain` → `Player` →
  `PlayerMotor` — the shell runs **after** the domain, so it consumes what the domain decided this tick.
- **No fixed step** (D-018): integration is dt-driven; frame-rate independence is asserted by tests at
  1/60, 1/30, 1/120 (within 5%). Re-opens only as a new decision.

### Unity boundary

- **Default:** plain C# classes. MonoBehaviours only when Unity requires them (physics/trigger
  callbacks, `Animator`, authored prefab references) and only as **thin shells** that forward
  events/data into plain classes.
- Authored numbers live on shells/ScriptableObjects and are copied into plain data objects passed to
  constructors. Domain never reads the Inspector and never does `GetComponent` in a hot path.

### Current reality vs decided

| Status | Items |
|-----------------|-------|
| **Exists + verified** | The tree compiles (B-002 `Verified` 2026-11-20, CLI gate). The test suite runs as **EditMode** on code (`unity cmd test run --mode editor`): 19 tests, 18 pass, 1 fails (**B-003**, the spray does not saturate). `Bootstrapper` is in `FnB_Gameplay.unity` on `=== System`, so the tick runs. |
| **Exists, not verified** | The push model's *behaviour in the scene*: no `Player` GameObject, so `PlayerMotor` is attached to nothing (B-004). No editor feel check (TODO), no web build (D-019). |
| **Decided, not built** | Push-source registry routing (D-016), spray shove with the 0.1 s flat-curve refresh (phase 2), hydrant burst tuning, control-loss lockout (cut by D-016), fire/burnable systems, WebGL budgets for fire/particles/physics, anisotropic ground friction (needs a contact normal, D-020). |
| **Still scaffolding** | `Player.prefab` carries a non-kinematic Rigidbody beside the `CharacterController`; `EditorBuildSettings` lists the deleted `SampleScene.unity` (G-003); WebGL has no pipeline (B-001). |

### Adding a new system — checklist

1. State & rules in **Domain**.
2. Engine services (input, physics queries, audio…) → declare a **port**, implement it in Adapters.
3. Register for the tick at composition time, in the required order (there is no fixed tick, D-018).
4. Author tunable numbers on a shell/ScriptableObject → pass as plain data.
5. Keep MonoBehaviours thin; no rules in them.
6. Write the perf budget **before** code (D-007), then the test, then the code.

### Notes

- Update this section when the shape changes; never edit the READ-ONLY RULES above.
- This file states **how to build**. Rationale and history live in DECISIONS.md; state of the files in
  DECISIONS.md §Current structure; work in TODO.md; traps in GOTCHAS.md (D-024).

