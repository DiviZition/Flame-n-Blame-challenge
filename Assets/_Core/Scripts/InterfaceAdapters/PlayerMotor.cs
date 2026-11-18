using Flame_n_Blame_Challenge.Domain;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    [CreateAssetMenu(fileName = "Player Motor Config", menuName = "Configs/Player Motor")]
    public class PlayerMotorConfig : ScriptableObject
    {
        [field: SerializeField] public AnimationCurve JumpCurve { get; private set; }
        [field: SerializeField] public float Gravity { get; private set; }
        [field: SerializeField] public float JumpDuration { get; private set; }
    }

    public class PlayerMotor : MonoBehaviour
    {
        [field: SerializeField] public AnimationCurve JumpCurve { get; private set; }
        [field: SerializeField] public float Gravity { get; private set; } = 10;
        [field: SerializeField] public float JumpDuration { get; private set; }
        [field: SerializeField] public ForcePreset TestForcePreset { get; private set; }

        [SerializeField] private CharacterController _controller;
        [SerializeField] private Transform _cameraHolder; // pitch rotates this

        private Player _player;
        private ForceInstance _actieveForce;
        private float _verticalVelocity;

        public void Construct(Player player)
        {
            _player = player;
        }

        private void Update()
        {
            if (_player == null) return;

            // 1. Feed grounded state into Domain
            _player.Movement.SetIsGrounded(_controller.isGrounded);
            // 2. Apply look
            RotatePlayer();
            // 3. Apply movement
            _actieveForce.UpdateState(Time.deltaTime);
            CalculateGravity();
            MovePlayer();
        }

        public void RotatePlayer()
        {
            transform.rotation = Quaternion.Euler(0f, _player.LookRotation.Yaw, 0f);
            _cameraHolder.localRotation = Quaternion.Euler(_player.LookRotation.Pitch, 0f, 0f);
        }

        public void MovePlayer()
        {
            Vector3 velocity = _player.Movement.DesiredVelocity;
            //velocity += _actieveForce.GetCurrentWorldForce();
            velocity.y += _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        private void CalculateGravity()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                // Small downward force keeps isGrounded stable on slopes and edges
                _verticalVelocity = -2f;   // or -0.5f / -1f — tune to feel
            }
            else
            {
                // Real gravity when in the air
                _verticalVelocity -= Mathf.Abs(Gravity) * Time.deltaTime;  // Gravity should be negative (e.g. -15f or -9.81f)
            }
        }
        public void ApplyForce(ForcePreset forcePreset) => _actieveForce = new ForceInstance(forcePreset);
        public void TestApplyForce() => _actieveForce = new ForceInstance(TestForcePreset);
    }

    public struct ForceInstance
    {
        private ForcePreset _preset;
        private float _state;

        public ForceInstance(ForcePreset forcePreset)
        {
            _preset = forcePreset;
            _state = 0;
        }

        public void UpdateState(float deltaTime)
        {
            if (_state >= 1)
                return;

            _state = Mathf.Clamp(_state + deltaTime / _preset.Duration, 0, 1);
        }

        public Vector3 GetCurrentWorldForce()
        {
            if (_state >= 1f)
                return Vector3.zero;

            return Vector3.zero;
            Vector3 localForce = _preset.ForceCurve.GetPosition(_state);

            // Treat the curve’s Z as the main push strength in the given Direction
            // and keep the curve’s Y as world-up contribution
            Vector3 worldForce =
                _preset.Direction.normalized * localForce.z +
                Vector3.up * localForce.y +
                Vector3.Cross(Vector3.up, _preset.Direction).normalized * localForce.x; // optional side component

            return worldForce;
        }
    }

    [Serializable]
    public struct ForcePreset
    {
        public Vector3 Direction;
        public float Duration;
        public Vector3Curve ForceCurve;

        public ForcePreset(Vector3 direction, float duration, Vector3Curve curve)
        {
            Direction = direction;
            Duration = duration;
            ForceCurve = curve;
        }

        public void OverrideDirection(Vector3 direction) => Direction = direction;
    }

    [Serializable]
    public struct Vector3Curve
    {
        public AnimationCurve X;
        public AnimationCurve Y;
        public AnimationCurve Z;

        public Vector3 GetPosition(float evaluation)
        {
            return new Vector3(X.Evaluate(evaluation), Y.Evaluate(evaluation), Z.Evaluate(evaluation));
        }
    }
}
