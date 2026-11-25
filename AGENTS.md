# AGENTS.md

`CLAUDE.md` at the repo root is the source of truth for agents: **read it first and treat it as this
file.** It is thin by design, and it carries the facts most often guessed wrong — the Unity CLI over
batch mode, our code in `Assets/_Core` only, game rules as plain C# classes, one `Update()` on
`Bootstrapper`, namespaces by layer, the perf budget before code.

Reading order, routing and the doc budgets live in [`docs/README.md`](docs/README.md); the current-state
map is `docs/DECISIONS.md` §Current structure. This file contains nothing else, on purpose.
