using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Thin shell (D-012): it Moves by exactly what the domain decided, reports what Unity observed,
    /// and holds no rules. Registered on GameUpdater AFTER the player, so input -> domain -> shell is
    /// one list in code and there is no second Update() in the scene (D-013).
    public class PlayerMotor : MonoBehaviour, IUpdatable
    {
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Transform _cameraHolder;

        private Player _player;

        public void Construct(Player player) => _player = player;

        public void UpdateLogic(float time, float deltaTime)
        {
            if (_player == null)
                return;

            ApplyLook();

            // displacement = Velocity * dt is arithmetic with no rules in it - the whole shell.
            CollisionFlags flags = _controller.Move(_player.Velocity * deltaTime);

            // Report the flags for THIS Move; the domain applies contact at the start of the next tick.
            _player.ReportContact(_controller.isGrounded, (flags & CollisionFlags.Sides) != 0);
        }

        /// The only Unity-side way to reach the push model. Forwards, nothing else.
        public void ApplyForce(PushPresetData preset, Quaternion worldOrientation)
        {
            _player?.ReceivePush(preset, worldOrientation);
        }

        private void ApplyLook()
        {
            transform.rotation = Quaternion.Euler(0f, _player.LookRotation.Yaw, 0f);
            _cameraHolder.localRotation = Quaternion.Euler(_player.LookRotation.Pitch, 0f, 0f);
        }
    }
}
