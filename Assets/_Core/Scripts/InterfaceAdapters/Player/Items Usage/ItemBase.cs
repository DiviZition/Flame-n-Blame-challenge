using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class ItemBase : MonoBehaviour, IItemView
    {
        [field: SerializeField] public Transform Transform { get; private set; }
        [field: SerializeField] public float EquipTime { get; private set; } = 1;

        [SerializeField] private Outline _outline;
        [SerializeField] private Collider _collider;
        [SerializeField] private Rigidbody _rigidbody;

        public void DisableCollision()
        {
            _collider.enabled = false;
            _rigidbody.isKinematic = true;
        }
        public void EnableCollision()
        {
            _collider.enabled = true;
            _rigidbody.isKinematic = false;
        }

        public void StartHighlighting() => _outline.enabled = true;
        public void StopHighlighting() => _outline.enabled = false;

        private void Start() => StopHighlighting();
    }
}
