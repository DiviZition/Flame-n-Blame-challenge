using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flame_n_Blame_Challenge.Adapters
{
    [CreateAssetMenu(fileName = "Resources", menuName = "Configs/Resources", order = 0)]
    public class ResourcesProvider : ScriptableObject
    {
        [field: SerializeField] public PlayerMotor PlayerMotor { get; private set; }
    }

    public class ResourceSpawner : IResourceSpawner
    {
        private readonly ResourcesProvider _resourcesProvider;

        public ResourceSpawner(ResourcesProvider resourcesProvider)
        {
            _resourcesProvider = resourcesProvider;
        }

        public PlayerMotor SpawnPlayer() => Object.Instantiate(_resourcesProvider.PlayerMotor);
    }

    public interface IResourceSpawner
    {
        PlayerMotor SpawnPlayer();
        // add more only when you actually need them
    }
}
