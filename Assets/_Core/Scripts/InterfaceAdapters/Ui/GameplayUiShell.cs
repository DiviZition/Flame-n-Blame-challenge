using Flame_n_Blame_Challenge.UseCases;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Thin shell for the launch window: it shows what the flow decided, tweens the window open and closed,
    /// and forwards clicks inward. It decides nothing (D-012) and has no Update() (the perf budget lives in
    /// TODO.md). Button feel is a separate shell on each button (`UiButtonFeel`) — uGUI gives the pointer
    /// fact to the Button itself, not to a handler further up (G-042).
    public class GameplayUiShell : MonoBehaviour
    {
        [Tooltip("The paper window. Shown in Briefing and Paused, hidden in Playing."), SerializeField]
        private GameObject _window;

        [Tooltip("The window's alpha channel: the open and close tweens write it."), SerializeField]
        private CanvasGroup _panel;

        [Tooltip("The task line the window shows."), SerializeField]
        private TextMeshProUGUI _windowText;

        [Tooltip("Starts the game. Shown in Briefing."), SerializeField]
        private Button _startButton;

        [Tooltip("Restarts the game. Shown in Paused only."), SerializeField]
        private Button _restartButton;

        [Tooltip("The task line written on the window: what the player is asked to do."), SerializeField]
        private string _taskText;

        [Tooltip("Seconds the window takes to open and to close."), SerializeField]
        private float _openDuration = 0.22f;

        [Tooltip("How far the window travels as it opens, in pixels."), SerializeField]
        private float _openOffset = 34f;

        private GameFlow _flow;
        private Sequence _open;
        private Sequence _close;

        private Vector3 _restPosition;
        private Vector3 _closedPosition;

        private void Awake()
        {
            _restPosition = _panel.transform.localPosition;
            _closedPosition = _restPosition + Vector3.down * _openOffset;
            _windowText.text = _taskText;
        }

        /// Composition door, the house pattern (`PlayerMotor.Construct(player)`): the shell takes the flow,
        /// subscribes to its state, and wires both buttons to the flow's requests.
        public void Construct(GameFlow flow)
        {
            _flow = flow;

            _startButton.onClick.AddListener(flow.RequestStart);
            _restartButton.onClick.AddListener(flow.RequestRestart);

            flow.StateChanged += OnStateChanged;

            // The flow is born in Briefing, so the window opens once here and never re-plays the state.
            Show(flow.State);
        }

        public void Dispose()
        {
            UnsubscribeFromFlow();

            _open.Stop();
            _close.Stop();
        }

        private void OnDisable()
        {
            UnsubscribeFromFlow();
        }

        private void UnsubscribeFromFlow()
        {
            if (_flow == null)
                return;

            _flow.StateChanged -= OnStateChanged;
            _flow = null;
        }

        private void OnStateChanged(GameFlowState state)
        {
            if (state == GameFlowState.Playing)
                Hide();
            else
                Show(state);
        }

        private void Show(GameFlowState state)
        {
            _close.Stop();
            _window.SetActive(true);

            _open = Sequence.Create()
                .Group(Tween.Alpha(_panel, 0f, 1f, _openDuration, Ease.OutQuad))
                .Group(Tween.Scale(_panel.transform, 0.92f, 1f, _openDuration, Ease.OutQuad))
                .Group(Tween.LocalPosition(_panel.transform, _closedPosition, _restPosition, _openDuration, Ease.OutQuad));

            _startButton.gameObject.SetActive(state == GameFlowState.Briefing);
            _restartButton.gameObject.SetActive(state == GameFlowState.Paused);
        }

        private void Hide()
        {
            _open.Stop();

            _close = Sequence.Create()
                .Group(Tween.Alpha(_panel, 1f, 0f, _openDuration, Ease.InQuad))
                .Group(Tween.Scale(_panel.transform, 1f, 0.92f, _openDuration, Ease.InQuad))
                .Group(Tween.LocalPosition(_panel.transform, _restPosition, _closedPosition, _openDuration, Ease.InQuad))
                .ChainCallback(() => _window.SetActive(false));
        }
    }
}
