using Flame_n_Blame_Challenge.Adapters;
using Flame_n_Blame_Challenge.Domain;
using System;

namespace Flame_n_Blame_Challenge.UseCases
{
    /// Owns the lifetime of everything that exists only during an active session, and does the
    /// registering - the domain never registers itself (ARCHITECTURE §2, §4).
    public class GamePlaySession : IDisposable
    {
        private readonly IGameSettings _gameSettings;
        private readonly IPushPresets _pushPresets;
        private readonly IResourceSpawner _resourceSpawner;
        private readonly IPlayerInput _inputs;
        private readonly Updater _globalUpdater;

        private Updater _ownUpdater;
        private bool _gameStarted;

        public GamePlaySession(Updater updater, IPlayerInput inputs, IGameSettings gameSettings,
            IPushPresets pushPresets, IResourceSpawner resourceSpawner)
        {
            _globalUpdater = updater;
            _inputs = inputs;
            _gameSettings = gameSettings;
            _pushPresets = pushPresets;
            _resourceSpawner = resourceSpawner;
        }

        public void StartGamePlay()
        {
            _ownUpdater = new Updater();
            _gameStarted = true;

            var player = new Player(_inputs, _gameSettings.Physics, _gameSettings.Control, _pushPresets);

            var playerMotor = _resourceSpawner.SpawnPlayer();
            playerMotor.Construct(player);

            // Registration order IS the execution order contract (D-013). `PlayerInputMain` is ticked by the
            // composition root ahead of this list, so the order stays input -> player -> motor -> burn.
            _ownUpdater.RegisterUpdatable(player);
            _ownUpdater.RegisterUpdatable(playerMotor);

            var burnSystem = new BurnSystem(_resourceSpawner);
            _ownUpdater.RegisterUpdatable(burnSystem);

            _globalUpdater.RegisterUpdatable(_ownUpdater);
        }

        public void Dispose()
        {
            if (_gameStarted == false)
                return;

            _globalUpdater.RemoveUpdatable(_ownUpdater);
            _ownUpdater.Clear();
        }
    }

    // TODO: better resource control over the shells and particles.
    public class BurnSystem : IUpdatable
    {
        private SmokePuffPool _puffPool;
        private Updater _updater;

        public BurnSystem(IResourceSpawner resourceSpawner)
        {
            var fireSpread = new FireSpreadSystem();
            _updater = new Updater();
            _puffPool = resourceSpawner.SpawnPuffPool();

            if (_puffPool != null)
                _puffPool.Construct();

            var burnShells = UnityEngine.Object.FindObjectsByType<BurnableShell>();

            for (int i = 0; i < burnShells.Length; i++)
            {
                var burnable = new BurnableObject(burnShells[i].Preset, burnShells[i].transform.position);
                burnShells[i].Construct(burnable, _puffPool);

                fireSpread.Add(burnable);

                _updater.RegisterUpdatable(burnable);
                _updater.RegisterUpdatable(burnShells[i]);
            }

            _updater.RegisterUpdatable(fireSpread);
        }

        public void UpdateLogic(float time, float deltaTime) => _updater.UpdateLogic(time, deltaTime);
    }
}
