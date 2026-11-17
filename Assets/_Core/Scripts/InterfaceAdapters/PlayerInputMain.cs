using System;
using UnityEngine;
using InputCallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class PlayerInputMain : IPlayerInput, IUpdatable, IDisposable
    {
        public Vector2 LookDelta { get; private set;  }
        public Vector2 InputDirection {  get; private set; }
        public bool IsJumping {  get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsWalking => InputDirection != Vector2.zero;

        public event Action<bool> OnInteractButton;
        public event Action<bool> OnUseButton;
        public event Action<bool> OnThrowButton;

        private InputSystem_Actions _inputMap;

        public PlayerInputMain()
        {
            _inputMap = new InputSystem_Actions();
            _inputMap.Enable();
            SubscribeToInputs(_inputMap);
        }

        public void Update(float time, float deltaTime)
        {
            LookDelta = _inputMap.Player.Look.ReadValue<Vector2>();
            InputDirection = _inputMap.Player.Move.ReadValue<Vector2>();

            IsJumping = _inputMap.Player.Jump.ReadValue<float>() > 0;
            IsRunning = _inputMap.Player.Sprint.ReadValue<float>() > 0;
        }

        private void SubscribeToInputs(InputSystem_Actions inputs)
        {
            inputs.Player.Use.performed += CallOnUse;
            inputs.Player.Interact.performed += CallOnInteract;
            inputs.Player.Throw.performed += CallOnThrow;
        }

        private void UnsubscribeFromInputs()
        {
            _inputMap.Player.Use.performed -= CallOnUse;
            _inputMap.Player.Interact.performed -= CallOnInteract;
            _inputMap.Player.Throw.performed -= CallOnThrow;
        }

        private void CallOnUse(InputCallbackContext context) => OnUseButton?.Invoke(context.ReadValue<bool>());
        private void CallOnThrow(InputCallbackContext context) => OnThrowButton?.Invoke(context.ReadValue<bool>());
        private void CallOnInteract(InputCallbackContext context) => OnInteractButton?.Invoke(context.ReadValue<bool>());

        public void Dispose()
        {
            UnsubscribeFromInputs();
            _inputMap.Disable();
            _inputMap?.Dispose();
            _inputMap = null;
        }
    }
}
