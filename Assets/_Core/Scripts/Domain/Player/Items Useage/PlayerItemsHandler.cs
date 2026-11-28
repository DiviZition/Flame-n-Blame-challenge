using Flame_n_Blame_Challenge.Adapters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Flame_n_Blame_Challenge.Domain
{
    public class PlayerItemsHandler : IUpdatable
    {
        public event Action<HandSide, IHoldable> OnItemPickedUp;
        public event Action<HandSide> OnItemDropped;

        public ItemData RightHand { get; private set; }
        public ItemData LeftHand { get; private set; }

        public IHoldable AvailableItemToPickUp { get; private set; }

        private readonly IPlayerInput _input;
        private readonly Player _player;

        public PlayerItemsHandler(IPlayerInput input, Player player)
        {
            //TODO: Не должен знать про Player, вместо этого нужен отдельный обработчик эффектов.
            _input = input;
            _player = player;
            RightHand = new ItemData(HandSide.Right);
            LeftHand = new ItemData(HandSide.Left);
        }

        public void SetItemAvailableToPickUp(IHoldable item) => AvailableItemToPickUp = item;

        public void UpdateLogic(float time, float deltaTime)
        {
            RightHand.DecreaseTimer(deltaTime);
            LeftHand.DecreaseTimer(deltaTime);

            TryPickUpItem(RightHand, _input.UseRightItemPressed);
            TryPickUpItem(LeftHand, _input.UseLeftItemPressed);

            TryDropItem(RightHand, _input.UseRightItemPressed);
            TryDropItem(LeftHand, _input.UseLeftItemPressed);

            TryUseItem(RightHand, _input.UseRightItemPressed);
            TryUseItem(LeftHand, _input.UseLeftItemPressed);

        }

        private void TryUseItem(ItemData hand, bool isUseButtonPressed)
        {
            if (_input.ThrowButtonPressed == true)
                return;

            if (_input.PickUpButtonPressed == true)
                return;

            if (isUseButtonPressed == false)
                return;

            if (hand.Slot == null)
                return;

            if (hand.TimeToUse > 0)
                return;

            if (hand.Slot is not IUsable usableItem)
                return;

            usableItem.Use(_player);
            hand.SetTimeToUse(usableItem.UseCoolDown);
        }

        private void TryDropItem(ItemData hand, bool isUseButtonPressed)
        {
            if (_input.ThrowButtonPressed == false)
                return;

            if (_input.PickUpButtonPressed == true)
                return;

            if (isUseButtonPressed == false)
                return;

            if (hand.Slot == null)
                return;

            if (hand.TimeToUse > 0)
                return;

            hand.SetItem(null);
            OnItemDropped?.Invoke(hand.HandSide);
        }
        private void TryPickUpItem(ItemData hand, bool isUseButtonPressed)
        {
            if (_input.ThrowButtonPressed == true)
                return;

            if (_input.PickUpButtonPressed == false)
                return;

            if (isUseButtonPressed == false)
                return;

            if (hand.Slot != null)
                return;

            if (AvailableItemToPickUp == null)
                return;
            
            hand.SetItem(AvailableItemToPickUp);
            
            SetItemAvailableToPickUp(null);
            OnItemPickedUp?.Invoke(hand.HandSide, hand.Slot);
        }
    }
}
