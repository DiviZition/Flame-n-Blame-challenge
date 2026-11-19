using Flame_n_Blame_Challenge.Domain;
using NUnit.Framework;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Behaviour tests for the push model (U1-U6, D-016, G-012). Expectations are computed from the
    /// requirements, never read off the implementation, so a broken model fails them - a duration-
    /// authoritative velocity model, a push that overwrites input, a spray that stacks, all fail here.
    /// Nothing touches PlayerMotor, position, or CollisionFlags: the domain public API is the surface.
    public class PlayerMotionTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerance = 0.0001f;

        private StubSettings _settings;
        private StubInput _input;
        private PlayerLookRotator _look;
        private PlayerMovement _movement;
        private PlayerMotion _motion;

        [SetUp]
        public void SetUp()
        {
            _settings = new StubSettings();
            _input = new StubInput();

            _look = new PlayerLookRotator(_input, _settings);
            _movement = new PlayerMovement(_input, _settings, _look);
            _motion = new PlayerMotion(_movement, _settings);
        }

        /// One tick, the way the session drives it: contact reported, then the tick runs.
        private void Tick(int ticks, bool grounded, float dt = Dt)
        {
            for (int i = 0; i < ticks; i++)
                TickOnce(grounded, false, dt);
        }

        private void TickOnce(bool grounded, bool sideContact, float dt = Dt)
        {
            _look.UpdateLogic(0f, dt);
            _movement.SetIsGrounded(grounded);
            _movement.UpdateLogic(0f, dt);
            _motion.ReportContact(grounded, sideContact);
            _motion.UpdateLogic(0f, dt);
        }

        // T1 - momentum survives the force timer, and decays only by drag. The analytic expectation is
        // pure geometric decay: v(t) = v0 * (1 - k*dt)^n.
        [Test]
        public void Momentum_SurvivesTheForceTimer_AndDecaysOnlyByDrag()
        {
            _settings.AirDrag = 0.5f;
            _settings.MaxPushSpeed = 100f;

            _motion.ReportContact(false, false);
            _motion.ApplyPush(Presets.ImpulseForward(PushType.HydrantPush, 200f), Quaternion.identity);

            const int ticks = 120;
            float previous = float.MaxValue;

            for (int i = 0; i < ticks; i++)
            {
                _movement.SetIsGrounded(false);
                _movement.UpdateLogic(0f, Dt);
                _motion.ReportContact(false, false);
                _motion.UpdateLogic(0f, Dt);

                float speed = _motion.Velocity.z;
                Assert.Greater(speed, 0f, "momentum must survive the timer (tick " + i + ")");
                Assert.IsTrue(previous >= speed, "velocity may only decrease, by drag (tick " + i + ")");
                previous = speed;
            }

            Assert.AreEqual(0, _motion.ActivePushCount, "the force itself must be finished");

            float expected = 10f * Mathf.Pow(1f - _settings.AirDrag * Dt, ticks);
            Assert.AreEqual(expected, _motion.Velocity.z, 0.1f, "decay must be pure drag, not the timer");
        }

        // T2 - a wall absorbs the launch momentum and leaves the player's own movement intact, so the
        // player can still walk away from it. The expectation is a DIFF against the identical run that
        // never meets a wall: only the push channel may differ. Absolute control values would be wrong
        // here - control drifts toward the reduced air speed while airborne, and that drift belongs to
        // the input rules, not to the wall.
        [Test]
        public void SideContact_ZeroesThePushedAxis_AndLeavesControlIntact()
        {
            Vector3 freeFlight = LaunchAgainstWall(false);   // x = control, z = pushed axis
            Vector3 wall = LaunchAgainstWall(true);

            Assert.Greater(freeFlight.z, 7.5f, "the launch keeps flying until something absorbs it");
            Assert.AreEqual(0f, wall.z, Tolerance, "the wall absorbs the pushed axis");
            Assert.AreEqual(freeFlight.x, wall.x, Tolerance, "the wall must not touch the player's own movement");
        }

        /// One run, in both variants: walk on the ground, get launched, fly, hit a wall or not.
        private Vector3 LaunchAgainstWall(bool sideContact)
        {
            SetUp();
            _settings.AirDrag = 0f;
            _settings.GroundDrag = 0f;
            _settings.MaxPushSpeed = 100f;
            _input.InputDirection = new Vector2(1f, 0f);

            Tick(60, true);
            _motion.ApplyPush(Presets.ImpulseForward(PushType.HydrantPush, 160f), Quaternion.identity);
            TickOnce(false, false);
            TickOnce(false, sideContact);

            return new Vector3(_motion.ControlVelocity.x, 0f, _motion.PushVelocity.z);
        }

        // T3 - landing absorbs the vertical momentum and returns to the authored grounded gravity,
        // without eating the horizontal momentum carried into the landing.
        [Test]
        public void Landing_KillsVerticalMomentum_AndKeepsHorizontalMomentum()
        {
            _settings.Gravity = 20f;
            _settings.AirDrag = 0f;
            _settings.GroundDrag = 0f;
            _settings.MaxPushSpeed = 100f;

            Tick(10, false);
            Assert.AreEqual(-20f * 10f * Dt, _motion.VerticalVelocity, Tolerance, "falling accumulates");

            _motion.ApplyPush(Presets.ImpulseForward(PushType.HydrantPush, 120f), Quaternion.identity);
            TickOnce(false, false);
            float carried = _motion.PushVelocity.z;

            TickOnce(true, false);

            Assert.AreEqual(_settings.GroundedGravity - _settings.Gravity * Dt, _motion.VerticalVelocity, Tolerance,
                "the ground absorbs the fall momentum");
            Assert.AreEqual(carried, _motion.PushVelocity.z, Tolerance, "landing must not absorb horizontal momentum");
        }

        // T4 - a launch follows the source's rotation, not the player's facing. Same push, four facings.
        [Test]
        public void LaunchDirection_ComesFromTheSource_NotThePlayersFacing()
        {
            _settings.AirDrag = 0f;
            _settings.MaxPushSpeed = 100f;

            Vector3[] results = new Vector3[4];
            for (int turn = 0; turn < 4; turn++)
            {
                SetUp();
                _input.LookDelta = new Vector2(90f, 0f);

                Tick(turn, false);
                _motion.ApplyPush(Presets.ImpulseForward(PushType.HydrantPush, 80f),
                    Quaternion.LookRotation(Vector3.forward));
                TickOnce(false, false);

                results[turn] = _motion.Velocity;
            }

            for (int turn = 1; turn < 4; turn++)
            {
                Assert.AreEqual(results[0].z, results[turn].z, Tolerance,
                    "the launch must not depend on facing (turn " + turn + ")");
                Assert.AreEqual(results[0].x, results[turn].x, Tolerance,
                    "the launch must not depend on facing (turn " + turn + ")");
            }
        }

        // T5 - pushes add to the player's own movement, never replace it. Walking east, shoved north:
        // both components survive.
        [Test]
        public void Pushes_AddToMovement_TheyNeverReplaceIt()
        {
            _settings.AirDrag = 0f;
            _settings.GroundDrag = 0f;
            _settings.MaxPushSpeed = 100f;
            _input.InputDirection = new Vector2(1f, 0f);

            Tick(60, true);
            float walkedEast = _motion.ControlVelocity.x;

            _motion.ApplyPush(Presets.ImpulseForward(PushType.ExtinguisherShove, 60f),
                Quaternion.LookRotation(Vector3.forward));
            TickOnce(true, false);

            Assert.Greater(_motion.PushVelocity.z, 2.5f, "the shove is applied");
            Assert.AreEqual(walkedEast, _motion.ControlVelocity.x, Tolerance,
                "the shove must not steal the player's own movement");
        }

        // T6 - the anti-accumulation test. A spray re-fired every 0.1 s for 10 s saturates at the
        // force/drag equilibrium under MaxPushSpeed, and keeps ONE entry in the active list.
        [Test]
        public void ContinuousSpray_Saturates_WithoutStackingPushes()
        {
            _settings.AirDrag = 0.5f;
            _settings.MaxPushSpeed = 12f;

            const float force = 80f, duration = 0.1f;
            const int refireEvery = 6, ticks = 600;

            int maxCount = 0;
            float meanEarly = 0f, meanLate = 0f, lastSpeed = 0f;

            for (int tick = 0; tick < ticks; tick++)
            {
                if (tick % refireEvery == 0)
                    _motion.ApplyPush(Presets.SustainedForward(PushType.ExtinguisherShove, force, duration),
                        Quaternion.LookRotation(Vector3.forward));

                _movement.SetIsGrounded(false);
                _movement.UpdateLogic(0f, Dt);
                _motion.ReportContact(false, false);
                _motion.UpdateLogic(0f, Dt);

                int count = _motion.ActivePushCount;
                float speed = _motion.PushVelocity.magnitude;

                maxCount = Mathf.Max(maxCount, count);
                Assert.IsTrue(speed <= _settings.MaxPushSpeed + Tolerance,
                    "the push channel must saturate at MaxPushSpeed (tick " + tick + ")");

                if (tick >= 180 && tick < 240) meanEarly += speed;
                if (tick >= 540) meanLate += speed;
                lastSpeed = speed;
            }

            meanEarly /= 60f;
            meanLate /= 60f;

            Assert.AreEqual(1, maxCount, "600 calls over 10 s must leave ONE entry, not 600");
            Assert.Greater(lastSpeed, 1f, "the spray must actually shove");
            Assert.IsTrue(meanLate - meanEarly < 0.1f,
                "the shove must converge, not keep growing (early " + meanEarly + ", late " + meanLate + ")");

            float equilibrium = force / _settings.Mass / _settings.AirDrag;
            Assert.AreEqual(equilibrium, meanLate, equilibrium * 0.05f,
                "saturation should sit at the force/drag equilibrium, under the cap");
        }

        // T7 - replace-per-type is what bounds impulses: 100 fires of one Type is one impulse.
        [Test]
        public void ReFiringOneType_GivesOneImpulse_NotAHundred()
        {
            _settings.AirDrag = 0f;
            _settings.MaxPushSpeed = 100f;

            for (int i = 0; i < 100; i++)
                _motion.ApplyPush(Presets.ImpulseForward(PushType.HydrantPush, 40f), Quaternion.identity);

            Assert.AreEqual(1, _motion.ActivePushCount, "one entry per Type, not 100");

            TickOnce(false, false);

            float oneImpulse = 40f / _settings.Mass;
            Assert.AreEqual(oneImpulse, _motion.PushVelocity.z, Tolerance, "one impulse, not 100");
        }

        // T8 - the integration must not depend on one tick rate.
        [Test]
        public void Integration_IsFrameRateIndependent()
        {
            float at60 = RunDecayScenario(1f / 60f);
            float at30 = RunDecayScenario(1f / 30f);
            float at120 = RunDecayScenario(1f / 120f);

            Assert.AreEqual(at60, at30, at60 * 0.05f, "1/60 vs 1/30 must agree");
            Assert.AreEqual(at60, at120, at60 * 0.05f, "1/60 vs 1/120 must agree");
            Assert.Greater(at60, 0f);
        }

        private float RunDecayScenario(float dt)
        {
            SetUp();
            _settings.AirDrag = 0.5f;
            _settings.MaxPushSpeed = 100f;

            _motion.ReportContact(false, false);
            _motion.ApplyPush(Presets.ImpulseForward(PushType.HydrantPush, 200f), Quaternion.identity);

            int ticks = Mathf.RoundToInt(2f / dt);
            for (int i = 0; i < ticks; i++)
            {
                _movement.SetIsGrounded(false);
                _movement.UpdateLogic(0f, dt);
                _motion.ReportContact(false, false);
                _motion.UpdateLogic(0f, dt);
            }

            return _motion.Velocity.z;
        }

        // T9 - a GroundedOnly force dies at a ledge, an Always force keeps pulling (U6).
        [Test]
        public void GroundedOnlyForce_StopsApplying_Airborne()
        {
            _settings.AirDrag = 0f;
            _settings.MaxPushSpeed = 100f;

            _motion.ApplyPush(Presets.GroundedOnlyForward(PushType.ExtinguisherShove, 80f, 1f),
                Quaternion.LookRotation(Vector3.forward));
            Tick(60, false);

            Assert.AreEqual(0f, _motion.PushVelocity.z, Tolerance, "a ground shove must not apply airborne");

            SetUp();
            _settings.AirDrag = 0f;
            _settings.GroundDrag = 0f;
            _settings.MaxPushSpeed = 100f;

            _motion.ApplyPush(Presets.GroundedOnlyForward(PushType.ExtinguisherShove, 80f, 1f),
                Quaternion.LookRotation(Vector3.forward));
            Tick(60, true);

            float expected = 80f / _settings.Mass * 1f;
            Assert.AreEqual(expected, _motion.PushVelocity.z, 0.001f, "the same preset while grounded");
        }

        // T11 - degenerate authored data must not produce NaN, infinity, or an unbounded list.
        [Test]
        public void DegeneratePresets_ProduceNoNaNAndNoUnboundedList()
        {
            // Authored numbers are read when the domain is constructed (they come from an asset and do
            // not change mid-session, D-012), so a degenerate Mass is a player built with that Mass.
            _settings.AirDrag = 0f;
            _settings.GroundDrag = 0f;
            _settings.MaxPushSpeed = 0f;
            Rebuild(0f);

            for (int i = 0; i < 9; i++)
            {
                PushPresetData preset = new PushPresetData
                {
                    Type = (PushType)i,
                    Kind = PushKind.Sustained,
                    Applies = PushApplication.Always,
                    Force = 0f,
                    Curve = null,
                    Duration = 0f
                };
                _motion.ApplyPush(preset, Quaternion.identity);
            }

            Assert.AreEqual(8, _motion.ActivePushCount, "the list is capped at the budget, not growing");
            Assert.AreEqual((PushType)1, _motion.ActiveTypeAt(0), "the oldest push is dropped, not silently kept");

            for (int tick = 0; tick < 60; tick++)
            {
                _movement.SetIsGrounded(false);
                _movement.UpdateLogic(0f, Dt);
                _motion.ReportContact(false, false);
                _motion.UpdateLogic(0f, Dt);

                Assert.IsFalse(float.IsNaN(_motion.Velocity.x), "NaN on x at tick " + tick);
                Assert.IsFalse(float.IsInfinity(_motion.Velocity.x), "infinity on x at tick " + tick);
                Assert.IsFalse(float.IsNaN(_motion.Velocity.y), "NaN on y at tick " + tick);
                Assert.IsFalse(float.IsInfinity(_motion.Velocity.y), "infinity on y at tick " + tick);
                Assert.IsFalse(float.IsNaN(_motion.Velocity.z), "NaN on z at tick " + tick);
                Assert.IsFalse(float.IsInfinity(_motion.Velocity.z), "infinity on z at tick " + tick);
            }
        }

        // T12 - the perf budget, in the one place it can be measured without a web build: 8 pushes,
        // 10000 ticks, nothing allocated per tick.
        [Test]
        public void ActivePushTicking_AllocatesNothingPerTick()
        {
            _settings.AirDrag = 0.5f;
            _settings.MaxPushSpeed = 100f;

            for (int i = 0; i < 8; i++)
                _motion.ApplyPush(Presets.SustainedForward((PushType)i, 10f, 1000f), Quaternion.identity);

            Assert.AreEqual(8, _motion.ActivePushCount);

            GC.Collect();
            long before = GC.GetTotalMemory(false);

            Tick(10000, false);

            long allocated = GC.GetTotalMemory(false) - before;
            Assert.IsTrue(allocated < 20000L, "no per-tick allocation in the hot path (got " + allocated + " bytes)");
        }

        // T13 - the requirement behind "the player slides like the ground is ice": the ground must absorb
        // a shove and the air must not. Same shove, same elapsed time, two contact states. A model with
        // one decay rate for both - which is what the first cut had - fails the second expectation.
        [Test]
        public void GroundContact_AbsorbsAShove_TheAirDoesNot()
        {
            const int ticks = 60;
            const float shove = 10f;

            float inAir = RunAShove(shove, 0.5f, false);
            float onGround = RunAShove(shove, 4f, true);

            Assert.AreEqual(shove * Mathf.Pow(1f - 0.5f / 60f, ticks), inAir, 0.05f,
                "in the air the shove keeps its inertia, decaying only by AirDrag (D-016)");
            Assert.AreEqual(shove * Mathf.Pow(1f - 4f / 60f, ticks), onGround, 0.05f,
                "on the ground it decays by the authored GroundDrag");

            Assert.Greater(inAir, 1f, "the air must keep the player flying (U1)");
            Assert.Less(onGround, 0.5f, "the ground must have absorbed it inside a second: no ice");
        }

        /// One shove of shove m/s (Force = shove * Mass, the stub's authored Mass is 20), for one second,
        /// in one contact state. Returns how much of the shove is left.
        private float RunAShove(float shove, float dragRate, bool grounded)
        {
            SetUp();
            _settings.MaxPushSpeed = 100f;
            _settings.GroundDrag = grounded ? dragRate : 0f;
            _settings.AirDrag = grounded ? 0f : dragRate;

            _motion.ApplyPush(Presets.ImpulseForward(PushType.HydrantPush, shove * _settings.Mass),
                Quaternion.identity);

            const int ticks = 60;
            for (int tick = 0; tick < ticks; tick++)
                TickOnce(grounded, false);

            return _motion.PushVelocity.z;
        }

        /// Authored numbers are read when the domain is constructed - the asset exists before the player
        /// does, and it never changes mid-session (D-012) - so a scenario that needs different values
        /// sets them and then builds the parts, exactly as the session does.
        private void Rebuild(float mass)
        {
            _settings.Mass = mass;
            _input = new StubInput();
            _look = new PlayerLookRotator(_input, _settings);
            _movement = new PlayerMovement(_input, _settings, _look);
            _motion = new PlayerMotion(_movement, _settings);
        }
    }
}
