using Flame_n_Blame_Challenge.Adapters;
using Flame_n_Blame_Challenge.UseCases;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Flame_n_Blame_Challenge
{
    /// Composition root: the only scene object that knows concrete types exist. No game logic, and
    /// the only Update() in the scene (D-012, D-013). It lives in the boot scene and survives the load
    /// of the gameplay scene, so the session is built after the world is there (D-052).
    public class Bootstrapper : MonoBehaviour
    {
        [Tooltip("Gameplay scene loaded on launch. The session is built when it reports ready."), SerializeField]
        private string _gameplaySceneName;

        [SerializeField] private GameSettings _gameSettingsConfig;
        [SerializeField] private ResourcesProvider _resourcesConfig;
        [SerializeField] private PushPresets _pushPresetsConfig;

        private Updater _gameUpdater;
        private IPlayerInput _inputs;
        private bool _launchRequested;

        public GamePlaySession CurrentGameSession { get; private set; }

        private void Awake()
        {
            // The root outlives the session scene: a single-mode load destroys the boot scene, and the
            // composition must be alive to build the session and to load the scene again later.
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            _inputs = new PlayerInputMain();
            _gameUpdater = new Updater();

            LaunchGameplay();
        }

        /// The one load door: a restart in Phase 2 is dispose + load again, and the session is rebuilt
        /// on the load, so no restart state is kept here (D-052).
        private void LaunchGameplay()
        {
            CurrentGameSession?.Dispose();
            CurrentGameSession = null;

            _launchRequested = true;
            SceneManager.LoadScene(_gameplaySceneName, LoadSceneMode.Single);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // The boot scene fires this event too, and any load nobody requested must not build a session.
            if (_launchRequested == false || scene.name != _gameplaySceneName)
                return;

            _launchRequested = false;

            SpawnPoints[] spawnPoints = UnityEngine.Object.FindObjectsByType<SpawnPoints>();
            if (spawnPoints.Length == 0)
                return;

            CurrentGameSession = new GamePlaySession(
                _gameUpdater,
                _inputs,
                _gameSettingsConfig,
                _pushPresetsConfig,
                new ResourceSpawner(_resourcesConfig, spawnPoints[0]));

            CurrentGameSession.StartGamePlay();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
