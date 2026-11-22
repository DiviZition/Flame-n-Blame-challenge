using Flame_n_Blame_Challenge.Adapters;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Authored physics numbers as plain data, set per test (D-037). The defaults are the round numbers the
    /// analytic expectations are hand-checkable with (Mass 20, Gravity 0, TerminalVelocity 100, ground rate 12),
    /// not the asset's (Mass 80, Gravity 19.62, ground rate 8) — a test sets what it needs **before**
    /// construction (D-012). `GroundFriction` is the ground's ONE rate (D-038): it ramps the control, stops it
    /// on release, and eats a shove in every direction, so a test cannot make the ground inertial without
    /// also losing its ramp; a test that wants clean push math runs in the air with `AirDrag = 0f`.
    public class StubPhysics : IPhysicsSettings
    {
        public float Mass { get; set; } = 20f;
        public float Gravity { get; set; } = 0f;
        public float GroundedGravity { get; set; } = -2f;
        public float TerminalVelocity { get; set; } = 100f;

        /// The ground's single rate, m/s². 12 is the round number the ground-rule expectations use.
        public float GroundFriction { get; set; } = 12f;

        public float AirDrag { get; set; } = 0.5f;

        public float MaxPushSpeed { get; set; } = 12f;

        /// The jump carries this fraction of the player's own speed into the push channel (D-034). Default 1
        /// so a test that never mentions it sees the real rule; a test that wants clean vertical math sets 0.
        public float JumpCarry { get; set; } = 1f;
    }
}
