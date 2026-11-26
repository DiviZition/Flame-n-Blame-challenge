namespace Flame_n_Blame_Challenge.Domain
{
    public class ItemData
    {
        public HandSide HandSide { get; private set; }
        public IHoldable Slot { get; private set; }
        public float TimeToUse { get; private set; }

        public ItemData(HandSide handSide) => HandSide = handSide;

        public void SetItem(IHoldable item)
        {
            Slot = item;

            float tmeTouse = item == null ? item.EquipTime : 0;
            SetTimeToUse(tmeTouse);
        }

        public void DecreaseTimer(float value) => SetTimeToUse(TimeToUse - value);
        public void SetTimeToUse(float time)
        {
            TimeToUse = time;

            if (TimeToUse < 0)
                TimeToUse = 0;
        }
    }
}
