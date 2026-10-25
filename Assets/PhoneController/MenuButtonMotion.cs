using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyHigh.PhoneController
{
    /// <summary>Presentation only; the clickable root never moves or changes size.</summary>
    public sealed class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private RectTransform visual;
        private bool hovered, selected, pressed;
        private float scale = 1;
        public void Initialize(RectTransform target) { visual = target; }
        private void Update()
        {
            if (visual == null) return;
            float target = pressed ? .975f : hovered || selected ? 1.035f : 1;
            scale = Mathf.Lerp(scale, target, 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
            if (Mathf.Abs(scale-target)<.0001f) scale=target;
            visual.localScale = Vector3.one * scale;
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; }
        public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        public void OnSelect(BaseEventData e) { selected = true; }
        public void OnDeselect(BaseEventData e) { selected = false; pressed = false; }
        private void OnEnable() { hovered = pressed = false; scale = 1; if(visual!=null)visual.localScale=Vector3.one; }
        private void OnDisable() { hovered = selected = pressed = false; scale = 1; if(visual!=null)visual.localScale=Vector3.one; }
    }
}
