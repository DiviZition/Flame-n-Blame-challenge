using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    [CreateAssetMenu(fileName = "Game Settings", menuName = "Configs/Game Settings", order = 99)]
    public class GameSettings : ScriptableObject, IGameSettings
    {
        [field: SerializeField] public float PlayerSpeedBase { get; private set; }
        [field: SerializeField] public float PlayerSpeedSprint { get; private set; }
        [field: SerializeField] public float PlayerSpeedInAir { get; private set; }
        [field: SerializeField] public float PlayerJumpForce { get; private set; }

        [field: SerializeField] public float LookSensitivity { get; private set; }
        [field: SerializeField] public Vector2 LookPitchBounds { get; private set; }

    }

    public interface IGameSettings
    {
        public float PlayerSpeedBase { get; }
        public float PlayerSpeedSprint { get; }
        public float PlayerSpeedInAir { get; }
        public float PlayerJumpForce { get; }

        public float LookSensitivity { get; }
        public Vector2 LookPitchBounds { get; }
    }
}
