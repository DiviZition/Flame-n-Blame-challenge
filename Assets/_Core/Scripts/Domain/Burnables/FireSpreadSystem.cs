using Flame_n_Blame_Challenge.Adapters;
using System.Collections.Generic;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// Fire spread over the set of burnables: a burning object heats its neighbours, and a neighbour that has
    /// absorbed enough heat ignites (D-008: ~10 s of proximity, so a fire propagates itself).
    ///
    /// The work is **a cursor over the list, not a full sweep every frame** (D-043, WebGL budget in TODO.md):
    /// `n = ceil(count / FRAMES_PER_SWEEP)` objects are serviced per frame, so each object is visited every
    /// ~4 frames and a frame pays `n × count` distance checks — 24 objects, n = 6 → **144 checks a frame**,
    /// no allocation.
    ///
    /// **Determinism:** a visit carries `elapsed` — the time since that object's own last visit — so the heat
    /// one object receives per second is the same whatever `n` is. The sweep is an integral over time, not a
    /// sampling of the state.
    ///
    /// **The heat leaves through the shell's stream door** (`IHeatSink`), the same door the spray uses, so the
    /// shell hears the fire as a stream, not as a number growing in a body it cannot see (D-052). A visit also
    /// names the **strongest** in-range neighbour: who is heating me is a fact about the fire, and the shell
    /// puts the char front on the side that neighbour stands on.
    ///
    /// Positions are the ones the shells reported at composition: the props of this game do not move, so the
    /// system never asks for a live position (ARCHITECTURE §5 — the engine owns the body).
    public class FireSpreadSystem : IUpdatable
    {
        private const int FRAMES_PER_SWEEP = 4;

        private readonly List<IBurnable> _burnables = new List<IBurnable>(64);
        private readonly List<IHeatSink> _sinks = new List<IHeatSink>(64);
        private readonly List<float> _lastVisit = new List<float>(64);
        private int _cursor;

        /// Composition: the host, its sink, and where it stands. The position is the shell's, handed once — the
        /// spread system holds no authority over the world.
        public void Add(IBurnable burnable, IHeatSink sink)
        {
            _burnables.Add(burnable);
            _sinks.Add(sink);
            _lastVisit.Add(0f);
        }

        public void UpdateLogic(float time, float deltaTime)
        {
            int count = _burnables.Count;
            if (count == 0)
                return;

            int perVisit = (count + FRAMES_PER_SWEEP - 1) / FRAMES_PER_SWEEP;

            for (int k = 0; k < perVisit; k++)
            {
                int target = _cursor;
                _cursor = (_cursor + 1) % count;

                float elapsed = time - _lastVisit[target];
                _lastVisit[target] = time;

                IBurnable burnable = _burnables[target];
                if (burnable == null || burnable.IsBurnedOut)
                    continue;

                Vector3 targetPosition = burnable.Position;
                float influence = 0f;
                float strongestContribution = 0f;
                int strongestSource = -1;

                for (int i = 0; i < count; i++)
                {
                    if (i == target)
                        continue;

                    IBurnable source = _burnables[i];
                    if (source == null || !source.IsBurning)
                        continue;

                    float contribution = source.GetFireInfluenceAtDistance(
                        Vector3.Distance(targetPosition, source.Position));

                    influence += contribution;

                    if (contribution > strongestContribution)
                    {
                        strongestContribution = contribution;
                        strongestSource = i;
                    }
                }

                // No burning neighbour in reach — no stream. `influence` above 0 means the strongest is found.
                if (influence <= 0f)
                    continue;

                _sinks[target].ApplyStream(influence * elapsed, _burnables[strongestSource].Position);
            }
        }
    }
}
