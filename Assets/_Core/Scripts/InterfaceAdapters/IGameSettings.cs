using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Port for authored motion values. Split out of GameSettings.cs (one type per file, STYLE.md).
    public interface IGameSettings
    {
        float PlayerSpeedBase { get; }
        float PlayerSpeedSprint { get; }
        float PlayerSpeedInAir { get; }

        float LookSensitivity { get; }
        Vector2 LookPitchBounds { get; }

        float Mass { get; }
        float Gravity { get; }
        float GroundedGravity { get; }
        float TerminalVelocity { get; }

        float GroundAcceleration { get; }
        float AirAcceleration { get; }
        float GroundFriction { get; }

        /// The push channel's two decay rates: `GroundDrag` absorbs a shove on the ground, `AirDrag`
        /// carries its inertia in flight. One rate for both is what makes the ground read as ice.
        float GroundDrag { get; }
        float AirDrag { get; }

        float MaxPushSpeed { get; }

        PushPresetData JumpPreset { get; }
    }
}
