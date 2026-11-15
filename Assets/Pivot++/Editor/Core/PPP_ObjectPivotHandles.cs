using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Draws the per-object extra pivots: transform handles in Default mode, and
    /// move/rotate/delete handles in Edit mode. Owns the drag state of every
    /// object pivot plus the latch that tracks which handle a drag started on.
    /// </summary>
    internal static class PPP_ObjectPivotHandles
    {
        private static readonly List<PPP_PivotDragState> _states = new List<PPP_PivotDragState>();
        private static Transform _stateTarget;
        private static int _activeHandleIndex = -1;
        private static bool _sharedHandleDragging;

        // Pivot buttons (delete, create parent): radius and spacing as fractions
        // of the handle size at the button.
        private const float PIVOT_BUTTON_SIZE = 0.14f;
        private const float PIVOT_BUTTON_SPACING = 0.36f;

        // Handle caps can't take extra arguments, so the create-parent cap reads
        // the state of the button being drawn from here.
        private static bool _createParentAvailable;
        private static string _createParentTooltip;
        private static GUIStyle _tooltipStyle;

        // Scene view tooltips don't wrap, so the lines are broken by hand.
        private const string CREATE_PARENT_TOOLTIP =
            "Create a parent at this pivot.\n" +
            "Once selected, the parent's handle follows Pivot++'s shared pivot, so it may show up elsewhere.\n" +
            "The yellow dot marks where the parent actually is.";

        /// <summary>True while a drag that started on an object pivot handle is active.</summary>
        internal static bool IsObjectPivotDragActive => _activeHandleIndex >= 0;

        /// <summary>
        /// Per-frame bookkeeping that must run before any handle is drawn: clear
        /// the shared-drag flag once the mouse is released, and reset all pivot
        /// state when the active selection changes.
        /// </summary>
        internal static void SyncSelection(Transform activeTransform)
        {
            if (GUIUtility.hotControl == 0)
                _sharedHandleDragging = false;

            if (activeTransform == _stateTarget)
                return;

            _states.Clear();
            _activeHandleIndex = -1;
            _sharedHandleDragging = false;
            _stateTarget = activeTransform;
        }

        /// <summary>
        /// Called when a drag started on the shared pivot handle, so the object
        /// pivots know to hide themselves for the duration of that drag.
        /// </summary>
        internal static void NotifySharedHandleDragStarted()
        {
            _sharedHandleDragging = true;
        }

        /// <summary>Drops the active-handle latch (used after undo/redo).</summary>
        internal static void CancelActiveHandle()
        {
            _activeHandleIndex = -1;
        }

        internal static void DrawDefaultModeHandles(Transform activeTransform, Pose[] offsets)
        {
            EnsureStateCapacity(offsets.Length);

            if (GUIUtility.hotControl == 0)
                _activeHandleIndex = -1;

            if (_activeHandleIndex >= offsets.Length)
                _activeHandleIndex = -1;

            // A drag is active that isn't one of these object pivot handles - e.g.
            // the shared pivot handle, or scene view navigation (Alt+drag to orbit).
            // In that case every object pivot hides and only the shared pivot stays.
            bool otherDragActive = _sharedHandleDragging ||
                                   (GUIUtility.hotControl != 0 && _activeHandleIndex < 0);

            for (int i = 0; i < offsets.Length; i++)
            {
                PPP_PivotDragState state = _states[i];
                Pose pose = offsets[i];
                Quaternion offsetRotation = PPP_PivotMath.GetSafeRotation(pose.rotation);

                bool hideHandle = otherDragActive ||
                                  (_activeHandleIndex >= 0 && i != _activeHandleIndex);

                Vector3 liveWorldPos = activeTransform.TransformPoint(pose.position);
                Vector3 pivotPosition = state.pivotFrozen ? state.frozenPivotPosition : liveWorldPos;
                if (hideHandle)
                    pivotPosition = PPP_PivotMath.GetOffscreenHandlePosition();

                int hotControlBefore = GUIUtility.hotControl;
                switch (Tools.current)
                {
                    case Tool.Move:
                        DrawMoveHandle(activeTransform, pivotPosition, state, offsetRotation);
                        break;
                    case Tool.Rotate:
                        DrawRotateHandle(activeTransform, pivotPosition, state, offsetRotation);
                        break;
                    case Tool.Scale:
                        DrawScaleHandle(activeTransform, pivotPosition);
                        break;
                    case Tool.Transform:
                        DrawTransformHandle(activeTransform, pivotPosition, state, offsetRotation);
                        break;
                }

                // Only latch this handle as the active one if the drag actually
                // started on it (control went 0 -> non-zero during its draw). A
                // plain "hotControl != 0" check would misfire on any other drag,
                // such as an Alt+drag camera orbit, latching pivot 0 by mistake.
                if (!otherDragActive && _activeHandleIndex < 0 &&
                    hotControlBefore == 0 && GUIUtility.hotControl != 0)
                    _activeHandleIndex = i;
            }
        }

        internal static void DrawEditModeHandles(Transform activeTransform, Pose[] offsets)
        {
            GameObject targetObject = activeTransform.gameObject;

            EnsureStateCapacity(offsets.Length);

            if (GUIUtility.hotControl == 0)
                _activeHandleIndex = -1;

            if (_activeHandleIndex >= offsets.Length)
                _activeHandleIndex = -1;

            for (int i = 0; i < offsets.Length; i++)
            {
                PPP_PivotDragState state = _states[i];
                Pose pose = offsets[i];
                Quaternion offsetRotation = PPP_PivotMath.GetSafeRotation(pose.rotation);

                state.pivotFrozen = false;

                bool hideHandle = _activeHandleIndex >= 0 && i != _activeHandleIndex;

                Vector3 pivotPosition = activeTransform.TransformPoint(pose.position);
                if (hideHandle)
                    pivotPosition = PPP_PivotMath.GetOffscreenHandlePosition();

                int hotControlBefore = GUIUtility.hotControl;

                DrawOffsetMoveHandle(targetObject, i, activeTransform, pivotPosition, state, offsetRotation);
                DrawOffsetRotateHandle(targetObject, i, activeTransform, pivotPosition, state, offsetRotation);

                if (DrawPivotDeleteButton(targetObject, i, pivotPosition, hideHandle))
                    break;

                if (DrawCreateParentButton(activeTransform, i, pivotPosition, hideHandle))
                    break;

                // Latch only when the drag started on this handle, so an Alt+drag
                // camera orbit doesn't get mistaken for grabbing pivot 0.
                if (_activeHandleIndex < 0 &&
                    hotControlBefore == 0 && GUIUtility.hotControl != 0)
                    _activeHandleIndex = i;
            }
        }

        private static Quaternion GetHandleRotation(PPP_PivotDragState state, Transform t, Quaternion offsetRotation)
        {
            return Tools.pivotRotation == PivotRotation.Local
                ? PPP_PivotMath.GetSafeRotation(t.rotation * offsetRotation)
                : PPP_PivotMath.GetSafeRotation(state.globalRotationHandleState);
        }

        private static void DrawMoveHandle(Transform t, Vector3 pivotPosition, PPP_PivotDragState state, Quaternion offsetRotation)
        {
            Quaternion handleRotation = GetHandleRotation(state, t, offsetRotation);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(pivotPosition, handleRotation);
            if (EditorGUI.EndChangeCheck())
                PPP_TransformOps.Move(t, newPosition - pivotPosition, "Move via RePivot handle");
        }

        private static void DrawRotateHandle(Transform t, Vector3 pivotPosition, PPP_PivotDragState state, Quaternion offsetRotation)
        {
            Quaternion handleRotation = GetHandleRotation(state, t, offsetRotation);

            EditorGUI.BeginChangeCheck();
            Quaternion newHandleRotation = Handles.RotationHandle(handleRotation, pivotPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Quaternion delta = newHandleRotation * Quaternion.Inverse(handleRotation);
                PPP_TransformOps.RotateAround(t, pivotPosition, delta, "Rotate via RePivot handle");

                if (Tools.pivotRotation != PivotRotation.Local)
                    state.globalRotationHandleState = newHandleRotation;
            }

            state.UpdateAfterFreezableHandle(pivotPosition, allowFreeze: true);
        }

        private static void DrawScaleHandle(Transform t, Vector3 pivotPosition)
        {
            float handleSize = HandleUtility.GetHandleSize(pivotPosition);
            bool scaleLocked = PPP_ScaleLock.IsEnabled(t);

            if (PPP_ScaleLock.TryDragScale(t.localScale, pivotPosition, t.rotation, handleSize, scaleLocked,
                    out Vector3 scaleRatio))
                PPP_TransformOps.ScaleAround(t, pivotPosition, scaleRatio, "Scale via RePivot handle");
        }

        private static void DrawTransformHandle(Transform t, Vector3 pivotPosition, PPP_PivotDragState state, Quaternion offsetRotation)
        {
            Quaternion baseHandleRotation = GetHandleRotation(state, t, offsetRotation);

            Vector3 handlePosition = pivotPosition;
            Quaternion handleRotation = baseHandleRotation;
            Vector3 handleScale = t.localScale;

            EditorGUI.BeginChangeCheck();
            Handles.TransformHandle(ref handlePosition, ref handleRotation, ref handleScale);
            if (EditorGUI.EndChangeCheck())
            {
                Quaternion rotationDelta = handleRotation * Quaternion.Inverse(baseHandleRotation);
                Vector3 positionDelta = handlePosition - pivotPosition;
                Vector3 scaleRatio = PPP_PivotMath.GetScaleRatio(t.localScale, handleScale);

                Undo.RecordObject(t, "Transform via RePivot handle");

                // Unlike the shared handle, the pivot may carry its own rotation, so
                // the scale part has to be conjugated through that offset rotation.
                Vector3 posAfterRotate = pivotPosition + rotationDelta * (t.position - pivotPosition);
                Quaternion rotAfterRotate = rotationDelta * t.rotation;

                Quaternion scaleHandleRotation = rotAfterRotate * offsetRotation;
                Vector3 posAfterScale = pivotPosition + PPP_PivotMath.ApplyWorldScaleToVector(
                    posAfterRotate - pivotPosition, scaleHandleRotation, scaleRatio);
                Vector3 newLocalScale = PPP_PivotMath.ConjugateLocalScale(t.localScale, offsetRotation, scaleRatio);

                t.rotation = rotAfterRotate;
                t.localScale = newLocalScale;
                t.position = posAfterScale + positionDelta;

                if (state.pivotFrozen)
                    state.frozenPivotPosition += positionDelta;

                if (Tools.pivotRotation != PivotRotation.Local)
                    state.globalRotationHandleState = handleRotation;
            }

            state.UpdateAfterFreezableHandle(pivotPosition, allowFreeze: true);
        }

        private static void DrawOffsetMoveHandle(GameObject targetObject, int index, Transform t, Vector3 pivotPosition,
            PPP_PivotDragState state, Quaternion offsetRotation)
        {
            Quaternion handleRotation = GetHandleRotation(state, t, offsetRotation);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(pivotPosition, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                // Record before touching the offsets: GetMultiPivotOffsets returns the
                // settings' internal array, so the edits below happen in place.
                PPP_Settings.RecordUndo("Move Pivot");
                Pose[] offsets = PPP_Settings.GetMultiPivotOffsets(targetObject);
                Pose pose = offsets[index];
                pose.position = t.InverseTransformPoint(newPosition);
                offsets[index] = pose;
                PPP_Settings.SetMultiPivotOffsets(targetObject, offsets);
            }

            state.UpdateAfterEditHandle();
        }

        private static void DrawOffsetRotateHandle(GameObject targetObject, int index, Transform t, Vector3 pivotPosition,
            PPP_PivotDragState state, Quaternion offsetRotation)
        {
            Quaternion handleRotation = GetHandleRotation(state, t, offsetRotation);

            EditorGUI.BeginChangeCheck();
            Quaternion newHandleRotation = Handles.RotationHandle(handleRotation, pivotPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Quaternion delta = newHandleRotation * Quaternion.Inverse(handleRotation);

                // Record before touching the offsets (in-place edit, see move handle).
                PPP_Settings.RecordUndo("Rotate Pivot");
                Pose[] offsets = PPP_Settings.GetMultiPivotOffsets(targetObject);
                Pose pose = offsets[index];
                Quaternion currentWorldRotation = PPP_PivotMath.GetSafeRotation(t.rotation * pose.rotation);
                pose.rotation = PPP_PivotMath.GetSafeRotation(Quaternion.Inverse(t.rotation) * delta * currentWorldRotation);
                offsets[index] = pose;
                PPP_Settings.SetMultiPivotOffsets(targetObject, offsets);

                if (Tools.pivotRotation != PivotRotation.Local)
                    state.globalRotationHandleState = newHandleRotation;
            }

            state.UpdateAfterEditHandle();
        }

        /// <summary>
        /// Where the pivot's small buttons sit: up and to the right of the pivot,
        /// in a row from <paramref name="slot"/> 0 (delete) rightwards.
        /// </summary>
        private static Vector3 GetPivotButtonPosition(Vector3 pivotPosition, bool hidden, int slot)
        {
            if (hidden)
                return PPP_PivotMath.GetOffscreenHandlePosition();

            float handleSize = HandleUtility.GetHandleSize(pivotPosition);
            Camera sceneCamera = Camera.current;
            Vector3 screenOffset = sceneCamera != null
                ? (sceneCamera.transform.right + sceneCamera.transform.up).normalized
                : Vector3.up;
            Vector3 buttonPosition = pivotPosition + screenOffset * handleSize * 0.55f;

            if (slot > 0)
            {
                Vector3 right = sceneCamera != null ? sceneCamera.transform.right : Vector3.right;
                buttonPosition += right * HandleUtility.GetHandleSize(buttonPosition) * PIVOT_BUTTON_SPACING * slot;
            }

            return buttonPosition;
        }

        private static bool DrawPivotDeleteButton(GameObject targetObject, int index, Vector3 pivotPosition, bool hidden)
        {
            Vector3 buttonPosition = GetPivotButtonPosition(pivotPosition, hidden, 0);

            float size = HandleUtility.GetHandleSize(buttonPosition) * PIVOT_BUTTON_SIZE;
            if (!Handles.Button(buttonPosition, Quaternion.identity, size, size, DeleteHandleCap))
                return false;

            Pose[] offsets = PPP_Settings.GetMultiPivotOffsets(targetObject);
            if (index >= offsets.Length)
                return false;

            PPP_Settings.RecordUndo("Delete Pivot");
            System.Array.Copy(offsets, index + 1, offsets, index, offsets.Length - index - 1);
            System.Array.Resize(ref offsets, offsets.Length - 1);

            // Removing the last pivot clears the entry, which also drops the object
            // back to Default mode so the shared pivot reappears.
            PPP_Settings.SetMultiPivotOffsets(targetObject, offsets);
            _activeHandleIndex = -1;
            SceneView.RepaintAll();
            return true;
        }

        private static bool DrawCreateParentButton(Transform target, int index, Vector3 pivotPosition, bool hidden)
        {
            Vector3 buttonPosition = GetPivotButtonPosition(pivotPosition, hidden, 1);

            _createParentAvailable = PPP_CreateParent.CanCreateAtObjectPivot(target, out string reason);
            _createParentTooltip = _createParentAvailable ? CREATE_PARENT_TOOLTIP : reason;

            float size = HandleUtility.GetHandleSize(buttonPosition) * PIVOT_BUTTON_SIZE;
            if (!Handles.Button(buttonPosition, Quaternion.identity, size, size, CreateParentHandleCap) ||
                !_createParentAvailable)
                return false;

            // The flow may open popups and restructure the hierarchy, neither of
            // which belongs in the middle of a scene GUI pass.
            EditorApplication.delayCall += () => PPP_CreateParent.CreateAtObjectPivot(target, index);
            _activeHandleIndex = -1;
            return true;
        }

        private static void DeleteHandleCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            DrawButtonCap(controlId, position, size, eventType,
                new Color(0.8f, 0f, 0f, 0.65f), new Color(1f, 0.15f, 0.15f, 0.95f), "Delete this pivot",
                (right, up) =>
                {
                    Handles.DrawLine(position - right - up, position + right + up, 3f);
                    Handles.DrawLine(position - right + up, position + right - up, 3f);
                });
        }

        private static void CreateParentHandleCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Color accent = PPP_OverlayWidgets.AccentColor;
            Color baseColor = _createParentAvailable ? new Color(accent.r, accent.g, accent.b, 0.7f) : new Color(0.45f, 0.45f, 0.45f, 0.5f);
            Color hotColor = _createParentAvailable ? new Color(accent.r, accent.g, accent.b, 1f) : baseColor;

            DrawButtonCap(controlId, position, size, eventType, baseColor, hotColor, _createParentTooltip,
                (right, up) =>
                {
                    Handles.DrawLine(position - right, position + right, 3f);
                    Handles.DrawLine(position - up, position + up, 3f);
                });
        }

        /// <summary>
        /// A camera-facing disc with a white glyph, plus a tooltip while hovered
        /// (scene view handles have no tooltips of their own).
        /// </summary>
        private static void DrawButtonCap(int controlId, Vector3 position, float size, EventType eventType,
            Color baseColor, Color hotColor, string tooltip, System.Action<Vector3, Vector3> drawGlyph)
        {
            if (eventType == EventType.Layout)
            {
                HandleUtility.AddControl(controlId, HandleUtility.DistanceToCircle(position, size));
                return;
            }

            if (eventType != EventType.Repaint)
                return;

            Camera sceneCamera = Camera.current;
            Vector3 normal = sceneCamera != null ? -sceneCamera.transform.forward : Vector3.forward;
            Quaternion billboardRotation = sceneCamera != null
                ? Quaternion.LookRotation(normal, sceneCamera.transform.up)
                : Quaternion.identity;

            bool isHot = HandleUtility.nearestControl == controlId || GUIUtility.hotControl == controlId;

            Color previousColor = Handles.color;

            Handles.color = isHot ? hotColor : baseColor;
            Handles.DrawSolidDisc(position, normal, size);

            Handles.color = Color.white;
            Handles.DrawWireDisc(position, normal, size);

            Vector3 right = billboardRotation * Vector3.right * size * 0.55f;
            Vector3 up = billboardRotation * Vector3.up * size * 0.55f;
            drawGlyph(right, up);

            Handles.color = previousColor;

            if (isHot && GUIUtility.hotControl == 0 && !string.IsNullOrEmpty(tooltip))
                DrawSceneTooltip(position, tooltip);
        }

        private static void DrawSceneTooltip(Vector3 worldPosition, string text)
        {
            if (_tooltipStyle == null)
            {
                _tooltipStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    padding = new RectOffset(6, 6, 3, 3),
                    normal = { textColor = new Color(0.92f, 0.93f, 0.95f) }
                };
            }

            GUIContent content = new GUIContent(text);
            Vector2 contentSize = _tooltipStyle.CalcSize(content);
            Vector2 guiPoint = HandleUtility.WorldToGUIPoint(worldPosition);
            Rect rect = new Rect(guiPoint.x - contentSize.x * 0.5f, guiPoint.y + 14f, contentSize.x, contentSize.y);

            Handles.BeginGUI();
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.105f, 0.115f, 0.94f));
            GUI.Label(rect, content, _tooltipStyle);
            Handles.EndGUI();
        }

        private static void EnsureStateCapacity(int count)
        {
            while (_states.Count < count)
                _states.Add(new PPP_PivotDragState());
        }
    }
}
