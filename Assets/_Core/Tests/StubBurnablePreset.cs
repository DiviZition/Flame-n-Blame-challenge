using Flame_n_Blame_Challenge.Domain;

namespace Flame_n_Blame_Challenge.Tests
{
    /// The five authored numbers of a burnable, set per test (D-037: a port holds what its reader reads).
    /// Defaults are the round numbers every expectation is computed from: threshold 0.3, resistance 1,
    /// durability 4, intensity 0.1, reach 3 m.
    public class StubBurnablePreset : IBurnablePreset
    {
        public float IgnitionThreshold { get; set; } = 0.3f;
        public float FireResistance { get; set; } = 1f;
        public float MaxBurnDurability { get; set; } = 4f;
        public float MaxBurnIntensity { get; set; } = 0.1f;
        public float MaxFirePopulationRange { get; set; } = 3f;
    }
}
