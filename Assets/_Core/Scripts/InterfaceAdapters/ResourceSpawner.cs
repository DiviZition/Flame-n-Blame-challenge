using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// The Unity-side implementation of IResourceSpawner. It instantiates; it owns no rules, and it
    /// never registers anything - the session does that (D-012).
    public class ResourceSpawner : IResourceSpawner
    {
        private readonly ResourcesProvider _resourcesProvider;

        public ResourceSpawner(ResourcesProvider resourcesProvider)
        {
            _resourcesProvider = resourcesProvider;
        }

        public PlayerMotor SpawnPlayer() => Object.Instantiate(_resourcesProvider.PlayerMotor);
    }
}
