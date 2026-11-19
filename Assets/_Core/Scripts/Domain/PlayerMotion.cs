using Flame_n_Blame_Challenge.Adapters;
using System.Collections.Generic;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// Owns the player's motion state. CharacterController keeps no velocity, no mass, no AddForce, so
    /// this is the only place momentum exists in the project. Position stays in Unity; Velocity is the
    /// intent the shell integrates (ARCHITECTURE §5).
    ///
    /// Three channels, summed: ControlVelocity is the player's own movement and is never touched by a
    /// push, PushVelocity is momentum from pushes, VerticalVelocity is gravity plus the vertical part
    /// of a launch. A single vector would let the input steering step eat a launch back toward the
    /// input intent; the split is what makes "pushes add, never replace" structural (U5).
    ///
    /// Momentum has two decay rates, because contact changes what absorbs it: authored `GroundDrag`
    /// while grounded (a shove is friction against the ground and stops within about a second) and
    /// authored `AirDrag` while airborne (the inertia of a launch during flight, which is the whole
    /// point of D-016). One rate for both is what makes the ground read as ice.
    ///
    /// Perf budget (TODO.md, written before code): 8 simultaneous pushes, no per-tick allocation,
    /// < 0.05 ms per player per tick.
    public class PlayerMotion : IUpdatable
    {
        private const int MaxSimultaneousPushes = 8;
        private const float DurationFloor = 0.001f;
        private const float MassFloor = 0.001f;

        private readonly PlayerMovement _movement;
        private readonly IGameSettings _settings;
        private readonly List<ActivePush> _activePushes = new(MaxSimultaneousPushes);

        private Vector3 _controlVelocity;
        private Vector3 _pushVelocity;
        private float _verticalVelocity;
        private float _inverseMass;

        private bool _contactGrounded;
        private bool _contactSide;
        private bool _jumpFiredThisContact;

        public Vector3 Velocity { get; private set; }
        public bool IsGrounded { get; private set; }

        // Read only for tests: the channels must stay separable, not just the sum.
        public Vector3 ControlVelocity => _controlVelocity;
        public Vector3 PushVelocity => _pushVelocity;
        public float VerticalVelocity => _verticalVelocity;
        public int ActivePushCount => _activePushes.Count;

        public PlayerMotion(PlayerMovement movement, IGameSettings settings)
        {
            _movement = movement;
            _settings = settings;

            // Authored numbers are read once, here: they come from an asset and do not change mid-session
            // (D-012). A test that wants another Mass builds a player with that Mass, it does not mutate a
            // live one. Guarded once, not per tick: Mass 0 authored must not produce infinity (T11).
            _inverseMass = 1f / Mathf.Max(settings.Mass, MassFloor);
            IsGrounded = true;
        }

        /// Jump is one of the pushes (D-017), and its gate lives here: the two facts it needs are contact
        /// (the ground the launch leaves) and the push channel (the momentum it adds), and this is the
        /// domain class that owns both. `PlayerMovement` exposes the *request* only, never the rule.
        private void TryJump()
        {
            if (_movement.IsJumpRequested == false || IsGrounded == false || _jumpFiredThisContact)
                return;

            _jumpFiredThisContact = true;
            ApplyPush(_settings.JumpPreset, Quaternion.identity);
        }

        /// The shell reports what Unity observed on the last Move. Side contact is edge-triggered:
        /// one Move absorbs one wall hit.
        public void ReportContact(bool grounded, bool sideContact)
        {
            _contactGrounded = grounded;
            _contactSide = sideContact;
        }

        /// The single entry point for every push source. Re-applying the same Type replaces the live
        /// entry and resets its progress, which is what keeps a 0.1 s spray interval from stacking.
        public void ApplyPush(PushPresetData preset, Quaternion worldOrientation)
        {
            if (preset == null)
                return;

            ActivePush fresh = new ActivePush
            {
                Preset = preset,
                Direction = worldOrientation,
                Progress = 0f,
                FiredImpulse = false
            };

            int existing = FindPushIndex(preset.Type);
            if (existing >= 0)
            {
                _activePushes[existing] = fresh;
                return;
            }

            // The budget is a cap, not a silent growth: drop the oldest, never exceed 8.
            if (_activePushes.Count >= MaxSimultaneousPushes)
                _activePushes.RemoveAt(0);

            _activePushes.Add(fresh);
        }

        public void UpdateLogic(float time, float deltaTime)
        {
            // The order is the rule-set. Contact first, so a jump fired in the same tick survives the
            // ground it launched from; a force ending later never touches velocity.
            ApplyContact();
            TryJump();
            ApplyGravity(deltaTime);
            TickPushes(deltaTime);
            ApplyInputControl(deltaTime);
            ApplyDrag(deltaTime);
            ClampPushVelocity();

            Velocity = _controlVelocity + _pushVelocity + Vector3.up * _verticalVelocity;
        }

        private void ApplyContact()
        {
            IsGrounded = _contactGrounded;

            if (IsGrounded)
            {
                _pushVelocity.y = 0f;
                _verticalVelocity = _settings.GroundedGravity;
            }
            else
            {
                // Leaving the ground is what makes the jump available again, so the lock is released
                // here and not on a timer (D-017: one press per contact).
                _jumpFiredThisContact = false;
            }

            if (_contactSide)
            {
                // The wall absorbs the launch momentum and leaves control intact: the player can still
                // walk away from it (U4).
                _pushVelocity.x = 0f;
                _pushVelocity.z = 0f;
                _contactSide = false;
            }
        }

        private void ApplyGravity(float deltaTime)
        {
            _verticalVelocity -= _settings.Gravity * deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, -_settings.TerminalVelocity);
        }

        private void TickPushes(float deltaTime)
        {
            for (int i = _activePushes.Count - 1; i >= 0; i--)
            {
                ActivePush push = _activePushes[i];

                if (push.IsFinished)
                {
                    _activePushes.RemoveAt(i);
                    continue;
                }

                bool allowed = push.Preset.Applies == PushApplication.Always || IsGrounded;

                if (push.Preset.Kind == PushKind.Impulse)
                {
                    if (allowed && !push.FiredImpulse)
                        _pushVelocity += push.Direction * EnvelopeAt(push.Preset.Curve, 0f) * (push.Preset.Force * _inverseMass);

                    _activePushes.RemoveAt(i);
                    continue;
                }

                float progressStep = deltaTime / Mathf.Max(push.Preset.Duration, DurationFloor);
                push.Progress = Mathf.Min(1f, push.Progress + progressStep);
                _activePushes[i] = push;

                if (!allowed)
                    continue;

                Vector3 localEnvelope = EnvelopeAt(push.Preset.Curve, push.Progress);
                _pushVelocity += push.Direction * localEnvelope * (push.Preset.Force * _inverseMass) * deltaTime;
            }
        }

        private void ApplyInputControl(float deltaTime)
        {
            Vector3 desired = _movement.DesiredVelocity;
            Vector3 current = new Vector3(_controlVelocity.x, 0f, _controlVelocity.z);

            float approach;
            if (desired.sqrMagnitude < 0.0001f)
                approach = IsGrounded ? _settings.GroundFriction : 0f;   // no friction in the air
            else
                approach = IsGrounded ? _settings.GroundAcceleration : _settings.AirAcceleration;

            Vector3 steered = current + (desired - current) * Mathf.Clamp01(approach * deltaTime);
            _controlVelocity = new Vector3(steered.x, 0f, steered.z);
        }

        private void ApplyDrag(float deltaTime)
        {
            if (IsGrounded)
            {
                // Ground friction acts in the ground plane, on the slide. It must not touch the vertical
                // component: contact already owns vertical momentum, and a launch fired on this very tick
                // would otherwise be eaten by the friction that is supposed to absorb it.
                float groundRetained = Mathf.Max(0f, 1f - _settings.GroundDrag * deltaTime);
                _pushVelocity.x *= groundRetained;
                _pushVelocity.z *= groundRetained;
                return;
            }

            float airRetained = Mathf.Max(0f, 1f - _settings.AirDrag * deltaTime);
            _pushVelocity *= airRetained;
        }

        private void ClampPushVelocity()
        {
            float cap = _settings.MaxPushSpeed;
            if (cap <= 0f)
                return;   // 0 authored means no cap

            float speed = _pushVelocity.magnitude;
            if (speed > cap)
                _pushVelocity *= cap / speed;
        }

        /// A preset authored with no curve at all (an empty Inspector slot) is a constant force, not a
        /// crash: the envelope reads as flat.
        private static Vector3 EnvelopeAt(Vector3CurveData curve, float progress)
        {
            if (curve == null)
                return Vector3.one;

            return curve.Evaluate(progress);
        }

        /// Read only for tests: the list order is observable so overflow (drop the oldest) can be
        /// asserted from outside.
        public PushType ActiveTypeAt(int index) => _activePushes[index].Preset.Type;

        private int FindPushIndex(PushType type)
        {
            for (int i = 0; i < _activePushes.Count; i++)
                if (_activePushes[i].Preset.Type == type)
                    return i;

            return -1;
        }
    }
}
