using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Draws the shared pivot handle (the replacement for Unity's built-in
    /// gizmo) and owns its rotation / freeze drag state.
    /// </summary>
    internal static class PPP_SharedPivotHandles
    {
        private static readonly PPP_PivotDragState _dragState = new PPP_PivotDragState();
        private static Transform _lastRotationTarget;

        /// <summary>
        /// While a multi-target rotate/transform drag is active, the pivot stays
        /// frozen at its drag-start position instead of chasing the moving bounds.
        /// </summary>
        internal static bool TryGetFrozenPivot(out Vector3 position)
        {
            position = _dragState.frozenPivotPosition;
            return _dragState.pivotFrozen;
        }

        internal static Quaternion GetHandleRotation(Transform target)
        {
            if (Tools.pivotRotation == PivotRotation.Local)
                return target.rotation;

            if (target != _lastRotationTarget)
            {
                _lastRotationTarget = target;
                _dragState.globalRotationHandleState = Quaternion.identity;
            }

            return _dragState.globalRotationHandleState;
        }

        /// <summary>
        /// The same rotation <see cref="GetHandleRotation"/> would report, without
        /// claiming the target. The overlay polls this several times a second, and
        /// the handle bookkeeping has to stay driven by drawing alone.
        /// </summary>
        internal static Quaternion PeekHandleRotation(Transform target)
        {
            if (Tools.pivotRotation == PivotRotation.Local)
                return target.rotation;

            // A target the handles have not drawn yet is one GetHandleRotation
            // would reset to identity.
            return target == _lastRotationTarget
                ? _dragState.globalRotationHandleState
                : Quaternion.identity;
        }

        internal static void Draw(Tool tool, Transform[] targets, Transform activeTransform, Vector3 pivotPosition)
        {
            switch (tool)
            {
                case Tool.Move:
                    DrawMoveHandle(targets, activeTransform, pivotPosition);
                    break;
                case Tool.Rotate:
                    DrawRotateHandle(targets, activeTransform, pivotPosition);
                    break;
                case Tool.Scale:
                    DrawScaleHandle(targets, activeTransform, pivotPosition);
                    break;
                case Tool.Transform:
                    DrawTransformHandle(targets, activeTransform, pivotPosition);
                    break;
            }
        }

        private static void DrawMoveHandle(Transform[] targets, Transform activeTransform, Vector3 pivotPosition)
        {
            Quaternion handleRotation = GetHandleRotation(activeTransform);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(pivotPosition, handleRotation);
            if (EditorGUI.EndChangeCheck())
                PPP_TransformOps.Move(targets, newPosition - pivotPosition, "Move Objects");
        }

        private static void DrawRotateHandle(Transform[] targets, Transform activeTransform, Vector3 pivotPosition)
        {
            Quaternion handleRotation = GetHandleRotation(activeTransform);

            EditorGUI.BeginChangeCheck();
            Quaternion newHandleRotation = Handles.RotationHandle(handleRotation, pivotPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Quaternion delta = newHandleRotation * Quaternion.Inverse(handleRotation);
                PPP_TransformOps.RotateAround(targets, pivotPosition, delta, "Rotate Objects");

                if (Tools.pivotRotation != PivotRotation.Local)
                    _dragState.globalRotationHandleState = newHandleRotation;
            }

            _dragState.UpdateAfterFreezableHandle(pivotPosition, targets.Length > 1);
        }

        private static void DrawScaleHandle(Transform[] targets, Transform activeTransform, Vector3 pivotPosition)
        {
            float handleSize = HandleUtility.GetHandleSize(pivotPosition);

            // With the inspector's scale lock on, the handles go white and every
            // axis has to scale by the same ratio - same as Unity's own scale tool.
            bool scaleLocked = PPP_ScaleLock.IsEnabled(targets);

            if (PPP_ScaleLock.TryDragScale(activeTransform.localScale, pivotPosition, activeTransform.rotation,
                    handleSize, scaleLocked, out Vector3 scaleRatio))
                ApplyScale(targets, activeTransform, pivotPosition, scaleRatio);
        }

        /// <summary>
        /// Applies a scale ratio about the shared pivot, the same way the scale
        /// handle does. Shared with the overlay's numeric scale fields.
        /// </summary>
        internal static void ApplyScale(Transform[] targets, Transform activeTransform, Vector3 pivotPosition, Vector3 scaleRatio)
        {
            if (targets.Length == 1)
                ScalePreservingPivot(activeTransform, pivotPosition, scaleRatio);
            else
                PPP_TransformOps.ScaleAround(targets, pivotPosition, scaleRatio, "Scale Objects");
        }

        /// <summary>
        /// Scales a single target in place and then repositions it so the pivot
        /// recomputes to exactly where it was. In Unbounded mode with object scale
        /// off the offset is a fixed world distance from the (now moved) bounds
        /// center, so a plain scale-about-pivot would leave the pivot somewhere
        /// new once the drag ends. Instead of hiding that with a frozen handle we
        /// physically place the object where the pivot naturally lands back on its
        /// original spot, so it stays put across deselect/reselect too. For every
        /// other mode the pivot is a scale-independent local point, so the applied
        /// correction is zero and behaviour is unchanged.
        /// </summary>
        private static void ScalePreservingPivot(Transform target, Vector3 pivotPosition, Vector3 scaleRatio)
        {
            Undo.RecordObject(target, "Scale Objects");
            target.localScale = Vector3.Scale(target.localScale, scaleRatio);

            if (PPP_Handles.TryGetSharedPivotWorldPosition(target, out Vector3 pivotAfterScale))
                target.position += pivotPosition - pivotAfterScale;
        }

        private static void DrawTransformHandle(Transform[] targets, Transform activeTransform, Vector3 pivotPosition)
        {
            Quaternion baseHandleRotation = GetHandleRotation(activeTransform);

            Vector3 handlePosition = pivotPosition;
            Quaternion handleRotation = baseHandleRotation;
            Vector3 handleScale = activeTransform.localScale;

            EditorGUI.BeginChangeCheck();
            Handles.TransformHandle(ref handlePosition, ref handleRotation, ref handleScale);
            if (EditorGUI.EndChangeCheck())
            {
                Quaternion rotationDelta = handleRotation * Quaternion.Inverse(baseHandleRotation);
                Vector3 positionDelta = handlePosition - pivotPosition;
                Vector3 scaleRatio = PPP_PivotMath.GetScaleRatio(activeTransform.localScale, handleScale);

                PPP_TransformOps.TransformAround(targets, pivotPosition, positionDelta, rotationDelta, scaleRatio,
                    "Transform Objects");

                if (Tools.pivotRotation != PivotRotation.Local)
                    _dragState.globalRotationHandleState = handleRotation;
            }

            _dragState.UpdateAfterFreezableHandle(pivotPosition, targets.Length > 1);
        }
    }
}
