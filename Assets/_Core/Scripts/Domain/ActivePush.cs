using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// One live push instance. A struct so the active list never allocates per tick (perf budget).
    public struct ActivePush
    {
        public PushPresetData Preset;

        /// World orientation of the SOURCE, never the player's facing - a hydrant launches you along
        /// its nozzle, whichever way the player is looking.
        public Quaternion Direction;

        public float Progress;

        /// Impulse entries fire on the first tick they are processed; replace-per-type is what bounds
        /// them, so 100 fires of one Type give one impulse's worth, not 100.
        public bool FiredImpulse;

        public bool IsFinished => Progress >= 1f;
    }
}
