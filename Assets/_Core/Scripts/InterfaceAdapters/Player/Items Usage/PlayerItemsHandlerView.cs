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
        [SerializeField] private PlayerAnimations _animations;

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
            if (holdable is not IItemView itemView)
                throw new ArgumentNullException($"Item handler domain is trying to equip an Item without the view. Do not pass IHoldables to domain from aside, please");
            
            var newItemParent = _hands[side].ItemSpot;

            itemView.DisableCollision();
            itemView.StopHighlighting();
            itemView.Transform.SetParent(newItemParent);

            _animations.HideHand(side);

            Sequence.Create()
                .Group(Tween.LocalPosition(itemView.Transform, Vector3.zero, itemView.EquipTime, Ease.OutQuad))
                .Group(Tween.LocalRotation(itemView.Transform, Quaternion.identity, itemView.EquipTime, Ease.OutQuad));
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

        [Serializable]
        public class ViewHandData
        {
            [field: SerializeField] public Transform ItemSpot { get; private set; }
            [field: SerializeField] public IItemView ItemView { get; private set; }
        }
    }
}
