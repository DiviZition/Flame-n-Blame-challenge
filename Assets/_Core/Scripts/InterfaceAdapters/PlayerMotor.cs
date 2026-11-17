using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    [CreateAssetMenu(fileName = "Player Motor Config", menuName = "Configs/Player Motor")]
    public class PlayerMotorConfig : ScriptableObject
    {
        [field: SerializeField] public AnimationCurve JumpCurve { get; private set; }
        [field: SerializeField] public float Gravity { get; private set; }
        [field: SerializeField] public float JumpDuration { get; private set; }
    }

    public class PlayerMotor : MonoBehaviour
    {
        [field: SerializeField] public AnimationCurve JumpCurve { get; private set; }
        [field: SerializeField] public float Gravity { get; private set; } = 10;
        [field: SerializeField] public float JumpDuration { get; private set; }

        [SerializeField] private CharacterController _controller;
        [SerializeField] private Transform _cameraHolder; // pitch rotates this

        private Player _player;

        public void Construct(Player player)
        {
            _player = player;
        }

        private void Update()
        {
            if (_player == null) return;

            // 1. Feed grounded state into Domain
            _player.Movement.SetIsGrounded(_controller.isGrounded);

            // 2. Apply look
            transform.rotation = Quaternion.Euler(0f, _player.LookRotation.Yaw, 0f);
            _cameraHolder.localRotation = Quaternion.Euler(_player.LookRotation.Pitch, 0f, 0f);

            // 3. Apply movement
            Vector3 velocity = _player.Movement.DesiredVelocity;
            velocity.y -= Gravity;
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}
