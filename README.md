# Flame & Blame Challenge

A firefighter mini-game built for the **Flame & Blame** mechanic challenge: take one mechanic and
make it work *as a system*, not as a standalone effect. Firefighters, co-op chaos, and physical
comedy in the spirit of the game's trailer.

**The build:** you are a firefighter on FPS feet, carrying one thing in your hands at a time.
A house is burning — the fire doesn't sit still, it spreads to nearby objects and grows itself, so
the only way out is the extinguisher in your hands. The spray both kills flames and shoves things
(and you) around. On the street outside stands a modified fire hydrant you can rotate 360°: put a
finger on the gushing water and the camera and sound strain harder and harder until it breaks free
and launches you along the nozzle. Goal: get the house extinguished and walk out alive.

**The whole point is chaos that reads at a glance** — the mechanic has to answer every action the
player takes, and interact with the props and other systems rather than being an isolated effect.

## Target platform

**WebGL is the shipping target from day one** — not a final checkbox. The build has to run in a
browser tab, which means a single shared performance budget for fire, particles and physics, and no
haptics at all. Every system is judged against a web build, not just against the editor
([docs/DECISIONS.md](docs/DECISIONS.md) D-007).

## Scope

Four pillars: the FPS controller with a single carry slot, the object-based fire system, the
extinguisher, and the hydrant catapult. Meta/scoring, full co-op, building destruction, ragdoll/IK and
the dam are deliberately out — the reasoning is in
[docs/DECISIONS.md](docs/DECISIONS.md) D-008. (The design contract `TDD.md` is not in the repo; the
pillars are recorded through D-008.)

## Documentation

All project docs live in the [`docs/`](docs/) folder, one job per file: the design contract
(`TDD.md`, `Preproduction.md`) and the working memory we keep current as we build — `TODO.md` (the work
list, bugs included), `DECISIONS.md` (current structure + the decision log), `ARCHITECTURE.md`
(how to build), `GOTCHAS.md` (traps), `STYLE.md` (conventions). Start with
[`docs/README.md`](docs/README.md) for the reading order, and
[`CLAUDE.md`](CLAUDE.md) for behaviour, commands and vendor boundaries.

## Engine

Unity 6 (6000.6.5f1), URP 17.6.0, Input System, PrimeTween, UniTask, Cinemachine, Feel (MMF_Player).
Game code lives only in `Assets/_Core`: game logic as plain C# classes driven by a tick we own, with
Unity kept at the edge of it — the map is in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).
