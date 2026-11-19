using Flame_n_Blame_Challenge.Adapters;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// Composes the player's parts and forwards the two things the shells exchange with it: contact in,
    /// pushes in. It holds **no rules** — the jump, the contact absorption and the velocity belong to the
    /// parts (D-016, D-017). A class whose only job is composition and forwarding is also the reason no
    /// shell may implement any of it (D-012). Registration by the session, never self-registration
    /// (ARCHITECTURE §4).
    public class Player : IUpdatable
    {
        public readonly PlayerLookRotator LookRotation;
        public readonly PlayerMovement Movement;
        public readonly PlayerMotion Motion;

        public Vector3 Velocity => Motion.Velocity;

        public Player(IPlayerInput input, IGameSettings settings)
        {
            LookRotation = new PlayerLookRotator(input, settings);
            Movement = new PlayerMovement(input, settings, LookRotation);
            Motion = new PlayerMotion(Movement, settings);
        }

        /// Order within the composite: look, then the intent that reads it, then the velocity that reads
        /// the intent. `PlayerMotion` ticks the player's own push (the jump) inside its own step, so the
        /// whole player is one call and one order (D-013).
        public void UpdateLogic(float time, float deltaTime)
        {
            LookRotation.UpdateLogic(time, deltaTime);
            Movement.UpdateLogic(time, deltaTime);
            Motion.UpdateLogic(time, deltaTime);
        }

        /// Every push source reaches the player through here and nothing else: the hydrant, the spray,
        /// debris. The player's own push (the jump) is fired by `PlayerMotion` itself.
        public void ReceivePush(PushPresetData preset, Quaternion worldOrientation)
        {
            Motion.ApplyPush(preset, worldOrientation);
        }

        /// One contact report feeds both halves of the domain that care about the ground: the locomotion
        /// intent (air speed) and the motion rules (what absorbs momentum, what re-arms the jump).
        public void ReportContact(bool grounded, bool sideContact)
        {
            Movement.SetIsGrounded(grounded);
            Motion.ReportContact(grounded, sideContact);
        }
    }
}
