using Flame_n_Blame_Challenge.Adapters;
using UnityEngine;

namespace Flame_n_Blame_Challenge
{
    public class PlayerAnimations : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerMotor _motor;

        private void Update()
        {
            _animator.SetBool("IsSprinting", _motor.isSprinting);
            _animator.SetBool("IsGrounded", _motor.IsGrounded);
            _animator.SetFloat("MoveInputDirection_X", _motor.MoveDirection.x);
            _animator.SetFloat("MoveInputDirection_Z", _motor.MoveDirection.z);
        }
    }
}
