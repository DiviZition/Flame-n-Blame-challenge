# TODO.md — The Work List

The one place for work: what is done, what is in progress, what is next. **Bugs live here too** (the
`B-` ids from the retired BUGS.md are kept). One line per item, with the date it was added.

**Rules:**
- `- [ ]` open, `- [x]` done. Mark done on the date the **test passed**, not the date it was written.
- Bug statuses: `Open` → `In progress` → `Fixed` → `Verified` (verified = a test/gate run through the
  Unity CLI). If a fix didn't fix it, back to `Open` and say why on the same line.
- A decision → DECISIONS.md; the shape of the code → ARCHITECTURE.md; a trap we fell into → GOTCHAS.md;
  a convention → STYLE.md. **This file holds work only.**
- Review at the start of every session; prune stale lines.

**Scope is locked** to the four pillars (D-008). **WebGL is the shipping target** (D-007): "done" means
done on web. Until B-001 is fixed the honest line is *editor-verified, web-unverified* (D-019).

---

## Bugs

- [ ] **B-001** `Open` — **WebGL has no render pipeline.** Quality level 0 "Mobile" points at a deleted
  `Mobile_RPAsset` GUID; `Web_RPAsset` is bound only to the "PC" level, which excludes WebGL;
  `GraphicsSettings.asset` has no fallback. A web build renders magenta while the editor looks fine
  (`m_CurrentQuality: 1`). Fix: give WebGL a level pointing at `Web_RPAsset`, then re-cut the levels for
  web (G-010, D-009). **Every web measurement is worthless until this is done.**
- [x] **B-003** `Verified 2026-11-20` — **the spray-saturation test was unpassable by construction.**
  `MaxPushSpeed` **was** clamped all along (`ClampPushVelocity`); the bug was the assertion:
  `meanLate - meanEarly < 0.1` compares the first second of an exponential ramp (τ = 1/AirDrag = 2 s)
  against the asymptote, so it fails at any τ; and the cap (12) sat above the force/drag equilibrium (8),
  so the clamp was never exercised. Fixed in D-027: convergence asserted analytically (the late window on
  the equilibrium, the series monotone, the plateau inside 3τ) and a second test with the equilibrium above
  the cap. Repro: `unity cmd test run --mode editor` → **20 tests, 20 passed**, 0.15 s.
- [ ] **B-004** `Open` — **`PlayerMotor` is attached to nothing.** The scene has no `Player` GameObject
  (`unity cmd gameobject find --type PlayerMotor` → null), so nothing moves in Play Mode. The tick runs
  (`Bootstrapper` is on `=== System`), the domain computes velocity, no shell consumes it. Same work as
  the first item below.
- [x] **B-002** `Verified 2026-11-20` — the tree did not compile (two duplicate `ForcePreset` drafts,
  `using NUnit.Framework` in the runtime asmdef, a dangling statement in `Bootstrapper`). Fixed by
  D-016; verified by 5 CLI gates: `unity cmd script recompile` → `up_to_date`, `console get` →
  `compilationFailed: false`. The batch-mode gate stays unrun, and `autoRefresh` is **disabled**, so the
  CLI gate is the standing check — run it after every edit.

## Now (blocking, in this order)

- [ ] **B-004:** `ResourceSpawner.SpawnPlayer()` instantiates `Player.prefab` at the prefab's own origin
  (0,0,0) and ignores `/=== World/Player Spawn Point` (world 2.84, 0, 7.78), so the player starts 2.5 m
  from the hydrant and never touches its trigger. Fix: place the clone at the spawn point (the spawner owns
  the position, the prefab keeps its origin). Measured 2026-11-20: `pos=(0.00, 0.00, 0.00)` at play start.
- [x] 2026-11-20 — **Suite made EditMode** (`includePlatforms: ["Editor"]` in
  `FlameNBlame.Tests.asmdef`): `unity cmd test run --mode editor` → **19 tests, 18 passed, 1 failed**
  (B-003), synchronous, no scene load, ~0.11 s (D-026).
