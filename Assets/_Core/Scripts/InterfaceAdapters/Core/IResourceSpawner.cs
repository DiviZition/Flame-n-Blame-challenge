namespace Flame_n_Blame_Challenge.Adapters
{
    /// The port the session uses to get a live shell instance, without knowing that Unity
    /// instantiates prefabs (ARCHITECTURE §6: interfaces for external needs).
    public interface IResourceSpawner
    {
        PlayerShell SpawnPlayer();
        SmokePuffPool SpawnPuffPool();
        // add more only when you actually need them
    }
}
