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
    /// It is also the pause gate: the tick runs only while the flow says Playing (D-053).
    public class Bootstrapper : MonoBehaviour
    {
        [Tooltip("Gameplay scene loaded on launch. The session is built when it reports ready."), SerializeField]
        private string _gameplaySceneName;

        [SerializeField] private GameSettings _gameSettingsConfig;
        [SerializeField] private ResourcesProvider _resourcesConfig;
        [SerializeField] private PushPresets _pushPresetsConfig;

        [Tooltip("The launch window, in the boot scene. Constructed with the flow at Start."), SerializeField]
        private GameplayUiShell _ui;

        private Updater _gameUpdater;
        private IPlayerInput _inputs;
        private IUpdatable _inputsTick;
        private GameFlow _flow;
        private bool _launchRequested;

        private GamePlaySession _session;

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
            _inputsTick = _inputs as IUpdatable;

            _flow = new GameFlow();
            _flow.StateChanged += OnFlowState;
            _flow.RestartRequested += LaunchGameplay;

            _ui.Construct(_flow);

            LaunchGameplay();
        }

        /// The one load door: a restart is dispose + load again, and the session is rebuilt
        /// on the load, so no restart state is kept here (D-052).
        private void LaunchGameplay()
        {
            _session?.Dispose();
            _session = null;

            // Dispose stages the session's removal, and the stage is flushed only by the tick that owns it.
            // The pause gate can skip that tick, so the flush is done here: a destroyed shell must never be
            // left in the list for the next Playing (G-044).
            _gameUpdater.Flush();

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

            _session = new GamePlaySession(
                _gameUpdater,
                _inputs,
                _gameSettingsConfig,
                _pushPresetsConfig,
                new ResourceSpawner(_resourcesConfig, spawnPoints[0]));

            _session.StartGamePlay();
        }

        private void Update()
        {
            // Input is ticked every frame, with no state check: the ESC press fact has to stay fresh while
            // the world is frozen (D-053). The session graph is what the pause gate stops.
            _inputsTick.UpdateLogic(Time.time, Time.deltaTime);

            DriveFlowFromPause();

            if (_flow.State == GameFlowState.Playing)
                _gameUpdater.UpdateLogic(Time.time, Time.deltaTime);
        }

        private void DriveFlowFromPause()
        {
            if (_inputs.PausePressed == false)
                return;

            if (_flow.State == GameFlowState.Playing)
                _flow.RequestPause();
            else if (_flow.State == GameFlowState.Paused)
                _flow.RequestResume();
        }

        private void OnFlowState(GameFlowState state)
        {
            bool playing = state == GameFlowState.Playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
        }

        private void OnDestroy()
        {
            _session?.Dispose();
            (_inputs as IDisposable)?.Dispose();

            if (_flow != null)
                _flow.StateChanged -= OnFlowState;

            if (_ui != null)
                _ui.Dispose();
        }
    }
}
