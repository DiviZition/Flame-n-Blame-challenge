using System;
using UnityEngine;
using InputCallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class PlayerInputMain : IPlayerInput, IDisposable
    {
        public Vector2 InputDirection {  get; private set; }
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

        private void SubscribeToInputs(InputSystem_Actions inputs)
        {
            inputs.Player.Move.performed += ReadInputDirection;
            inputs.Player.Sprint.performed += ReadIsRunning;
            inputs.Player.Use.performed += CallOnUse;
            inputs.Player.Interact.performed += CallOnInteract;
            inputs.Player.Throw.performed += CallOnThrow;
        }

        public void Dispose() => UnsubscribeFromInputs();
        private void UnsubscribeFromInputs()
        {
            _inputMap.Player.Move.performed -= ReadInputDirection;
            _inputMap.Player.Sprint.performed -= ReadIsRunning;
            _inputMap.Player.Use.performed -= CallOnUse;
            _inputMap.Player.Interact.performed -= CallOnInteract;
            _inputMap.Player.Throw.performed -= CallOnThrow;
        }

        private void ReadInputDirection(InputCallbackContext context) => InputDirection = context.ReadValue<Vector2>();
        private void ReadIsRunning(InputCallbackContext context) => IsRunning = context.ReadValue<bool>();
        private void CallOnUse(InputCallbackContext context) => OnUseButton?.Invoke(context.ReadValue<bool>());
        private void CallOnThrow(InputCallbackContext context) => OnThrowButton?.Invoke(context.ReadValue<bool>());
        private void CallOnInteract(InputCallbackContext context) => OnThrowButton?.Invoke(context.ReadValue<bool>());
    }
}
