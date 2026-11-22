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

        /// The airborne approach rate. Grounded, the ramp is `IPhysicsSettings.GroundFriction` - the reaction
        /// force the legs push against, so the ground has one number (D-038) - and this port owns only the
        /// rate in flight, where there is no reaction: a jump's momentum is carried in the push channel, not
        /// steered here (D-016), and in flight control never adds to a shove (D-036).
        float AirAcceleration { get; }

        float LookSensitivity { get; }
        Vector2 LookPitchBounds { get; }
    }
}
