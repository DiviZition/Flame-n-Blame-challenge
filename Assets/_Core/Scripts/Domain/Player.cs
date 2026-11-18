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

        private Vector3 _pendingImpulse;

        public Player(IPlayerInput input, IGameSettings gameSettings)
        {
            LookRotation = new PlayerLookRotator(input, gameSettings);
            Movement = new PlayerMovement(input, gameSettings, LookRotation);
        }

        public void Update(float time, float deltaTime)
        {
            LookRotation.Update(time, deltaTime);
        }

        public void ApplyExternalImpulse(Vector3 worldImpulse)
        {
            _pendingImpulse = worldImpulse;
        }

        public Vector3 ConsumePendingImpulse()
        {
            var impulse = _pendingImpulse;
            _pendingImpulse = Vector3.zero;
            return impulse;
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

    public class PlayerMovement 
    {
        private readonly IPlayerInput _input;
        private readonly IGameSettings _settings;
        private readonly PlayerLookRotator _look;

        public Vector3 DesiredVelocity => CalculateDesiredVelocity();
        public bool IsGrounded { get; private set; }
        public bool IsSprinting => _input.IsRunning && IsGrounded;
        public bool IsJumping => _input.IsJumping && IsGrounded;

        public PlayerMovement(IPlayerInput input, IGameSettings settings, PlayerLookRotator look)
        {
            _input = input;
            _settings = settings;
            _look = look;
        }

        public void SetIsGrounded(bool isGrounded) => IsGrounded = isGrounded;

        private Vector3 CalculateDesiredVelocity()
        {
            Vector2 input = _input.InputDirection;

            Vector3 horizontal = 
                _look.Forward * input.y + 
                _look.Right * input.x;

            horizontal *= GetSpeed();

            return new Vector3(horizontal.x, 0, horizontal.z);
        }

        private float GetSpeed()
        {
            if (IsGrounded == false)
                return _settings.PlayerSpeedInAir;
            else if (IsSprinting)
                return _settings.PlayerSpeedSprint;
            else
                return _settings.PlayerSpeedBase;
        }
    }

    public class ForcePreset
    {
        public PushForceType Type;
        public PushForceInteraptionType InterationType;
        public float Duration;
        public float Progress;
    }

    public enum PushForceType
    {
        PlayerJump,
        HydrantPush,
        Extinguisher,
    }

    public enum PushForceInteraptionType
    {
        None,
        OnGrounded,
        Override,
    }
}
