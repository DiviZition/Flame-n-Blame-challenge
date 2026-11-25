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

        private SmokePuffPool _puffPool;

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

            _ownUpdater.RegisterUpdatable(_inputs as IUpdatable);
            var player = new Player(_inputs, _gameSettings.Physics, _gameSettings.Control, _pushPresets);

            var playerMotor = _resourceSpawner.SpawnPlayer();
            playerMotor.Construct(player);

            // Registration order IS the execution order contract (D-013): input, then the domain that
            // decides velocity, then the shell that consumes what the domain decided this tick.
            _ownUpdater.RegisterUpdatable(player);
            _ownUpdater.RegisterUpdatable(playerMotor);

            StartFire();

            _globalUpdater.RegisterUpdatable(_ownUpdater);
        }

        private void StartFire()
        {
            var fireSpread = new FireSpreadSystem();
            _puffPool = _resourceSpawner.SpawnPuffPool();

            if (_puffPool != null)
                _puffPool.Construct();

            var burnShells = UnityEngine.Object.FindObjectsByType<BurnableShell>();

            for (int i = 0; i < burnShells.Length; i++)
            {
                var burnable = new BurnableObject(burnShells[i].Preset, burnShells[i].transform.position);
                burnShells[i].Construct(burnable, _puffPool);

                fireSpread.Add(burnable);

                _ownUpdater.RegisterUpdatable(burnable);
                _ownUpdater.RegisterUpdatable(burnShells[i]);
            }

            _ownUpdater.RegisterUpdatable(fireSpread);
        }

        public void Dispose()
        {
            if (_gameStarted == false)
                return;

            _globalUpdater.RemoveUpdatable(_ownUpdater);
            _ownUpdater.Clear();

            _puffPool = null;
        }
    }
}
