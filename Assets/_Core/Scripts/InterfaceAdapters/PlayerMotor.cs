using Flame_n_Blame_Challenge.Domain;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Transform _cameraHolder; // pitch rotates this

        [field: SerializeField] public float Gravity { get; private set; } = 10;
        [field: SerializeField] public float GroundedGravity { get; private set; } = -2;
        [field: SerializeField] public ForcePreset JumpPreset { get; private set; }

        private Player _player;
        private readonly List<ForceInstance> _activeForces = new();
        private float _verticalVelocity;

        public void Construct(Player player) => _player = player;

        private void Update()
        {
            if (_player == null) return;

            // 1. Domain ← grounded
            _player.Movement.SetIsGrounded(_controller.isGrounded);

            // 2. Look
            ApplyLook();

            // 3. Tick forces
            TickForces(Time.deltaTime);

            // 4. Build final displacement
            Vector3 displacement = Vector3.zero;

            // Horizontal / desired movement from Domain
            displacement += _player.Movement.DesiredVelocity * Time.deltaTime;

            // Vertical (gravity + jump)
            UpdateVerticalVelocity();
            displacement += Vector3.up * _verticalVelocity * Time.deltaTime;

            // External forces
            displacement += EvaluateAllForces() * Time.deltaTime;

            // 5. Single Move call
            _controller.Move(displacement);
        }

        void ApplyLook()
        {
            transform.rotation = Quaternion.Euler(0f, _player.LookRotation.Yaw, 0f);
            _cameraHolder.localRotation = Quaternion.Euler(_player.LookRotation.Pitch, 0f, 0f);
        }

        void UpdateVerticalVelocity()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = GroundedGravity;
            else
                _verticalVelocity -= Gravity * Time.deltaTime;
        }

        void TickForces(float dt)
        {
            for (int i = _activeForces.Count - 1; i >= 0; i--)
            {
                _activeForces[i].Tick(dt);
                if (_activeForces[i].IsFinished)
                    _activeForces.RemoveAt(i);
            }
        }

        Vector3 EvaluateAllForces()
        {
            Vector3 sum = Vector3.zero;
            foreach (var f in _activeForces)
                sum += f.EvaluateWorldForce();
            return sum;
        }

        // Public API used by gameplay systems (hydrant, etc.)
        public void ApplyForce(ForcePreset preset, Quaternion orientation)
        {
            int existingIndex = _activeForces.FindIndex(force => force.Preset.Type == preset.Type);
            var instance = new ForceInstance(preset, orientation);

            if (existingIndex >= 0)
                _activeForces[existingIndex] = instance;
            else
                _activeForces.Add(instance);
        }

        // Convenience overload when you only have a direction
        public void ApplyForce(ForcePreset preset, Vector3 forward)
        {
            // Build an orientation that looks along 'forward'
            Quaternion orient = Quaternion.LookRotation(forward.normalized, Vector3.up);
            ApplyForce(preset, orient);
        }
        public void TestApplyForce() => ApplyForce(JumpPreset, Vector3.up);
    }

    public class ForceInstance
    {
        public readonly ForcePreset Preset;
        public readonly Quaternion Orientation;   // world rotation that defines the local axes
        public float Progress { get; private set; }

        public ForceInstance(ForcePreset preset, Quaternion orientation)
        {
            Preset = preset;
            Orientation = orientation;
            Progress = 0f;
        }

        public bool IsFinished => Progress >= 1f;

        public void Tick(float dt)
        {
            if (IsFinished) return;
            Progress = Mathf.Clamp01(Progress + dt / Mathf.Max(Preset.Duration, 0.001f));
        }

        public Vector3 EvaluateWorldForce()
        {
            if (IsFinished) return Vector3.zero;

            Vector3 local = Preset.Curve.GetPosition(Progress);   // (right, up, forward)

            if (Preset.IgnoreOrientation)
                return local;                                 // pure world-space curve

            return Orientation * local;                      // Rotate local force into world
        }
    }

    [Serializable]
    public struct ForcePreset
    {
        public PushForceType Type;
        public Vector3Curve Curve;
        public float Duration;
        public bool IgnoreOrientation;
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
