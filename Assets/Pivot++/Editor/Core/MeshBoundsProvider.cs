using UnityEngine;
using UnityEngine.UI;

namespace PivotPlusPlus.EditorStuff
{
    public interface IMeshBoundsProvider
    {
        bool HasChanged { get; }
        bool TryGetBounds(out Bounds bounds);
    }

    internal static class UIBoundsUtility
    {
        public static Bounds BoundsForSize(RectTransform rectTransform, Vector2 size)
        {
            Vector3 center = new Vector3(
                (0.5f - rectTransform.pivot.x) * size.x,
                (0.5f - rectTransform.pivot.y) * size.y,
                0);
            return new Bounds(center, new Vector3(size.x, size.y, 0));
        }
    }

    public class MeshRendererBoundsProvider : IMeshBoundsProvider
    {
        private readonly MeshFilter _meshFilter;
        private Mesh _lastMesh;
        public bool HasChanged => _meshFilter == null || _meshFilter.sharedMesh != _lastMesh;

        public MeshRendererBoundsProvider(MeshFilter meshFilter)
        {
            _meshFilter = meshFilter;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (_meshFilter == null)
            {
                bounds = new Bounds();
                return false;
            }
            
            
            _lastMesh = _meshFilter.sharedMesh;
            if (_lastMesh != null && _meshFilter.TryGetComponent(out MeshRenderer renderer) && renderer.enabled)
            {
                bounds = _lastMesh.bounds;
                return true;
            }

            bounds = new Bounds();
            return false;
        }
    }

    public class SkinnedMeshRendererBoundsProvider : IMeshBoundsProvider
    {
        private readonly SkinnedMeshRenderer _skinnedMeshRenderer;
        private Mesh _lastMesh;
        public bool HasChanged => _skinnedMeshRenderer == null || _skinnedMeshRenderer.sharedMesh != _lastMesh;

        public SkinnedMeshRendererBoundsProvider(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            _skinnedMeshRenderer = skinnedMeshRenderer;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (_skinnedMeshRenderer == null || !_skinnedMeshRenderer.enabled)
            {
                bounds = new Bounds();
                return false;
            }
            
            _lastMesh = _skinnedMeshRenderer.sharedMesh;
            if (_lastMesh != null)
            {
                bounds = _lastMesh.bounds;
                return true;
            }

            bounds = new Bounds();
            return false;
        }
    }
    
    public class SpriteRendererBoundsProvider : IMeshBoundsProvider
    {
        private readonly SpriteRenderer _spriteRenderer;
        private Sprite _lastSprite;
        public bool HasChanged => _spriteRenderer == null || _spriteRenderer.sprite != _lastSprite;

        public SpriteRendererBoundsProvider(SpriteRenderer spriteRenderer)
        {
            _spriteRenderer = spriteRenderer;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (_spriteRenderer == null || !_spriteRenderer.enabled)
            {
                bounds = new Bounds();
                return false;
            }

            _lastSprite = _spriteRenderer.sprite;
            if (_lastSprite != null)
            {
                bounds = _lastSprite.bounds;
                return true;
            }

            bounds = new Bounds();
            return false;
        }
    }

    public class RectTransformBoundsProvider : IMeshBoundsProvider
    {
        private readonly RectTransform _rectTransform;
        private CanvasBoundsMode _lastBoundsMode;

        public bool HasChanged => _lastBoundsMode != PPP_Settings.CanvasBoundsMode;

