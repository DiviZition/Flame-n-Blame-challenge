using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// One live push instance. A struct so the active list never allocates per tick (perf budget).
    public struct ActivePush
    {
        /// Keyed by its Type (replace-per-type) and shaped by the **type of the DTO** (Impulse / Sustained),
        /// so the branch lives in `TickPushes` reading the entry, authored, not inferred (D-031).
        public PushPresetData Preset;

        /// The world direction the **source** asked for (`PushDirection.forward` of the hydrant, up for a
        /// jump), never the player's facing. Normalized on entry, split into horizontal and vertical
        /// (G-016).
        public Vector3 Direction;

        public float Progress;

        public bool IsFinished => Progress >= 1f;
    }
}
