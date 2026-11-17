using Flame_n_Blame_Challenge.Adapters;
using Flame_n_Blame_Challenge.Domain;
using Flame_n_Blame_Challenge.UseCases;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge
{
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private GameSettings _gameSettingsConfig;
        [SerializeField] private ResourcesProvider _resourcesConfig;

        private GameUpdater _gameUpdater;
        private IPlayerInput _inputs;

        public GamePlaySession CurrentGameSession { get; private set; }

        private void Start()
        {
            _inputs = new PlayerInputMain();
            _gameUpdater = new GameUpdater();

            CurrentGameSession = new GamePlaySession(_gameUpdater, _inputs, _gameSettingsConfig, new ResourceSpawner(_resourcesConfig));

            CurrentGameSession.StartGamePlay();
        }

        private void Update()
        {
            _gameUpdater.Update(Time.time, Time.deltaTime);
        }

        private void OnDestroy()
        {
            CurrentGameSession?.Dispose();
            (_inputs as IDisposable)?.Dispose();
        }
    }
}

namespace Flame_n_Blame_Challenge.UseCases
{
    public class GamePlaySession : IDisposable
    {
        private GameUpdater _updater;
        private readonly IGameSettings _gameSettings;
        private readonly IResourceSpawner _resourceSpawner;
        private readonly IPlayerInput _inputs;

        private Player _player;

        public GamePlaySession(GameUpdater updater, IPlayerInput inputs, IGameSettings gameSettings, IResourceSpawner resourceSpawner)
        {
            _updater = updater;
            _gameSettings = gameSettings;
            _resourceSpawner = resourceSpawner;

            _inputs = inputs;
            _updater.RegisterUpdatable(_inputs as IUpdatable);
        }

        public void StartGamePlay()
        {
            _player = new Player(_inputs, _gameSettings);
            var playerMotor = _resourceSpawner.SpawnPlayer();
            playerMotor.Construct(_player);

            // Spawn the player;
            // Provide input;
            // Spawn the level;
            // Initialize level systems;
            // Initialize Objectieves?;

            _updater.RegisterUpdatable(_player);
        }

        public void Dispose()
        {
            if (_player == null)
                return;

            _updater.RemoveUpdatable(_player);
            //Player cleanup?
        }
    }
}