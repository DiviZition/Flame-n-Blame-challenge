using Flame_n_Blame_Challenge.Adapters;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// What the player commands, as plain data, set per test (D-037). The ground's ramp is not here: it is the
    /// ground's reaction rate, `StubPhysics.GroundFriction` (D-038). `AirAcceleration` is 20 here, not the
    /// authored 6, so a test that runs in the air reaches its command in three ticks instead of ten.
    public class StubControl : IControlSettings
    {
        public float PlayerSpeedBase { get; set; } = 5f;
        public float PlayerSpeedSprint { get; set; } = 8f;
        public float PlayerSpeedInAir { get; set; } = 2f;

        public float AirAcceleration { get; set; } = 20f;

        public float LookSensitivity { get; set; } = 1f;
        public Vector2 LookPitchBounds { get; set; } = new Vector2(-90f, 90f);
    }
}
