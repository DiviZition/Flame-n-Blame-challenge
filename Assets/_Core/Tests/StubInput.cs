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
        public bool IsJumping { get; set; }

        public event Action<bool> OnInteractButton;
        public event Action<bool> OnUseButton;
        public event Action<bool> OnThrowButton;
    }
}
