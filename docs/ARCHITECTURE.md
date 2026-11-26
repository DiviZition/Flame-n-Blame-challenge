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

> **Mutable.** Update it in the same change that moves a layer; never edit the READ-ONLY rules. The
> file-by-file map, the scene contents and the rationale live in `DECISIONS.md`, work in `TODO.md`, traps
> in `GOTCHAS.md` (D-024). **Never** measured numbers, gate outputs, test counts, CLI commands, coordinates.

### Layers & namespaces

| Layer | Namespace | Folder | Role |
|---|---|---|---|
| Composition | `Flame_n_Blame_Challenge` | `Assets/_Core/Scripts/` | `Bootstrapper`, `GameUpdater` — build the graph, own the tick, and from the boot scene own the load → session lifetime (D-052); the pause gate that decides whether the tick runs (D-053) |
| Use Cases | `.UseCases` | `Scripts/UseCases/` | session orchestration (`GamePlaySession`) |
| Domain | `.Domain` | `Scripts/Domain/` | pure game state & rules |
| Adapters | `.Adapters` | `Scripts/InterfaceAdapters/` | ports, Unity-side implementations, authored ScriptableObjects |

Dependencies: Composition → UseCases → Domain; all three may depend on Adapter **ports**, and only
Composition instantiates concrete Adapter implementations. `Scripts/Application/` was deleted — one folder
per layer (D-012, D-014).

### The push rule, structurally

Why: D-016, D-027, D-028, D-030, D-031, D-034, D-038, D-039. Traps: G-012, G-016, G-018, G-020, G-021, G-023.

- A push enters only through `Player.ReceivePush(preset, sourceDirection)`. The DTO's **type** is the force shape (Impulse lands in one tick; Sustained accumulates `Force·Curve/Mass` over `Duration`), so the entry point cannot branch.
- Channels: `ControlVelocity` (input) and `PushVelocity` (shove) are **horizontal**, `VerticalVelocity`
  owns gravity and every launch. Contact absorbs momentum: the ground absorbs vertical **iff** the live
  vertical force ≤ `Mass·Gravity`, a wall absorbs **along its normal** and keeps the parallel part.
- Decay: the ground's **one** rate `GroundFriction` grounded, `AirDrag` airborne, horizontal saturated at
  `MaxPushSpeed`; `Duration` ends the *adding*, never the velocity; a launch that lifts needs
  `Force > Mass·Gravity`.
- A source decides **whether** to fire, `PlayerMotion` what a fired push does; the jump gate is two
  conditions (press fact + grounded) with zero stored jump state. A `UseCases` source class is written
  where a source has scenario logic (aim, hit test, interval), not for a gate. A shell that evaluates
  curves, sums forces, or zeroes velocity on a timer has business rules in it.

### The fire rule, structurally

Why: D-040…D-048. Traps: G-026, G-027, G-031, G-034, G-035, G-036, G-040.

- One axis: `IgnitionStrength` 0..1 + `CurrentDurability` (HP), **one heat door**
  `AffectIgnitionStrength(amount)`: the source integrates the amount, `dt` never enters the domain,
  `FireResistance` scales it inside — one number makes a body hard to ignite **and** hard to douse.
  `MaxBurnIntensity` drives the ramp, the neighbour heat and the HP drain; burn length is its ratio to durability.
- A stream douses only when `stream × FireResistance > MaxBurnIntensity` (G-034); the amount is
  **negative** and the sign is the source's (G-031), and the sign picks the cosmetic: a positive contact
  writes the char front once, on the first ignition; a negative contact plays the splash there (G-035, G-036).
- The shell **copies** and decides nothing: `FireLevel` → `_Fire`, `Charredness` → `_Burn` (the shader's
  char axis is `_Burn`), the origin, the extent and its own authored look numbers, through a
  `MaterialPropertyBlock` on the **shared** material — 24 objects, one shader variant. Particle rates are
  the shell's numbers; a domain number never sets a particle speed.
