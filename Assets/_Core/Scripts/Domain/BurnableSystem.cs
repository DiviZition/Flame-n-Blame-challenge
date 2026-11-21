using Flame_n_Blame_Challenge.Adapters;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// The state of a burnable object (D-008 pillar 1: object state, never a fluid sim). The contract is
    /// authored here; the implementation (ignite -> blacken -> collapse, and fire spread from a burning
    /// neighbour) is the Phase 1 work in `TODO.md`, and nothing implements this interface yet.
    public interface IBurnable
    {
        public float IgnitionStrength { get; } // Normalized heat/fire intensity

        public float CurrentBurnIntensity { get; }
        public float CurrentSmokeIntensity { get; }
        public bool IsBurnedOut { get; }
        public bool IsBurning { get; }

        public float GetFireInfluenceAtDistance(float distance);
    }

    public class BurnableObject : IBurnable, IUpdatable
    {
        private IBurnablePreset _settings;
        public float CurrentBurnIntensity => IgnitionStrength * _settings.MaxBurnIntensity;
        public float CurrentSmokeIntensity => GetCurrentSmokingIntensity();
        public bool IsBurnedOut => _settings.MaxBurnDurability <= 0;
        public bool IsBurning => !IsBurnedOut && IgnitionStrength > 0;

        public float IgnitionStrength { get; private set;  } // Normalized heat/fire intensity
        public float CurrentDurability { get; private set; } // HP

        public BurnableObject(IBurnablePreset settings, float initialDamage = 0)
        {
            _settings = settings;
        }

        public void UpdateLogic(float time, float deltaTime) => TickBurn(deltaTime);
        public void TickBurn(float deltaTime)
        {

        }

        public float GetFireInfluenceAtDistance(float distance) => GetQuadFireInfluence(distance);
        private float GetQuadFalloffFireInfluence(float distance)
        {
            if (distance > _settings.MaxFirePopulationRange)
                return 0f;

            float t = distance / _settings.MaxFirePopulationRange;
            return CurrentBurnIntensity * (1f - t * t);
        }
        private float GetQuadFireInfluence(float distance)
        {
            if (distance >= _settings.MaxFirePopulationRange)
                return 0f;

            float t = distance / _settings.MaxFirePopulationRange;
            float influence = 1f - t;

            influence *= influence;

            return influence * CurrentBurnIntensity;
        }
        private float GetLinearFireInfluence(float distance)
        {
            if (distance >= _settings.MaxFirePopulationRange)
                return 0f;

            if (distance <= 0f)
                return CurrentBurnIntensity;

            float normalizedDistance = distance / _settings.MaxFirePopulationRange;

            return (1f - normalizedDistance) * CurrentBurnIntensity;
        }

        private float GetCurrentSmokingIntensity()
        {
            if (_settings.IgnitionThreshold<= 0f)
                return 0f;

            if (IgnitionStrength <= 0f)
                return 0f;

            if (IgnitionStrength >= _settings.IgnitionThreshold)
                return 0f;

            return IgnitionStrength / _settings.IgnitionThreshold;
        }

        private float GetCurrentBurnIntensity()
        {
            if (_settings.IgnitionThreshold >= 1f)
                return 0f;

            if (IgnitionStrength <= _settings.IgnitionThreshold)
                return 0f;

            if (IgnitionStrength >= 1f)
                return 1f;

            return (IgnitionStrength - _settings.IgnitionThreshold) / (1f - _settings.IgnitionThreshold);
        }
    }

    [Serializable]
    public class BurnablePreset : IBurnablePreset
    {
        [field: SerializeField] public float IgnitionThreshold { get; private set; } // IgnitionProgress below this - means no fire
        [field: SerializeField] public float FireResistance { get; private set; } // BurnDurability drain multiplier
        [field: SerializeField] public float MaxBurnDurability { get; private set; } // Max HP
        [field: SerializeField] public float MaxBurnIntensity { get; private set; } // Influences fire population
        [field: SerializeField] public float MaxFirePopulationRange { get; private set; } // Fow far can we populate the fire at max strength
    }

    public interface IBurnablePreset
    {
        public float IgnitionThreshold { get; } // IgnitionProgress below this - means no fire
        public float FireResistance { get; } // BurnDurability drain multiplier
        public float MaxBurnDurability { get; } // HP
        public float MaxBurnIntensity { get; } // Fow much fire can be influenced at max strength
        public float MaxFirePopulationRange { get; } // Fow far can we populate the fire at max strength
    }
}
