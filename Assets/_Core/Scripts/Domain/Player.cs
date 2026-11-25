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

        private readonly IPlayerInput _input;
        public Vector3 Velocity => Motion.Velocity;
        public Vector2 RawInputDirection => _input.InputDirection;

        public Player(IPlayerInput input, IPhysicsSettings physics, IControlSettings control,
            IPushPresets presets)
        {
            LookRotation = new PlayerLookRotator(input, control);
            Movement = new PlayerMovement(input, control, LookRotation);
            Motion = new PlayerMotion(Movement, physics, control, presets);
            _input = input;
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

        /// Every push source reaches the player through this one entry point and nothing else: the hydrant,
        /// the spray, debris. The player's own push (the jump) is fired by `PlayerMotion` itself. The
        /// direction is the **source's** Vector3 (`PushDirection.forward`), never the player's facing
        /// (D-028); the type of the preset is its force shape, so the entry point does not branch (D-033).
        public void ReceivePush(PushPresetData preset, Vector3 worldDirection)
        {
            Motion.ApplyPush(preset, worldDirection);
        }

        /// One contact report feeds both halves of the domain that care about the ground: the locomotion
        /// intent (air speed) and the motion rules (what absorbs momentum, and the grounded fact the jump gate
        /// reads, D-039). The normal is what lets the motion absorb a wall along its axis, not across the room.
        public void ReportContact(bool grounded, Vector3 contactNormal)
        {
            Movement.SetIsGrounded(grounded);
            Motion.ReportContact(grounded, contactNormal);
        }
    }
}
