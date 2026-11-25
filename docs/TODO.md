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

- [ ] **B-001** `Open` — **WebGL has no render pipeline.** The quality levels bind a deleted
  `Mobile_RPAsset`, and `Web_RPAsset` sits on a level that excludes WebGL, with no fallback in
  `GraphicsSettings` — a web build renders magenta while the editor looks fine. Fix: a WebGL level on
  `Web_RPAsset`, then re-cut the levels for web (G-010, D-009). Every web measurement is worthless until then.
- [x] **B-003** `Verified 2026-11-20` — **the spray-saturation test was unpassable by construction:** a
  window at the start of an exponential ramp compared against the asymptote fails at any τ, and the cap sat
  above the force/drag equilibrium, so the clamp was never exercised. Fixed by D-027: the analytic
  equilibrium, the monotone series, the plateau inside 3τ, plus a case with the equilibrium above the cap.
- [ ] **B-004** `Open` — **`PlayerMotor` is attached to nothing.** The scene has no `Player` GameObject, so
  nothing moves in Play Mode: the tick runs, the domain computes velocity, no shell consumes it. The same
  work as the first item below.
- [x] **B-002** `Verified 2026-11-20` — the tree did not compile (two duplicate `ForcePreset` drafts,
  `using NUnit.Framework` in the runtime asmdef, a dangling statement in `Bootstrapper`). Fixed by D-016.
  `autoRefresh` is **disabled**, so the CLI recompile gate after every edit is the standing check.

## Now (blocking, in this order)

- [ ] **B-004:** `ResourceSpawner.SpawnPlayer()` instantiates `Player.prefab` at the prefab's own origin and
  ignores `/=== World/Player Spawn Point`, so the player starts far from the hydrant and never touches its
  trigger. Fix: place the clone at the spawn point (the spawner owns the position, the prefab keeps its
  origin).
- [ ] **Recover the design contract files** (`TDD.md`, `Preproduction.md`) into the repo, or delete every
  reference to them — today the contract exists only as D-008 + `../README.md`.
- [ ] **Push UseCases** (the sources side, D-027's second half): `PushDirector` in UseCases +
  `HydrantLaunchUseCase` (timed, curve), `JumpUseCase` (timed, the gate moves here from `PlayerMotion`),
  `SprayShoveUseCase`, `ImpulseKickUseCase` (one-shot, no duration). Each owns its trigger and conditions;
  the simulation stays free of any player knowledge. Order in the updater: input → use cases → player →
  shell.
- [ ] **Play checks (5)**, each made by the user playing (G-025 — the CLI is a facts tool, it does not
  capture): (a) launch direction follows the hydrant's rotation, not the player's facing; (b) the player keeps
  moving after a 2 s preset ends; (c) velocity dies on **landing**, not in mid-air; (d) input steers in flight
  at the reduced air speed; (e) a 0.1 s spray for 3 s **saturates**, not accelerates (B-003).
- [ ] **Fire tests after the feel verdict** (D-026). The lab stand is deleted (D-048), so the look and the
  preset numbers are the user's call, made by playing props that burn.
- [ ] **Asmdef housekeeping:** drop the `CartoonFX` vendor-demo reference (an asset-store update can
  delete it and break our compile) and fix the `name` typo `FlameNBlame-asmdf` (G-001, D-014).
- [ ] **Delete the stray `Player.cs` at repo root** — an untracked copy of
  `Assets/_Core/Scripts/Domain/Player.cs` that does not compile on its own (G-006).
- [ ] `Move Assets/_Core/Scripts/X Bot@Falling.fbx` to `Assets/_Core/Art/…` and rename per STYLE.md.
- [ ] Set `companyName` in ProjectSettings (now `DefaultCompany`) — it becomes the web build folder and
  product path, so set it before the first web build.

## Phase 1 — base (pillar 1)

- [ ] FPS controller: move, mouse-look, jump — [x] the jump gate and the push model exist (D-016, D-021)
- [ ] Interaction (E / LMB) with props and the hydrant
- [ ] One carry slot: pick up / hold / activate a single item
- [ ] Sound for ignite / burn / extinguish / collapse (no VFX sound yet)
- [ ] Location: a house full of burnables + a street with the hydrant; music + ambience
- [ ] Menu with a Play button; load/unload of the gameplay scene
- [ ] Game state control: menu / in-game / pause
- [ ] Goal: extinguish the house and walk out (scoring stays deferred, D-008)

## Phase 2 — extinguisher, the main system (pillar 2)

- [ ] Take / carry / throw, Rigidbody-based
- [ ] Spray: a Particle System stream that extinguishes fire — it enters through
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

- [x] 2026-11-25 — **Docs compacted to their budgets, and the budgets made into a gate**: `DocBudgetTests`
  asserts the table in docs/README.md, routing is written once in docs/README.md, `AGENTS.md` is now a pure
  pointer, DECISIONS/GOTCHAS/ARCHITECTURE rewritten (D-049, D-024).
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
