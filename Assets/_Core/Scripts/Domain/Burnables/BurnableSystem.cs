using Flame_n_Blame_Challenge.Adapters;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// The state of a burnable object (D-008 pillar 1: object state, never a fluid sim). **One** normalized
    /// axis: `IgnitionStrength` is the heat the body is at (0..1), `CurrentDurability` is what the fire has
    /// not eaten yet. Fire, smoke, char and reach are all read off those two, so the visuals never own a
    /// scale of their own and the DTO stays 5 numbers (D-043).
    public interface IBurnable
    {
        public float IgnitionStrength { get; } // Normalized heat the body is at, 0..1
        public float CurrentDurability { get; }
        public Vector3 Position { get; }

        public float FireLevel { get; } // Normalized fire inside the band above the ignition threshold, 0..1
        public float SmokeLevel { get; } // Normalized smoke; full smoke at the threshold
        public float Charredness { get; } // How much of the body the fire has eaten, 0..1

        public bool IsBurning { get; }
        public bool IsBurnedOut { get; }

        /// The question a spread system asks: how much heat this body gives a neighbour at `distance`. The
        /// fire's strength with a quadratic falloff, 0 at and beyond the range.
        public float GetFireInfluenceAtDistance(float distance);

        /// How far this body's fire reaches, in metres: the reach is the pair test the adjacency table is
        /// built with (D-052), so the spread system reads it, and it is one of the five DTO numbers.
        public float MaxFirePopulationRange { get; }

        /// The only door heat goes through, in both directions. `amount` is what a source has integrated over
        /// its own time (the source owns its cadence, the domain never sees a `dt`); `FireResistance` is how
        /// strongly this body answers — a big wardrobe (res 0.2) heats and douses 5 times slower than a
        /// kindling, on the same normalized axis (D-043).
        public void AffectIgnitionStrength(float amount);
    }

    public class BurnableObject : IBurnable, IUpdatable
    {
        private readonly IBurnablePreset _preset;
        public Vector3 Position { private set; get; }

        /// The reach is authored, so it is a read of the preset, not state: the spread system builds its
        /// adjacency with it, and never asks for the live position.
        public float MaxFirePopulationRange => _preset.MaxFirePopulationRange;

        public float IgnitionStrength { get; private set; } // Normalized heat the body is at, 0..1
        public float CurrentDurability { get; private set; } // HP the fire has not eaten

        public bool IsBurnedOut => CurrentDurability <= 0f;

        public bool IsBurning => !IsBurnedOut && FireLevel > 0f;

        /// The band above the threshold, renormalized to 0..1: the threshold is the floor of the band, so a
        /// low-threshold prop has a wide band and reaches full fire earlier. Below the floor — no fire, smoke.
        public float FireLevel
        {
            get
            {
                if (IsBurnedOut || _preset.IgnitionThreshold >= 1f)
                    return 0f;

                if (IgnitionStrength <= _preset.IgnitionThreshold)
                    return 0f;

                return (IgnitionStrength - _preset.IgnitionThreshold) / (1f - _preset.IgnitionThreshold);
            }
        }

        /// The body's heat output — the number that feeds neighbours, eats the durability, and drives the
        /// body's own ramp (D-045). Private: the interface is the axis and the derived reads, not this product
        /// (D-043).
        private float CurrentBurnIntensity => FireLevel * _preset.MaxBurnIntensity;

        public float SmokeLevel
        {
            get
            {
                if (IsBurnedOut)
                    return 0f;

                // A threshold of 0 means the body burns as soon as it is warm, so smoke follows heat.
                if (_preset.IgnitionThreshold <= 0f)
                    return Mathf.Clamp01(IgnitionStrength);

                return Mathf.Min(IgnitionStrength / _preset.IgnitionThreshold, 1f);
            }
        }

        /// Char has no scale of its own: the fire's damage to the body is what reads as black, and HP never
        /// comes back, so an extinguished object stays black — the douse stops the char, it does not undo it.
        public float Charredness
        {
            get
            {
                if (_preset.MaxBurnDurability <= 0f)
                    return 1f;

                return 1f - CurrentDurability / _preset.MaxBurnDurability;
            }
        }

        public BurnableObject(IBurnablePreset preset, Vector3 position)
        {
            _preset = preset;
            Position = position;
            CurrentDurability = preset.MaxBurnDurability;
        }

        public void AffectIgnitionStrength(float amount)
        {
            if (IsBurnedOut || amount == 0f)
                return;

            IgnitionStrength = Mathf.Clamp01(IgnitionStrength + amount * _preset.FireResistance);
        }

        public void UpdateLogic(float time, float deltaTime) => TickBurn(deltaTime);

        /// One tick of burning. **Private**: the door in is `AffectIgnitionStrength`, the tick out is
        /// `UpdateLogic`, and a public second entry to the same rule invites a caller to tick the body twice
        /// in a frame. A burning body feeds itself **by its own intensity**: the heat the fire gives
        /// out is also what drives the ramp, so `MaxBurnIntensity` is felt on the speed of growth (0.1 = full
        /// fire in ~28 s at a 0.3 threshold, 1 = in ~2.8 s, D-045). Then the fire eats the body; resistance is
        /// not in this path, the burn length is `MaxBurnDurability / MaxBurnIntensity` seconds of full fire.
        /// Nothing cools by itself — heat leaves only through a stream.
        private void TickBurn(float deltaTime)
        {
            if (IsBurnedOut)
                return;

            IgnitionStrength = Mathf.Clamp01(IgnitionStrength + CurrentBurnIntensity * deltaTime);
            CurrentDurability = Mathf.Max(0f, CurrentDurability - CurrentBurnIntensity * deltaTime);
        }

        public float GetFireInfluenceAtDistance(float distance)
        {
            if (IsBurnedOut || distance >= _preset.MaxFirePopulationRange)
                return 0f;

            float t = distance / _preset.MaxFirePopulationRange;
            float falloff = (1f - t) * (1f - t);

            return CurrentBurnIntensity * falloff;
        }
    }

    /// The authored numbers of one burnable, copied out of the Inspector into the constructor (D-012). Five
    /// numbers: the axis floor, the body's response, and the three that set the fire's size, reach and length
    /// (D-043). Every number states its unit and what it decides (D-035).
    [Serializable]
    public class BurnablePreset : IBurnablePreset
    {
        [Tooltip("Heat (0..1) below which the object only smokes; above it it burns. "
            + "The floor of the fire band."), SerializeField]
        private float _ignitionThreshold = 0.3f;

        [Tooltip("How strongly this body answers to heat exchange, 0..1. 0 = fire and "
            + "water barely touch it, 1 = full response. Applies to heat in AND to dousing, so a big wardrobe "
            + "(0.2) heats and douses 5 times slower than a kindling. **A stream wins only when "
            + "stream × FireResistance > MaxBurnIntensity** — below that the self-feed outruns the water "
            + "(G-034)."), SerializeField]
        private float _fireResistance = 1f;

        [Tooltip("HP the fire eats. Full fire eats MaxBurnIntensity HP a second, so **the burn length in "
            + "seconds is MaxBurnDurability / MaxBurnIntensity**: 4 / 0.1 = 40 s, 1 / 1 = 1 s. 0 = burns out at "
            + "once. Resistance is not in this path."), SerializeField]
        private float _maxBurnDurability = 4f;

        [Tooltip("Heat the body gives out at full fire, in axis units per second. It feeds the neighbours "
            + "(0.1 at a 3 m reach is the ~10 s ignition rule of D-008), eats the durability, **and drives the "
            + "body's own ramp** (D-045): the ramp is (1 − threshold) / MaxBurnIntensity, so 1 = full fire in "
            + "~2.8 s, 0.1 in ~28 s. A stream that douses must be stronger than this times FireResistance "
            + "(G-034)."), SerializeField]
        private float _maxBurnIntensity = 0.1f;

        [Tooltip("How far this body's fire reaches at full fire, in metres. Neighbours "
            + "inside it gain heat, quadratic falloff."), SerializeField]
        private float _maxFirePopulationRange = 3f;

        public float IgnitionThreshold => _ignitionThreshold;
        public float FireResistance => _fireResistance;
        public float MaxBurnDurability => _maxBurnDurability;
        public float MaxBurnIntensity => _maxBurnIntensity;
        public float MaxFirePopulationRange => _maxFirePopulationRange;
    }

    public interface IBurnablePreset
    {
        public float IgnitionThreshold { get; } // Heat (0..1) below which the object only smokes
        public float FireResistance { get; } // How strongly the body answers to heat exchange, 0..1
        public float MaxBurnDurability { get; } // HP the fire eats; the burn length
        public float MaxBurnIntensity { get; } // Heat the body gives out at full fire
        public float MaxFirePopulationRange { get; } // Reach of the fire at full fire, in metres
    }
}