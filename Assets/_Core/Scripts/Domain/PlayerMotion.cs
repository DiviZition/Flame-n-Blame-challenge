using Flame_n_Blame_Challenge.Adapters;
using System.Collections.Generic;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// Owns the body's motion state. CharacterController keeps no velocity, no mass, no AddForce, so this
    /// is the only place momentum exists in the project. Position stays in Unity; Velocity is the intent
    /// the shell integrates (ARCHITECTURE §5).
    ///
    /// Two channels, summed at the end: **horizontal** and **vertical**. ControlVelocity is the player's
    /// own movement and is never touched by a push, PushVelocity is horizontal momentum from pushes,
    /// VerticalVelocity is gravity plus the vertical part of every launch. A single vector would let the
    /// input steering step eat a launch back toward the input intent; the split is what makes
    /// "pushes add, never replace" structural (U5). The player's own jump is the one push built from this
    /// split: its authored launch goes vertically, and a fraction of `ControlVelocity` is carried into the
    /// push channel as forward inertia (D-034).
    ///
    /// The vertical split is what makes a **timed** launch possible. The ground zeroes vertical momentum,
    /// so a launch that lives in the same channel as the ground is eaten by the next grounded tick: a
    /// 2 s hydrant force, or a 0.5 s jump curve, accumulates nothing. The rule that keeps a launch alive is
    /// the **normal force**, not a state and not a sign: the ground can only push up, so it absorbs
    /// downward momentum **when the live vertical force does not exceed weight** (`Mass * Gravity`). A weak
    /// jet is cancelled by the contact — physically right, a weak jet cannot lift you; a launch stronger
    /// than weight breaks the contact, accumulates, leaves the ground, and its momentum survives flight
    /// (G-016, G-020). No launch flag is stored: contact, the live force sum and the sign of vertical are
    /// the whole rule.
    ///
    /// Wall absorption is by the **contact normal**, not by `CollisionFlags.Sides`: `Sides` gives no axis,
    /// so zeroing "the wall" on both axes kills momentum running parallel to it — sliding along a wall
    /// stops dead. The normal is reported by the shell (G-016); only the component along the normal is
    /// absorbed.
    ///
    /// Momentum is absorbed by contact, and the ground has **one** authored number: `GroundFriction`
    /// (μN/Mass, m/s²). It is the reaction the legs can push against, so the same rate both ramps
    /// `ControlVelocity` to its commanded speed (and to rest when the input is released) **and** eats the
    /// momentum a shove leaves behind (D-038). Airborne, there is no reaction, so `AirDrag` is the only rate:
    /// a launch keeps its inertia through the flight, which is the whole point of D-016. One rate for ground
    /// and air is what made the floor read as ice (G-012); splitting the ground's force into two rates — one
    /// for the shove, one for the control — is what made the numbers fight each other, and that is the second
    /// ice symptom this file had.
    ///
    /// Contact decides the **authority of the player's own effort** over momentum (D-036). Airborne, the legs
    /// have no ground to push against: control keeps its own air speed and can never add to a shove, so the
    /// component of `ControlVelocity` along the shove is removed in flight — the speed in the shove's
    /// direction is the shove's. Grounded, the reaction is real, so the run ramps at `GroundFriction` and the
    /// shove decays at the **same** rate in every direction: running **against** a shove kills it inside a
    /// second and the player runs at full speed, running **with** it gets no boost, because the excess is
    /// eaten by the same force. No sign rule, no second ground rate.
    ///
    /// Perf budget (TODO.md, written before code): 8 simultaneous pushes, no per-tick allocation,
    /// < 0.05 ms per player per tick.
    public class PlayerMotion : IUpdatable
    {
        private const int MaxSimultaneousPushes = 8;
        private const float DurationFloor = 0.001f;
        private const float MassFloor = 0.001f;

        /// A contact normal is a wall when it faces sideways. 0.5 is the boundary of a 60° slope, so a
        /// steep ramp absorbs the wall component like a wall and a walkable slope does not.
        private const float WallNormalMaxY = 0.5f;

        private readonly PlayerMovement _movement;
        private readonly IPhysicsSettings _physics;
        private readonly IControlSettings _control;
        private readonly IPushPresets _presets;
        private readonly List<ActivePush> _activePushes = new(MaxSimultaneousPushes);

        private Vector3 _controlVelocity;
        private Vector3 _pushVelocity;
        private float _verticalVelocity;
        private float _inverseMass;
        private float _mass;

        private bool _contactGrounded;
        private Vector3 _contactNormal;

        public Vector3 Velocity { get; private set; }
        public bool IsGrounded { get; private set; }

        /// The body is rising. Not a stored state: rising is what a launch leaves behind, and the ground
        /// absorbs only the opposite direction.
        public bool IsLaunched => _verticalVelocity > 0f;

        /// The normal-force fact: how much vertical force the live **Sustained** pushes apply right now,
        /// envelope included. Impulse pushes are not in the sum — an impulse lands as velocity, not as
        /// force, so the sign rule covers it. Derived from the active list, never stored (D-030). It is
        /// read by `ApplyContact` alone, so it is private: the ground's rule is the only reader (D-033).
        private float LiveVerticalForce()
        {
            float total = 0f;

            for (int i = 0; i < _activePushes.Count; i++)
            {
                ActivePush push = _activePushes[i];
                if (push.IsFinished || push.Preset.IsImpulse)
                    continue;

                SustainedPresetData sustained = (SustainedPresetData)push.Preset;
                total += sustained.Force * sustained.EnvelopeAt(push.Progress) * push.Direction.y;
            }

            return total;
        }

        // Read only for tests: the channels must stay separable, not just the sum.
        public Vector3 ControlVelocity => _controlVelocity;
        public Vector3 PushVelocity => _pushVelocity;
        public float VerticalVelocity => _verticalVelocity;
        public int ActivePushCount => _activePushes.Count;

        public PlayerMotion(PlayerMovement movement, IPhysicsSettings physics, IControlSettings control,
            IPushPresets presets)
        {
            _movement = movement;
            _physics = physics;
            _control = control;
            _presets = presets;

            // Authored numbers are read once, here: they come from an asset and do not change mid-session
            // (D-012). A test that wants another Mass builds a body with that Mass, it does not mutate a
            // live one. Guarded once, not per tick: Mass 0 authored must not produce infinity (T11).
            _mass = Mathf.Max(physics.Mass, MassFloor);
            _inverseMass = 1f / _mass;
            IsGrounded = true;
        }

        /// Jump is one of the pushes (D-017), and its gate lives here for now. The two facts it needs are the
        /// **press fact** (`PlayerMovement` exposes the request of this tick only: the shell reports the press
        /// for one tick, so a held key cannot fire twice — D-039) and contact (the ground the launch leaves).
        /// `PlayerMotion` is the domain class that owns contact, so the gate is here, and `PlayerMovement`
        /// exposes the request, never the rule. No press flag is stored: a fact that lasts one tick cannot be
        /// consumed twice, and a press seen in the air is not remembered, so nothing fires on the landing.
        /// This gate is what keeps bunny-hop out of the model. Moving it to a `JumpUseCase` is D-027's second
        /// half.
        private void TryJump()
        {
            if (_movement.IsJumpRequested == false || IsGrounded == false)
                return;

            // The authored preset launches **straight up**: its Force is the launch, so tilting the direction
            // by speed trades height for reach — a running jump lower than a standing one, because the
            // direction is normalized and the impulse magnitude is fixed. The speed goes into the push
            // channel as **carried inertia** instead (`JumpCarry * control velocity`): slow run carries
            // little, sprint carries most, standing carries exactly zero. No ratio to divide by, so standing
            // has no 0/0, and the carry is bounded by `MaxPushSpeed` and decays by `AirDrag` through the
            // flight, the same rule as any shove (D-016, D-034).
            ApplyPush(_presets.PresetOf(PushType.PlayerJump), Vector3.up);

            Vector3 carried = _controlVelocity * Mathf.Max(0f, _physics.JumpCarry);
            if (carried.sqrMagnitude > 0.0001f)
                AddPushDelta(carried);
        }

        /// The shell reports what Unity observed on the last Move: grounded, and the normal of the surface
        /// it collided with (`OnControllerColliderHit`). The normal is edge-triggered: one Move absorbs one
        /// wall hit.
        public void ReportContact(bool grounded, Vector3 contactNormal)
        {
            _contactGrounded = grounded;
            _contactNormal = contactNormal;
        }

        /// The one entry point a push source uses. The **type of the DTO is the shape** (D-031), so the
        /// entry point cannot branch: an `ImpulsePresetData` lands in one tick, a `SustainedPresetData`
        /// accumulates `Force * Curve` over its `Duration`, and `TickPushes` reads that from the entry.
        /// One method, not two named by shape: a second name for one behavior is dead surface (D-033).
        public void ApplyPush(PushPresetData preset, Vector3 worldDirection)
        {
            if (preset == null)
                return;

            AddActivePush(preset, worldDirection);
        }

        /// Re-applying the same Type replaces the live entry and resets its progress, which is what keeps
        /// a 0.1 s spray interval from stacking.
        private void AddActivePush(PushPresetData preset, Vector3 worldDirection)
        {
            Vector3 direction = worldDirection;

            if (direction.sqrMagnitude > 0.0001f)
                direction.Normalize();
            else
                direction = Vector3.zero;

            ActivePush fresh = new ActivePush
            {
                Preset = preset,
                Direction = direction,
                Progress = 0f
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
                // The ground's normal force can push only **up**, so it absorbs downward momentum — and only
                // while the live vertical force does not exceed weight. The sign alone is the trap: a launch
                // starts from the grounded rest value, which is negative, so a sign-only rule resets it every
                // tick and nothing ever accumulates (G-020). A force weaker than weight is cancelled by the
                // contact, which is the honest case: a weak jet cannot lift you.
                if (_verticalVelocity <= 0f && LiveVerticalForce() <= _mass * _physics.Gravity)
                    _verticalVelocity = _physics.GroundedGravity;
            }
            // Airborne, nothing is stored: the jump press is not remembered, so a press made in flight
            // never fires on the landing (D-039).

            if (IsWallContact(_contactNormal))
            {
                // Absorb only the component running INTO the surface; the component along it is kept, so
                // a shove parallel to a wall survives and the player slides along it.
                Vector3 normal = _contactNormal.normalized;
                Vector3 normalAxis = new Vector3(normal.x, 0f, normal.z);

                if (normalAxis.sqrMagnitude > 0.0001f)
                {
                    normalAxis.Normalize();
                    _pushVelocity -= normalAxis * Vector3.Dot(_pushVelocity, normalAxis);
                }

                _contactNormal = Vector3.zero;
            }
        }

        private static bool IsWallContact(Vector3 normal)
        {
            return normal.sqrMagnitude > 0.0001f && normal.y < WallNormalMaxY;
        }

        private void ApplyGravity(float deltaTime)
        {
            _verticalVelocity -= _physics.Gravity * deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, -_physics.TerminalVelocity);
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

                // The shape is the type of the DTO, so an Impulse has no Duration and no curve to read: its
                // Force lands now and the entry is done (D-028, D-031).
                if (push.Preset.IsImpulse)
                {
                    AddPushDelta(push.Direction * (push.Preset.Force * _inverseMass));
                    _activePushes.RemoveAt(i);
                    continue;
                }

                SustainedPresetData sustained = (SustainedPresetData)push.Preset;
                float progressStep = deltaTime / Mathf.Max(sustained.Duration, DurationFloor);
                push.Progress = Mathf.Min(1f, push.Progress + progressStep);
                _activePushes[i] = push;

                AddPushDelta(push.Direction * sustained.EnvelopeAt(push.Progress)
                    * (sustained.Force * _inverseMass) * deltaTime);
            }
        }

        /// The horizontal part of a push is the shove channel; its vertical part is a launch and belongs
        /// to the gravity channel, where the ground cannot eat it while it rises (G-016).
        private void AddPushDelta(Vector3 delta)
        {
            _pushVelocity += new Vector3(delta.x, 0f, delta.z);
            _verticalVelocity += delta.y;
        }

        private void ApplyInputControl(float deltaTime)
        {
            Vector3 desired = _movement.DesiredVelocity;
            Vector3 current = new Vector3(_controlVelocity.x, 0f, _controlVelocity.z);

            // Grounded, the legs have one thing to push against - the ground's reaction - so ONE rate governs
            // both directions: it ramps the control to the commanded speed, and brings it to rest when the
            // input is released (D-038). Airborne, there is no reaction, so the control keeps its authored air
            // speed: it ramps toward the input at `AirAcceleration`, and does not decay on release (D-016).
            float approach = IsGrounded
                ? _physics.GroundFriction
                : (desired.sqrMagnitude < 0.0001f ? 0f : _control.AirAcceleration);

            Vector3 steered = current + (desired - current) * Mathf.Clamp01(approach * deltaTime);
            _controlVelocity = new Vector3(steered.x, 0f, steered.z);

            // Airborne, the legs have no ground to push against, so control has no authority over momentum:
            // it can steer its own authored air speed, it can never add to a shove. The component along the
            // shove is removed, so the speed in the shove's direction stays the shove's and running with a
            // launch in flight reads as one flight, not as a boost (D-036).
            if (IsGrounded == false)
            {
                Vector3 shoveAxis = ShoveAxis();
                float along = Vector3.Dot(_controlVelocity, shoveAxis);
                if (along > 0f)
                    _controlVelocity -= shoveAxis * along;
            }
        }

        private void ApplyDrag(float deltaTime)
        {
            // Drag is a horizontal-plane rule: the vertical channel is kept by gravity and terminal
            // velocity, and ground contact already owns vertical momentum. Grounded, the ground's reaction
            // eats the body's momentum at the ONE authored ground rate, the same rate that ramps the control
            // (D-038); airborne, a launch keeps its inertia through the flight at `AirDrag` (D-016).
            float retained = IsGrounded
                ? Mathf.Max(0f, 1f - _physics.GroundFriction * deltaTime)
                : Mathf.Max(0f, 1f - _physics.AirDrag * deltaTime);

            _pushVelocity.x *= retained;
            _pushVelocity.z *= retained;
        }

        /// The shove's direction on the horizontal plane, `Vector3.zero` when there is no momentum to
        /// fight. The air clamp reads it, so the rule reads the same axis the shove left behind.
        private Vector3 ShoveAxis()
        {
            Vector3 shove = new Vector3(_pushVelocity.x, 0f, _pushVelocity.z);
            return shove.sqrMagnitude > 0.0001f ? shove.normalized : Vector3.zero;
        }

        private void ClampPushVelocity()
        {
            float cap = _physics.MaxPushSpeed;
            if (cap <= 0f)
                return;   // 0 authored means no cap

            float speed = _pushVelocity.magnitude;
            if (speed > cap)
                _pushVelocity *= cap / speed;
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
