namespace Flame_n_Blame_Challenge.Domain
{
    /// Identifies who is pushing, which is what makes replace-per-type work: a spray re-fired every
    /// 0.1 s keeps ONE entry in the active list, not thirty. The order is load-bearing — serialized
    /// presets store Type as an int, so the indices of the entries the asset authors must not move
    /// (G-021). Only sources that exist are listed: a value nothing fires and no preset is keyed by is
    /// authored surface with no reader (D-033).
    public enum PushType
    {
        HydrantPush,
        ExtinguisherShove,
        PlayerJump,
        ExtinguisherKick,
    }
}
