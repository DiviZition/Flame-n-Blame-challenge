using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// The numeric counterpart to the shared pivot handles: reads the current
    /// position / rotation / scale of the selection (position of the offset pivot,
    /// the active object's world rotation and local scale) and applies typed values
    /// back through the exact same operations a drag would use. The values are
    /// expressed in world space regardless of the scene view's Local/Global toggle,
    /// which only decides how the gizmo is oriented, not what the numbers mean.
    /// </summary>
    internal static class PPP_OffsetTransform
    {
        internal struct Snapshot
        {
            public Transform[] targets;
            public Transform activeTransform;
            public Vector3 pivotPosition;
            public Vector3 rotationEuler;
            public Vector3 scale;
            public bool scaleLocked;
        }

        // Quaternion -> euler is many-to-one, so re-reading a typed rotation can
        // hand back a different (equivalent) triple and make the fields jump while
        // the user is still editing them. Remembering the euler that produced the
        // current rotation keeps the displayed numbers stable, the same trick
        // Unity's own inspector uses.
        private static Vector3 _eulerHint;
        private static Quaternion _eulerHintSource = Quaternion.identity;
        private static bool _hasEulerHint;

        /// <summary>
        /// Current values for the selection, or false when there is nothing the
        /// fields can meaningfully act on (no selection, no bounds, edit mode...).
        /// </summary>
        internal static bool TryGetSnapshot(out Snapshot snapshot)
        {
            snapshot = default;

            if (!PPP_Settings.Enabled)
                return false;

            Transform[] targets = Selection.transforms;
            Transform activeTransform = Selection.activeTransform;

            if (targets.Length == 0 || activeTransform == null || activeTransform.gameObject.scene.rootCount == 0)
                return false;

            // Editing the object's own pivots hides the shared pivot, and with it
            // the frame these fields are expressed in.
            if (targets.Length == 1 &&
                PPP_Settings.GetMultiPivotOffsets(activeTransform.gameObject).Length > 0 &&
                PPP_Settings.GetMultiPivotMode(activeTransform.gameObject) == PivotMode.Edit)
                return false;

            if (!PPP_Handles.TryGetSharedPivotPosition(targets, activeTransform, out Vector3 pivotPosition))
                return false;

            snapshot.targets = targets;
            snapshot.activeTransform = activeTransform;
            snapshot.pivotPosition = pivotPosition;
            snapshot.rotationEuler = GetDisplayEuler(activeTransform.rotation);
            snapshot.scale = activeTransform.localScale;
            snapshot.scaleLocked = PPP_ScaleLock.IsEnabled(targets);
            return true;
        }

        /// <summary>Moves the selection so the offset pivot lands on the given world position.</summary>
        internal static void SetPosition(Vector3 worldPosition)
        {
            if (!TryGetSnapshot(out Snapshot snapshot))
                return;

            Vector3 delta = worldPosition - snapshot.pivotPosition;
            if (delta == Vector3.zero)
                return;

            PPP_TransformOps.Move(snapshot.targets, delta, "Move Objects");
        }

        /// <summary>Rotates the selection about the offset pivot to the given world euler.</summary>
        internal static void SetRotation(Vector3 euler)
        {
            if (!TryGetSnapshot(out Snapshot snapshot))
                return;

            Quaternion baseRotation = snapshot.activeTransform.rotation;
            Quaternion newRotation = Quaternion.Euler(euler);

            // Kept even when the rotation is unchanged, so an edit that lands on an
            // equivalent-but-differently-spelled euler triple does not make the
            // other two fields jump on the next refresh.
            SetEulerHint(euler, newRotation);

            Quaternion delta = newRotation * Quaternion.Inverse(baseRotation);
            if (delta == Quaternion.identity)
                return;

            PPP_TransformOps.RotateAround(snapshot.targets, snapshot.pivotPosition, delta, "Rotate Objects");
        }

        /// <summary>
        /// Scales the selection about the offset pivot. <paramref name="changedAxis"/>
        /// is the axis the user edited; with the inspector's scale lock on, its
        /// ratio is applied to all three axes just like the scale handle does.
        /// </summary>
        internal static void SetScale(Vector3 scale, int changedAxis)
        {
            if (!TryGetSnapshot(out Snapshot snapshot))
                return;

            // Scaling about the pivot is expressed as a ratio against the current
            // scale, and nothing multiplies a zero back up again - so an axis
            // typed (or passed through) as 0 would be a one-way trip. Typing "0.5"
            // walks through exactly that on its first keystroke, so a zero target
            // leaves the axis where it is instead.
            for (int axis = 0; axis < 3; axis++)
            {
                if (scale[axis] == 0f)
                    scale[axis] = snapshot.scale[axis];
            }

            Vector3 ratio = PPP_PivotMath.GetScaleRatio(snapshot.scale, scale);
            if (snapshot.scaleLocked)
                ratio = Vector3.one * ratio[changedAxis];

            if (ratio == Vector3.one)
                return;

            PPP_SharedPivotHandles.ApplyScale(snapshot.targets, snapshot.activeTransform, snapshot.pivotPosition, ratio);
        }

        private static Vector3 GetDisplayEuler(Quaternion rotation)
        {
            if (_hasEulerHint && Quaternion.Angle(_eulerHintSource, rotation) < 0.01f)
                return Normalize(_eulerHint);

            _hasEulerHint = false;
            return Normalize(rotation.eulerAngles);
        }

        /// <summary>
        /// Folds an euler triple into the (-180, 180] range the fields display in.
        /// Quaternion.eulerAngles reports 0..360, and a typed or dragged value can
        /// run off in either direction, so both paths are wrapped on the way out.
        /// </summary>
        private static Vector3 Normalize(Vector3 euler)
        {
            return new Vector3(NormalizeAngle(euler.x), NormalizeAngle(euler.y), NormalizeAngle(euler.z));
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;

            if (angle > 180f)
                angle -= 360f;
            else if (angle <= -180f)
                angle += 360f;

            return angle;
        }

        private static void SetEulerHint(Vector3 euler, Quaternion rotation)
        {
            _eulerHint = euler;
            _eulerHintSource = rotation;
            _hasEulerHint = true;
        }
    }
}
