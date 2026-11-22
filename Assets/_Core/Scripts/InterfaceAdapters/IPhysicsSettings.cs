namespace Flame_n_Blame_Challenge.Adapters
{
    /// Port for the body and the contact: what a push does to a mass, what the ground absorbs, what the air
    /// keeps. Split out of `GameSettings` (D-037, one type per file, STYLE.md). It is not player-only: a
    /// debris piece, a wave or a burnable body reads the same numbers, so the port is the project's physics,
    /// not `Player`'s.
    public interface IPhysicsSettings
    {
        float Mass { get; }
        float Gravity { get; }
        float GroundedGravity { get; }
        float TerminalVelocity { get; }

        /// The ground's reaction force, m/s². It decelerates the player's own control when the input is
        /// released, and since D-036 it eats the momentum a shove leaves behind when the player runs against
        /// it. One contact force, so one number.
        float GroundFriction { get; }

        /// The push channel's two decay rates: `GroundDrag` absorbs a shove on the ground, `AirDrag` carries
        /// its inertia in flight. One rate for both is what makes the ground read as ice (G-012).
        float GroundDrag { get; }
        float AirDrag { get; }

        /// The push channel's cap, m/s, 0 = no cap.
        float MaxPushSpeed { get; }

        /// The jump's forward carry: the fraction of the player's own speed carried into the flight as
        /// push-channel momentum. 1 = keep all of it, 0 = a vertical-only jump. The vertical launch is the
        /// authored preset alone, so this number never trades height for reach (D-034).
        float JumpCarry { get; }
    }
}
