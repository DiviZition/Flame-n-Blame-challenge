using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Applies handle deltas to transforms, recording the change on the Undo
    /// stack. The per-transform math lives here once and is shared between the
    /// shared pivot handles (many targets) and object pivot handles (one target).
    /// </summary>
    internal static class PPP_TransformOps
    {
        internal static void Move(Transform[] targets, Vector3 delta, string undoName)
        {
            Undo.RecordObjects(ToObjectArray(targets), undoName);
            foreach (Transform target in targets)
                ApplyMove(target, delta);
        }

        internal static void Move(Transform target, Vector3 delta, string undoName)
        {
            Undo.RecordObject(target, undoName);
            ApplyMove(target, delta);
        }

        internal static void RotateAround(Transform[] targets, Vector3 pivotPosition, Quaternion delta, string undoName)
        {
            Undo.RecordObjects(ToObjectArray(targets), undoName);
            foreach (Transform target in targets)
                ApplyRotation(target, pivotPosition, delta);
        }

        internal static void RotateAround(Transform target, Vector3 pivotPosition, Quaternion delta, string undoName)
        {
            Undo.RecordObject(target, undoName);
            ApplyRotation(target, pivotPosition, delta);
        }

        internal static void ScaleAround(Transform[] targets, Vector3 pivotPosition, Vector3 scaleRatio, string undoName)
        {
            Undo.RecordObjects(ToObjectArray(targets), undoName);
            foreach (Transform target in targets)
                ApplyScale(target, pivotPosition, scaleRatio);
        }

        internal static void ScaleAround(Transform target, Vector3 pivotPosition, Vector3 scaleRatio, string undoName)
        {
            Undo.RecordObject(target, undoName);
            ApplyScale(target, pivotPosition, scaleRatio);
        }

        /// <summary>
        /// Combined rotate + scale + move for the Transform tool: rotate around the
        /// pivot first, then scale the rotated offset in the target's new frame,
        /// then translate.
        /// </summary>
        internal static void TransformAround(Transform[] targets, Vector3 pivotPosition, Vector3 positionDelta,
            Quaternion rotationDelta, Vector3 scaleRatio, string undoName)
        {
            Undo.RecordObjects(ToObjectArray(targets), undoName);

            foreach (Transform target in targets)
            {
                Vector3 rotatedRelativePosition = rotationDelta * (target.position - pivotPosition);

                Quaternion inverseRotation = Quaternion.Inverse(rotationDelta * target.rotation);
                Vector3 localRelativePosition = inverseRotation * rotatedRelativePosition;
                Vector3 scaledLocalRelativePosition = Vector3.Scale(localRelativePosition, scaleRatio);
                Vector3 scaledRelativePosition = (rotationDelta * target.rotation) * scaledLocalRelativePosition;

                target.rotation = rotationDelta * target.rotation;
                target.localScale = Vector3.Scale(target.localScale, scaleRatio);
                target.position = pivotPosition + positionDelta + scaledRelativePosition;
            }
        }

        private static void ApplyMove(Transform target, Vector3 delta)
        {
            target.position += delta;
        }

        private static void ApplyRotation(Transform target, Vector3 pivotPosition, Quaternion delta)
        {
            target.position = pivotPosition + delta * (target.position - pivotPosition);
            target.rotation = delta * target.rotation;
        }

        private static void ApplyScale(Transform target, Vector3 pivotPosition, Vector3 scaleRatio)
        {
            Quaternion inverseRotation = Quaternion.Inverse(target.rotation);
            Vector3 localRelativePosition = inverseRotation * (target.position - pivotPosition);
            Vector3 scaledLocalRelativePosition = Vector3.Scale(localRelativePosition, scaleRatio);

            target.localScale = Vector3.Scale(target.localScale, scaleRatio);
            target.position = pivotPosition + target.rotation * scaledLocalRelativePosition;
        }

        private static Object[] ToObjectArray(Transform[] targets)
        {
            Object[] objects = new Object[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                objects[i] = targets[i];
            return objects;
        }
    }
}
