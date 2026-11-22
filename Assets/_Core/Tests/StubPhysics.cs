using Flame_n_Blame_Challenge.Adapters;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Authored physics numbers as plain data, set per test (D-037). The defaults are the round numbers the
    /// analytic expectations are hand-checkable with (Mass 20, Gravity 0, TerminalVelocity 100), not the asset's
    /// (Mass 80, Gravity 19.62) — a test sets what it needs **before** construction (D-012). The contact rates
    /// (`GroundFriction` 12, `GroundDrag` 4, `AirDrag` 0.5) mirror the authored shape, so a test that wants clean
    /// push math sets them to 0 explicitly: a test that forgets is testing the real absorption rule (D-036), not
    /// getting a free pass.
    public class StubPhysics : IPhysicsSettings
    {
        public float Mass { get; set; } = 20f;
        public float Gravity { get; set; } = 0f;
        public float GroundedGravity { get; set; } = -2f;
        public float TerminalVelocity { get; set; } = 100f;

        public float GroundFriction { get; set; } = 12f;

        public float GroundDrag { get; set; } = 4f;
        public float AirDrag { get; set; } = 0.5f;

        public float MaxPushSpeed { get; set; } = 12f;

        /// The jump carries this fraction of the player's own speed into the push channel (D-034). Default 1
        /// so a test that never mentions it sees the real rule; a test that wants clean vertical math sets 0.
        public float JumpCarry { get; set; } = 1f;
    }
}
