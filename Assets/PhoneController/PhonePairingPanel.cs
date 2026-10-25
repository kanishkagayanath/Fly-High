using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Small phone-only LAN picker; existing manual connection and pedals stay available.</summary>
    public sealed class PhonePairingPanel : MonoBehaviour
    {
        private PhoneSender sender;
        private TMP_InputField address, code;
        private RectTransform overlay;
        private TMP_Text searchStatus;
        private readonly UnityEngine.UI.Button[] rows = new UnityEngine.UI.Button[ControllerDiscovery.MaximumGames];
        private readonly TMP_Text[] labels = new TMP_Text[ControllerDiscovery.MaximumGames];
        private int revision = -1;

        public void Initialize(RectTransform parent, PhoneSender phone, TMP_InputField addressField, TMP_InputField codeField)
        {
            sender = phone; address = addressField; code = codeField;
            var find = Button(parent, "FIND PC", new Vector2(.06f, .47f), new Vector2(.33f, .59f));
            find.onClick.AddListener(Open);
            overlay = Rect(parent, "Choose PC", Vector2.zero, Vector2.one);
            overlay.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.035f, .075f, .11f, 1);
            Text(overlay, "CONNECT TO YOUR PC", new Vector2(.06f, .82f), new Vector2(.94f, .96f), 36);
            searchStatus = Text(overlay, "", new Vector2(.06f, .70f), new Vector2(.94f, .82f), 24);

            var scrollRoot = Rect(overlay, "PC list", new Vector2(.06f, .17f), new Vector2(.94f, .69f));
            var scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport = Rect(scrollRoot, "Viewport", Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.white;
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var content = Rect(viewport, "Content", new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 10; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                rows[i] = Button(content, "PC", Vector2.zero, Vector2.one);
                rows[i].gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 72;
                labels[i] = rows[i].GetComponentInChildren<TMP_Text>();
                labels[i].fontSize = 26;
                rows[i].onClick.AddListener(() => Choose(index));
                rows[i].gameObject.SetActive(false);
            }
            Button(overlay, "SEARCH AGAIN", new Vector2(.06f, .04f), new Vector2(.48f, .13f)).onClick.AddListener(Search);
            Button(overlay, "BACK", new Vector2(.52f, .04f), new Vector2(.94f, .13f)).onClick.AddListener(Close);
            overlay.gameObject.SetActive(false);
        }
        public void Open() { overlay.SetAsLastSibling(); overlay.gameObject.SetActive(true); Search(); }
        private void Search() { sender.FindPCs(); revision = -1; }
        private void Close() { sender.CancelDiscovery(); overlay.gameObject.SetActive(false); }
        private void Choose(int index)
        {
            if (index >= sender.Games.Count || sender.Games[index].Busy) return;
            var pc = sender.Games[index];
            address.text = pc.Address; code.text = pc.PairingCode;
            overlay.gameObject.SetActive(false);
            sender.Connect(pc.Address, pc.PairingCode);
        }
        private void Update()
        {
            if (overlay == null || !overlay.gameObject.activeSelf) return;
            if (searchStatus.text != sender.Status) searchStatus.text = sender.Status;
            if (revision == sender.DiscoveryVersion) return;
            revision = sender.DiscoveryVersion;
            for (int i = 0; i < rows.Length; i++)
            {
                bool visible = i < sender.Games.Count;
                rows[i].gameObject.SetActive(visible);
                if (!visible) continue;
                var pc = sender.Games[i];
                labels[i].text = pc.Name + "   —   " + (pc.Busy ? "IN USE" : "TAP TO CONNECT") + "\n" + pc.Address;
                rows[i].interactable = !pc.Busy;
            }
        }
        private void OnDisable() { if (sender != null) sender.CancelDiscovery(); if (overlay != null) overlay.gameObject.SetActive(false); }
        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private static TMP_Text Text(Transform parent, string text, Vector2 min, Vector2 max, int size)
        {
            var label = Rect(parent, "Label", min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text; label.fontSize = size; label.color = Color.white; label.richText = false;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = size; return label;
        }
        private static UnityEngine.UI.Button Button(Transform parent, string title, Vector2 min, Vector2 max)
        {
            var rect = Rect(parent, title, min, max);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.1f, .34f, .48f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            Text(rect, title, new Vector2(.02f, .04f), new Vector2(.98f, .96f), 28);
            return button;
        }
    }
}
