using Flame_n_Blame_Challenge.Adapters;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    public class Player : IUpdatable
    {
        // PlayerWorldInteract
        // PlayerItemControl
        public readonly PlayerMovement Movement;
        public readonly PlayerLookRotator LookRotation;
        // PlayerHealth
        // PlayerStatuses

        public Player(IPlayerInput input, IGameSettings gameSettings)
        {
            LookRotation = new PlayerLookRotator(input, gameSettings);
            Movement = new PlayerMovement(input, gameSettings, LookRotation);
        }

        public void Update(float time, float deltaTime)
        {
            Movement.Update(time, deltaTime);
            LookRotation.Update(time, deltaTime);
        }
    }

    public class PlayerLookRotator : IUpdatable
    {
        private readonly IPlayerInput _input;
        private readonly IGameSettings _settings;

        public float Yaw { get; private set; }     // degrees, around Y
        public float Pitch { get; private set; }   // degrees, up/down

        // Convenient world-space directions (Y = 0)
        public Vector3 Forward => Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
        public Vector3 Right => Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;

        public PlayerLookRotator(IPlayerInput input, IGameSettings settings)
        {
            _input = input;
            _settings = settings;
        }

        public void Update(float _, float deltaTime)
        {
            // Mouse delta is expected in the input port
            Vector2 lookDelta = _input.LookDelta;

            Yaw += lookDelta.x * _settings.LookSensitivity;
            Pitch -= lookDelta.y * _settings.LookSensitivity;   // inverted Y is common

            Pitch = Mathf.Clamp(Pitch, _settings.LookPitchBounds.x, _settings.LookPitchBounds.y);
        }
    }

    public class PlayerMovement : IUpdatable
    {
        private readonly IPlayerInput _input;
        private readonly IGameSettings _settings;
        private readonly PlayerLookRotator _look;

        public Vector3 DesiredVelocity { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsSprinting => _input.IsRunning;

        public PlayerMovement(IPlayerInput input, IGameSettings settings, PlayerLookRotator look)
        {
            _input = input;
            _settings = settings;
            _look = look;
        }

        public void SetIsGrounded(bool isGrounded) => IsGrounded = isGrounded;

        /// <summary>
        /// Call this every frame (from Player or from an Adapter).
        /// </summary>
        public void Update(float time, float deltaTime)
        {
            // --- Horizontal movement ---
            float speed = IsSprinting
                ? _settings.PlayerSpeedSprint
                : _settings.PlayerSpeedBase;

            Vector2 input = _input.InputDirection;

            Vector3 horizontal =
                _look.Forward * input.y +
                _look.Right * input.x;

            horizontal *= speed;

            // TODO: Rewrite jump as an event

            // --- Jump --- 
            float vertical = 0;
            if (_input.IsJumping && IsGrounded)
            {
                vertical = _settings.PlayerJumpForce;
            }

            DesiredVelocity = new Vector3(horizontal.x, vertical, horizontal.z);
        }
    }
}
