namespace Flame_n_Blame_Challenge.Domain
{
    /// Identifies who is pushing, which is what makes replace-per-type work: a spray re-fired every
    /// 0.1 s keeps ONE entry in the active list, not thirty. The order is load-bearing - serialized
    /// presets store Type as an int, and the scene's authored hydrant preset is index 0.
    public enum PushType
    {
        HydrantPush,
        ExtinguisherShove,
        PlayerJump,
    }
}
