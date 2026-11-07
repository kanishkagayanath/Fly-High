using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace FlyHigh.PhoneController
{
    /// <summary>Tweens button artwork over a fixed input area.</summary>
    public sealed class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private RectTransform visual;
        private UnityEngine.UI.Button button;
        private Tween motion;
        private bool hovered, selected;
        private int? pressedPointer;
        private float targetScale = 1;

        public void Initialize(RectTransform target)
        {
            visual = target;
            button = GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic.raycastTarget = false;
            var hitArea = gameObject.AddComponent<UnityEngine.UI.Image>();
            hitArea.color = Color.clear;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (e is ExtendedPointerEventData input && input.pointerType == UIPointerType.Touch) return;
            hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (!(e is ExtendedPointerEventData input && input.pointerType == UIPointerType.Touch))
                hovered = false;
            Release(e);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !button.IsInteractable() || pressedPointer.HasValue) return;
            pressedPointer = e.pointerId;
            Refresh();
        }

        public void OnPointerUp(PointerEventData e) => Release(e);

        public void OnSelect(BaseEventData e)
        {
            selected = !(e is PointerEventData);
            Refresh();
        }

        public void OnDeselect(BaseEventData e)
        {
            selected = false;
            Refresh();
        }

        private void Release(PointerEventData e)
        {
            if (pressedPointer == e.pointerId) pressedPointer = null;
            Refresh();
        }

        private void Refresh()
        {
            if (visual == null || !isActiveAndEnabled) return;
            bool interactive = button.IsInteractable();
            if (!interactive) pressedPointer = null;
            float scale = !interactive ? 1f : pressedPointer.HasValue ? .965f : hovered || selected ? 1.045f : 1f;
            if (scale == targetScale) return;
            targetScale = scale;
            motion?.Kill();
            motion = visual.DOScale(scale, pressedPointer.HasValue ? .09f : .16f)
                .SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => motion = null);
        }

        private void Update()
        {
            // Networked menu buttons can become disabled while a pointer stays on them.
            if (targetScale != 1f && !button.IsInteractable()) Refresh();
        }

        private void OnDisable()
        {
            motion?.Kill();
            motion = null;
            hovered = selected = false;
            pressedPointer = null;
            targetScale = 1f;
            if (visual != null) visual.localScale = Vector3.one;
        }
    }
}