        public RectTransformBoundsProvider(RectTransform rectTransform)
        {
            _rectTransform = rectTransform;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (_rectTransform == null)
            {
                bounds = new Bounds();
                return false;
            }

            _lastBoundsMode = PPP_Settings.CanvasBoundsMode;
            var size = _rectTransform.rect.size;

#if UNITY_UI
            if (_lastBoundsMode != CanvasBoundsMode.RectTransform)
            {
                var layoutSize = _lastBoundsMode == CanvasBoundsMode.MinimumSize
                    ? new Vector2(LayoutUtility.GetMinWidth(_rectTransform), LayoutUtility.GetMinHeight(_rectTransform))
                    : new Vector2(LayoutUtility.GetPreferredWidth(_rectTransform), LayoutUtility.GetPreferredHeight(_rectTransform));

                if (layoutSize.x > 0) size.x = layoutSize.x;
                if (layoutSize.y > 0) size.y = layoutSize.y;
            }
#endif

            bounds = UIBoundsUtility.BoundsForSize(_rectTransform, size);
            return true;
        }
    }
    
#if UNITY_PHYSICS
    public class ColliderBoundsProvider : IMeshBoundsProvider
    {
        private readonly Collider _collider;
        private Bounds _lastBounds;

        public bool HasChanged
        {
            get
            {
                if (_collider == null) return true;
                return _lastBounds != ComputeBounds();
            }
        }

        public ColliderBoundsProvider(Collider collider)
        {
            _collider = collider;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (_collider == null || !_collider.enabled)
            {
                bounds = new Bounds();
                return false;
            }

            bounds = ComputeBounds();
            _lastBounds = bounds;
            return true;
        }

        private Bounds ComputeBounds()
        {
            switch (_collider)
            {
                case BoxCollider box:
                    return new Bounds(box.center, box.size);
                case SphereCollider sphere:
                    return new Bounds(sphere.center, Vector3.one * sphere.radius * 2);
                case CapsuleCollider capsule:
                {
                    var size = Vector3.one * capsule.radius * 2;
                    size[capsule.direction] = capsule.height;
                    return new Bounds(capsule.center, size);
                }
                case MeshCollider meshCollider:
                    return meshCollider.sharedMesh != null ? meshCollider.sharedMesh.bounds : new Bounds();
                case CharacterController controller:
                    return new Bounds(controller.center, new Vector3(controller.radius * 2, controller.height, controller.radius * 2));
                default:
                    Debug.LogError($"[ColliderBoundsProvider.ComputeBounds] Unsupported collider type: {_collider.GetType()}");
                    return new Bounds();
            }

        }
    }
#endif

#if UNITY_PHYSICS_2D
    public class Collider2DBoundsProvider : IMeshBoundsProvider
    {
        private readonly Collider2D _collider;
        private Bounds _lastBounds;

        public bool HasChanged
        {
            get
            {
                if (_collider == null)
                {
                    return true;
                }

                return _lastBounds != ComputeBounds();
            }
        }

        public Collider2DBoundsProvider(Collider2D collider)
        {
            _collider = collider;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (_collider == null || !_collider.enabled) 
            {
                bounds = new Bounds();
                return false;
            }

            bounds = ComputeBounds();
            _lastBounds = bounds;
            return true;
        }

        private Bounds ComputeBounds()
        {
            if (_collider is BoxCollider2D box)
            {
                return new Bounds(box.offset, box.size + Vector2.one * box.edgeRadius);
            }

            if (_collider is CircleCollider2D circle)
            {
                return new Bounds(circle.offset, Vector2.one * circle.radius * 2);
            }

            if (_collider is CapsuleCollider2D capsule)
            {
                return new Bounds(capsule.offset, capsule.size);
            }

            Debug.LogError($"[Collider2DBoundsProvider.ComputeBounds] Unsupported collider type: {_collider.GetType()}");
            return new Bounds();
        }
    }
#endif

#if UNITY_UI
    public class LayoutElementBoundsProvider : IMeshBoundsProvider
    {
        private readonly MonoBehaviour _layoutElement;
        private readonly RectTransform _rectTransform;
        private CanvasBoundsMode _lastBoundsMode;

        public bool HasChanged => _lastBoundsMode != PPP_Settings.CanvasBoundsMode;

