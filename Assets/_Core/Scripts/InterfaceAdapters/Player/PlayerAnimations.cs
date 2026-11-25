using Flame_n_Blame_Challenge.Adapters;
using UnityEngine;

namespace Flame_n_Blame_Challenge
{
    public class PlayerAnimations : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private float _smoothSpeed = 10f;

        private Vector2 _smoothedInput;

        private void Update()
        {
            // Keyboard input is binary: 0/1, it arrives in one frame. The blend tree needs a ramp,
            // MoveTowards gives one in finite time and reaches exact zero, so IsWalking turns off
            // only when the ramp is done.
            _smoothedInput = Vector2.MoveTowards(_smoothedInput, _motor.InputDirection, _smoothSpeed * Time.deltaTime);

            _animator.SetBool("IsRunning", _motor.isSprinting);
            _animator.SetBool("IsGrounded", _motor.IsGrounded);
            _animator.SetBool("IsWalking", _smoothedInput != Vector2.zero);
            _animator.SetFloat("MoveInputDirection_X", _smoothedInput.x);
            _animator.SetFloat("MoveInputDirection_Z", _smoothedInput.y);
        }
    }
}
