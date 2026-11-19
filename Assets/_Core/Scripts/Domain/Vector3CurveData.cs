using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// Authored envelope of a push, in the source's local space (right, up, forward), rotated into
    /// the world by the source's orientation. A missing curve reads as flat 1, so authoring Force
    /// alone is enough for a constant shove.
    [Serializable]
    public class Vector3CurveData
    {
        public AnimationCurve X;
        public AnimationCurve Y;
        public AnimationCurve Z;

        public Vector3CurveData() { }

        public Vector3CurveData(AnimationCurve x, AnimationCurve y, AnimationCurve z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3 Evaluate(float progress)
        {
            return new Vector3(EvaluateAxis(X, progress), EvaluateAxis(Y, progress), EvaluateAxis(Z, progress));
        }

        /// Flat (1) when the curve is not authored, so a Sustained push with no curve is a constant force.
        private static float EvaluateAxis(AnimationCurve curve, float progress)
        {
            if (curve == null || curve.length == 0)
                return 1f;

            return curve.Evaluate(progress);
        }

        public static Vector3CurveData Flat()
        {
            return new Vector3CurveData(FlatAxis(), FlatAxis(), FlatAxis());
        }

        /// Up only - the shape a jump wants, so an Impulse preset with this curve never shoves sideways.
        public static Vector3CurveData Up()
        {
            return new Vector3CurveData(ZeroAxis(), FlatAxis(), ZeroAxis());
        }

        private static AnimationCurve FlatAxis()
        {
            return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
        }

        private static AnimationCurve ZeroAxis()
        {
            return new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
        }
    }
}
