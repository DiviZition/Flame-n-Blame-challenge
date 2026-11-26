using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class PlayerShell : MonoBehaviour, IUpdatable
    {
        [field: SerializeField] public PlayerMotor Motor { get; private set; }
        [field: SerializeField] public PlayerItemsHandlerView ItemsHandler { get; private set; }

        private Player _player;

        public void Construct(Player player)
        {
            _player = player;
            Motor.Construct(_player);
            ItemsHandler.Construct(_player);
        }

        public void UpdateLogic(float time, float deltaTime)
        {
            Motor.UpdateLogic(time, deltaTime);
            ItemsHandler.UpdateLogic(time, deltaTime);
        }
    }
}
