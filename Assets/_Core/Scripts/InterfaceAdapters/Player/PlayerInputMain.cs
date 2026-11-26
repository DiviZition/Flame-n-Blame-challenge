using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class PlayerInputMain : IPlayerInput, IUpdatable, IDisposable
    {
        public Vector2 LookDelta { get; private set;  }
        public Vector2 InputDirection {  get; private set; }
        public bool JumpPressed {  get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsWalking => InputDirection != Vector2.zero;

        public bool UseLeftItemPressed { get; private set; }
        public bool UseRightItemPressed { get; private set; }
        public bool ThrowButtonPressed { get; private set; }

        private InputSystem_Actions _inputMap;

        public PlayerInputMain()
        {
            _inputMap = new InputSystem_Actions();
            _inputMap.Enable();
        }

        public void UpdateLogic(float time, float deltaTime)
        {
            LookDelta = _inputMap.Player.Look.ReadValue<Vector2>();
            InputDirection = _inputMap.Player.Move.ReadValue<Vector2>();

            // The press is a fact, not a state: true only on the tick the button started, so the gate consumes
            // one press per jump and a held key cannot bounce (D-039).
            JumpPressed = _inputMap.Player.Jump.WasPressedThisFrame();
            IsRunning = _inputMap.Player.Sprint.ReadValue<float>() > 0;


            UseRightItemPressed = _inputMap.Player.UseRightItem.ReadValue<float>() > 0;
            UseLeftItemPressed = _inputMap.Player.UseLeftItem.ReadValue<float>() > 0;
            ThrowButtonPressed = _inputMap.Player.Throw.ReadValue<float>() > 0;
        }

        public void Dispose()
        {
            _inputMap.Disable();
            _inputMap?.Dispose();
            _inputMap = null;
        }
    }
}
