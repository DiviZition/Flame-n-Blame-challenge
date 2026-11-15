using System;
using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Scene view entry point. Decides each frame which pivot handles are shown
    /// (shared pivot, object pivots, or the pivot-edit handles) and in what
    /// order they are drawn.
    ///
    /// The draw order is load-bearing: IMGUI control ids are assigned by call
    /// order, so the shared handle is always drawn before the object pivot
    /// handles, and hidden handles are moved offscreen rather than skipped.
    /// </summary>
    [InitializeOnLoad]
    public static class PPP_Handles
    {
        public enum State
        {
            NotInitialized,
            Visible,
            NoTargets,
            EditPivotMode,
            UnsupportedTool,
            NoBounds,
            NotEnabled,
            PanelHidden
        }

        // The object's real pivot marker, as a fraction of the handle size.
        private const float ORIGIN_DOT_SIZE = 0.045f;
        // Shared with the overlay, whose "move the pivot here" button wears the
        // same dot: the button only reads as that marker if the colour matches.
        internal static readonly Color OriginDotColor = new Color(1f, 0.82f, 0.18f);
        private static readonly Color OriginDotOutlineColor = new Color(0f, 0f, 0f, 0.6f);

        private static State _state = State.NotInitialized;
        public static event Action<State> OnStateChanged;

        public static State CurrentState
        {
            get
            {
                return _state;
            }
            private set
            {
                Tools.hidden = value is State.Visible or State.EditPivotMode;
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged?.Invoke(_state);
                }
            }
        }

        static PPP_Handles()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnUndoRedoPerformed()
        {
            // Pivot edits live on the settings singleton; rebuild its lookups and
            // repaint so an undone pivot move/rotate/delete shows immediately.
            PPP_Settings.OnUndoRedoPerformed();
            PPP_ObjectPivotHandles.CancelActiveHandle();
            PPP_Bounds.InvalidateAll();
            SceneView.RepaintAll();
        }

        private static void OnHierarchyChanged()
        {
            // Mesh/child additions, removals and reparenting can change local
            // bounds; the cheapest safe reaction is to drop the whole cache.
            PPP_Bounds.InvalidateAll();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            // No panel on screen, no takeover. Hiding the panel - overlay or
            // window - would otherwise leave the default transform gizmo
            // overridden with no way to hand it back. Checked before the settings
            // flag because this one is about whether Pivot++ is present at all,
            // not whether it's switched on.
            if (!PPP_PanelHost.IsPanelVisible)
            {
                CurrentState = State.PanelHidden;
                return;
            }

            if (!PPP_Settings.Enabled)
            {
                CurrentState = State.NotEnabled;
                return;
            }

            Transform[] targets = Selection.transforms;
            Transform activeTransform = Selection.activeTransform;

            if (targets.Length == 0 || activeTransform == null || activeTransform.gameObject.scene.rootCount == 0)
            {
                CurrentState = State.NoTargets;
                return;
            }

            // Drawn before any handle, so the handles render on top of it.
            DrawOriginDots(targets);

            PPP_ObjectPivotHandles.SyncSelection(activeTransform);

            if (Tools.current is not Tool.Move and not Tool.Rotate and not Tool.Scale and not Tool.Transform)
            {
                CurrentState = State.UnsupportedTool;
                return;
            }

            // Object-specific pivots only apply to single selection.
            Pose[] objectPivots = targets.Length == 1
                ? PPP_Settings.GetMultiPivotOffsets(activeTransform.gameObject)
                : System.Array.Empty<Pose>();

            if (objectPivots.Length > 0 && PPP_Settings.GetMultiPivotMode(activeTransform.gameObject) == PivotMode.Edit)
            {
                // Edit mode: shared pivot is hidden, only object pivots are shown.
                CurrentState = State.EditPivotMode;
                PPP_ObjectPivotHandles.DrawEditModeHandles(activeTransform, objectPivots);
                return;
            }

            if (!TryGetSharedPivotPosition(targets, activeTransform, out Vector3 pivotPosition))
            {
                CurrentState = State.NoBounds;
                return;
            }

            CurrentState = State.Visible;

            // Shared pivot handle. Moved offscreen (not skipped, to keep control ids
            // stable) while an object pivot handle is being dragged.
            bool sharedHidden = PPP_ObjectPivotHandles.IsObjectPivotDragActive;
            Vector3 sharedHandlePosition = sharedHidden
                ? PPP_PivotMath.GetOffscreenHandlePosition()
                : pivotPosition;

            int hotControlBefore = GUIUtility.hotControl;
            PPP_SharedPivotHandles.Draw(Tools.current, targets, activeTransform, sharedHandlePosition);
            if (hotControlBefore == 0 && GUIUtility.hotControl != 0)
                PPP_ObjectPivotHandles.NotifySharedHandleDragStarted();

            if (objectPivots.Length > 0)
                PPP_ObjectPivotHandles.DrawDefaultModeHandles(activeTransform, objectPivots);
        }

        /// <summary>
        /// Marks each selected object's real pivot (its transform position) with a
        /// yellow dot. The Pivot++ handle usually sits somewhere else, and seeing
        /// both all the time teaches the difference before it matters - e.g. when
        /// a new parent's handle doesn't appear where the parent was created.
        /// Drawing only: no control is registered, so the dot never takes a click
        /// or shifts the handles' control ids.
        /// </summary>
        private static void DrawOriginDots(Transform[] targets)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Camera sceneCamera = Camera.current;
            Vector3 normal = sceneCamera != null ? -sceneCamera.transform.forward : Vector3.forward;
            Color previousColor = Handles.color;

            foreach (Transform target in targets)
            {
                Vector3 position = target.position;
                float radius = HandleUtility.GetHandleSize(position) * ORIGIN_DOT_SIZE;

                // A dark rim keeps the dot readable on bright geometry.
                Handles.color = OriginDotOutlineColor;
                Handles.DrawSolidDisc(position, normal, radius * 1.35f);
                Handles.color = OriginDotColor;
                Handles.DrawSolidDisc(position, normal, radius);
            }

            Handles.color = previousColor;
        }

        /// <summary>
        /// World position of the shared pivot for the current selection. Shared by
        /// the scene GUI and the overlay's numeric transform fields, so both agree
        /// on where the pivot is for single and multi selections alike.
        /// </summary>
        internal static bool TryGetSharedPivotPosition(Transform[] targets, Transform activeTransform, out Vector3 pivotPosition)
        {
            GameObject activeObject = activeTransform.gameObject;
            UnitMode unitMode = PPP_Settings.GetUnitMode(activeObject);
            Vector3 offsetValues = unitMode == UnitMode.Unbounded
                ? PPP_Settings.GetAbsoluteValues(activeObject)
                : PPP_Settings.GetSliderValues(activeObject);

            if (targets.Length == 1)
            {
                pivotPosition = default;
                if (!PPP_Bounds.TryGetLocalBounds(activeTransform, out Bounds localBounds))
                    return false;

                bool useObjectScale = PPP_Settings.GetUseObjectScale(activeObject);
                pivotPosition = PPP_PivotMath.GetPivotPosition(activeTransform, localBounds, offsetValues, unitMode, useObjectScale);
                return true;
            }

            if (PPP_SharedPivotHandles.TryGetFrozenPivot(out pivotPosition))
                return true;

            Quaternion referenceRotation = PPP_SharedPivotHandles.GetHandleRotation(activeTransform);
            if (!PPP_Bounds.TryGetCombinedBounds(targets, referenceRotation, out Bounds combinedBounds))
                return false;

            pivotPosition = PPP_PivotMath.GetPivotPosition(referenceRotation, combinedBounds, offsetValues, unitMode);
            return true;
        }

        /// <summary>
        /// Bounded offset values that put the shared pivot back onto the active
        /// object's own origin - the yellow dot drawn above. The inverse of the
        /// bounded half of <see cref="TryGetSharedPivotPosition"/>, kept beside it
        /// so the two cannot drift apart.
        ///
        /// Values are returned unclamped: an origin outside the object's bounds is
        /// out of reach in Bounded mode, and the caller is the one that can say so.
        /// </summary>
        internal static bool TryGetOriginOffsetValues(Transform[] targets, Transform activeTransform, out Vector3 offsetValues)
        {
            offsetValues = default;
            if (activeTransform == null || targets.Length == 0)
                return false;

            if (targets.Length == 1)
            {
                if (!PPP_Bounds.TryGetLocalBounds(activeTransform, out Bounds localBounds))
                    return false;

                // Local bounds are measured in the object's own space, where the
                // origin is simply zero.
                offsetValues = PPP_PivotMath.GetBoundedValuesForPoint(localBounds, Vector3.zero);
                return true;
            }

            // Peeked, not fetched: this runs from the overlay's poll, which must
            // not drive the handles' own rotation bookkeeping.
            Quaternion referenceRotation = PPP_SharedPivotHandles.PeekHandleRotation(activeTransform);
            if (!PPP_Bounds.TryGetCombinedBounds(targets, referenceRotation, out Bounds combinedBounds))
                return false;

            // Combined bounds live in the handle's rotated frame, so the active
            // object's origin has to be expressed there too.
            Vector3 referencePoint = Quaternion.Inverse(referenceRotation) * activeTransform.position;
            offsetValues = PPP_PivotMath.GetBoundedValuesForPoint(combinedBounds, referencePoint);
            return true;
        }

        /// <summary>
        /// World position of the shared pivot for a single target. Used by the
        /// overlay to spawn a new object pivot where the shared pivot currently is.
        /// </summary>
        public static bool TryGetSharedPivotWorldPosition(Transform target, out Vector3 worldPosition)
        {
            worldPosition = default;
            if (target == null || !PPP_Bounds.TryGetLocalBounds(target, out Bounds localBounds))
                return false;

            GameObject targetObject = target.gameObject;
            UnitMode unitMode = PPP_Settings.GetUnitMode(targetObject);
            Vector3 offsetValues = unitMode == UnitMode.Unbounded
                ? PPP_Settings.GetAbsoluteValues(targetObject)
                : PPP_Settings.GetSliderValues(targetObject);

            bool useObjectScale = PPP_Settings.GetUseObjectScale(targetObject);
            worldPosition = PPP_PivotMath.GetPivotPosition(target, localBounds, offsetValues, unitMode, useObjectScale);
            return true;
        }
    }
}
