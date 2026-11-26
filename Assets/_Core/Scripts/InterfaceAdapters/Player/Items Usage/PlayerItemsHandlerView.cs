using Flame_n_Blame_Challenge.Domain;
using PrimeTween;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class PlayerItemsHandlerView : MonoBehaviour, IUpdatable
    {
        [SerializeField] private ViewHandData _rightHand;
        [SerializeField] private ViewHandData _leftHand;

        [Header("Items Detection")]
        [SerializeField] private float _detectionRadius = 1.0f;
        [SerializeField] private float _detectionRange = 2.0f;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private LayerMask _itemsMask;

        private IItemView _availableItem;
        private Dictionary<HandSide, ViewHandData> _hands;
        private Player _player;

        internal void Construct(Player player)
        {
            _hands = new Dictionary<HandSide, ViewHandData> { [HandSide.Left] = _leftHand, [HandSide.Right] = _rightHand };

            _player = player;

            _player.ItemsHandler.OnItemPickedUp += PickUpItem;
        }

        public void UpdateLogic(float time, float deltaTime)
        {
            DetectAvailableItems();
        }

        private void PickUpItem(HandSide side, IHoldable holdable)
        {
            var newItemPos = _hands[side].ItemSpot;
            if (holdable is not IItemView itemView)
                throw new ArgumentNullException($"Item handler domain is trying to equip an Item without the view. Do not pass IHoldables to domain from aside, please");

            itemView.DisableCollision();
            itemView.StopHighlighting();

            Sequence.Create()
                .Group(Tween.Position(itemView.Transform, newItemPos.position, itemView.EquipTime, Ease.OutCubic))
                .Group(Tween.Rotation(itemView.Transform, newItemPos.rotation, itemView.EquipTime, Ease.OutCubic));
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
                if (hitinfo.transform == _availableItem?.Transform)
                    return;

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
