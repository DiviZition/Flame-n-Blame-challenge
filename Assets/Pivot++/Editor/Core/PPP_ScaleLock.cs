using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Bridge to the Transform inspector's "constrain proportions" toggle (the
    /// little chain link next to Scale).
    ///
    /// Unity's built-in scale tool reads that flag: when it is on the axis handles
    /// are drawn white and dragging any of them scales every axis by the same
    /// ratio. Our replacement handle has to do the same, otherwise turning the lock
    /// on appears to do nothing once you leave the inspector.
    ///
    /// Both halves of that behaviour are internal to UnityEditor
    /// (Selection.DoAllGOsHaveConstrainProportionsEnabled and the isProportionalScale
    /// overload of Handles.DoScaleHandle), so they are bound by reflection once and
    /// cached as delegates. If a future Unity version renames them we fall back to
    /// the public scale handle plus a manually uniformed ratio: the handles then
    /// look unlocked, but the lock still constrains the scaling.
    ///
    /// Note that Unity's own Transform (combined) tool passes isProportionalScale:false,
    /// i.e. the scale lock is deliberately ignored there - so this is only used by
    /// the Scale tool, matching stock behaviour.
    /// </summary>
    internal static class PPP_ScaleLock
    {
        private delegate Vector3 ProportionalScaleHandleDelegate(Vector3 scale, Vector3 position, Quaternion rotation,
            float size, bool isProportionalScale);

        private delegate bool AllConstrainedDelegate(UnityEngine.Object[] objects);

        private static readonly ProportionalScaleHandleDelegate _proportionalScaleHandle;
        private static readonly AllConstrainedDelegate _allConstrained;
        private static readonly PropertyInfo _transformFlag;

        /// <summary>Scratch buffer so the per-frame state query doesn't allocate.</summary>
        private static UnityEngine.Object[] _queryBuffer = new UnityEngine.Object[1];

        /// <summary>Lock state latched at drag start, so toggling mid-drag can't change the basis.</summary>
        private static bool _dragLocked;

        /// <summary>Absolute ratio (relative to the scale at drag start) already applied this drag.</summary>
        private static Vector3 _appliedRatio = Vector3.one;

        static PPP_ScaleLock()
        {
            const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            MethodInfo doScaleHandle = typeof(Handles).GetMethod("DoScaleHandle", staticFlags, null,
                new[] { typeof(Vector3), typeof(Vector3), typeof(Quaternion), typeof(float), typeof(bool) }, null);
            if (doScaleHandle != null)
                _proportionalScaleHandle = Delegate.CreateDelegate(typeof(ProportionalScaleHandleDelegate),
                    doScaleHandle, false) as ProportionalScaleHandleDelegate;

            MethodInfo allConstrained = typeof(Selection).GetMethod("DoAllGOsHaveConstrainProportionsEnabled",
                staticFlags, null, new[] { typeof(UnityEngine.Object[]) }, null);
            if (allConstrained != null)
                _allConstrained = Delegate.CreateDelegate(typeof(AllConstrainedDelegate),
                    allConstrained, false) as AllConstrainedDelegate;

            _transformFlag = typeof(Transform).GetProperty("constrainProportionsScale",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        internal static bool IsEnabled(Transform target)
        {
            if (target == null)
                return false;

            if (_queryBuffer.Length != 1)
                _queryBuffer = new UnityEngine.Object[1];
            _queryBuffer[0] = target.gameObject;

            return IsEnabled(_queryBuffer);
        }

        /// <summary>
        /// True when every target has the scale lock on. Matching Unity, a mixed
        /// selection counts as unlocked.
        /// </summary>
        internal static bool IsEnabled(Transform[] targets)
        {
            if (targets == null || targets.Length == 0)
                return false;

            if (_queryBuffer.Length != targets.Length)
                _queryBuffer = new UnityEngine.Object[targets.Length];

            for (int i = 0; i < targets.Length; i++)
                _queryBuffer[i] = targets[i] != null ? targets[i].gameObject : null;

            return IsEnabled(_queryBuffer);
        }

        private static bool IsEnabled(UnityEngine.Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null)
                    return false;
            }

            if (_allConstrained != null)
                return _allConstrained(objects);

            if (_transformFlag == null)
                return false;

            // Fallback path: ask each transform directly.
            for (int i = 0; i < objects.Length; i++)
            {
                Transform transform = ((GameObject)objects[i]).transform;
                if (!(bool)_transformFlag.GetValue(transform))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Draws the scale handle and reports the ratio to apply this frame, or
        /// false when the handle wasn't dragged.
        ///
        /// The two lock states need different bases. Unlocked, the per-axis sliders
        /// hand back the scale they were given adjusted by the drag, so passing the
        /// live localScale and diffing against it yields the per-frame delta - the
        /// path that already worked, kept as-is.
        ///
        /// Locked, Unity's proportional handle does not return a value in the basis
        /// it was handed: it rebuilds the whole vector from its own drag-start
        /// reference so every axis moves together. Diffing that against a localScale
        /// we have already been mutating compares two different bases, and the error
        /// is fed back in every frame, so a small drag compounds into a runaway
        /// scale (worst with a non-uniform or non-1 starting scale). So for a locked
        /// drag the handle is driven from a unit basis instead: its return value is
        /// then exactly the absolute ratio since the drag began, and only the part
        /// not yet applied is handed on. Handle rendering is unaffected - the arms
        /// are drawn from the drag-relative ratio, not from the absolute scale.
        /// </summary>
        internal static bool TryDragScale(Vector3 currentScale, Vector3 position, Quaternion rotation, float size,
            bool scaleLocked, out Vector3 ratio)
        {
            // Between drags: re-latch the lock state and drop the accumulated ratio.
            if (GUIUtility.hotControl == 0)
            {
                _dragLocked = scaleLocked;
                _appliedRatio = Vector3.one;
            }

            bool locked = _dragLocked;
            Vector3 basisScale = locked ? Vector3.one : currentScale;

            EditorGUI.BeginChangeCheck();
            Vector3 newScale = ScaleHandle(basisScale, position, rotation, size, locked);
            if (!EditorGUI.EndChangeCheck())
            {
                ratio = Vector3.one;
                return false;
            }

            if (!locked)
            {
                ratio = PPP_PivotMath.GetScaleRatio(currentScale, newScale);
                return IsUsable(ratio);
            }

            // newScale is the absolute ratio since drag start; take out what has
            // already been applied so the caller still receives a per-frame delta.
            Vector3 absolute = ConstrainRatio(Sanitize(newScale));
            ratio = PPP_PivotMath.GetScaleRatio(_appliedRatio, absolute);
            if (!IsUsable(ratio))
                return false;

            _appliedRatio = absolute;
            return true;
        }

        /// <summary>
        /// Replaces components that would corrupt a transform (NaN, infinity, or a
        /// collapse to zero that can never be scaled back up) with 1.
        /// </summary>
        private static Vector3 Sanitize(Vector3 ratio)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                float value = ratio[axis];
                if (float.IsNaN(value) || float.IsInfinity(value) || Mathf.Abs(value) < 0.00001f)
                    ratio[axis] = 1f;
            }

            return ratio;
        }

        private static bool IsUsable(Vector3 ratio)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                float value = ratio[axis];
                if (float.IsNaN(value) || float.IsInfinity(value) || Mathf.Abs(value) < 0.00001f)
                    return false;
            }

            return ratio != Vector3.one;
        }

        /// <summary>
        /// Draws the scale handle. When <paramref name="scaleLocked"/> is true this
        /// is the same call Unity's scale tool makes, so the handles render white
        /// and drag proportionally.
        /// </summary>
        private static Vector3 ScaleHandle(Vector3 scale, Vector3 position, Quaternion rotation, float size,
            bool scaleLocked)
        {
            if (_proportionalScaleHandle != null)
                return _proportionalScaleHandle(scale, position, rotation, size, scaleLocked);

            return Handles.ScaleHandle(scale, position, rotation, size);
        }

        /// <summary>
        /// Forces a scale ratio to be uniform by taking the axis that moved furthest
        /// from 1. A no-op when the ratio is already uniform (which it is whenever
        /// Unity's proportional handle produced it), so this doubles as the safety
        /// net for the fallback draw path.
        /// </summary>
        private static Vector3 ConstrainRatio(Vector3 ratio)
        {
            float uniform = 1f;
            float largestDelta = 0f;

            for (int axis = 0; axis < 3; axis++)
            {
                float value = ratio[axis];
                if (float.IsNaN(value) || float.IsInfinity(value))
                    continue;

                float delta = Mathf.Abs(value - 1f);
                if (delta <= largestDelta)
                    continue;

                largestDelta = delta;
                uniform = value;
            }

            return new Vector3(uniform, uniform, uniform);
        }
    }
}