        public LayoutElementBoundsProvider(MonoBehaviour layoutElement)
        {
            _layoutElement = layoutElement;
            _rectTransform = layoutElement.transform as RectTransform;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (_layoutElement == null || _rectTransform == null || !_layoutElement.enabled)
            {
                bounds = new Bounds();
                return false;
            }

            var le = _layoutElement as UnityEngine.UI.ILayoutElement;
            _lastBoundsMode = PPP_Settings.CanvasBoundsMode;
            Vector2 size;
            switch (_lastBoundsMode)
            {
                case CanvasBoundsMode.MinimumSize:
                    size = new Vector2(le.minWidth, le.minHeight);
                    break;
                case CanvasBoundsMode.PreferredSize:
                    size = new Vector2(le.preferredWidth, le.preferredHeight);
                    break;
                default: // RectTransform: use the laid-out rect directly.
                    size = _rectTransform.rect.size;
                    break;
            }

            // minWidth/preferredWidth return -1 when the layout element leaves the
            // dimension unconstrained; fall back to the actual rect in that case.
            if (size.x < 0) size.x = _rectTransform.rect.size.x;
            if (size.y < 0) size.y = _rectTransform.rect.size.y;

            var center = new Vector3(
                (0.5f - _rectTransform.pivot.x) * size.x,
                (0.5f - _rectTransform.pivot.y) * size.y,
                0);

            bounds = new Bounds(center, new Vector3(size.x, size.y, 0));
            return true;
        }
    }
    
    public class LegacyTextBoundsProvider : IMeshBoundsProvider
    {
        private readonly UnityEngine.UI.Text _text;
        private CanvasBoundsMode _lastBoundsMode;
        private string _lastText;

        public bool HasChanged => _lastBoundsMode != PPP_Settings.CanvasBoundsMode || _text == null || _text.text != _lastText;

        public LegacyTextBoundsProvider(UnityEngine.UI.Text text)
        {
            _text = text;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (_text == null || !_text.enabled)
            {
                bounds = new Bounds();
                return false;
            }
            
            _lastBoundsMode = PPP_Settings.CanvasBoundsMode;
            _lastText = _text.text;

            if (_lastBoundsMode == CanvasBoundsMode.RectTransform)
            {
                bounds = UIBoundsUtility.BoundsForSize(_text.rectTransform, _text.rectTransform.rect.size);
                return true;
            }

            // Text has no distinct minimum layout size, so Minimum reuses the
            // preferred glyph extents from the layout text generator.
            var settings = _text.GetGenerationSettings(Vector2.zero);
            var generator = _text.cachedTextGeneratorForLayout;
            var size = new Vector2(
                generator.GetPreferredWidth(_text.text, settings),
                generator.GetPreferredHeight(_text.text, settings));

            bounds = new Bounds(Vector3.zero, new Vector3(size.x, size.y, 0));
            return true;
        }
    }
#endif

#if UNITY_TMPRO
    public class TMP_TextBoundsProvider : IMeshBoundsProvider
    {
        private readonly TMPro.TMP_Text _text;
        private CanvasBoundsMode _lastBoundsMode;
        private string _lastText;

        public bool HasChanged => _lastBoundsMode != PPP_Settings.CanvasBoundsMode || _text == null || _text.text != _lastText;

        public TMP_TextBoundsProvider(TMPro.TMP_Text text)
        {
            _text = text;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (_text == null || !_text.enabled)
            {
                bounds = new Bounds();
                return false;
            }

            _lastText = _text.text;
            _lastBoundsMode = PPP_Settings.CanvasBoundsMode;

            if (_lastBoundsMode == CanvasBoundsMode.RectTransform)
            {
                bounds = UIBoundsUtility.BoundsForSize(_text.rectTransform, _text.rectTransform.rect.size);
                return true;
            }

            // TMP exposes no separate minimum size, so Minimum reuses the
            // preferred values.
            var size = _text.GetPreferredValues();
            bounds = new Bounds(Vector3.zero, new Vector3(size.x, size.y, 0));
            return true;
        }
    }
#endif
}
