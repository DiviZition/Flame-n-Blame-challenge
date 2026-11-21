namespace Flame_n_Blame_Challenge.Domain
{
    /// What every push preset shares: the key that makes replace-per-type work — a source re-fired while
    /// its push is live replaces the live entry, so it never stacks — and the two facts the domain reads
    /// while a push ticks: how much force, and whether it lands in one tick or runs over a Duration.
    /// Presets are authored in one ScriptableObject and read through IPushPresets (D-012).
    public interface IPushPreset
    {
        PushType Type { get; }

        /// N·s for an Impulse, N for a Sustained. Divided by Mass in the domain; for a Sustained the launch
        /// is the integral of Force * Curve, not Force.
        float Force { get; }

        /// The shape, as a fact of the type, never as an authored flag (D-031).
        bool IsImpulse { get; }
    }
}
