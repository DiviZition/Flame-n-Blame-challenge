using Flame_n_Blame_Challenge.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Jump is one of the pushes (D-017), so these tests assert it through the same entry point the hydrant
    /// and the spray use: an authored preset, a force over a duration, contact absorbs what is left. The
    /// player is driven through ReportContact + Update, exactly as the session drives it - no PlayerMotor,
    /// no scene. Expectations are the analytic integral of the preset: launch = Force * Duration / Mass -
    /// Gravity * Duration, so a preset change moves the number and a hard-coded jump does not. The launch
    /// assertions use a 0.2 tolerance: one tick of gravity (10/60 = 0.1667) is the discretization floor of
    /// a tick-integrated force, and the analytic number is the integral, not the sum of ticks.
    public class PlayerJumpTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerance = 0.0001f;
        private const float LaunchTolerance = 0.2f;

        private StubSettings _settings;
        private StubPresets _presets;
        private StubInput _input;
        private Player _player;

        [SetUp]
        public void SetUp()
        {
            // The stub jump is Force 400 N, flat envelope, Duration 1 s; Mass 20 and Gravity 10 make the
            // launch (400/20 - 10) * 1 = 10 m/s, hand-checkable. Zero drags and GroundedGravity 0 keep the
            // vertical channel readable.
            _settings = new StubSettings
            {
                Gravity = 10f,
                GroundedGravity = 0f,
                AirDrag = 0f,
                GroundDrag = 0f,
                MaxPushSpeed = 100f
            };
            _presets = new StubPresets();
            _input = new StubInput();
            _player = new Player(_input, _settings, _presets);
        }

        private void Tick(int ticks, bool grounded)
        {
            for (int i = 0; i < ticks; i++)
            {
                _player.ReportContact(grounded, grounded ? Vector3.up : Vector3.zero);
                _player.UpdateLogic(0f, Dt);
            }
        }

        /// Pressing jump on a grounded body, and the tick that fires it. The launch then runs for
        /// Duration: 60 ticks at 60 Hz, so 59 more ticks complete its 60 applications.
        private void PressAndFire()
        {
            Tick(1, true);
            _input.IsJumping = true;
            Tick(1, true);
        }

        /// The analytic launch the stub preset must produce: the integral of Force over Duration, divided by
        /// Mass, minus what gravity takes in the same span.
        private float PresetLaunch()
        {
            SustainedPresetData jump = (SustainedPresetData)_presets.PresetOf(PushType.PlayerJump);
            return jump.Force * jump.Duration / _settings.Mass - _settings.Gravity * jump.Duration;
        }

        // T10a - a jump press in the air produces nothing.
        [Test]
        public void JumpPressedInAir_ProducesNoLaunch()
        {
            _input.IsJumping = true;

            Tick(60, false);

            Assert.AreEqual(0, _player.Motion.ActivePushCount, "an airborne jump press creates no push");
            Assert.IsFalse(_player.Motion.IsLaunched, "an airborne jump press launches nothing");
            Assert.Less(_player.Motion.VerticalVelocity, 0f, "the body only falls");
        }

        // T10b - a grounded press is the authored preset, accumulated over its Duration.
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

        // T10c - holding the press through flight must not stack launches: the launch keeps accumulating
        // while its force runs, and there is ONE entry.
        [Test]
        public void JumpHeldInFlight_DoesNotFireAgain_KeepsAccumulating()
        {
            PressAndFire();

            float afterFirstTick = _player.Motion.VerticalVelocity;
            Assert.Greater(afterFirstTick, 0f, "the launch rises from the first tick");

            Tick(10, false);

            Assert.Greater(_player.Motion.VerticalVelocity, afterFirstTick,
                "the timed launch keeps accumulating while its force runs");
            Assert.AreEqual(1, _player.Motion.ActivePushCount, "one launch entry, never one per tick");

            int forceTicks = Mathf.RoundToInt(
                ((SustainedPresetData)_presets.PresetOf(PushType.PlayerJump)).Duration * 60f);
            Tick(forceTicks, false);   // 60 applications at 60 Hz: the force is done

            Assert.AreEqual(0, _player.Motion.ActivePushCount, "the force ends at Duration, the momentum stays");
            Assert.Greater(_player.Motion.VerticalVelocity, 1f, "the launch's momentum survived its own timer");
        }

        // T10d - the launch keeps its inertia in flight, gravity eats it, the landing absorbs it, and the
        // contact re-arms the jump (D-017: one press per contact).
        [Test]
        public void Landing_AbsorbsTheLaunch_AndReArmsTheJump()
        {
            PressAndFire();
            Tick(60, false);

            float atEndOfForce = _player.Motion.VerticalVelocity;
            Assert.Greater(atEndOfForce, 0f, "the launch carried the player up");

            Tick(50, false);
            Assert.Greater(_player.Motion.VerticalVelocity, 0f, "inertia is still rising: gravity has not eaten it");

            Tick(1, false);
            Assert.Less(_player.Motion.VerticalVelocity, atEndOfForce, "gravity is eating the launch's inertia");

            Tick(60, false);
            Assert.LessOrEqual(_player.Motion.VerticalVelocity, 0f, "the fall has arrived");

            _input.IsJumping = false;
            Tick(1, true);
            Assert.AreEqual(_settings.GroundedGravity - _settings.Gravity * Dt, _player.Motion.VerticalVelocity,
                0.01f, "contact absorbs the launch momentum");

            _input.IsJumping = true;
            Tick(60, true);
            Assert.AreEqual(PresetLaunch(), _player.Motion.VerticalVelocity, LaunchTolerance,
                "the jump re-arms on the next contact");
        }

        // D-017's own words - one press per contact, the contact re-arms it - mean a press held through a
        // landing bounces. This pins that feel choice: a jump that needs a re-press is a new decision, and
        // this test is what changes when it is made.
        [Test]
        public void PressHeldThroughALanding_ReBounces()
        {
            PressAndFire();
            Tick(122, false);     // force ends, the rise decays, the fall arrives

            Assert.LessOrEqual(_player.Motion.VerticalVelocity, 0f, "the body is falling onto the ground");

            Tick(1, true);        // landing, the press is still held - and the landing tick fires the jump
            Tick(60, true);

            Assert.AreEqual(PresetLaunch(), _player.Motion.VerticalVelocity, LaunchTolerance,
                "a held press bounces on landing");
            Assert.AreEqual(1, _player.Motion.ActivePushCount, "one launch entry, never one per contact");
        }

        // The jump is the same mechanism as an external push: authored, not coded. Gravity off so the
        // launch is the preset's integral alone, and doubling Force doubles the launch.
        [Test]
        public void JumpStrengthIsAuthored_NotHardcoded()
        {
            _settings.Gravity = 0f;

            PressAndFire();
            Tick(59, true);
            float first = _player.Motion.VerticalVelocity;

            float firstForce = _presets.PresetOf(PushType.PlayerJump).Force;

            SetUp();
            _settings.Gravity = 0f;
            _presets.Presets = new[] { Presets.Sustained(PushType.PlayerJump, firstForce * 2f, 1f) };

            PressAndFire();
            Tick(59, true);

            Assert.AreEqual(2f * first, _player.Motion.VerticalVelocity, LaunchTolerance,
                "twice the authored Force gives twice the launch, so the jump is data-driven");
        }

        // Mass is authored: the same preset shoves a heavier player half as much. A heavier player is a
        // player built with the heavier Mass - the domain reads authored numbers when it is constructed,
        // and an asset does not change under a live session (D-012).
        [Test]
        public void HeavierPlayerLaunchesLower_FromTheSamePreset()
        {
            _settings.Gravity = 0f;

            PressAndFire();
            Tick(59, true);
            float light = _player.Motion.VerticalVelocity;

            _settings.Mass *= 2f;
            _input = new StubInput();
            _player = new Player(_input, _settings, _presets);

            PressAndFire();
            Tick(59, true);

            Assert.AreEqual(light * 0.5f, _player.Motion.VerticalVelocity, LaunchTolerance,
                "the same preset shoves a heavier player half as much");
        }
    }
}
