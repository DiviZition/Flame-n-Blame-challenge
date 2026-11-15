using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Computes the mesh bounds the pivot offsets are expressed in: cached local
    /// bounds for a single target, and uncached combined bounds for a
    /// multi-selection (recomputed in the handle's rotated reference frame).
    /// </summary>
    internal static class PPP_Bounds
    {
        private struct TransformSnapshot
        {
            public Transform target;
            public Transform parent;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;

            public TransformSnapshot(Transform t)
            {
                target = t;
                parent = t.parent;
                localPosition = t.localPosition;
                localRotation = t.localRotation;
                localScale = t.localScale;
            }

            public bool IsUnchanged()
            {
                return target != null
                       && parent == target.parent
                       && localPosition == target.localPosition
                       && localRotation == target.localRotation
                       && localScale == target.localScale;
            }
        }

        private class LocalBoundsCache
        {
            public bool valid;
            public Bounds bounds;
            public bool hasBounds;
            public TransformSnapshot[] watchedSnapshots;
            public readonly List<IMeshBoundsProvider> providers = new List<IMeshBoundsProvider>();
        }

        private static readonly Dictionary<Transform, LocalBoundsCache> _localBoundsCache =
            new Dictionary<Transform, LocalBoundsCache>();

        private static readonly Func<MeshFilter, Mesh> _getFilterMesh = f => f.sharedMesh;
        private static readonly Func<SkinnedMeshRenderer, Mesh> _getSkinnedMesh = r => r.sharedMesh;

        /// <summary>
        /// Drops every cached entry. Called on undo/redo and hierarchy changes,
        /// where anything about the watched meshes may have changed.
        /// </summary>
        internal static void InvalidateAll()
        {
            _localBoundsCache.Clear();
        }

        internal static bool TryGetLocalBounds(Transform target, out Bounds bounds)
        {
            if (target == null)
            {
                bounds = default;
                return false;
            }

            if (!_localBoundsCache.TryGetValue(target, out LocalBoundsCache cache))
            {
                cache = new LocalBoundsCache();
                _localBoundsCache[target] = cache;
            }

            if (!cache.valid || IsStale(cache))
                Rebuild(target, cache);

            bounds = cache.bounds;
            return cache.hasBounds;
        }

        internal static bool TryGetCombinedBounds(Transform[] targets, Quaternion referenceRotation, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            Matrix4x4 worldToReference = Matrix4x4.Rotate(Quaternion.Inverse(referenceRotation));

            foreach (Transform target in targets)
            {
                if (target == null || !TryGetLocalBounds(target, out Bounds localBounds))
                    continue;

                Matrix4x4 targetLocalToReference = worldToReference * target.localToWorldMatrix;
                EncapsulateTransformedBounds(ref bounds, ref hasBounds, localBounds, targetLocalToReference);
            }

            return hasBounds;
        }

        private static bool IsStale(LocalBoundsCache cache)
        {
            TransformSnapshot[] watchedSnapshots = cache.watchedSnapshots;
            foreach (TransformSnapshot snapshot in watchedSnapshots)
            {
                if (!snapshot.IsUnchanged())
                    return true;
            }

            foreach (IMeshBoundsProvider provider in cache.providers)
            {
                if (provider.HasChanged)
                    return true;
            }

            return false;
        }

        private static void Rebuild(Transform target, LocalBoundsCache cache)
        {
            Bounds bounds = default;
            bool hasBounds = false;
            Matrix4x4 worldToTargetLocal = target.worldToLocalMatrix;
            cache.providers.Clear();

            Transform[] watchedTransforms = target.GetComponentsInChildren<Transform>();
            TransformSnapshot[] watchedSnapshots = new TransformSnapshot[watchedTransforms.Length];
            int index = 0;

            foreach (Transform tt in watchedTransforms) // tt = target transform
            {
                watchedSnapshots[index] = new TransformSnapshot(tt);
                IMeshBoundsProvider provider = GetBoundsProviders(tt);
                if (provider != null)
                {
                    cache.providers.Add(provider);
                    Matrix4x4 meshToReference = worldToTargetLocal * tt.localToWorldMatrix;
                    if (provider.TryGetBounds(out Bounds providerBounds))
                        EncapsulateTransformedBounds(ref bounds, ref hasBounds, providerBounds, meshToReference);
                }
                
                index++;
            }

            cache.valid = true;
            cache.bounds = bounds;
            cache.hasBounds = hasBounds;
            cache.watchedSnapshots = watchedSnapshots;
        }
        
        private static void EncapsulateTransformedBounds(ref Bounds bounds, ref bool hasBounds, Bounds localBounds, Matrix4x4 matrix)
        {
            Vector3 center = localBounds.center;
            Vector3 extents = localBounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 transformedCorner = matrix.MultiplyPoint3x4(corner);

                        if (!hasBounds)
                        {
                            bounds = new Bounds(transformedCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(transformedCorner);
                        }
                    }
                }
            }
        }

        private static IMeshBoundsProvider GetBoundsProviders(Transform target)
        {
            if (target.TryGetComponent(out MeshFilter meshFilter)) return new MeshRendererBoundsProvider(meshFilter);
            if (target.TryGetComponent(out SpriteRenderer spriteRenderer)) return new SpriteRendererBoundsProvider(spriteRenderer);
            if (target.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer)) return new SkinnedMeshRendererBoundsProvider(skinnedMeshRenderer);
            
#if UNITY_TMPRO
            if (target.TryGetComponent(out TMPro.TMP_Text tmpText)) return new TMP_TextBoundsProvider(tmpText);
#endif

#if UNITY_UI
            if (target.TryGetComponent(out Text legacyText)) return new LegacyTextBoundsProvider(legacyText);
            if (target.TryGetComponent(out ILayoutElement layoutElement)) return new LayoutElementBoundsProvider(layoutElement as MonoBehaviour);
#endif

            if (target.TryGetComponent(out RectTransform rectTransform)) return new RectTransformBoundsProvider(rectTransform);

#if UNITY_PHYSICS
            if (target.TryGetComponent(out Collider collider)) return new ColliderBoundsProvider(collider);
#endif
        
#if UNITY_PHYSICS_2D
            if (target.TryGetComponent(out Collider2D collider2D)) return new Collider2DBoundsProvider(collider2D);
#endif
            return null;
        }
    }
}
