using Flame_n_Blame_Challenge.Domain;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Port for authored push presets. Every preset is keyed by its PushType, so a source asks for the push
    /// it owns and nothing about a launch's shape is hard-coded in a source (D-012, D-029). Split out of
    /// PushPresets.cs (one type per file, STYLE.md).
    public interface IPushPresets
    {
        PushPresetData PresetOf(PushType type);
    }
}
