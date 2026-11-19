using Flame_n_Blame_Challenge.Adapters;
using Flame_n_Blame_Challenge.UseCases;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge
{
    /// Composition root: the only scene object that knows concrete types exist. No game logic, and
    /// the only Update() in the scene (D-012, D-013).
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

            CurrentGameSession = new GamePlaySession(
                _gameUpdater, _inputs, _gameSettingsConfig, new ResourceSpawner(_resourcesConfig));

            CurrentGameSession.StartGamePlay();
        }

        private void Update()
        {
            _gameUpdater.UpdateLogic(Time.time, Time.deltaTime);
        }

        private void OnDestroy()
        {
            CurrentGameSession?.Dispose();
            (_inputs as IDisposable)?.Dispose();
        }
    }
}
