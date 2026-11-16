# DECISIONS.md — Architectural Decision Log

Records important, non-trivial decisions made during development, so we don't relitigate them
later or "quickly improve" something and spend hours fixing what we broke.

**Rules:**
- Every entry gets a sequential ID (`D-001`), a date, and a status.
- Write entries *when* a decision is made, not after.
- A decision can only be changed by adding a new entry that supersedes the old one — never
  silently edit or delete an old decision (mark the old one `Superseded by D-XXX`).
- "Trivial" (obvious, easily reversible, no system-wide impact) → don't log it here.
- One decision per entry. Keep the *why* — that's the part we'll forget.

**Entry format:**
```
## D-XXX: <Short title>
- **Date:** YYYY-MM-DD
- **Status:** Accepted | Superseded by D-YYY | Reverted
- **Context:** What problem or constraint forced a choice.
- **Decision:** What we chose.
- **Why:** The reasoning, including the alternatives we rejected.
- **Consequences:** What this locks us into / what we now owe.
```

---

## D-001: Unity 6000.6.5f1 with URP
- **Date:** 2026-11-15
- **Status:** Accepted (made at project creation)
- **Context:** New 3D game project; needed a modern, supported pipeline with VFX Graph, Shader Graph and scalable quality (PC + mobile profiles exist in `Assets/Settings/`).
- **Decision:** Unity 6 6000.6.5f1, Universal Render Pipeline 17.6.0.
- **Why:** Unity 6 LTS line; URP covers both PC and mobile perf targets; VFX Graph available for fire/effects. HDRP rejected as overkill for the scope; Built-in rejected for tooling stagnation.
- **Consequences:** Must use URP-compatible shaders/assets; some HDRP-only assets are out.

## D-002: Input System package only (no legacy Input)
- **Date:** 2026-11-15
- **Status:** Accepted (project setting `activeInputHandler: 1`)
- **Context:** Project must handle keyboard/gamepad cleanly and possibly rebinding.
- **Decision:** Unity Input System 1.20.1 exclusively. Actions live in `Assets/_Core/InputSystem_Actions.inputactions` (Player + UI maps).
- **Why:** Legacy `Input.*` is deprecated-path; Input System gives device abstraction and action maps for free.
- **Consequences:** Never call legacy `Input.GetAxis/GetKey`; any new action must be added to the `.inputactions` asset. PlayerInput or direct `InputAction` usage both OK.

## D-003: All game code lives under `Assets/_Core` behind one asmdef
- **Date:** 2026-11-15
- **Status:** Accepted
- **Context:** Mixed workspace of store assets; need a hard boundary between "our code" and "vendor code".
- **Decision:** `Assets/_Core/{Art,Scenes,Scripts}` is the only place game code goes. Runtime assembly: `FlameNBlame.asmdef` referencing InputSystem, PrimeTween, Cinemachine, UniTask.
- **Why:** Vendor folders get overwritten by asset-store updates; a single game assembly keeps compile times low and dependency direction explicit.
- **Consequences:** New vendor dependency (e.g., Feel's `MoreMountains.Tools`) must be added to the asmdef reference list explicitly. Editor-only code goes in a separate `_Core/Editor` asmdef, never in the runtime one.

## D-004: PrimeTween for tweens, UniTask for async
- **Date:** 2026-11-15
- **Status:** Accepted (made at project setup)
- **Context:** Need tweening and async without allocation churn or coroutine spaghetti.
- **Decision:** PrimeTween (local tgz package) for all tweening; UniTask for all async/await.
- **Why:** PrimeTween is allocation-free and simple; UniTask integrates with Unity's player loop and replaces `StartCoroutine` entirely.
- **Consequences:** Don't use DOTween (not installed) or `StartCoroutine` in new code; both libs are referenced by the game asmdef already.

## D-005: Feel 6.3 for game juice and feedback
- **Date:** 2026-11-15
- **Status:** Accepted (made at project setup)
- **Context:** Game needs impactful feedback (screenshake, haptics, particles, sound) without hand-rolling each effect.
- **Decision:** Use MMF_Player feedbacks from `Assets/Feel` for juice; Nice Vibrations (bundled) for haptics.
- **Why:** Battle-tested component library; AI skills for it ship in `Assets/Feel/Skills/`.
- **Consequences:** Custom code must stay out of `Assets/Feel/` (updates overwrite it). Feel-specific workflows follow the skills in `Assets/Feel/Skills/`.

## D-006: Root-level Markdown files as project memory
- **Date:** 2026-11-15
- **Status:** Accepted
- **Context:** Decisions, mistakes, bugs and tasks need to survive session boundaries for both humans and AI agents.
- **Decision:** `DECISIONS.md`, `GOTCHAS.md`, `BUGS.md`, `TODO.md`, `STYLE.md` at repo root; `CLAUDE.md` points to them and stays under 200 lines.
- **Why:** Plain files are readable by everyone and versioned with the code; CLAUDE.md is re-read by every agent session, the others are consulted on demand.
- **Consequences:** Updating these files is part of doing the work, not an afterthought.
