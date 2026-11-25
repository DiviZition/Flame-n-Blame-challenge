using Flame_n_Blame_Challenge.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Jump is one of the pushes (D-017), so it is asserted through the same entry point the hydrant and the
    /// spray use: an authored preset, a force over a duration, contact absorbing what is left. The player is
    /// driven through ReportContact + Update, exactly as the session drives it - no PlayerMotor, no scene.
    /// Expectations are the analytic integral of the preset: launch = Force * Duration / Mass - Gravity *
    /// Duration, so a preset change moves the number and a hard-coded jump does not. The launch tolerance is
    /// 0.2: one tick of gravity (10/60 = 0.1667) is the discretization floor of a tick-integrated force, and
    /// the analytic number is the integral, not the sum of ticks.
    /// One rule per test (D-050): the analytic launch, the press fact in the air, the no-bounce gate, the
    /// standing zero, the running carry. Decay, contact absorption, the cap and frame-rate independence are
    /// asserted once in PlayerMotionTests, where those rules live.
    public class PlayerJumpTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerance = 0.0001f;
        private const float LaunchTolerance = 0.2f;

        private StubPhysics _physics;
        private StubControl _control;
        private StubPresets _presets;
        private StubInput _input;
        private Player _player;

        [SetUp]
        public void SetUp()
        {
            // The stub jump is Force 400 N, flat envelope, Duration 1 s; Mass 20 and Gravity 10 make the
            // launch (400/20 - 10) * 1 = 10 m/s, hand-checkable. AirDrag 0 and GroundedGravity 0 keep the
            // vertical channel readable. The ground rate stays the stub's 12: a running jump's carry is eaten
            // by the ground on the tick it is added (D-038), so the carry expectation names that rate.
            _physics = new StubPhysics
            {
                Gravity = 10f,
                GroundedGravity = 0f,
                AirDrag = 0f,
                MaxPushSpeed = 100f
            };
            _control = new StubControl();
            _presets = new StubPresets();
            _input = new StubInput();
            _player = new Player(_input, _physics, _control, _presets);
        }

        private void Tick(int ticks, bool grounded)
        {
            for (int i = 0; i < ticks; i++)
            {
                _player.ReportContact(grounded, grounded ? Vector3.up : Vector3.zero);
                _player.UpdateLogic(0f, Dt);
            }
        }

        /// The press as the shell reports it: `WasPressedThisFrame` is true for the tick the press started and
        /// false after, so a press held across a landing cannot fire a second time (D-039). The stub mirrors
        /// that: the press is set, the tick runs, the press is cleared.
        private void PressJump(bool grounded)
        {
            _input.JumpPressed = true;
            Tick(1, grounded);
            _input.JumpPressed = false;
        }

        /// Pressing jump on a grounded body, and the tick that fires it. The launch then runs for
        /// Duration: 60 ticks at 60 Hz, so 59 more ticks complete its 60 applications.
        private void PressAndFire()
        {
            Tick(1, true);
            PressJump(true);
        }

        /// The analytic launch the stub preset must produce: the integral of Force over Duration, divided by
        /// Mass, minus what gravity takes in the same span.
        private float PresetLaunch()
        {
            SustainedPresetData jump = (SustainedPresetData)_presets.PresetOf(PushType.PlayerJump);
            return jump.Force * jump.Duration / _physics.Mass - _physics.Gravity * jump.Duration;
        }

        // The press is one tick's fact (D-039), so an airborne press is not stored: no push, no launch, and no
        // later landing fires it.
        [Test]
        public void JumpPressedInAir_ProducesNoLaunch()
        {
            PressJump(false);
            Tick(59, false);

            Assert.AreEqual(0, _player.Motion.ActivePushCount, "an airborne jump press creates no push");
            Assert.IsFalse(_player.Motion.IsLaunched, "an airborne jump press launches nothing");
            Assert.Less(_player.Motion.VerticalVelocity, 0f, "the body only falls");
        }

        // A grounded press is the authored preset, accumulated over its Duration - the analytic integral, not a
        // number coded into the jump.
        [Test]
        public void JumpPressedGrounded_AccumulatesThePresetIntegral()
        {
            PressAndFire();
            Tick(59, true);

            Assert.Greater(PresetLaunch(), 0f, "the stub preset is strong enough to lift");
            Assert.AreEqual(PresetLaunch(), _player.Motion.VerticalVelocity, LaunchTolerance,
                "the launch is Force * Duration / Mass minus gravity, not a hard-coded number");
            Assert.IsTrue(_player.Motion.IsLaunched, "a launch strong enough to rise leaves the ground");
        }

        // D-039's pin: a press held through a landing does NOT bounce. The gate is two conditions - press fact
        // plus grounded - with zero stored jump state, so the landing absorbs the launch and only a fresh press
        // fires. This is the gate that keeps bunny-hop out of the model (the user's play check, 2026-11-23).
        [Test]
        public void PressHeldThroughALanding_DoesNotReBounce()
        {
            PressAndFire();
            Tick(122, false);     // force ends, the rise decays, the fall arrives

            Assert.LessOrEqual(_player.Motion.VerticalVelocity, 0f, "the body is falling onto the ground");

            Tick(1, true);        // landing with no press fact: nothing fires
            Assert.AreEqual(0, _player.Motion.ActivePushCount, "a held press fires nothing on the landing tick");
            Assert.AreEqual(_physics.GroundedGravity - _physics.Gravity * Dt, _player.Motion.VerticalVelocity,
                0.01f, "the landing absorbs the launch, there is no second launch");

            PressJump(true);
            Tick(60, true);
            Assert.AreEqual(PresetLaunch(), _player.Motion.VerticalVelocity, LaunchTolerance,
                "a fresh press on the contact fires the jump");
            Assert.AreEqual(1, _player.Motion.ActivePushCount, "one launch entry, never one per contact");
        }

        // Standing: the carry is exactly zero. This is the case that used to produce 0/0 = NaN, so the
        // expectation is "no momentum", not "no crash": a swallowed NaN reads as a pass on a magnitude (G-023).
        [Test]
        public void StandingJump_CarriesNoMomentum_HeightIsThePreset()
        {
            PressAndFire();
            Tick(59, true);

            Assert.AreEqual(0f, _player.Motion.PushVelocity.magnitude, Tolerance, "standing carries zero, not NaN");
            Assert.AreEqual(PresetLaunch(), _player.Motion.VerticalVelocity, LaunchTolerance,
                "the height is the authored preset");
        }

        // D-034's rule: the player's own speed is carried into the push channel as inertia, and the vertical
        // launch stays the preset's number. A tilt model fails the second expectation (it is sqrt(2) lower), a
        // ratio model fails the first (it carries proximity to the target speed, not speed). The ground's ONE
        // rate (D-038) eats the carry on the tick it is added.
        [Test]
        public void RunningJump_CarriesOwnSpeed_HeightIsUnchanged()
        {
            _input.InputDirection = new Vector2(1f, 0f);   // Right at yaw 0, so the run is along +x
            Tick(60, true);
            float walked = _player.Motion.ControlVelocity.x;
            Assert.Greater(walked, 4.9f, "the run-up has reached the authored walk speed");

            PressAndFire();   // the jump fires on the last tick of this pair

            Assert.AreEqual(walked * (1f - _physics.GroundFriction * Dt), _player.Motion.PushVelocity.x, 0.02f,
                "JumpCarry 1 carries the player's own speed, and the ground's ONE rate eats it on the tick "
                + "it is added (D-038)");

            Tick(59, true);
            Assert.AreEqual(PresetLaunch(), _player.Motion.VerticalVelocity, LaunchTolerance,
                "the launch height is the preset, not the preset minus a tilt");
        }
    }
}
