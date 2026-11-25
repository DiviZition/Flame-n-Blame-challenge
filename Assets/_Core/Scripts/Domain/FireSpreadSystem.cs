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
    /// Positions are the ones the shells reported at composition: the props of this game do not move, so the
    /// system never asks for a live position (ARCHITECTURE §5 — the engine owns the body).
    public class FireSpreadSystem : IUpdatable
    {
        private const int FRAMES_PER_SWEEP = 4;

        private readonly List<IBurnable> _burnables = new List<IBurnable>(64);
        private readonly List<float> _lastVisit = new List<float>(64);
        private int _cursor;

        /// Composition: the host, and where it stands. The position is the shell's, handed once — the spread
        /// system holds no authority over the world.
        public void Add(IBurnable burnable)
        {
            _burnables.Add(burnable);
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

                Vector3 targetPosition = _burnables[target].Position;
                float influence = 0f;

                for (int i = 0; i < count; i++)
                {
                    if (i == target)
                        continue;

                    IBurnable source = _burnables[i];
                    if (source == null || !source.IsBurning)
                        continue;

                    influence += source.GetFireInfluenceAtDistance(
                        Vector3.Distance(targetPosition, _burnables[i].Position));
                }

                burnable.AffectIgnitionStrength(influence * elapsed);
            }
        }
    }
}