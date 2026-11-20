# Project documentation

One job per file (D-024). Repo root keeps `README.md` (what the project is) and `CLAUDE.md` (how an
agent behaves) — `CLAUDE.md` cannot move here, see G-009.

## Design (source of truth)

| File | What it is | Language |
|------|-----------|----------|
| [TDD.md](TDD.md) | **Tech Design — the binding scope.** Base + extinguisher + hydrant catapult + fire system, in ~8 days. Final output by decision, see D-008. | UA |
| [Preproduction.md](Preproduction.md) | The challenge brief, judging criteria, trailer observations and the scored brainstorm that led to the TDD. Kept for the *why*, not the *what*. | EN/UA |

The originals came from `D:\Downloads\` and were copied in on 2026-11-16. They are kept in their
original language and wording — don't translate or tidy them, they're the contract; scope changes go in
`DECISIONS.md`.

> ⚠️ **Neither file is in `docs/`.** The copy never landed and the originals are gone, so the contract
> exists only as D-008 + `../README.md`. Cite the pillars through D-008, never as "TDD §n". Recovering
> them is a TODO item.

## Project memory

| File | Its one job |
|------|-----------|
| [DECISIONS.md](DECISIONS.md) | **§Current structure** — folders, namespaces, assemblies, who talks to what, what is in the scene. Plus the append-only decision log with the *why*. Supersede, never rewrite. |
| [ARCHITECTURE.md](ARCHITECTURE.md) | **How to build:** the READ-ONLY rules, layers, dependency direction, who owns the tick, the Unity boundary, the checklist for a new system. No rationale, no history. |
| [TODO.md](TODO.md) | **The work list** — bugs (the `B-` ids), what is now, the phase checklists, what is done. Bugs live here, not in a separate file. |
| [GOTCHAS.md](GOTCHAS.md) | **Traps we fell into**, each with a one-line Rule. |
| [STYLE.md](STYLE.md) | **Conventions** — C#, assets, perf, git. |

Routing: work/bug → TODO · call → DECISIONS · shape → ARCHITECTURE · burned → GOTCHAS · convention →
STYLE. `BUGS.md` was removed on 2026-11-20 (D-024).

## Reading order

1. `../CLAUDE.md` — behaviour, commands, boundaries.
2. `DECISIONS.md` §Current structure — what exists today (verify against the files: a doc is a map).
3. `ARCHITECTURE.md` — before adding a system.
4. `TODO.md` + `GOTCHAS.md` — what is next and what bites.
5. `STYLE.md` — how to write it; `DECISIONS.md` when a choice looks strange.
