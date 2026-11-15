using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Stateless math helpers shared by the pivot handle drawing code.
    /// </summary>
    internal static class PPP_PivotMath
    {
        /// <summary>
        /// Pivot position for a single target, offset inside its own local bounds.
        /// </summary>
        internal static Vector3 GetPivotPosition(Transform target, Bounds localBounds, Vector3 offsetValues, UnitMode unitMode, bool useObjectScale)
        {
            Vector3 localOffset;
            if (unitMode == UnitMode.Unbounded)
            {
                // The offset lives in local space, so localToWorldMatrix scales it by
                // the object's (lossy) scale. When the pivot should be scale-independent
                // we pre-divide by that scale so it cancels out, leaving a fixed world
                // distance regardless of how large the object is.
                localOffset = useObjectScale
                    ? offsetValues
                    : DivideByScale(offsetValues, target.lossyScale);
            }
            else
            {
                localOffset = Vector3.Scale(offsetValues, localBounds.extents);
            }
            Vector3 localPoint = localBounds.center + localOffset;
            return target.localToWorldMatrix.MultiplyPoint3x4(localPoint);
        }

        private static Vector3 DivideByScale(Vector3 v, Vector3 scale)
        {
            return new Vector3(
                scale.x != 0f ? v.x / scale.x : v.x,
                scale.y != 0f ? v.y / scale.y : v.y,
                scale.z != 0f ? v.z / scale.z : v.z);
        }

        /// <summary>
        /// Pivot position for a multi-selection, offset inside bounds that were
        /// computed in a rotated reference frame.
        /// </summary>
        internal static Vector3 GetPivotPosition(Quaternion referenceRotation, Bounds referenceBounds, Vector3 offsetValues, UnitMode unitMode)
        {
            Vector3 localOffset = unitMode == UnitMode.Unbounded
                ? offsetValues
                : Vector3.Scale(offsetValues, referenceBounds.extents);
            Vector3 referencePoint = referenceBounds.center + localOffset;
            return referenceRotation * referencePoint;
        }

        /// <summary>
        /// Inverse of the bounded offset: the per-axis values that land the pivot
        /// exactly on <paramref name="point"/>, expressed in the same space the
        /// bounds were measured in.
        ///
        /// Results are not clamped. A point outside the bounds gives a value past
        /// -1 / 1, which is the honest answer - bounded offsets simply cannot
        /// reach it - so the caller can say so rather than silently landing
        /// somewhere else. A flat axis has no span to slide along, so it reports
        /// 0: the bounds' own center, which is the only position it has.
        /// </summary>
        internal static Vector3 GetBoundedValuesForPoint(Bounds bounds, Vector3 point)
        {
            Vector3 extents = bounds.extents;
            Vector3 delta = point - bounds.center;

            return new Vector3(
                extents.x != 0f ? delta.x / extents.x : 0f,
                extents.y != 0f ? delta.y / extents.y : 0f,
                extents.z != 0f ? delta.z / extents.z : 0f);
        }

        internal static Vector3 GetScaleRatio(Vector3 oldScale, Vector3 newScale)
        {
            return new Vector3(
                oldScale.x != 0f ? newScale.x / oldScale.x : 1f,
                oldScale.y != 0f ? newScale.y / oldScale.y : 1f,
                oldScale.z != 0f ? newScale.z / oldScale.z : 1f);
        }

        internal static Quaternion GetSafeRotation(Quaternion rotation)
        {
            float magnitudeSquared = rotation.x * rotation.x + rotation.y * rotation.y +
                                     rotation.z * rotation.z + rotation.w * rotation.w;

            if (float.IsNaN(magnitudeSquared) || float.IsInfinity(magnitudeSquared) ||
                magnitudeSquared < 0.000001f)
                return Quaternion.identity;

            return Quaternion.Normalize(rotation);
        }

        /// <summary>
        /// Scales a vector along the (world-space) axes of the given handle rotation.
        /// </summary>
        internal static Vector3 ApplyWorldScaleToVector(Vector3 v, Quaternion handleRotation, Vector3 ratio)
        {
            Matrix4x4 r = Matrix4x4.Rotate(handleRotation);
            Matrix4x4 m = r * Matrix4x4.Scale(ratio) * r.transpose;
            return m.MultiplyVector(v);
        }

        /// <summary>
        /// Applies a scale ratio expressed in a rotated pivot frame back onto a
        /// transform's local scale (diagonal of the conjugated scale matrix).
        /// </summary>
        internal static Vector3 ConjugateLocalScale(Vector3 oldLocalScale, Quaternion offsetRotation, Vector3 ratio)
        {
            Matrix4x4 q = Matrix4x4.Rotate(offsetRotation);
            Matrix4x4 m = q * Matrix4x4.Scale(ratio) * q.transpose;
            return new Vector3(
                m.GetColumn(0).x * oldLocalScale.x,
                m.GetColumn(1).y * oldLocalScale.y,
                m.GetColumn(2).z * oldLocalScale.z);
        }

        /// <summary>
        /// A position far outside the camera frustum. Handles that must stay hidden
        /// are drawn here instead of being skipped, so every Handles.* call happens
        /// in the same order each frame and IMGUI control ids stay stable.
        /// </summary>
        internal static Vector3 GetOffscreenHandlePosition()
        {
            Camera sceneCamera = Camera.current;
            if (sceneCamera == null)
                return new Vector3(100000f, 100000f, 100000f);

            float distance = Mathf.Max(sceneCamera.nearClipPlane * 2f, 1f);
            return sceneCamera.ViewportToWorldPoint(new Vector3(-100f, -100f, distance));
        }
    }
}