- [x] 2026-11-20 — **Simulation fixed (D-027)**: the push channel is horizontal only, all vertical
  momentum (gravity + launch) lives in one channel guarded by a launch state, a wall absorbs the push
  along the **contact normal** (the shell reports it from `OnControllerColliderHit`), and the saturation
  test is analytic. `unity cmd test run --mode editor` → **20 tests, 20 passed**, 0.15 s.
- [x] 2026-11-20 — **Push API reshaped (D-028)**: direction is a **Vector3 from the source**
  (`PushDirection.forward`), the envelope is **one scalar curve**, two DTOs (`ImpulsePresetData`,
  `SustainedPresetData`) with two entry points, all presets in `Push Presets.asset` keyed by `PushType`,
  and ground absorption is directional (vertical **down** only), so no launch flag is stored.
  `unity cmd test run --mode editor` → **19 tests, 19 passed** (T9 deleted with its rule, the jump suite is
  now 7 timed-launch tests).
- [x] 2026-11-20 — **One preset list** (D-029): `PushPresetData{Type, Name, Kind, Force, Curve, Duration}`
  in a single `_presets` array keyed by `PushType`, read as `IPushPresets.PresetOf(PushType)`; `PushKind`
  returns as the **authoring switch** (the designer picks Impulse/Sustained, the code never infers it), and
  `PushPresetDrawer` (editor-only adapter) hides Curve/Duration for an Impulse and labels Force `N·s` vs
  `N`. The two DTOs are deleted, the asset re-authored as one list, tests re-signed → **19/19**.
- [x] 2026-11-20 — **Ground absorption is a normal force** (D-030, G-020): the sign-only rule ate every
  launch, because the grounded rest value is **negative** — a launch's first ticks read downward and the
  ground reset them. Now the ground absorbs downward vertical **iff** the live vertical force ≤
  `Mass·Gravity`. The first fix (a force-blind "no absorption while a push points up") was a **crutch** and
  was replaced by the force rule. **19/19**.
- [x] 2026-11-20 — **Reactive-first + the crutch rule** written into ARCHITECTURE (§Reactive first,
  §Fixing a bug): events over polls (a poll needs a stated reason), a domain crutch only in sketches with a
  TODO line. The polling idea for the hydrant sensor was rejected — `OnTriggerEnter` works and stays.
- [x] 2026-11-21 — **The preset list is `[SerializeReference]`** (D-031, G-021, G-022): a
  `[Serializable]` class array gave null slots the Inspector could not fill, so every design change was
  lost. Now the base `PushPresetData{Type, Name}` is abstract with concrete `ImpulsePresetData{Force}`
  and `SustainedPresetData{Force, Curve, Duration}` — **the DTO's type is the shape**, so an Impulse has no
  Duration and no curve fields to hide; `PushKind` and `PushPresetDrawer` are deleted, the runtime branch is
  `IsImpulse`, and the asset is authored through Unity (`script eval` filling
  `managedReferenceValue`), not hand-written YAML. The user confirms the jump and the hydrant both push up.
- [x] 2026-11-21 — **`Name` is gone from the preset** (D-032): the base `PushPresetData` holds `Type` only;
  nothing in the code read a label, and `PresetOf(PushType)` is the only lookup. The asset is re-authored
  without `_name`, numbers unchanged; the unit stays in the `[Tooltip]`. **19/19**.
- [x] 2026-11-21 — **No parameter sweeps for preset numbers** (D-032, ARCHITECTURE §Fixing a bug): numbers
  move from the user playing the mechanic (the `[v]` log is read after), the standing gate is the one suite
  run. The user's rule: sweeping values through tests is "very time consuming" and answers nothing about feel.
