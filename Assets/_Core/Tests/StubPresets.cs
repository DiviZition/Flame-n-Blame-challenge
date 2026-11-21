using Flame_n_Blame_Challenge.Adapters;
using Flame_n_Blame_Challenge.Domain;

namespace Flame_n_Blame_Challenge.Tests
{
    /// The presets the tests read, as plain data, set per test. Defaults are the round numbers the jump
    /// tests expect: Force 400 N over 1 s with a flat envelope, so with Mass 20 and Gravity 10 the launch
    /// is (400/20 - 10) * 1 = 10 m/s, hand-checkable. A test that wants another preset replaces the array,
    /// exactly as a designer authors it in the asset (D-029).
    public class StubPresets : IPushPresets
    {
        public PushPresetData[] Presets = new[] { Tests.Presets.Sustained(PushType.PlayerJump, 400f, 1f) };

        public PushPresetData PresetOf(PushType type)
        {
            for (int i = 0; i < Presets.Length; i++)
                if (Presets[i] != null && Presets[i].Type == type)
                    return Presets[i];

            return null;
        }
    }
}
