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
- [ ] 2026-11-17 — Fix the asmdef `name` field: it is the typo `FlameNBlame-asmdf` while every doc
      calls the assembly `FlameNBlame`; references are GUID-based so renaming is safe (do it with G-001)
- [ ] 2026-11-17 — Apply the composition shape to the existing scaffolding (ARCHITECTURE.md §8): create
      `Scripts/UseCases/` + `Scripts/Domain/`, move `GamePlaySession`/`PlayerState`/`PlayerMovement` out
      of `Bootstrapper.cs` (one type per file, STYLE.md), call `StartGamePlay()`, register updatables,
      and dispose `PlayerInputMain` on teardown
- [ ] 2026-11-17 — Add `IFixedUpdatable` + the single `FixedUpdate` host before implementing pillar 2's
      spray forces (D-013) — physics impulses and queries on the fixed step, visuals on the update step
- [ ] 2026-11-16 — Create first gameplay scene, add to Build Settings (G-003)
- [ ] 2026-11-16 — Add the WebGL module/build profile and get one empty-scene web build deploying, before any gameplay exists (D-007)
- [ ] 2026-11-16 — Set `companyName` in ProjectSettings (currently `DefaultCompany`) — it becomes the web build folder + product path
- [ ] 2026-11-16 — Create `.claude/skills` junction → `Assets/Feel/Skills` (G-004)

## WebGL budget & retune (D-007, D-009)

- [ ] 2026-11-16 — Write a perf budget **before** implementing each system: max simultaneous burning objects, max live particles for spray/fire, max awake rigidbodies, target frame time
- [ ] 2026-11-16 — Retune `Web_RPAsset`/`Web_Renderer` for web: the rename kept PC-grade settings (HDR, shadow distance, post-processing) under a web name — strip and measure
- [ ] 2026-11-16 — Re-cut quality levels for a web-only target (they are still named `Mobile`/`PC`) so what we test is what ships
- [ ] 2026-11-16 — Make a WebGL build part of every session's end-of-work check, not a milestone

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
- [ ] 2026-11-16 — Decide + log how player knockback is coupled (the TDD flags this as an open question — "how to adequately bind the player's own pushback? It's an external system"). Needs its own DECISIONS entry before coding.
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
- [ ] 2026-11-15 — When first tests are written: add `com.unity.test-framework` to manifest, create `_Core/Tests` asmdef (G-008)
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
