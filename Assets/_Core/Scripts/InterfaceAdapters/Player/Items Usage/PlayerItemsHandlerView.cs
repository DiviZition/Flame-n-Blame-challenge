using Flame_n_Blame_Challenge.Domain;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    public interface IItemView : IHoldable
    {
        public Transform Transform { get; }

        public void StartHighlighting();
        public void StopHighlighting();
    }

    public class PlayerItemsHandlerView : MonoBehaviour, IUpdatable
    {
        [SerializeField] private Transform _rightHand;
        [SerializeField] private Transform _leftHand;

        [Header("Items Detection")]
        [SerializeField] private float _detectionRadius = 1.0f;
        [SerializeField] private float _detectionRange = 2.0f;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private LayerMask _itemsMask;

        private IItemView _availableItem;
        private Player _player;

        internal void Construct(Player player)
        {
            _player = player;
        }

        public void UpdateLogic(float time, float deltaTime)
        {
            DetectAvailableItems();
        }
        
        private void DetectAvailableItems()
        {
            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward * _detectionRange);
            bool isHit = Physics.SphereCast(ray, _detectionRadius, out RaycastHit hitinfo, _detectionRange, _itemsMask);

            if (isHit)
                SwitchToNewAvailableItem();
            else
                LeaveAvailableItem();

            _player.ItemsHandler.SetItemAvailableToPickUp(_availableItem);

            void SwitchToNewAvailableItem()
            {
                LeaveAvailableItem();

                if (hitinfo.transform.TryGetComponent(out IItemView item) == false)
                    throw new NullReferenceException($"There is an object on the ITEMS layer, that does not contain IItemView Component");

                _availableItem = item;
                _availableItem.StartHighlighting();
            }
        }

        private void LeaveAvailableItem()
        {
            _availableItem?.StopHighlighting();
            _availableItem = null;
        }
    }
}
