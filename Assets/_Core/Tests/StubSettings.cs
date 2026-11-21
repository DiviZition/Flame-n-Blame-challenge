using Flame_n_Blame_Challenge.Adapters;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Authored numbers as plain data, set per test. Defaults are the round numbers that make the
    /// analytic expectations in the tests hand-checkable (Mass 20, AirDrag 0.5, no gravity). The rates
    /// that decide feel (GroundDrag, AirDrag) default to the authored ones, so a test that wants clean
    /// math says so by setting them to 0 — a test that forgets is testing the real absorption rule.
    public class StubSettings : IGameSettings
    {
        public float PlayerSpeedBase { get; set; } = 5f;
        public float PlayerSpeedSprint { get; set; } = 8f;
        public float PlayerSpeedInAir { get; set; } = 2f;

        public float LookSensitivity { get; set; } = 1f;
        public Vector2 LookPitchBounds { get; set; } = new Vector2(-90f, 90f);

        public float Mass { get; set; } = 20f;
        public float Gravity { get; set; } = 0f;
        public float GroundedGravity { get; set; } = -2f;
        public float TerminalVelocity { get; set; } = 100f;

        public float GroundAcceleration { get; set; } = 20f;
        public float AirAcceleration { get; set; } = 20f;
        public float GroundFriction { get; set; } = 12f;

        public float GroundDrag { get; set; } = 4f;
        public float AirDrag { get; set; } = 0.5f;

        public float MaxPushSpeed { get; set; } = 12f;
    }
}
