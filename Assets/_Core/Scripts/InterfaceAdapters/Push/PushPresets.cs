using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// All push presets of the game in ONE authored asset: one list, keyed by `PushType`, each entry a
    /// concrete DTO — `ImpulsePresetData` or `SustainedPresetData` — chosen by the Inspector's type dropdown.
    /// `[SerializeReference]` is what makes that editable: Unity draws the fields of the chosen type and
    /// creates the instance itself, so an entry is never a null slot and the numbers the author types are
    /// the numbers the game reads (G-021: a `[Serializable]` class array gives null elements that no drawer
    /// can fill, so Inspector edits were lost).
    ///
    /// Presets live here and not on the sources so the numbers of one push stay in one place: a hydrant, a
    /// jump and a spray are entries of one file, and a designer tunes without opening the scene (D-029).
    /// Lookup is linear over a handful of entries, and runs when a source **fires**, never per tick, so the
    /// WebGL budget is untouched.
    [CreateAssetMenu(fileName = "Push Presets", menuName = "Configs/Push Presets", order = 98)]
    public class PushPresets : ScriptableObject, IPushPresets
    {
        [SerializeField, SerializeReference] private PushPresetData[] _presets;

        public PushPresetData PresetOf(PushType type)
        {
            if (_presets == null)
                return null;

            for (int i = 0; i < _presets.Length; i++)
                if (_presets[i] != null && _presets[i].Type == type)
                    return _presets[i];

            return null;
        }
    }
}