- [x] 2026-11-21 — **Dead surface deleted** (D-033): `ApplyImpulse`/`ApplySustained` → one
  `PlayerMotion.ApplyPush`; `LiveVerticalForce()` made private; `IPushPreset` gains `Force` + `IsImpulse`;
  `PushType` keeps the 4 sources that exist (`DebrisShove`, `FireBlast`, `HydrantBurst`, `SprayPuff` gone,
  indices of the kept four unchanged); `BurnableSystem` + `Burnable` deleted, `IBurnable` kept as the Phase 1
  contract; the `[v]` diagnostic log leaves `PlayerMotor`. **19/19**.
- [ ] **Launch-survives-ground test** (with the feel pass, D-026): a weak jet (300 N, < `Mass·Gravity`)
  stays absorbed on the ground; a 2000 N launch accumulates through grounded ticks and rises. Written only
  after the user approves the feel.
- [ ] **Feel pass, one ground number** (the merge D-038 made it one): play a hydrant shove + run **against**
  it, and a shove + run **with** it, at the authored `GroundFriction` **8** in `Physics Settings.asset`. The
  `CounterAbsorb` question is closed — the two runs decay at the same rate, and the run ramps at that rate, so
  the verdict is only the shape of the number: 0→5 m/s in 0.63 s, 0→8 m/s sprint in 1 s, a 10 m/s shove gone
  inside a second. If the feel wants more, **the number moves, not the code** (D-026).
- [ ] **Play check, the press gate** (D-039): hold Space — the player hops once and stops, it must not bounce;
  press once per landing — each hop fires. The run-up jump must still reach further than a standing one
  (D-034 intact). View-driving CLI commands are not allowed (G-025: `capture`/`set_selection` break the
  Pipeline connection), so the user plays and reports.
- [x] 2026-11-22 — **Ice fix: contact gives control authority over momentum** (D-036): in the air the
  component of `ControlVelocity` **along** a shove is removed every tick (running with a launch adds nothing,
  steering across keeps `PlayerSpeedInAir`); on the ground running **against** a shove eats it at
  `GroundFriction` on top of `GroundDrag` (16/s → a 10 m/s shove is gone in ~0.1 s), running **with** it eats
  nothing. The sign of `Dot(shoveAxis, desired)` is the whole rule; `GroundFriction` is reused, no new field.
  **T15a-T15c** (air clamp, steering across, the two decays) → **27 tests, 27 passed**; the 24 existing pass
  through the rule's early exits, so the suite is a witness, not a victim.
- [x] 2026-11-22 — **Authored config split** (D-037): the 16 numbers left the single `GameSettings` screen.
  `PhysicsSettings` + `Physics Settings.asset` = Mass, Gravity, GroundedGravity, TerminalVelocity,
  `GroundFriction`, `GroundDrag`, `AirDrag`, `MaxPushSpeed`, `JumpCarry`; `PlayerControlSettings` +
  `Player Control Settings.asset` = the 3 speeds, the 2 steering rates, `LookSensitivity`, `LookPitchBounds`;
  `IGameSettings` is now `Physics` + `Control` with **no numbers of its own**, and `Game Settings.asset` is two
  object slots. Ports follow readers: `PlayerLookRotator(input, control)`, `PlayerMovement(input, control, look)`,
  `PlayerMotion(movement, physics, control, presets)`, `Player(input, physics, control, presets)`; `StubSettings`
  is `StubPhysics` + `StubControl`. The physics port is **not** player-only (a debris piece or a burnable body
  reads the same numbers), so the name has no `Player` prefix — a rename is one type, one asset, one field.
  The user's tuned `GroundDrag` **8** and `LookPitchBounds` **-75/45** survived, written through Unity (the
  `set_field` object-reference trap is G-024). `unity cmd script recompile` → 0 errors,
  `unity cmd test run --mode editor` → **27 tests, 27 passed** — analytic expectations untouched, so the suite
  is the gate for the move.
