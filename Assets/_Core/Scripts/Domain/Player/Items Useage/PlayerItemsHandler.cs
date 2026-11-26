using Flame_n_Blame_Challenge.Adapters;
using System;

namespace Flame_n_Blame_Challenge.Domain
{
    public class PlayerItemsHandler : IUpdatable
    {
        public event Action<HandSide, IHoldable> OnItemPickedUp;
        public event Action<HandSide> OnItemDropped;
        public event Action<IUsable> OnItemUsed;

        public ItemData RightHand { get; private set; }
        public ItemData LeftHand { get; private set; }

        public IHoldable AvailableItemToPickUp { get; private set; }

        private readonly IPlayerInput _input;

        public PlayerItemsHandler(IPlayerInput input)
        {
            _input = input;
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

            TryUseItem(RightHand, _input.UseRightItemPressed);
            TryUseItem(LeftHand, _input.UseLeftItemPressed);

            TryDropItem(RightHand, _input.UseRightItemPressed);
            TryDropItem(LeftHand, _input.UseLeftItemPressed);
        }

        private void TryUseItem(ItemData hand, bool isUseButtonPressed)
        {
            if (hand.Slot == null)
                return;

            if (isUseButtonPressed == false)
                return;

            if (_input.ThrowButtonPressed == true)
                return;

            if (hand.TimeToUse > 0)
                return;

            if (hand is not IUsable usableItem)
                return;

            usableItem.Use();
            hand.SetTimeToUse(usableItem.UseCoolDown);
            OnItemUsed?.Invoke(usableItem);
        }

        private void TryDropItem(ItemData hand, bool isUseButtonPressed)
        {
            if (hand.Slot == null)
                return;

            if (isUseButtonPressed == false)
                return;

            if (_input.ThrowButtonPressed == false)
                return;

            if (hand.TimeToUse > 0)
                return;

            hand.SetItem(null);
            OnItemDropped?.Invoke(hand.HandSide);
        }

        private void TryPickUpItem(ItemData hand, bool isUseButtonPressed)
        {
            if (hand.Slot != null || AvailableItemToPickUp == null)
                return;

            if (_input.ThrowButtonPressed == false)
                return;

            if (isUseButtonPressed == false)
                return;
            
            hand.SetItem(AvailableItemToPickUp);
            SetItemAvailableToPickUp(null);
            OnItemPickedUp?.Invoke(hand.HandSide, hand.Slot);
        }
    }
}
