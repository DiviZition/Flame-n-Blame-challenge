using System;
using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.Demo
{
    /// <summary>
    /// Flies the scene view camera between demo stations while the editor is in edit mode.
    /// Nothing here runs in play mode or in the game view - the tween is driven by
    /// <see cref="EditorApplication.update"/> and writes straight into the scene view each frame.
    /// </summary>
    internal static class PPP_DemoCameraTween
    {
        /// <summary>How long a short hop takes.</summary>
        private const float MIN_DURATION = 0.45f;

        /// <summary>Ceiling for the longest trip down the strip, so the tour never feels slow.</summary>
        private const float MAX_DURATION = 1.25f;

        /// <summary>World units of travel that add one second, before clamping.</summary>
        private const float UNITS_PER_SECOND = 55f;

        private static SceneView _view;
        private static Vector3 _fromPivot;
        private static Vector3 _toPivot;
        private static Quaternion _fromRotation;
        private static Quaternion _toRotation;
        private static float _fromSize;
        private static float _toSize;
        private static double _startTime;
        private static float _duration;
        private static Action _onComplete;
        private static bool _running;

        internal static bool IsRunning => _running;

        /// <summary>
        /// Starts a flight to the given scene view pose. Calling this mid-flight re-aims from wherever
        /// the camera currently is, so clicking through stations quickly never snaps.
        /// </summary>
        /// <param name="onComplete">Raised once the camera has settled, or immediately if it cannot tween.</param>
        internal static void FlyTo(Vector3 pivot, Quaternion rotation, float size, Action onComplete = null)
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null)
                view = EditorWindow.GetWindow<SceneView>();

            if (view == null)
            {
                Debug.LogWarning("[Pivot++ Demo] No Scene view is open to move.");
                onComplete?.Invoke();
                return;
            }

            // A previous flight is abandoned rather than finished - its callback would apply state
            // that belongs to a station the user has already moved past.
            Stop();

            _view = view;
            _fromPivot = view.pivot;
            _fromRotation = view.rotation;
            _fromSize = Mathf.Max(view.size, 0.01f);
            _toPivot = pivot;
            _toRotation = rotation;
            _toSize = Mathf.Max(size, 0.01f);
            _onComplete = onComplete;

            _duration = Mathf.Clamp(
                Vector3.Distance(_fromPivot, _toPivot) / UNITS_PER_SECOND,
                MIN_DURATION,
                MAX_DURATION);

            _startTime = EditorApplication.timeSinceStartup;
            _running = true;

            EditorApplication.update -= Step;
            EditorApplication.update += Step;
        }

        /// <summary>Abandons the current flight without applying its completion callback.</summary>
        internal static void Stop()
        {
            EditorApplication.update -= Step;
            _running = false;
            _onComplete = null;
            _view = null;
        }

        private static void Step()
        {
            if (_view == null)
            {
                Finish();
                return;
            }

            float t = (float)((EditorApplication.timeSinceStartup - _startTime) / _duration);

            if (t >= 1f)
            {
                Apply(1f);
                Finish();
                return;
            }

            Apply(Ease(Mathf.Clamp01(t)));
        }

        private static void Apply(float t)
        {
            _view.pivot = Vector3.Lerp(_fromPivot, _toPivot, t);
            _view.rotation = Quaternion.Slerp(_fromRotation, _toRotation, t);

            // Zoom is interpolated geometrically. A straight lerp between 1.6 and 100 spends almost
            // the whole flight visually parked at the far end.
            _view.size = Mathf.Exp(Mathf.Lerp(Mathf.Log(_fromSize), Mathf.Log(_toSize), t));

            _view.Repaint();
        }

        private static void Finish()
        {
            EditorApplication.update -= Step;
            _running = false;
            _view = null;

            Action callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        private static float Ease(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}
