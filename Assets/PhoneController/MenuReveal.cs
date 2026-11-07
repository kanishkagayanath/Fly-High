using DG.Tweening;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Fades a menu layer while scaling only its contents, leaving the viewport backdrop in place.</summary>
    internal sealed class MenuReveal : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform content;
        private Vector3 restingScale;
        private Sequence transition;

        internal bool IsShown { get; private set; }

        internal static MenuReveal Attach(GameObject layer, RectTransform content = null)
        {
            var reveal = layer.AddComponent<MenuReveal>();
            reveal.group = layer.AddComponent<CanvasGroup>();
            reveal.content = content != null ? content : (RectTransform)layer.transform;
            reveal.restingScale = reveal.content.localScale;
            if (layer.activeInHierarchy) reveal.Enter();
            return reveal;
        }

        internal void SetVisible(bool visible)
        {
            if (visible == IsShown) return;
            if (visible && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                return;
            }

            IsShown = visible;
            Animate();
        }

        private void OnEnable()
        {
            if (group != null) Enter();
        }

        private void Enter()
        {
            IsShown = true;
            group.alpha = 0;
            content.localScale = restingScale * .965f;
            Animate();
        }

        private void Animate()
        {
            transition?.Kill();
            group.blocksRaycasts = group.interactable = IsShown;
            float duration = IsShown ? .24f : .14f;
            Ease ease = IsShown ? Ease.OutCubic : Ease.InQuad;

            transition = DOTween.Sequence().SetUpdate(true)
                .Join(group.DOFade(IsShown ? 1f : 0f, duration).SetEase(ease))
                .Join(content.DOScale(restingScale * (IsShown ? 1f : .98f), duration).SetEase(ease))
                .OnComplete(() =>
                {
                    transition = null;
                    if (!IsShown) gameObject.SetActive(false);
                });
        }

        private void OnDisable()
        {
            transition?.Kill();
            transition = null;
            IsShown = false;
            if (group != null) group.alpha = 1;
            if (content != null) content.localScale = restingScale;
        }
    }
}
