using Flame_n_Blame_Challenge.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Jump is one of the pushes (D-017), so these tests assert it through the same entry point the
    /// hydrant and the spray use: authored preset, impulse, contact absorbs it. The player is driven
    /// through ReportContact + Update, exactly as the session drives it - no PlayerMotor, no scene.
    public class PlayerJumpTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerance = 0.0001f;

        private StubSettings _settings;
        private StubInput _input;
        private Player _player;

        [SetUp]
        public void SetUp()
        {
            _settings = new StubSettings
            {
                Gravity = 20f,
                AirDrag = 0f,
                GroundDrag = 0f,
                MaxPushSpeed = 100f
            };
            _input = new StubInput();
            _player = new Player(_input, _settings);
        }

        private void Tick(int ticks, bool grounded)
        {
            for (int i = 0; i < ticks; i++)
            {
                _player.ReportContact(grounded, false);
                _player.UpdateLogic(0f, Dt);
            }
        }

        // T10a - a jump press in the air produces nothing.
        [Test]
        public void JumpPressedInAir_ProducesNoImpulse()
        {
            _input.IsJumping = true;

            Tick(60, false);

            Assert.AreEqual(0, _player.Motion.ActivePushCount, "an airborne jump press creates no push");
            Assert.AreEqual(0f, _player.Motion.PushVelocity.y, Tolerance, "an airborne jump press adds no momentum");
        }

        // T10b - a grounded jump press is one impulse, and its size comes from the authored preset.
        [Test]
        public void JumpPressedGrounded_IsOnePresetImpulse()
        {
            Tick(1, true);
            _input.IsJumping = true;

            Tick(1, true);

            float expected = _settings.JumpPreset.Force / _settings.Mass;
            Assert.AreEqual(expected, _player.Motion.PushVelocity.y, Tolerance,
                "the jump launch is Force/Mass of the authored preset, not a hard-coded number");
        }

        // T10c - holding the jump through flight must not stack impulses.
        [Test]
        public void JumpHeldInFlight_DoesNotFireAgain_KeepsTheLaunch()
        {
            Tick(1, true);
            _input.IsJumping = true;
            Tick(1, true);

            float launch = _player.Motion.PushVelocity.y;
            Assert.Greater(launch, 0f, "the jump launched");

            Tick(10, false);

            Assert.AreEqual(0, _player.Motion.ActivePushCount, "the jump push is finished, not live");
            Assert.AreEqual(launch, _player.Motion.PushVelocity.y, Tolerance,
                "no second impulse in flight, and the inertia of the first one survives");
        }

        // T10d - contact absorbs the momentum the launch carried, and the contact re-arms the jump
        // (D-017: one press per contact). The two are only separable by the press state: a press that is
        // still held when the player lands would start a new launch on the same tick.
        [Test]
        public void Landing_AbsorbsTheLaunch_AndReArmsTheJump()
        {
            Tick(1, true);
            _input.IsJumping = true;
            Tick(1, true);

            float launch = _player.Motion.PushVelocity.y;
            Tick(10, false);
            Assert.AreEqual(launch, _player.Motion.PushVelocity.y, Tolerance, "inertia keeps the player flying");

            _input.IsJumping = false;
            Tick(1, true);
            Assert.AreEqual(0f, _player.Motion.PushVelocity.y, Tolerance, "contact absorbs the jump momentum");

            _input.IsJumping = true;
            Tick(1, true);
            float expected = _settings.JumpPreset.Force / _settings.Mass;
            Assert.AreEqual(expected, _player.Motion.PushVelocity.y, Tolerance, "the jump re-arms on the next contact");
        }

        // D-017's own words - one press per contact, the contact re-arms it - mean a press held through a
        // landing bounces. This pins that feel choice: a jump that needs a re-press is a new decision, and
        // this test is what changes when it is made.
        [Test]
        public void PressHeldThroughALanding_ReBounces()
        {
            Tick(1, true);
            _input.IsJumping = true;
            Tick(1, true);
            Tick(10, false);
            Tick(1, true);

            float expected = _settings.JumpPreset.Force / _settings.Mass;
            Assert.AreEqual(expected, _player.Motion.PushVelocity.y, Tolerance, "a held press bounces on landing");
            Assert.AreEqual(0, _player.Motion.ActivePushCount, "one jump entry, never one per contact");
        }

        // The jump is the same mechanism as an external push: authored, not coded.
        [Test]
        public void JumpStrengthIsAuthored_NotHardcoded()
        {
            Tick(1, true);
            _input.IsJumping = true;
            Tick(1, true);

            float first = _player.Motion.PushVelocity.y;

            SetUp();
            _settings.JumpPreset.Force *= 2f;

            Tick(1, true);
            _input.IsJumping = true;
            Tick(1, true);

            Assert.AreEqual(2f * first, _player.Motion.PushVelocity.y, Tolerance,
                "twice the authored Force gives twice the launch, so the jump is data-driven");
        }

        // Mass is authored: the same preset shoves a heavier player half as much. A heavier player is a
        // player built with the heavier Mass - the domain reads authored numbers when it is constructed,
        // and an asset does not change under a live session (D-012).
        [Test]
        public void HeavierPlayerLaunchesLower_FromTheSamePreset()
        {
            Tick(1, true);
            _input.IsJumping = true;
            Tick(1, true);

            float light = _player.Motion.PushVelocity.y;

            _settings.Mass *= 2f;
            _input = new StubInput();
            _player = new Player(_input, _settings);

            Tick(1, true);
            _input.IsJumping = true;
            Tick(1, true);

            Assert.AreEqual(light * 0.5f, _player.Motion.PushVelocity.y, Tolerance,
                "the same preset shoves a heavier player half as much");
        }
    }
}
