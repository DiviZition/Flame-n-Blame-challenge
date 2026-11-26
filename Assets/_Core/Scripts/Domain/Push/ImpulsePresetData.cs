using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// A one-shot shove: `Force` is newton-seconds, it lands in one tick, and there is nothing to time — so
    /// this DTO has **no** `Duration` and **no** curve, not even as fields to author (D-028's rule, restored
    /// by D-031: the DTO's type is the shape, so a field a shape cannot read does not exist).
    [Serializable]
    public class ImpulsePresetData : PushPresetData
    {
        /// N·s. Divided by Mass on the tick it fires, which is the velocity the shove leaves behind.
        [Tooltip("N·s. Lands in one tick: Force / Mass is the velocity the shove leaves behind."), SerializeField]
        private float _force;

        public ImpulsePresetData() { }

        public ImpulsePresetData(PushType type, float forceNewtonSeconds) : base(type)
        {
            _force = forceNewtonSeconds;
        }

        public override float Force => _force;
        public override bool IsImpulse => true;
    }
}
