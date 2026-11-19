using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// Presets built from the requirements, not copied from the scene: flat curve = constant force,
    /// decay envelope = one puff. Impulse Force is newton-seconds, Sustained Force is newtons.
    public static class Presets
    {
        public static AnimationCurve Flat()
        {
            return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
        }

        public static AnimationCurve Zero()
        {
            return new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
        }

        public static PushPresetData ImpulseForward(PushType type, float forceNewtonSeconds)
        {
            return new PushPresetData
            {
                Type = type,
                Kind = PushKind.Impulse,
                Applies = PushApplication.Always,
                Force = forceNewtonSeconds,
                Curve = new Vector3CurveData(Zero(), Zero(), Flat()),
                Duration = 0.1f
            };
        }

        public static PushPresetData SustainedForward(PushType type, float forceNewtons, float duration)
        {
            return new PushPresetData
            {
                Type = type,
                Kind = PushKind.Sustained,
                Applies = PushApplication.Always,
                Force = forceNewtons,
                Curve = new Vector3CurveData(Zero(), Zero(), Flat()),
                Duration = duration
            };
        }

        public static PushPresetData GroundedOnlyForward(PushType type, float forceNewtons, float duration)
        {
            PushPresetData preset = SustainedForward(type, forceNewtons, duration);
            preset.Applies = PushApplication.GroundedOnly;
            return preset;
        }
    }
}
