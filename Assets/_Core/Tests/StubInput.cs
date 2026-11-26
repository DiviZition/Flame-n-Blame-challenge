using System;
using Flame_n_Blame_Challenge.Adapters;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Tests drive intent directly through this stub - no Input System, no scene, no PlayerMotor.
    public class StubInput : IPlayerInput
    {
        public Vector2 InputDirection { get; set; }
        public Vector2 LookDelta { get; set; }
        public bool IsRunning { get; set; }
        public bool IsWalking => InputDirection != Vector2.zero;
        /// The press fact, the way the shell reports it: true for the tick it is set, and a test clears it
        /// after that tick (`PressJump` in the jump tests), because a held press must not fire again (D-039).
        public bool JumpPressed { get; set; }
        /// The same one-tick fact for the pause gate: a test sets it for one tick and clears it after.
        public bool PausePressed { get; set; }

        public bool UseRightItemPressed { get; set; }
        public bool UseLeftItemPressed { get; set; }
        public bool ThrowButtonPressed { get; set; }
    }
}
