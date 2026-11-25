using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Presets built from the requirements, not copied from the scene: a flat curve is a constant force.
    /// Impulse Force is newton-seconds, Sustained Force is newtons, and the **type of the DTO is the shape**
    /// — exactly what the Inspector authors with its type dropdown (D-028, D-031). A decaying envelope has no
    /// reader in the suite, so it is not authored here (D-033).
    public static class Presets
    {
        public static AnimationCurve Flat()
        {
            return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
        }

        public static PushPresetData Impulse(PushType type, float forceNewtonSeconds)
        {
            return new ImpulsePresetData(type, forceNewtonSeconds);
        }

        public static SustainedPresetData Sustained(PushType type, float forceNewtons, float duration)
        {
            return new SustainedPresetData(type, forceNewtons, Flat(), duration);
        }
    }
}
