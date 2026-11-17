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

### Who owns the tick

- Exactly one Unity `Update()` exists — on `Bootstrapper`.
- It calls `GameUpdater.Update(time, deltaTime)`.
- `GameUpdater` calls `Update` on every registered `IUpdatable` **in registration order**.
- Registration order **is** the execution order contract. Do not rely on Script Execution Order.
- Safe removal during iteration is supported (staged queue + swap-remove).
- Physics will use a parallel `IFixedUpdatable` driven from a single `FixedUpdate` (impulses & queries on fixed step, visuals on normal update).

### Unity boundary

- **Default:** plain C# classes.
- **MonoBehaviours are allowed only when Unity requires them** (physics/trigger callbacks, Animator, authored prefab references, etc.).
- These MonoBehaviours must stay **thin shells**: they forward events/data into plain classes. No game rules inside shells.
- Authored numbers live on shells or ScriptableObjects, then are copied into **plain data objects** that are passed to constructors. Domain never reads from the Inspector or does `GetComponent` in hot paths.

### Current reality vs decided

| Status              | Items |
|---------------------|-------|
| **Exists today**    | `Bootstrapper`, `GameUpdater`, `IUpdatable`, `IPlayerInput`, `PlayerInputMain`, asmdef |
| **Decided, not built yet** | `IFixedUpdatable` + fixed-step host, proper `UseCases/` + `Domain/` folders, plain data-object pattern, per-system perf budgets |
| **Still scaffolding** | Nothing registered in `GameUpdater` yet, `GamePlaySession` not fully wired, some types still live inside `Bootstrapper.cs` |

### Adding a new system — checklist

1. Put state & rules in **Domain**.
2. If it needs engine services (input, physics queries, audio…), declare a **port** and implement it in Adapters.
3. Register it for the tick (or fixed tick) at composition time, in the required order.
4. Author tunable numbers on a shell/ScriptableObject → pass as plain data.
5. Keep MonoBehaviours thin; put no rules in them.

### Notes

- Prefer updating this section when the shape changes.
- Never edit the READ-ONLY RULES above.
