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
- [ ] **B-003** `Open` — **the push channel never saturates.** `PlayerMotionTests.ContinuousSpray_
  Saturates_WithoutStackingPushes` fails at `PlayerMotionTests.cs:234`: *"the shove must converge, not
  keep growing (early 6.556155, late 7.865624)"*. `MaxPushSpeed` is authored but never clamped in
  `PlayerMotion`. Repro: `unity cmd test run --mode editor` (synchronous, EditMode, no scene load),
  result `1 of 19 failed` (deterministic, 0.006 s).
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

- [ ] **B-004:** create the `Player` GameObject under `=== World/GrayBox Level` near `Player Spawn
  Point` (world 2.84, 0, 7.78), attach `PlayerMotor` + `CharacterController`, save the scene.
- [x] 2026-11-20 — **Suite made EditMode** (`includePlatforms: ["Editor"]` in
  `FlameNBlame.Tests.asmdef`): `unity cmd test run --mode editor` → **19 tests, 18 passed, 1 failed**
  (B-003), synchronous, no scene load, ~0.11 s (D-026).
- [ ] **B-003:** clamp the push channel at `MaxPushSpeed` in `PlayerMotion`, re-run the suite.
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
- [ ] **Re-tune `GroundDrag`** (authored 4, in `GameSettings` + `Game Settings.asset`): ~1 = a long skid,
  past 8 = a hard stop; `GroundFriction` stays authored separately (D-020).
- [ ] **Recover the design contract.** `docs/TDD.md` + `docs/Preproduction.md` are not in the repo and
  the originals are gone, so the contract exists only as D-008 + `README.md`. Find them, or restate the
  contract as a new DECISIONS entry; until then nothing cites a "TDD §n" as if the file existed.
- [ ] **Asmdef housekeeping:** drop the `CartoonFX` vendor-demo reference (an asset-store update can
  delete it and break our compile) and fix the `name` typo `FlameNBlame-asmdf` (G-001, D-014).
- [ ] **`Player.prefab`:** remove the orphan `JumpPreset` block (D-017); decide the non-kinematic
  Rigidbody sitting beside the `CharacterController` (delete it, or make it kinematic) and log the call.
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
- [ ] **Open question, not decided: does `PushApplication`/`Applies` exist at all?** Its `GroundedOnly`
  was used by exactly one preset (the jump), and D-021's gate makes that use redundant; the spray wants
  `Always`. Cutting it removes an enum file, one DTO field, one branch in `TickPushes` and test T9 — the
  requirement T9 asserts was invented, never stated by the design. Nothing cut yet.

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
