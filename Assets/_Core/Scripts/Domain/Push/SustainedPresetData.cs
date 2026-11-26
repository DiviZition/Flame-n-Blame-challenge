using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// A force that runs for `Duration`: Force * Curve accumulates over the duration, which is what a hydrant
    /// burst, a spray shove and a timed jump are. The envelope is **one scalar** curve over progress
    /// (0..1 across Duration), never per-axis (G-018), and `Duration` ends the **adding**, never the
    /// velocity — momentum survives the timer and is absorbed by contact (G-012).
    [Serializable]
    public class SustainedPresetData : PushPresetData
    {
        /// N. Integrated over Duration as Force * Curve / Mass * dt. A launch that lifts needs this above
        /// Mass·Gravity (80 kg x 19.62 = 1569 N), because the ground absorbs what it can carry (D-030).
        [Tooltip("N. Force above Mass*Gravity (=1569 N here) is what can lift the player; below weight the "
            + "ground absorbs it."), SerializeField]
        private float _force;

        /// Evaluated at progress (0..1 across Duration), so its keys are the authored shape of the push.
        [Tooltip("Envelope over progress 0..1 across Duration: 1 = full force at that moment, 0 = none. "
            + "One scalar, never per axis."), SerializeField]
        private AnimationCurve _curve;

        /// How long the FORCE runs.
        [Tooltip("Seconds the FORCE runs. It ends the adding, never the velocity: momentum survives the "
            + "timer and dies on contact."), SerializeField]
        private float _duration;

        public SustainedPresetData() { }

        public SustainedPresetData(PushType type, float forceNewtons, AnimationCurve curve, float duration)
            : base(type)
        {
            _force = forceNewtons;
            _curve = curve;
            _duration = duration;
        }

        public override float Force => _force;
        public override bool IsImpulse => false;

        public float Duration => _duration;

        /// A preset authored with no curve at all (an empty Inspector slot) is a constant force, not a crash:
        /// the envelope reads as flat 1.
        public float EnvelopeAt(float progress)
        {
            if (_curve == null || _curve.length == 0)
                return 1f;

            return _curve.Evaluate(progress);
        }
    }
}
