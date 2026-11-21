namespace Flame_n_Blame_Challenge.Domain
{
    /// The state of a burnable object (D-008 pillar 1: object state, never a fluid sim). The contract is
    /// authored here; the implementation (ignite -> blacken -> collapse, and fire spread from a burning
    /// neighbour) is the Phase 1 work in `TODO.md`, and nothing implements this interface yet.
    public interface IBurnable
    {
        public float IgnitionProgress { get; } // Normalized heat/fire intensity

        public float IgnitionThreshold { get; } // IgnitionProgress below this - means no fire
        public float FireResistance { get; } // BurnDurability drain multiplier
        public float BurnDurability { get; } // HP
        public float MaxBurnIntensity { get; } // Influences fire population

        public bool IsBurnedOut => BurnDurability <= 0;
        public bool IsBurning => !IsBurnedOut && IgnitionProgress > 0;
    }
}