- Spread is a **cursor, not a sweep**: `n = ceil(count/4)` objects per frame, each fed `Σ influence ×
  elapsed_of_that_object`, so the result is `n`-independent. The shader stays as accepted (D-041, D-042):
  quantized facets, hard `step` boundaries, no mask texture, no soft particles.
- The **one** puff pool is injected at composition; a per-prop pool multiplies the budget (G-040). A shell
  that grows fire, decays heat on a timer, counts pulses, or picks the sign of a stream has business
  rules in it.

### Who owns the tick

- Exactly one Unity `Update()` — `Bootstrapper` → `GameUpdater.Update(time, deltaTime)` → every
  registered `IUpdatable` **in registration order**, which **is** the execution-order contract (never
  Script Execution Order). Safe removal during iteration (staged queue + swap-remove).
- Today's order: the composition root ticks `PlayerInputMain` itself, ahead of the global updater, then
  `GamePlaySession.StartGamePlay()` registers `Player → PlayerMotor → BurnableShell[] + FireSpreadSystem` — pools
  constructed first, shells before the spread system sees them; a shell runs **after** the domain, so it consumes
  what the domain decided (D-013).
- The gate: the root ticks the input **every frame, with no state check** and the session graph **only while the
  flow says Playing**, so a pause fact stays fresh while the world is frozen (D-053). Pausing is one state check,
  not a second loop. A removal is staged, so a teardown that gates the tick **flushes** the queue (G-044).
- **No fixed step** (D-018): integration is dt-driven; frame-rate independence is asserted at `dt = 1/60, 1/30, 1/120` within 5%. Re-opens only as a new decision.

### Unity boundary

- **Default:** plain C# classes. MonoBehaviours only when Unity requires them (physics/trigger callbacks,
  `Animator`, authored prefab references) and only as **thin shells** forwarding events/data inward.
- Authored numbers live on shells/ScriptableObjects, copied into plain data objects passed to
  constructors. Domain never reads the Inspector, never `GetComponent` in a hot path.

### Reactive first

- Prefer **reactive** facts: Unity events (`OnTriggerEnter`, `OnControllerColliderHit`, input callbacks)
  report what happened, and a thin shell forwards it inward. A poll (`Physics.CheckSphere`, a timer scan,
  a per-tick world query) is written only when reactivity cannot deliver the fact at the right moment,
  **or** when polling is a measured win on the WebGL budget. No poll without one of those two reasons.
- Same rule inside the domain: a rule consumes the facts it was given, it never searches the world.

### Fixing a bug

- After a fix, look at it from the outside: does it make the mechanic honest, or does it compensate a
  symptom? Compensating a symptom is a **crutch** — a rule that guards a sign, a timer that hides a
  missing fact, a guard that fires because the real rule is absent. If the fix is a crutch, stop patching
  and design the mechanic so the rule is the physical truth (the sign is not the rule; the force is).
- A crutch in **domain** logic is allowed only in quick / temporary / test sketches, with a `TODO.md` line
  marked *fix later*; in tertiary, non-influencing adapters, only where what it touches is not critical
  (cosmetics, feel, a debug path).
- **Numbers are tuned by play, not by test sweeps** (D-032): the standing gate is the one suite run,
  never a search over parameter values; a preset's Force, curve and Duration are chosen by the user
  playing.

### Adding a new system — checklist

1. State & rules in **Domain**.
2. Engine services (input, physics queries, audio…) → declare a **port**, implement it in Adapters.
3. Register for the tick at composition time, in the required order (there is no fixed tick, D-018).
4. Author tunable numbers on a shell/ScriptableObject → pass as plain data.
5. Keep MonoBehaviours thin; no rules in them.
6. Write the perf budget **before** code (D-007), then the test, then the code.
7. **KISS** (D-042, the user's rule): the **shortest** mechanism that gives the feel is the one to
   write — a knob, a branch, a parameter, an extra pass is added only when **the feel or the budget asks
   for it by name**, not because it is tunable. Write the simple version, play, complicate only the exact
   spot the eyes complained about; several variants are the budget for a first draft, not for every part.

