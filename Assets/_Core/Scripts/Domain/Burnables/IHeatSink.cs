using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// The door heat leaves the domain through, in both directions: the extinguisher spray and the fire spread
    /// are both streams, so the shell's `ApplyStream` is the one surface (D-046, D-052). `amount` is what the
    /// source integrated over its own time, and the sign is the source's (G-031) — spread is fire, so its
    /// amount is positive. `contactPoint` is where the heat comes from: the spray gives the hit point, the
    /// spread gives the burning neighbour's position; the shell clamps either to the mesh's surface.
    public interface IHeatSink
    {
        public void ApplyStream(float amount, Vector3? contactPoint);
    }
}
