namespace Flame_n_Blame_Challenge.Domain
{
    /// Impulse fires once, when the push is processed: Force is newton-seconds, so delta-v = Force / Mass.
    /// Sustained applies acceleration every tick for its Duration: Force is newtons.
    public enum PushKind
    {
        Impulse,
        Sustained,
    }
}
