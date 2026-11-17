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
        public bool IsJumping { get; }

        public event Action<bool> OnInteractButton;
        public event Action<bool> OnUseButton;
        public event Action<bool> OnThrowButton;
    }
}
