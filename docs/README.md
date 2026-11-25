# Project documentation

This file is the **only** place that defines where content goes and how long it may be. Every other doc
points here; do not restate these rules inside a doc.

## The files

| File | Its one job |
|---|---|
| `../CLAUDE.md` | How an agent behaves: rules, commands, vendor boundaries. Root only, auto-loaded (G-009). |
| `../AGENTS.md` | A pointer to `CLAUDE.md`. Contains nothing else. |
| `../README.md` | What the project is. |
| `DECISIONS.md` | §Current structure — the state map, rewritten freely — and the decision log, the *why*. |
| `ARCHITECTURE.md` | How to build: READ-ONLY rules, layers, tick ownership, the Unity boundary, the checklist. No rationale, no history. |
| `TODO.md` | The work list, bugs included (`- [ ] B-xxx`, done when the test passes). |
| `GOTCHAS.md` | Traps we fell into, each with a one-line Rule. |
| `STYLE.md` | Conventions: C#, perf, tests, assets, git. |

## Routing — where a new fact goes

- Work, bug, status of a task → **TODO.md**, one line.
- A non-obvious call, and its *why* → **DECISIONS.md**, one entry ≤ 10 lines.
- A shape of the code (layer, tick, boundary, checklist item) → **ARCHITECTURE.md**, in the same change that moves the layer.
- A trap we fell into → **GOTCHAS.md**, the moment we get burned, ≤ 4 lines.
- A convention → **STYLE.md**.
- Numbers, gate outputs, test counts, CLI transcripts, scene coordinates, probe values → **the commit body**, never a doc.
- Obvious, easily reversible, no system impact → nowhere. Do not log it.

## Budgets (checked by `DocBudgetTests`, a failing test is the signal to compact)

| File | Lines | Per entry | Max line |
|---|---|---|---|
| `CLAUDE.md` | 135 | — | 110 |
| `AGENTS.md` | 25 | — | 110 |
| `README.md` (root) | 50 | — | 110 |
| `docs/README.md` | 60 | — | 140 |
| `DECISIONS.md` | 400 | 10 | 320 |
| `ARCHITECTURE.md` | 200 | — | 320 |
| `TODO.md` | 150 | — | 120 |
| `GOTCHAS.md` | 160 | 4 | 320 |
| `STYLE.md` | 110 | — | 120 |

The numbers live here and in `DocBudgetTests` — change both together, never one. A doc over its budget is
compacted, not the budget raised: the number is the point of the exercise.

**Session start:** skim `TODO.md` + `GOTCHAS.md`, `ARCHITECTURE.md` before building, `DECISIONS.md`
§Current structure when a choice looks strange. If a file is over budget, compact it **before** writing
code — a doc that grows past its budget is a doc whose content is being read less.

**Design docs (`TDD.md`, `Preproduction.md`) are the contract:** read, never rewrite. **They are not in
the repo** — the copy never landed, the originals are gone, so the contract exists only as D-008 +
`../README.md`: cite pillars through D-008, never "TDD §n"; a scope change is a new DECISIONS entry.

## Reading order

1. `../CLAUDE.md` — behaviour, commands, boundaries.
2. `DECISIONS.md` §Current structure — what exists today (verify against the files: a doc is a map).
3. `ARCHITECTURE.md` — before adding a system.
4. `TODO.md` + `GOTCHAS.md` — what is next and what bites.
5. `STYLE.md` — how to write it; `DECISIONS.md` when a choice looks strange.
