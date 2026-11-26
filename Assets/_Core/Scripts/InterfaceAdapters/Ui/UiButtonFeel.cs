using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Thin shell for one button's feel: hover and press are pointer facts Unity reports, and the whole body
    /// is scale tweens. No rules, no Update() (the perf budget lives in TODO.md).
    ///
    /// It sits on the button's own GameObject: uGUI hands the pointer fact to the first handler it finds
    /// walking up from the raycasted object, and a Button is already a handler (G-042).
    public class UiButtonFeel : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Scale the button grows to while the pointer is on it."), SerializeField]
        private float _hoverScale = 1.06f;

        [Tooltip("Scale the button is squeezed to on press."), SerializeField]
        private float _pressScale = 0.94f;

        private Tween _scale;

        public void OnPointerEnter(PointerEventData data)
        {
            ScaleTo(_hoverScale, 0.12f, Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData data)
        {
            ScaleTo(1f, 0.12f, Ease.OutQuad);
        }

        public void OnPointerDown(PointerEventData data)
        {
            ScaleTo(_pressScale, 0.06f, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData data)
        {
            ScaleTo(_hoverScale, 0.1f, Ease.OutQuad);
        }

        private void ScaleTo(float target, float duration, Ease ease)
        {
            _scale.Stop();
            _scale = Tween.Scale(transform, transform.localScale.x, target, duration, ease);
        }
    }
}
