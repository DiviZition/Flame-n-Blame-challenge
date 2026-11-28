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
        [SerializeField] private float _ikFallowSpeed = 2f;

        [SerializeField] private HandIKRefs _leftIKHandRefs;
        [SerializeField] private HandIKRefs _rightIKHandRefs;

        private Dictionary<HandSide, HandIKRefs> _handRefs;

        private Vector2 _smoothedInput;

        private void Start()
        {
            _handRefs = new Dictionary<HandSide, HandIKRefs>(2);
            _handRefs.Add(HandSide.Left, _leftIKHandRefs);
            _handRefs.Add(HandSide.Right, _rightIKHandRefs);
            _leftIKHandRefs.CurrentReference = _leftIKHandRefs.InitialIKReference;
            _rightIKHandRefs.CurrentReference = _rightIKHandRefs.InitialIKReference;
        }

        private void Update()
        {
            // Keyboard input is binary: 0/1, it arrives in one frame. The blend tree needs a ramp,
            // MoveTowards gives one in finite time and reaches exact zero, so IsWalking turns off
            // only when the ramp is done.
            _smoothedInput = Vector2.MoveTowards(_smoothedInput, _motor.InputDirection, _smoothSpeed * Time.deltaTime);
            UpdateHandIKTransform(_handRefs[HandSide.Left]);
            UpdateHandIKTransform(_handRefs[HandSide.Right]);

            _animator.SetBool("IsRunning", _motor.isSprinting);
            _animator.SetBool("IsGrounded", _motor.IsGrounded);
            _animator.SetBool("IsWalking", _smoothedInput != Vector2.zero);
            _animator.SetFloat("MoveInputDirection_X", _smoothedInput.x);
            _animator.SetFloat("MoveInputDirection_Z", _smoothedInput.y);
        }

        public void UnHideHand(HandSide handSide)
        {
            Debug.Log($"New current target - {_handRefs[handSide].PreviousReference}");
            if (_handRefs[handSide].PreviousReference == null)
                return;

            _handRefs[handSide].CurrentReference = _handRefs[handSide].PreviousReference;
        }

        public void HideHand(HandSide handSide)
        {
            if (_handRefs[handSide].CurrentReference == _handRefs[handSide].HiddenIKSpot)
                return;

            _handRefs[handSide].PreviousReference = _handRefs[handSide].CurrentReference;
            _handRefs[handSide].CurrentReference = _handRefs[handSide].HiddenIKSpot;
        }

        private void UpdateHandIKTransform(HandIKRefs handRef)
        {
            if (handRef.CurrentReference == null)
                return;

            Vector3 position = Vector3.MoveTowards(handRef.IKTarget.position, handRef.CurrentReference.position, _ikFallowSpeed * Time.deltaTime);
            handRef.IKTarget.position = position;
            Quaternion rotation = Quaternion.Lerp(handRef.IKTarget.rotation, handRef.CurrentReference.rotation, _ikFallowSpeed * Time.deltaTime);
            handRef.IKTarget.rotation = rotation;
        }

        [Serializable]
        public class HandIKRefs
        {
            [field: SerializeField] public Transform IKTarget { get; private set; }
            [field: SerializeField] public Transform InitialIKReference { get; private set; }
            [field: SerializeField] public Transform HiddenIKSpot { get; private set; }
            public Transform PreviousReference { get; set; }
            public Transform CurrentReference { get; set; }
        }
    }
}
