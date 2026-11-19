using Flame_n_Blame_Challenge.Adapters;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// Turns input + look into a desired velocity. Intent only: no state, no integration.
    /// The lower PlayerSpeedInAir is the whole air-control model - pushes add to what this produces,
    /// they never replace it (U5).
    public class PlayerMovement : IUpdatable
    {
        private readonly IPlayerInput _input;
        private readonly IGameSettings _settings;
        private readonly PlayerLookRotator _look;

        public Vector3 DesiredVelocity { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsSprinting => _input.IsRunning && IsGrounded;

        /// The jump *request* only. The rule — when a request may become a launch, and once per contact
        /// at that — is `PlayerMotion`'s, because that is the class that owns contact and the push
        /// channel. Movement cannot apply the push itself: it is a dependency of `PlayerMotion`, so
        /// asking for the reference back would be a construction cycle.
        public bool IsJumpRequested => _input.IsJumping;

        public PlayerMovement(IPlayerInput input, IGameSettings settings, PlayerLookRotator look)
        {
            _input = input;
            _settings = settings;
            _look = look;
        }

        public void SetIsGrounded(bool isGrounded) => IsGrounded = isGrounded;

        public void UpdateLogic(float time, float deltaTime)
        {
            Vector2 input = _input.InputDirection;

            Vector3 horizontal =
                _look.Forward * input.y +
                _look.Right * input.x;

            horizontal *= GetSpeed();

            DesiredVelocity = new Vector3(horizontal.x, 0f, horizontal.z);
        }

        private float GetSpeed()
        {
            if (IsGrounded == false)
                return _settings.PlayerSpeedInAir;
            if (IsSprinting)
                return _settings.PlayerSpeedSprint;

            return _settings.PlayerSpeedBase;
        }
    }
}
