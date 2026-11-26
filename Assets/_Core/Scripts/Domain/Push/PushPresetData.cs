using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Domain
{
    /// The base of one authored push, and the key of the single preset list. `Type` is the replace-per-type
    /// key a source stores, and the ONLY authored identity: lookup is `PresetOf(PushType)`, so a label next to
    /// the key is a second name for one entry, with nothing reading it (D-032). The **force shape is the type
    /// of the DTO** (`ImpulsePresetData` / `SustainedPresetData`), so a field the shape cannot read does not
    /// exist (D-028's rule, restored by D-031): an Inspector for an Impulse has no Duration and no curve,
    /// because there are no such fields to author, and nothing to hide.
    ///
    /// The list is `[SerializeField, SerializeReference]`, so the Inspector shows a concrete-type dropdown per
    /// entry and Unity itself instantiates the instance. A `[Serializable]` class array cannot do that:
    /// adding an element gives a null slot, and a drawer on the DTO cannot fill it, so the Inspector edits
    /// did not apply (G-021).
    [Serializable]
    public abstract class PushPresetData : IPushPreset
    {
        [Tooltip("Which source this entry belongs to. A source re-fired while its push is live replaces "
            + "the live entry, so one source never stacks."), SerializeField]
        private PushType _type;

        // Public parameterless constructor: Unity's SerializeReference instantiation needs it, so the
        // Inspector's type dropdown can create an entry (G-021).
        public PushPresetData() { }

        public PushPresetData(PushType type)
        {
            _type = type;
        }

        public PushType Type => _type;

        /// N·s for an Impulse, N for a Sustained. Divided by Mass in the domain; for a Sustained the launch
        /// is the integral of Force * Curve, not Force.
        public abstract float Force { get; }

        /// The shape, as a fact of the type, never as an authored flag.
        public abstract bool IsImpulse { get; }
    }
}
