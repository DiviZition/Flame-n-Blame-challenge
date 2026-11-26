using UnityEngine;
using Type = Flame_n_Blame_Challenge.SpawnPoints.SpawnPointType;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// The Unity-side implementation of IResourceSpawner. It instantiates; it owns no rules, and it
    /// never registers anything - the session does that (D-012).
    public class ResourceSpawner : IResourceSpawner
    {
        private readonly ResourcesProvider _resourcesProvider;
        private readonly SpawnPoints _spawnPoints;

        public ResourceSpawner(ResourcesProvider resourcesProvider, SpawnPoints spawnPoints)
        {
            _resourcesProvider = resourcesProvider;
            _spawnPoints = spawnPoints;
        }

        public PlayerMotor SpawnPlayer()
        {
            _spawnPoints.GetPoint(Type.Player, out Vector3 pos, out Quaternion rot);
            return Object.Instantiate(_resourcesProvider.PlayerMotor, pos, rot);
        } 
        public SmokePuffPool SpawnPuffPool() => Object.Instantiate(_resourcesProvider.PuffPull);
    }
}
