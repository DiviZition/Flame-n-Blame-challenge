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

        public event Action<bool> OnInteractButton;
        public event Action<bool> OnUseButton;
        public event Action<bool> OnThrowButton;
    }
}
