using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Authored prefab references, the only thing a spawner is allowed to know about Unity objects
    /// (D-012). A shell that Unity instantiates, and the spawner hands the instance to the session.
    [CreateAssetMenu(fileName = "Resources", menuName = "Configs/Resources", order = 0)]
    public class ResourcesProvider : ScriptableObject
    {
        [field: SerializeField] public PlayerMotor PlayerMotor { get; private set; }
    }
}
