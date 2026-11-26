using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Thin shell (D-012): it Moves by exactly what the domain decided, reports what Unity observed,
    /// and holds no rules. Registered on GameUpdater AFTER the player, so input -> domain -> shell is
    /// one list in code and there is no second Update() in the scene (D-013).
    ///
    /// The contact normal comes from Unity's own collision callback, not from a physics query: `Move`
    /// fires `OnControllerColliderHit` synchronously inside itself, so the normal is fresh the moment the
    /// Move returns. `CollisionFlags.Sides` gives no axis, and the axis is the whole point of absorbing a
    /// wall (G-016).
    public class PlayerMotor : MonoBehaviour, IUpdatable
    {
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Transform _cameraHolder;

        public bool isSprinting => _player?.Movement.IsSprinting ?? false;
        public bool IsGrounded => _player?.Movement.IsGrounded ?? false;
        public Vector2 InputDirection => _player?.RawInputDirection ?? Vector3.zero;

        private Player _player;
        private Vector3 _contactNormal;

        public void Construct(Player player) => _player = player;

        public void UpdateLogic(float time, float deltaTime)
        {
            if (_player == null)
                return;

            ApplyLook();

            // displacement = Velocity * dt is arithmetic with no rules in it - the whole shell.
            _controller.Move(_player.Velocity * deltaTime);

            // Report what THIS Move observed; the domain applies contact at the start of the next tick.
            _player.ReportContact(_controller.isGrounded, _contactNormal);
            _contactNormal = Vector3.zero;
        }

        /// The only Unity-side way to reach the push model. Forwards, nothing else. The direction is the
        /// source's Vector3, never a rotation: a shell that turns a direction into a quaternion is where
        /// the diagonal-launch trap lives (G-018).
        public void ApplyForce(PushPresetData preset, Vector3 worldDirection)
        {
            _player?.ReceivePush(preset, worldDirection);
        }

        /// Unity calls this during `Move`, once per collider touched. The normal is the surface's outward
        /// normal, which is exactly the axis the domain needs to absorb momentum along.
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _contactNormal = hit.normal;
        }

        private void ApplyLook()
        {
            transform.rotation = Quaternion.Euler(0f, _player.LookRotation.Yaw, 0f);
            _cameraHolder.localRotation = Quaternion.Euler(_player.LookRotation.Pitch, 0f, 0f);
        }
    }
}
