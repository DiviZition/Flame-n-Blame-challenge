using Flame_n_Blame_Challenge.Adapters;
using Flame_n_Blame_Challenge.Domain;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flame_n_Blame_Challenge
{
    public class PlayerAnimations : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private float _smoothSpeed = 10f;

        [SerializeField] private Transform _hiddenHandsSpot;
        [SerializeField] private HandIKRefs _leftIKHandRefs;
        [SerializeField] private HandIKRefs _rightIKHandRefs;

        private Dictionary<HandSide, HandIKRefs> _handRefs;

        private Vector2 _smoothedInput;

        private void Start()
        {
            _handRefs = new Dictionary<HandSide, HandIKRefs>(2);
            _handRefs.Add(HandSide.Left, _leftIKHandRefs);
            _handRefs.Add(HandSide.Right, _rightIKHandRefs);
        }

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

        public void UnHideHand(HandSide handSide)
        {
            Transform handIK = _handRefs[handSide].IKTarget;
            Transform previousParent = _handRefs[handSide].PreviousParent;

            if (previousParent == null || handIK.parent == previousParent)
                return;

            handIK.SetParent(previousParent);
            handIK.localPosition = Vector3.zero;
            previousParent = null;
        }

        public void HideHand(HandSide handSide)
        {
            Transform handIK = _handRefs[handSide].IKTarget;
            Transform previousParent = _handRefs[handSide].PreviousParent;

            if (handIK.parent == _hiddenHandsSpot)
                return;

            previousParent = handIK.parent;
            handIK.SetParent(_hiddenHandsSpot);
            handIK.localPosition = Vector3.zero;
        }

        [Serializable]
        public class HandIKRefs
        {
            [field: SerializeField] public Transform IKTarget {  get; private set; }
            public Transform PreviousParent { get; set; }
        }
    }
}
