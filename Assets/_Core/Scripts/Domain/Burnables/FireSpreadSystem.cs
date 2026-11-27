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
    /// ~4 frames.
    ///
    /// **The adjacency is built once, at composition** (D-052): the props of this game do not move, so the
    /// distances are known before the first frame. A pair is in the table when the source's reach covers it —
    /// influence beyond the reach is 0, so the table and a full sweep give the same heat. A visit then pays
    /// table reads, no distance: 24 objects, n = 6, ~8 neighbours each → **~48 reads a frame, no sqrt, no
    /// allocation.**
    ///
    /// **Determinism:** a visit carries `elapsed` — the time since that object's own last visit — so the heat
    /// one object receives per second is the same whatever `n` is. The sweep is an integral over time, not a
    /// sampling of the state.
    ///
    /// **The heat leaves through the shell's stream door** (`IHeatSink`), the same door the spray uses, so the
    /// shell hears the fire as a stream, not as a number growing in a body it cannot see. A visit also names
    /// the **strongest** in-range neighbour: who is heating me is a fact about the fire, and the shell puts
    /// the char front on the side that neighbour stands on (D-052).
    public class FireSpreadSystem : IUpdatable
    {
        private const int FRAMES_PER_SWEEP = 4;

        private readonly List<IBurnable> _burnables = new List<IBurnable>(64);
        private readonly List<IHeatSink> _sinks = new List<IHeatSink>(64);
        private readonly List<float> _lastVisit = new List<float>(64);

        // Per target, the sources that reach him and how far each stands — filled at composition, read-only
        // after. Positions never change, so the table is built once (D-052).
        private readonly List<List<int>> _neighbours = new List<List<int>>(64);
        private readonly List<List<float>> _neighbourDistances = new List<List<float>>(64);

        private int _cursor;

        /// Composition: the host, its sink, and where it stands. The position is the shell's, handed once — the
        /// spread system holds no authority over the world. Every pair with the already-added bodies is settled
        /// here, in both directions: each body's table gets the sources that reach it.
        public void Add(IBurnable burnable, IHeatSink sink)
        {
            int target = _burnables.Count;
            _neighbours.Add(new List<int>());
            _neighbourDistances.Add(new List<float>());

            for (int source = 0; source < target; source++)
            {
                float distance = Vector3.Distance(_burnables[source].Position, burnable.Position);

                if (distance <= _burnables[source].MaxFirePopulationRange)
                {
                    _neighbours[target].Add(source);
                    _neighbourDistances[target].Add(distance);
                }

                if (distance <= burnable.MaxFirePopulationRange)
                {
                    _neighbours[source].Add(target);
                    _neighbourDistances[source].Add(distance);
                }
            }

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
                List<int> sources = _neighbours[target];
                List<float> distances = _neighbourDistances[target];

                float influence = 0f;
                float strongestContribution = 0f;
                int strongestSource = -1;

                for (int i = 0; i < sources.Count; i++)
                {
                    IBurnable source = _burnables[sources[i]];
                    if (!source.IsBurning)
                        continue;

                    float contribution = source.GetFireInfluenceAtDistance(distances[i]);

                    influence += contribution;

                    if (contribution > strongestContribution)
                    {
                        strongestContribution = contribution;
                        strongestSource = sources[i];
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
