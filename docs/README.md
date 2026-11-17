# Project documentation

Everything we know about this game, and everything we decided, lives here. The repo root keeps
only `README.md` (what the project is) and `CLAUDE.md` (what an AI agent must know before working)
— `CLAUDE.md` cannot move into this folder, see [GOTCHAS.md](GOTCHAS.md) G-009.

## Design (source of truth)

| File | What it is | Language |
|------|-----------|----------|
| [TDD.md](TDD.md) | **Tech Design — the binding scope.** Base + extinguisher + hydrant catapult + fire system, in ~8 days. Decided 2026-11-16 to be the final output; see [DECISIONS.md](DECISIONS.md) D-008. | UA |
| [Preproduction.md](Preproduction.md) | The challenge brief, judging criteria, 14 trailer observations and the scored idea brainstorm that led to the TDD. Kept for the *why*, not the *what*. | EN/UA |

Originals came from `D:\Downloads\Flame_n_Blame_Challenge_Tech_Design.md` and
`Flame_n_Blame_Challenge_Preproduction.md`, copied in on 2026-11-16. They are **kept in their
original language and wording** — don't translate or tidy them, they're the contract; edit the
decisions in `DECISIONS.md` instead.

> ⚠️ **Status 2026-11-17: neither file is actually in `docs/` yet.** The copy never landed (the
> write was blocked mid-session, see TODO.md "High priority"), and they are no longer in
> `D:\Downloads\` either — so the contract exists only in the summaries quoted in DECISIONS.md
> D-008 and in `../README.md`. Until they are recovered, cite the pillars via D-008, not by TDD
> section number.

## Project memory (part of doing the work, not an afterthought)

| File | Use it for |
|------|-----------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | The code map: layers, which way dependencies point, who owns the tick, the Unity boundary. Read before adding a system; update it whenever the shape moves (D-015). |
| [DECISIONS.md](DECISIONS.md) | Architecture calls with the *why*. Never edit an old entry — supersede it. |
| [GOTCHAS.md](GOTCHAS.md) | The moment we get burned or find a hard restriction. One-line **Rule** each. |
| [BUGS.md](BUGS.md) | Confirmed bugs, `Open → Repro → In progress → Fixed → Verified`. |
| [TODO.md](TODO.md) | Anything we must not forget, plus the TDD phase checklists. |
| [STYLE.md](STYLE.md) | C#, asset and git conventions. |

## Reading order for new work

1. `../CLAUDE.md` — commands, assembly, vendor boundaries.
2. `TDD.md` — what we are building (see the status warning above).
3. `ARCHITECTURE.md` — how the code is shaped, and where a new system goes.
4. `GOTCHAS.md` + `TODO.md` — what will bite us and what's next.
5. `DECISIONS.md` — when something looks like a strange choice, it's probably logged there.
