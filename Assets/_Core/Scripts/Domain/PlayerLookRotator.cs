using Flame_n_Blame_Challenge.Adapters;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// Yaw/pitch from the mouse delta, in degrees. Exposes the world-space directions PlayerMovement
    /// needs; the shell writes these angles onto the transform, nothing else reads Unity here.
    public class PlayerLookRotator : IUpdatable
    {
        private readonly IPlayerInput _input;
        private readonly IGameSettings _settings;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        public Vector3 Forward => Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
        public Vector3 Right => Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;

        public PlayerLookRotator(IPlayerInput input, IGameSettings settings)
        {
            _input = input;
            _settings = settings;
        }

        public void UpdateLogic(float time, float deltaTime)
        {
            Vector2 lookDelta = _input.LookDelta;

            Yaw += lookDelta.x * _settings.LookSensitivity;
            Pitch -= lookDelta.y * _settings.LookSensitivity;   // inverted Y is common

            Pitch = Mathf.Clamp(Pitch, _settings.LookPitchBounds.x, _settings.LookPitchBounds.y);
        }
    }
}
