using UnityEngine;
using UnityEngine.UI;

namespace FlyHigh.PhoneController
{
    /// <summary>Covers the viewport behind a menu whose controls live inside the safe area.</summary>
    internal sealed class ViewportBackdrop : MonoBehaviour
    {
        private RectTransform viewport;
        private RectTransform layer;
        private RectTransform surface;

        internal static void Create(RectTransform layer, Color tint, Texture2D artwork = null)
        {
            var surface = PhoneUi.Rect(layer, "Viewport backdrop", 0, 0, 1, 1);
            surface.SetAsFirstSibling();
            surface.gameObject.AddComponent<UnityEngine.UI.Image>().color = tint;

            var canvas = layer.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var backdrop = surface.gameObject.AddComponent<ViewportBackdrop>();
                backdrop.viewport = (RectTransform)canvas.rootCanvas.transform;
                backdrop.layer = layer;
                backdrop.surface = surface;
                backdrop.CoverViewport();
            }

            if (artwork == null) return;
            var art = PhoneUi.Rect(surface, "Full menu artwork", 0, 0, 1, 1);
            // On wide screens the extra image height extends below the screen, keeping the logo in view.
            art.pivot = new Vector2(.5f, 1f);
            art.offsetMin = art.offsetMax = Vector2.zero;
            var image = art.gameObject.AddComponent<RawImage>();
            image.texture = artwork;
            image.raycastTarget = false;
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectRatio = (float)artwork.width / artwork.height;
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }

        private void OnEnable() => CoverViewport();

        private void LateUpdate() => CoverViewport();

        private void CoverViewport()
        {
            if (viewport == null || layer == null || surface == null) return;

            Rect bounds = viewport.rect;
            Vector2 min = layer.InverseTransformPoint(viewport.TransformPoint(bounds.min));
            Vector2 max = layer.InverseTransformPoint(viewport.TransformPoint(bounds.max));
            Vector2 offsetMin = min - layer.rect.min;
            Vector2 offsetMax = max - layer.rect.max;
            if (surface.offsetMin != offsetMin) surface.offsetMin = offsetMin;
            if (surface.offsetMax != offsetMax) surface.offsetMax = offsetMax;
        }
    }
}
