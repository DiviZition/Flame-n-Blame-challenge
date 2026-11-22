using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Port for what the player commands: the speeds it asks for, the rate the command is reached at, and the
    /// camera's angles. Split out of `GameSettings` (D-037). These are commands, not forces: the air speed
    /// exists in a model with no reaction force (D-016).
    public interface IControlSettings
    {
        float PlayerSpeedBase { get; }
        float PlayerSpeedSprint { get; }
        float PlayerSpeedInAir { get; }

        /// The rates the player's own effort approaches its command at, grounded and airborne. The airborne
        /// rate is lower because a jump's momentum is carried in the push channel, not steered here (D-016),
        /// and in flight control never adds to a shove (D-036).
        float GroundAcceleration { get; }
        float AirAcceleration { get; }

        float LookSensitivity { get; }
        Vector2 LookPitchBounds { get; }
    }
}
