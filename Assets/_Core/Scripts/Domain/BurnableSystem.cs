using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    public class BurnableSystem
    {
        
    }

    public class Burnable
    {

        public float ThermalState { get; private set; } // Normalized heat/fire intensity

        public float IgnitionThreshold { get; private set; } // IgnitionProgress below this - means no fire
        public float FireResistance { get; private set; } // BurnDurability drain multiplier
        public float BurnDurability { get; private set; } // HP
        public float MaxBurnIntensity { get; private set; } // Influences fire population

        public bool IsBurnedOut => BurnDurability <= 0;
        public bool IsBurning => !IsBurnedOut && ThermalState > 0;

        private List<IBurnable> _nearbyBurnables = new();

        public void Tick(float addHeat)
        {
            float normalizedFireProgress =
                (ThermalState - IgnitionThreshold) /
                (1f - IgnitionThreshold);

            float ownFireInfluence =
                Math.Clamp(normalizedFireProgress, 0f, 1f) * MaxBurnIntensity;
        }

        private float GetFireInfluence()
        {
            if (IsBurnedOut || ThermalState <= IgnitionThreshold)
                return 0f;

            float progress = (ThermalState - IgnitionThreshold) / (1f - IgnitionThreshold);

            return progress * MaxBurnIntensity;
        }
    }

    public interface IBurnable //maybe IBurnableAffector. We might want to add global heat reduction later.
    {
        public float IgnitionProgress { get; } // Normalized heat/fire intensity

        public float IgnitionThreashold { get; } // IgnitionProgress below this - means no fire
        public float FireResistance { get; } // BurnDurability drain multiplier
        public float BurnDurability { get; } // HP
        public float MaxBurnIntensity { get; } // Influences fire population

        public bool IsBurnedOut => BurnDurability <= 0;
        public bool IsBurning => !IsBurnedOut && IgnitionProgress > 0;
    }
}
