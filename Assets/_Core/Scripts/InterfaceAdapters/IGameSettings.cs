namespace Flame_n_Blame_Challenge.Adapters
{
    /// The session's authored facade: the two config assets, handed to the domain through their own ports
    /// (D-037). A session rule that needs a number - a win score, a wave count, a timer - joins this port
    /// together with the system that reads it, never before: a field with no reader is dead surface (D-033).
    public interface IGameSettings
    {
        IPhysicsSettings Physics { get; }
        IControlSettings Control { get; }
    }
}
