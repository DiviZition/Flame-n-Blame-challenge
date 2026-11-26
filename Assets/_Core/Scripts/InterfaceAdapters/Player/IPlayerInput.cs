using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    public interface IPlayerInput
    {
        public Vector2 InputDirection { get; }
        public Vector2 LookDelta { get; }
        public bool IsRunning { get; }
        public bool IsWalking { get; }
        /// The jump press as a **one-tick fact**: the shell reports the frame the press started
        /// (`WasPressedThisFrame`), never the held state — so a press held across a landing cannot fire twice
        /// (D-039: the gate eats the press the tick it is seen).
        public bool JumpPressed { get; }
        /// The pause press as a **one-tick fact**, same mechanism as `JumpPressed`: the shell reports the
        /// frame the press started, never the held state, so one ESC press is one pause request (D-039).
        public bool PausePressed { get; }

        public bool UseRightItemPressed { get; }
        public bool UseLeftItemPressed { get; }
        public bool ThrowButtonPressed { get; }
    }
}
