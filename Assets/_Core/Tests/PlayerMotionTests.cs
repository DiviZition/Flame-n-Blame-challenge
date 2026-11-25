using Flame_n_Blame_Challenge.Domain;
using NUnit.Framework;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Behaviour tests for the push model (D-016, G-012). Expectations are computed from the requirements,
    /// never read off the implementation, so a broken model fails them: a duration-authoritative velocity
    /// model, a push that overwrites input, a spray that stacks, a ground that slides like ice. Nothing
    /// touches PlayerMotor, position or CollisionFlags - the domain public API is the surface. One rule per
    /// test (D-050): momentum outliving its timer, contact absorbed along a normal, the landing absorption,
    /// direction from the source, the drag equilibrium, the cap, frame-rate independence, degenerate authored
    /// data, the no-allocation budget, the air clamp (D-036). The ground's ONE rate (D-038) is the rate every
    /// grounded expectation names, and AirDrag is the only rate in flight.
    public class PlayerMotionTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerance = 0.0001f;

        private StubPhysics _physics;
        private StubControl _control;
        private StubPresets _presets;
        private StubInput _input;
        private PlayerLookRotator _look;
        private PlayerMovement _movement;
        private PlayerMotion _motion;

        [SetUp]
        public void SetUp()
        {
            _physics = new StubPhysics();
            _control = new StubControl();
            _presets = new StubPresets();
            _input = new StubInput();

            _look = new PlayerLookRotator(_input, _control);
            _movement = new PlayerMovement(_input, _control, _look);
            _motion = new PlayerMotion(_movement, _physics, _control, _presets);
        }

        /// One tick, the way the session drives it: contact reported, then the tick runs. The normal is the
        /// surface the shell collided with; Vector3.zero means no side contact.
        private void Tick(int ticks, bool grounded, float dt = Dt)
        {
            for (int i = 0; i < ticks; i++)
                TickOnce(grounded, Vector3.zero, dt);
        }

        private void TickOnce(bool grounded, Vector3 contactNormal, float dt = Dt)
        {
            _look.UpdateLogic(0f, dt);
            _movement.SetIsGrounded(grounded);
            _movement.UpdateLogic(0f, dt);
            _motion.ReportContact(grounded, contactNormal);
            _motion.UpdateLogic(0f, dt);
        }

        // Momentum survives the force timer and decays only by drag: the analytic expectation is pure
        // geometric decay, v(t) = v0 * (1 - k*dt)^n, and the series may never rise.
        [Test]
        public void Momentum_SurvivesTheForceTimer_AndDecaysOnlyByDrag()
        {
            _physics.AirDrag = 0.5f;
            _physics.MaxPushSpeed = 100f;

            _motion.ReportContact(false, Vector3.zero);
            _motion.ApplyPush(Presets.Impulse(PushType.HydrantPush, 200f), Vector3.forward);

            const int ticks = 120;
            float previous = float.MaxValue;

            for (int i = 0; i < ticks; i++)
            {
                _movement.SetIsGrounded(false);
                _movement.UpdateLogic(0f, Dt);
                _motion.ReportContact(false, Vector3.zero);
                _motion.UpdateLogic(0f, Dt);

                float speed = _motion.Velocity.z;
                Assert.Greater(speed, 0f, "momentum must survive the timer (tick " + i + ")");
                Assert.IsTrue(previous >= speed, "velocity may only decrease, by drag (tick " + i + ")");
                previous = speed;
            }

            Assert.AreEqual(0, _motion.ActivePushCount, "the force itself must be finished");

            float expected = 10f * Mathf.Pow(1f - _physics.AirDrag * Dt, ticks);
            Assert.AreEqual(expected, _motion.Velocity.z, 0.1f, "decay must be pure drag, not the timer");
        }

        // A wall absorbs the launch momentum along its normal and leaves the player's own movement intact, so
        // the player can still walk away from it. The expectation is a DIFF against the identical run that
        // never meets a wall: only the push channel may differ. Absolute control values would be wrong -
        // control drifts toward the reduced air speed while airborne, and that drift belongs to the input
        // rules, not to the wall (D-027).
        [Test]
        public void WallNormal_AbsorbsTheNormalAxis_AndLeavesControlIntact()
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
            _physics.AirDrag = 0f;
            _physics.MaxPushSpeed = 100f;
            _input.InputDirection = new Vector2(1f, 0f);

            Tick(60, true);
            _motion.ApplyPush(Presets.Impulse(PushType.HydrantPush, 160f), Vector3.forward);
            TickOnce(false, Vector3.zero);
            TickOnce(false, sideContact ? Vector3.back : Vector3.zero);

            return new Vector3(_motion.ControlVelocity.x, 0f, _motion.PushVelocity.z);
        }

        // Landing zeroes the vertical momentum to the authored grounded rest value (D-030) and takes over the
        // horizontal momentum on the same tick: the ground's ONE rate acts on the body's momentum, not only on
        // the control, so a shove carried into a landing decays from the first grounded tick (D-038).
        [Test]
        public void Landing_KillsVerticalMomentum_AndStartsEatingHorizontalMomentum()
        {
            _physics.Gravity = 20f;
            _physics.AirDrag = 0f;
            _physics.MaxPushSpeed = 100f;

            Tick(10, false);
            Assert.AreEqual(-20f * 10f * Dt, _motion.VerticalVelocity, Tolerance, "falling accumulates");

            _motion.ApplyPush(Presets.Impulse(PushType.HydrantPush, 120f), Vector3.forward);
            TickOnce(false, Vector3.zero);
            float carried = _motion.PushVelocity.z;
            Assert.Greater(carried, 5.9f, "the shove is airborne, so AirDrag 0 keeps it whole");

            TickOnce(true, Vector3.up);

            Assert.AreEqual(_physics.GroundedGravity - _physics.Gravity * Dt, _motion.VerticalVelocity, Tolerance,
                "the ground absorbs the fall momentum");
            Assert.AreEqual(carried * (1f - _physics.GroundFriction * Dt), _motion.PushVelocity.z, Tolerance,
                "the ground eats the horizontal momentum at its own rate on the first grounded tick (D-038)");
        }

        // A launch follows the direction the SOURCE gave, not the player's facing (D-028). Same push, four
        // facings: the direction is a Vector3 handed over by the source, so facing cannot bend it.
        [Test]
        public void LaunchDirection_ComesFromTheSource_NotThePlayersFacing()
        {
            _physics.AirDrag = 0f;
            _physics.MaxPushSpeed = 100f;

            Vector3[] results = new Vector3[4];
            for (int turn = 0; turn < 4; turn++)
            {
                SetUp();
                _input.LookDelta = new Vector2(90f, 0f);

                Tick(turn, false);
                _motion.ApplyPush(Presets.Impulse(PushType.HydrantPush, 80f), Vector3.forward);
                TickOnce(false, Vector3.zero);

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

        // The anti-accumulation test, below the cap: a spray re-fired every 0.1 s for 10 s keeps ONE entry
        // (replace-per-Type) and converges to the analytic force/drag equilibrium, v* = Force/(Mass*AirDrag)
        // with time constant 1/AirDrag. An absolute "late - early < 0.1" threshold is unpassable at any tau,
        // so convergence is asserted analytically: the late window sits on the equilibrium, the series is
        // monotone, and the plateau is reached inside the first 3 tau (B-003, D-027).
        [Test]
        public void ContinuousSpray_ConvergesToDragEquilibrium_WithoutStackingPushes()
        {
            int maxCount;
            float[] speeds = RunSpray(80f, 0.5f, 12f, out maxCount);

            Assert.AreEqual(1, maxCount, "600 calls over 10 s must leave ONE entry, not 600");

            float equilibrium = 80f / _physics.Mass / 0.5f;
            Assert.Greater(speeds[599], 1f, "the spray must actually shove");
            Assert.Less(speeds[599], _physics.MaxPushSpeed, "the equilibrium is below the cap, so no clamping here");

            for (int tick = 1; tick < 600; tick++)
                Assert.GreaterOrEqual(speeds[tick], speeds[tick - 1] - Tolerance,
                    "the shove must never dip: a re-fired spray stacks and releases (tick " + tick + ")");

            float meanEarly = Mean(speeds, 180, 240);      // t = 3-4 s
            float meanLate = Mean(speeds, 420, 600);       // t = 7-10 s = 3.5-5 tau
            Assert.AreEqual(equilibrium, meanLate, equilibrium * 0.05f,
                "the plateau is the force/drag equilibrium (late " + meanLate + ", expected " + equilibrium + ")");
            Assert.Greater(meanEarly, equilibrium * 0.5f,
                "the plateau must be reached inside 3 tau, not crawl there (early " + meanEarly + ")");
            Assert.Less(meanEarly, equilibrium, "the ramp must start below the plateau");
        }

        // The same spray with an equilibrium ABOVE the cap: only a real clamp can hold the channel at
        // MaxPushSpeed when the force/drag equilibrium is higher. Force 300 N, Mass 20, AirDrag 0.5 gives an
        // equilibrium of 30 m/s against a cap of 12, so this is the saturation test the invariant names.
        [Test]
        public void ContinuousSpray_SaturatesAtMaxPushSpeed_WhenEquilibriumIsAboveTheCap()
        {
            const float cap = 12f;
            int maxCount;
            float[] speeds = RunSpray(300f, 0.5f, cap, out maxCount);

            Assert.AreEqual(1, maxCount, "one entry per Type, never one per re-fire");

            for (int tick = 0; tick < 600; tick++)
                Assert.IsTrue(speeds[tick] <= cap + Tolerance,
                    "the push channel must never exceed MaxPushSpeed (tick " + tick + ")");

            float equilibrium = 300f / _physics.Mass / 0.5f;
            Assert.Greater(equilibrium, cap, "the force/drag equilibrium is above the cap, so this is clamping");

            float meanLate = Mean(speeds, 420, 600);
            Assert.AreEqual(cap, meanLate, cap * 0.01f,
                "the channel must sit ON the cap once the ramp reaches it (got " + meanLate + ")");
        }

        /// The spray scenario: re-fire every 0.1 s (6 ticks at 60 Hz), 10 s of airborne ticks, speeds recorded.
        /// A shared helper so both saturation expectations run the identical scenario.
        private float[] RunSpray(float force, float airDrag, float cap, out int maxCount)
        {
            SetUp();
            _physics.AirDrag = airDrag;
            _physics.MaxPushSpeed = cap;

            const int refireEvery = 6, ticks = 600;
            float[] speeds = new float[ticks];
            maxCount = 0;

            for (int tick = 0; tick < ticks; tick++)
            {
                if (tick % refireEvery == 0)
                    _motion.ApplyPush(Presets.Sustained(PushType.ExtinguisherShove, force, 0.1f),
                        Vector3.forward);

                TickOnce(false, Vector3.zero);

                speeds[tick] = _motion.PushVelocity.magnitude;
                maxCount = Mathf.Max(maxCount, _motion.ActivePushCount);
            }

            return speeds;
        }

        private static float Mean(float[] speeds, int from, int to)
        {
            float sum = 0f;
            for (int i = from; i < to; i++)
                sum += speeds[i];

            return sum / (to - from);
        }

        // The integration must not depend on one tick rate: 1/60, 1/30 and 1/120 agree within 5% (permanent
        // assertion, D-018).
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
            _physics.AirDrag = 0.5f;
            _physics.MaxPushSpeed = 100f;

            _motion.ReportContact(false, Vector3.zero);
            _motion.ApplyPush(Presets.Impulse(PushType.HydrantPush, 200f), Vector3.forward);

            int ticks = Mathf.RoundToInt(2f / dt);
            for (int i = 0; i < ticks; i++)
            {
                _movement.SetIsGrounded(false);
                _movement.UpdateLogic(0f, dt);
                _motion.ReportContact(false, Vector3.zero);
                _motion.UpdateLogic(0f, dt);
            }

            return _motion.Velocity.z;
        }

        // Degenerate authored data must not produce NaN, infinity, or an unbounded list: the list is capped at
        // the budget with the oldest entry dropped, and the zero-force, zero-duration, zero-Mass, zero-cap case
        // stays finite for every tick.
        [Test]
        public void DegeneratePresets_ProduceNoNaNAndNoUnboundedList()
        {
            // Authored numbers are read when the domain is constructed (they come from an asset and do not
            // change mid-session, D-012), so a degenerate Mass is a player built with that Mass.
            _physics.AirDrag = 0f;
            _physics.MaxPushSpeed = 0f;
            Rebuild(0f);

            for (int i = 0; i < 9; i++)
                _motion.ApplyPush(Presets.Sustained((PushType)i, 0f, 0f), Vector3.forward);

            Assert.AreEqual(8, _motion.ActivePushCount, "the list is capped at the budget, not growing");
            Assert.AreEqual((PushType)1, _motion.ActiveTypeAt(0), "the oldest push is dropped, not silently kept");

            for (int tick = 0; tick < 60; tick++)
            {
                _movement.SetIsGrounded(false);
                _movement.UpdateLogic(0f, Dt);
                _motion.ReportContact(false, Vector3.zero);
                _motion.UpdateLogic(0f, Dt);

                Assert.IsFalse(float.IsNaN(_motion.Velocity.x), "NaN on x at tick " + tick);
                Assert.IsFalse(float.IsInfinity(_motion.Velocity.x), "infinity on x at tick " + tick);
                Assert.IsFalse(float.IsNaN(_motion.Velocity.y), "NaN on y at tick " + tick);
                Assert.IsFalse(float.IsInfinity(_motion.Velocity.y), "infinity on y at tick " + tick);
                Assert.IsFalse(float.IsNaN(_motion.Velocity.z), "NaN on z at tick " + tick);
                Assert.IsFalse(float.IsInfinity(_motion.Velocity.z), "infinity on z at tick " + tick);
            }
        }

        // The perf budget, in the one place it can be measured without a web build: 8 pushes, 10000 ticks,
        // nothing allocated per tick (D-007).
        [Test]
        public void ActivePushTicking_AllocatesNothingPerTick()
        {
            _physics.AirDrag = 0.5f;
            _physics.MaxPushSpeed = 100f;

            for (int i = 0; i < 8; i++)
                _motion.ApplyPush(Presets.Sustained((PushType)i, 10f, 1000f), Vector3.forward);

            Assert.AreEqual(8, _motion.ActivePushCount);

            GC.Collect();
            long before = GC.GetTotalMemory(false);

            Tick(10000, false);

            long allocated = GC.GetTotalMemory(false) - before;
            Assert.IsTrue(allocated < 20000L, "no per-tick allocation in the hot path (got " + allocated + " bytes)");
        }

        // The air rule (D-036): a player cannot push against nothing, so control has no authority over
        // momentum in flight. The clamp is a projection, not a suppression: running ALONG a shove adds
        // nothing (the speed in that axis stays the shove's), steering ACROSS it keeps the player's own
        // PlayerSpeedInAir. Two runs, one shove, both expectations analytic.
        [Test]
        public void AirborneRun_AlongAShove_AddsNothing_AcrossAShove_KeepsItsOwnAirSpeed()
        {
            Vector3 along = AirShoveRun(new Vector2(0f, 1f), out Vector3 controlAlong);   // WITH the shove
            Vector3 across = AirShoveRun(new Vector2(1f, 0f), out Vector3 controlAcross); // ACROSS the shove

            Assert.AreEqual(10f, along.z, Tolerance,
                "control adds nothing along a shove in flight: the speed in that axis stays the shove's");

            Assert.AreEqual(10f, across.z, Tolerance, "the shove flies on, untouched");
            Assert.AreEqual(_control.PlayerSpeedInAir, across.x, 0.01f,
                "steering across a shove is the player's own air speed, not a suppressed one");
        }

        /// One shove of 10 m/s along +z, then 60 airborne ticks with the player running in `run`. Returns the
        /// velocity and the control velocity, so the along and across runs read the same authored numbers.
        private Vector3 AirShoveRun(Vector2 run, out Vector3 control)
        {
            SetUp();
            _physics.AirDrag = 0f;
            _physics.MaxPushSpeed = 100f;

            Tick(1, true);
            _motion.ApplyPush(Presets.Impulse(PushType.HydrantPush, 10f * _physics.Mass), Vector3.forward);

            _input.InputDirection = run;
            Tick(60, false);

            control = _motion.ControlVelocity;
            return _motion.Velocity;
        }

        // The ground rule (D-038): the ground's reaction is a real force, so it acts on the body's momentum.
        // Shoved forward, run AGAINST it and run WITH it: the ONE authored rate eats the shove in both
        // directions (no sign rule, no second ground rate), the player's own run reaches full speed in both,
        // and the two analytic decays are the same number.
        [Test]
        public void GroundedRun_EatsAShoveAtTheGroundRate_InBothDirections()
        {
            const float shove = 10f;
            const int ticks = 60;

            float[] against = RunGroundShove(shove, ticks, new Vector2(0f, -1f));
            float[] with = RunGroundShove(shove, ticks, new Vector2(0f, 1f));

            float eaten = shove * Mathf.Pow(1f - _physics.GroundFriction / 60f, ticks);
            Assert.AreEqual(eaten, against[0], 0.05f, "the counter-run eats the shove at the ONE ground rate");
            Assert.AreEqual(eaten, with[0], 0.05f, "running WITH a shove decays at the same rate: no boost, no extra decay");
            Assert.Less(against[0], 0.5f, "the shove is gone inside a second: the player can run");

            Assert.AreEqual(-_control.PlayerSpeedBase, against[1], 0.01f, "the run against the shove is full speed");
            Assert.AreEqual(_control.PlayerSpeedBase, with[1], 0.01f, "the run with the shove is full speed");
        }

        /// One shove of `shove` m/s along +z, grounded for `ticks`, running in `run`: returns the shove left
        /// and the player's own control on the same axis. The ground's ONE rate eats the shove and ramps the
        /// run, so both expectations come from `GroundFriction` (the stub's 12, D-038).
        private float[] RunGroundShove(float shove, int ticks, Vector2 run)
        {
            SetUp();
            _physics.AirDrag = 0f;
            _physics.MaxPushSpeed = 100f;

            Tick(1, true);
            _motion.ApplyPush(Presets.Impulse(PushType.HydrantPush, shove * _physics.Mass), Vector3.forward);

            _input.InputDirection = run;
            Tick(ticks, true);

            return new[] { _motion.PushVelocity.z, _motion.ControlVelocity.z };
        }

        /// Authored numbers are read when the domain is constructed - the asset exists before the player does,
        /// and it never changes mid-session (D-012) - so a scenario that needs a different Mass sets it and
        /// then builds the parts, exactly as the session does.
        private void Rebuild(float mass)
        {
            _physics.Mass = mass;
            _input = new StubInput();
            _look = new PlayerLookRotator(_input, _control);
            _movement = new PlayerMovement(_input, _control, _look);
            _motion = new PlayerMotion(_movement, _physics, _control, _presets);
        }
    }
}