- [x] 2026-11-22 — **`GameSettings` Inspector grouped, Odin not taken** (D-035): Unity `[field: Header]` +
  `[Tooltip]` in `GameSettings.cs` (Locomotion / Steering / Body & gravity / Jump / Push channel / Look),
  every number has its unit and what it decides. The dead `<JumpPreset>` block (78 lines, the type is gone
  since D-029) was deleted from `Game Settings.asset` through Unity; `component get_fields` reads back all
  16 values, the user's `LookPitchBounds` -75/45 intact. No runtime cost (Unity attributes), and the web cost
  of Odin (a 720 KB runtime chain) stays unmeasurable until B-001 is fixed. **24/24** (no logic touched).
- [x] 2026-11-22 — **Jump forward carry** (D-034): the jump is the preset **straight up** +
  `JumpCarry × ControlVelocity` in the push channel — a run-up reaches further, the height is untouched,
  standing carries exactly zero (the draft's 0/0 = NaN swallowed the jump, G-023). `JumpCarry` is authored in
  `GameSettings`, default 1. The user played and approved the feel. **T14a-T14e** (standing / carry == speed /
  walk 5 vs sprint 8 / saturates at `MaxPushSpeed` / decays by `AirDrag` alone) →
  `unity cmd test run --mode editor` → **24 tests, 24 passed**, 0.13 s.
- [x] 2026-11-21 — **Authoring numbers for the feel pass** (the asset, not the code): a launch that lifts
  needs `Force > Mass·Gravity` = **1569 N** (80 kg × 19.62), so a 45° nozzle needs ≈ 2220 N. Measured in
  Play Mode: **Jump 2600 N** up, plateau-then-fade over 1 s → peak y **1.85 m**, `vert` 3.24 m/s, and it
  re-arms on every landing while Space is held; **Hydrant 4500 N** (vertical 3182 N > weight) pushes the
  player up and forward — the user sees both in the game. The old 1000 N hydrant was a shove by physics,
  not by design.
- [ ] **B-004** fix so the hydrant is reachable in a simulated run: the spawn point is 2.5 m from the
  trigger, so key-simulation never enters it and the hydrant numbers can only be read by the user playing.
- [x] 2026-11-20 — **Scene wired for D-028** (`component set_field`): `HydrantPush._presets` and
  `Bootstrapper._pushPresetsConfig` → `Push Presets.asset`, `HydrantPush._pushType` → `HydrantPush`;
  `scene save` done. The asset is authored by hand-written YAML (`asset write_text`) — 1000 N decaying
  1→0 over 1 s, jump 2000 N plateau-then-fade over 1 s, spray 300 N flat over 0.1 s. The next step is the
  first **feel check (a)** in Play Mode: the launch must follow `PushDirection.forward`, not the diagonal.
- [x] 2026-11-20 — **Dead authoring surface cleaned** (the D-026 task, done ahead of the feel pass because
  it was the cause of the diagonal launch, D-028): `PushApplication`/`Applies`, `PushKind`,
  `Vector3CurveData` and `ActivePush.FiredImpulse` are deleted, and an Impulse DTO has no `Duration` and no
  curve field to author.
- [ ] **Push UseCases** (the sources side, D-027's second half): `PushDirector` in UseCases +
  `HydrantLaunchUseCase` (timed, curve), `JumpUseCase` (timed, the gate moves here from `PlayerMotion`),
  `SprayShoveUseCase`, `ImpulseKickUseCase` (one-shot, no duration). Each owns its trigger and conditions;
  the simulation stays free of any player knowledge. Order in the updater: input → use cases → player →
  shell.
- [ ] **B-001:** fix the WebGL pipeline routing, then set up the WebGL module/build profile and make the
  first web build (the pipeline asset is still PC-tuned, so re-tune in the same pass — see below).
- [ ] Add `Assets/_Core/Scenes/FnB_Gameplay.unity` to **Build Settings** (G-003) — on WebGL an empty
  scene list ships a page that loads and shows nothing.
- [ ] **Editor feel checks (5)**, each verified with `unity cmd capture scene_view/game_view`:
  (a) launch direction follows the hydrant's rotation, not the player's facing; (b) the player keeps
  moving after a 2 s preset ends; (c) velocity dies on **landing**, not in mid-air; (d) input steers in
  flight at the reduced air speed; (e) a 0.1 s spray for 3 s **saturates**, not accelerates (B-003).
  **The tests for this mechanic are written after the user approves it** (D-026) — the push-model suite
  is edited along with the feel pass, not ahead of it.
- [x] 2026-11-22 — **Friction merge** (D-038): **one authored ground rate**. `GroundFriction` (μN/Mass, m/s²)
  ramps the run to `DesiredVelocity`, brings the control to rest when the input is released, and eats shove
  momentum in every direction. `GroundDrag` (the user's tuned 8) and `GroundAcceleration` (20, unphysical above
  μ — the legs cannot push harder than the ground's reaction) are deleted from both ports, both assets, both
  stubs; `AbsorbCounterRun` (19 lines) and D-036's sign rule are deleted, `ShoveAxis()` stays for the air clamp
  only. The user's 8 is now the ground's authored value (0→5 m/s in 0.63 s, 0→8 m/s in 1 s, a 10 m/s shove gone
  inside a second). Tests re-signed analytically: T3, T5, T13, T15c, T14b/c/e — `unity cmd script recompile` →
  0 errors, `unity cmd test run --mode editor` → **27 tests, 27 passed**. The user's rule: *"за трение с
  поверхностью отвечал один параметр. Иначе это очень сложно настраивать, тестировать и вообще код становится
  сложнее"* — the wall between the two rates was the ice (G-012, D-020, D-036).
- [x] 2026-11-23 — **Bunny-hop gate** (D-039): `IPlayerInput.IsJumping` is now `JumpPressed`, the shell's
  **one-tick press fact** (`WasPressedThisFrame`), `PlayerMovement.IsJumpRequested` reads the fact,
  `PlayerMotion`'s gate (D-021) is **press fact + grounded** — the contact lock `_jumpFiredThisContact` is
  deleted (dead surface once the press is a fact, D-033), so a held Space cannot bounce and a press in flight
  is never remembered. `PressHeldThroughALanding_ReBounces` → `PressHeldThroughALanding_DoesNotReBounce`
  (the landing absorbs the launch; only a fresh press fires), T10c is renamed
  `TimedLaunch_KeepsAccumulating_WhileItsForceRuns_OneEntry`, and the jump tests drive the press through
  `PressJump` (set, one tick, cleared), mirroring the shell. `unity cmd script recompile` → 0 errors,
  `unity cmd test run --mode editor` → **27 tests, 27 passed**. AirDrag/AirAcceleration stay split by the user's
  call: no reaction force in flight, so the two air numbers are not one force.
- [ ] **Recover the design contract.** `docs/TDD.md` + `docs/Preproduction.md` are not in the repo and
  the originals are gone, so the contract exists only as D-008 + `README.md`. Find them, or restate the
  contract as a new DECISIONS entry; until then nothing cites a "TDD §n" as if the file existed.
- [ ] **Asmdef housekeeping:** drop the `CartoonFX` vendor-demo reference (an asset-store update can
  delete it and break our compile) and fix the `name` typo `FlameNBlame-asmdf` (G-001, D-014).
- [x] 2026-11-20 — **`Player.prefab`: the orphan `JumpPreset` block is gone** — `JumpPreset` left
  `IGameSettings`/`GameSettings` with D-028, so nothing references it; verified by grep of the prefab. The
  non-kinematic Rigidbody beside the `CharacterController` is still an open call.
- [ ] **Delete the stray `Player.cs` at repo root** — an untracked copy of
  `Assets/_Core/Scripts/Domain/Player.cs` that does not compile on its own (G-006).
- [ ] `Move Assets/_Core/Scripts/X Bot@Falling.fbx` to `Assets/_Core/Art/…` and rename per STYLE.md.
- [ ] Set `companyName` in ProjectSettings (now `DefaultCompany`) — it becomes the web build folder and
  product path, so set it before the first web build.

## The user's calls

- [x] 2026-11-20 — **`House Blockade` deleted** from `FnB_Gameplay.unity` by the user's call
  (`unity cmd gameobject delete` → `scene save`; verified in the Scene View capture). The CLI
  scene-driving proof stands (D-023).
- [x] 2026-11-20 — **Unity skills deleted** — `unity-cli` + `unity-pipeline` removed from
  `.opencode/skills/` and `~/.claude/skills/` (D-025). Nothing Unity generates in the repo now, so the
  git-tracking question is closed. Feel's 8 skill folders stay in `Assets/Feel/Skills/` (G-004).
- [x] 2026-11-20 — **`PushApplication`/`Applies` decided: cut** (D-028). The question was closed with the
  file deleted: `GroundedOnly` served only the jump, D-021's gate made it redundant, and the test T9 that
  asserted it was the invented requirement. Nothing left to supersede.

## Phase 1 — base (pillar 1)

- [ ] FPS controller: move, mouse-look, jump — [x] the jump gate and the push model exist (D-016, D-021)
- [ ] Interaction (E / LMB) with props and the hydrant
- [ ] One carry slot: pick up / hold / activate a single item
- [ ] `Burnable` object state: ignite → blacken → collapse and disappear (object state, never a fluid
  sim, D-008)
- [ ] Fire spread: a burnable near fire ignites after ~10 s, so fire propagates itself
- [ ] Extinguish path (temporary/brute-force is fine at this stage)
- [ ] Fire visuals + sound
- [ ] Location: a house full of burnables + a street with the hydrant; music + ambience
- [ ] Menu with a Play button; load/unload of the gameplay scene
- [ ] Game state control: menu / in-game / pause
- [ ] Goal: extinguish the house and walk out (scoring stays deferred, D-008)

## Phase 2 — extinguisher, the main system (pillar 2)

- [ ] Take / carry / throw, Rigidbody-based
- [ ] Spray: a Particle System stream that extinguishes fire
- [ ] Spray also applies physical force to objects **and to the player**
- [ ] **Spray shove convention (D-016, G-012):** re-call `ReceivePush` every 0.1 s with `Duration: 0.1`
  and a **flat** `Curve`, direction refreshed from the aim each call — replace-per-`Type` keeps one
  entry, `MaxPushSpeed` makes it saturate (B-003 is this rule's gate). A decay envelope is only for a
  one-shot puff.
- [ ] Extra reaction on hard impact / fall (foam, recoil)
- [ ] Noticeable feedback: sound, particles, camera — no haptics (G-011)
- [ ] Optional if time allows: consumable resource, "Turbo" mode

## Phase 3 — hydrant catapult, the wow effect (pillar 3)

- [ ] Hydrant rotatable 360°, water gushing continuously from the nozzle
- [ ] Block-the-water interaction: camera + audio strain escalate the longer it is held
- [ ] Burst: a strong impulse launching the player along the nozzle + upward
- [ ] Loss of player control during flight and landing (optional; the control-lock idea was cut by D-016,
  re-deciding needs a new entry)
- [ ] Verify it does not break level collisions critically

## Phase 4 — polish, stability, submission

- [ ] Fix critical bugs; balance forces and speeds
- [ ] Minimal VFX/SFX pass
- [ ] WebGL stability pass: long-session memory growth, shader-variant load hitches, reload-on-click
- [ ] Submission assets: scenario, video ≤ 5 min, a short write-up on what was made, what was cut on
  purpose, and how AI was used

## WebGL budget & retune (D-007, D-009)

- [x] 2026-11-19 — **Player push-model budget written before code:** 8 simultaneous pushes,
  `List<ActivePush>` capacity 8 with swap-remove and struct entries, no per-tick allocation, cost target
  < 0.05 ms per player per tick. Verified by test T12.
- [ ] Write the remaining perf budgets **before** code: max burning objects, live particles for
  spray/fire, awake rigidbodies, target frame time — one shared budget (D-007).
- [ ] Retune `Web_RPAsset`/`Web_Renderer` for web: the rename kept PC-grade settings (HDR, shadow
  distance, post-processing) under a web name — strip and measure.
- [ ] Re-cut the quality levels for a web-only target (they are still named `Mobile`/`PC`) so what we
  test is what ships (B-001).
- [ ] Web-build-every-session is **suspended** by D-019; un-suspend when B-001 is fixed. The
  budget-before-code rule stays in force.

## Deferred (needs a DECISIONS entry before entering scope, D-008)

- [ ] Push-source registry routing (deferred by D-016 — today a source calls the shell and the shell
  forwards). A `UseCases` source class per push is the right shape **where a source has scenario logic**
  (spray: aim + hit test + interval; hydrant charge) and the wrong shape for the jump.
- [ ] Anisotropic ground friction (needs a contact normal, which changes what the shell reports, D-020).
- [ ] Meta/scoring, full co-op, building destruction, ragdoll/IK, the dam.
- [ ] A fixed step (`IFixedUpdatable`) — struck by D-018; re-opens only as a new decision for a system
  that needs deterministic stepping.
- [ ] Optionally remove the duplicate tracked `Flame_Blame-challenge.slnx` (G-007).

## Done

- [x] 2026-11-20 — **Docs re-scoped, one job per file; BUGS.md merged into this file** (D-024).
- [x] 2026-11-20 — **CLI scene driving proven end to end**: `gameobject create` → `set_transform` →
  `component add` → `capture scene_view/game_view`, saved into `FnB_Gameplay.unity`; the test wall
  (`House Blockade`) was deleted after the user's call (D-023, G-014).
- [x] 2026-11-20 — **First PlayMode test run through the CLI**: 19 tests, 18 passed, 1 failed (B-003),
  0.09 s. G-008, D-016.
- [x] 2026-11-20 — **Compile gate verified, 5 runs**: `unity cmd script recompile` → `up_to_date`,
  `editor status` → `compiling: false`, `console get` → no `error CS`. B-002 `Verified` (D-022).
- [x] 2026-11-20 — Unity CLI updated 1.0.0 → 1.0.1; MCP probe done, bridge deliberately left unwired
  (D-022, G-013); skills went 37 → 2 → **0**: all Unity skill folders deleted from the project and the
  global folder, the CLI documents itself (D-023, D-025, G-015).
- [x] 2026-11-19 — Push model + jump written (D-016, D-017, D-020, D-021) and the 19 tests + the
  `FlameNBlame.Tests` asmdef; `com.unity.test-framework` made a direct dependency (G-008).
- [x] 2026-11-19 — `HydrantPush` moved into `InterfaceAdapters/` with its `.meta` GUID intact;
  `Scripts/Unity/` and `Scripts/Application/` deleted (D-012).
- [x] 2026-11-19 — Composition shape applied: `GamePlaySession.StartGamePlay()` registers
  `PlayerInputMain` → `Player` → `PlayerMotor`; one type per file; `IGameSettings` split out.
- [x] 2026-11-17 — Architecture map written (ARCHITECTURE.md, D-012–D-015); namespace root fixed
  (G-002, D-014); asmdef `name` fixed (G-001).
- [x] 2026-11-16 — Scope locked to the four pillars (D-008); WebGL from day one (D-007); Nice
  Vibrations dormant (D-010); `docs/` created (D-011); B-001 + G-010 discovered and logged.
- [x] 2026-11-15 — Docs scaffolding (DECISIONS/GOTCHAS/TODO/STYLE) + `CLAUDE.md`.
