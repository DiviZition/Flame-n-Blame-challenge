using System;
using Flame_n_Blame_Challenge.Adapters;
using Flame_n_Blame_Challenge.Domain;

namespace Flame_n_Blame_Challenge.UseCases
{
    /// Owns the lifetime of everything that exists only during an active session, and does the
    /// registering - the domain never registers itself (ARCHITECTURE §2, §4).
    public class GamePlaySession : IDisposable
    {
        private readonly GameUpdater _updater;
        private readonly IGameSettings _gameSettings;
        private readonly IPushPresets _pushPresets;
        private readonly IResourceSpawner _resourceSpawner;
        private readonly IPlayerInput _inputs;

        private Player _player;
        private PlayerMotor _playerMotor;

        public GamePlaySession(GameUpdater updater, IPlayerInput inputs, IGameSettings gameSettings,
            IPushPresets pushPresets, IResourceSpawner resourceSpawner)
        {
            _updater = updater;
            _inputs = inputs;
            _gameSettings = gameSettings;
            _pushPresets = pushPresets;
            _resourceSpawner = resourceSpawner;

            _updater.RegisterUpdatable(inputs as IUpdatable);
        }

        public void StartGamePlay()
        {
            _player = new Player(_inputs, _gameSettings, _pushPresets);

            _playerMotor = _resourceSpawner.SpawnPlayer();
            _playerMotor.Construct(_player);

            // Registration order IS the execution order contract (D-013): input, then the domain that
            // decides velocity, then the shell that consumes what the domain decided this tick.
            _updater.RegisterUpdatable(_player);
            _updater.RegisterUpdatable(_playerMotor);
        }

        public void Dispose()
        {
            if (_player == null)
                return;

            _updater.RemoveUpdatable(_player);
            _updater.RemoveUpdatable(_playerMotor);

            _player = null;
            _playerMotor = null;
        }
    }
}
