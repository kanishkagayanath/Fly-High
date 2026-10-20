using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FlyHigh.PhoneController
{
    /// <summary>PC menu overlay. Network updates continue while driving physics is paused.</summary>
    public sealed class DesktopMainMenu : MonoBehaviour
    {
        private const string VolumeKey = "FlyHigh.MasterVolume";
        private const string FullscreenKey = "FlyHigh.Fullscreen";
        private GameObject overlay, home, options, hud;
        private TMP_Text volumeLabel, fullscreenLabel;
        private UnityEngine.UI.Button playButton, backButton;
        private bool started, showingOptions, paused;
        private float savedTimeScale;

        public void Initialize(Transform uiParent, GameObject drivingHud)
        {
            hud = drivingHud;
            savedTimeScale = Time.timeScale;
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1));
#if !UNITY_EDITOR
            if (PlayerPrefs.HasKey(FullscreenKey)) Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) == 1;
#endif
            var background = Area(uiParent, "Main menu", 0, 0, 1, 1);
            overlay = background.gameObject;
            background.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.055f, 0.10f, 0.97f);
            var panel = Area(background, "Menu content", 0.08f, 0.09f, 0.92f, 0.92f);
            Text(panel, "CITY DRIVE / PHONE CONTROLLED", 0, 0.87f, 1, 0.95f, 20, new Color(0.25f, 0.85f, 0.9f));
            Text(panel, "FLY HIGH", 0, 0.66f, 1, 0.86f, 78, Color.white);
            Text(panel, "Your city. Your drive.", 0, 0.56f, 1, 0.65f, 26, new Color(0.7f, 0.79f, 0.87f));
            home = Area(panel, "Home", 0, 0.06f, 1, 0.52f).gameObject;
            playButton = Button(home.transform, "PLAY", 0, 0.68f, 0.47f, 0.96f, StartDriving, true);
            Button(home.transform, "OPTIONS", 0, 0.35f, 0.47f, 0.63f, ShowOptions);
            Button(home.transform, "EXIT", 0, 0.02f, 0.47f, 0.30f, Exit);
            Text(home.transform, "Explore freely\n\nTilt your phone to steer.\nHold the pedals to drive or reverse.",
                0.56f, 0.22f, 1, 0.94f, 25, new Color(0.75f, 0.83f, 0.9f));
            options = Area(panel, "Options", 0, 0.06f, 1, 0.52f).gameObject;
            volumeLabel = Text(options.transform, "", 0, 0.73f, 0.62f, 0.98f, 28, Color.white);
            Button(options.transform, "−", 0.68f, 0.73f, 0.81f, 0.98f, () => ChangeVolume(-0.1f));
            Button(options.transform, "+", 0.85f, 0.73f, 0.98f, 0.98f, () => ChangeVolume(0.1f));
            var fullscreen = Button(options.transform, "", 0, 0.38f, 0.98f, 0.65f, ToggleFullscreen);
            fullscreenLabel = fullscreen.GetComponentInChildren<TMP_Text>();
            backButton = Button(options.transform, "BACK", 0, 0.02f, 0.47f, 0.29f, ShowHome);
            Text(panel, "ESC — menu / back", 0, -0.06f, 1, 0.01f, 18, new Color(0.5f, 0.64f, 0.74f));
            options.SetActive(false);
            Pause();
            Select(playButton);
        }

        private void Update()
        {
            if (overlay == null || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (showingOptions) ShowHome();
            else if (overlay.activeSelf && started) StartDriving();
            else if (!overlay.activeSelf)
            {
                Pause(); ShowHome();
            }
        }
        private void Pause()
        {
            if (!paused) { savedTimeScale = Time.timeScale; paused = true; }
            Time.timeScale = 0;
            overlay.SetActive(true); hud.SetActive(false);
        }
        private void StartDriving()
        {
            started = true; showingOptions = false;
            playButton.GetComponentInChildren<TMP_Text>().text = "RESUME";
            overlay.SetActive(false); hud.SetActive(true);
            Time.timeScale = savedTimeScale; paused = false;
        }
        private void ShowOptions()
        {
            showingOptions = true; home.SetActive(false); options.SetActive(true);
            RefreshOptions(); Select(backButton);
        }
        private void ShowHome()
        {
            showingOptions = false; options.SetActive(false); home.SetActive(true); Select(playButton);
        }
        private void ChangeVolume(float amount)
        {
            AudioListener.volume = Mathf.Clamp01(Mathf.Round((AudioListener.volume + amount) * 10) / 10);
            PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume); PlayerPrefs.Save(); RefreshOptions();
        }
        private void ToggleFullscreen()
        {
#if UNITY_EDITOR
            fullscreenLabel.text = "FULLSCREEN — available in the PC build";
#else
            bool enabled = !Screen.fullScreen;
            Screen.fullScreen = enabled;
            PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0); PlayerPrefs.Save();
            fullscreenLabel.text = "FULLSCREEN: " + (enabled ? "ON" : "OFF");
#endif
        }
        private void RefreshOptions()
        {
            volumeLabel.text = "MASTER VOLUME   " + Mathf.RoundToInt(AudioListener.volume * 100) + "%";
#if UNITY_EDITOR
            fullscreenLabel.text = "FULLSCREEN — available in the PC build";
#else
            fullscreenLabel.text = "FULLSCREEN: " + (Screen.fullScreen ? "ON" : "OFF");
#endif
        }
        private static void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OnDisable()
        {
            if (paused) { Time.timeScale = savedTimeScale; paused = false; }
        }
        private static void Select(UnityEngine.UI.Button button)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        private static RectTransform Area(Transform parent, string name, float left, float bottom, float right, float top)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = new Vector2(left, bottom); rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private static TMP_Text Text(Transform parent, string value, float left, float bottom, float right, float top, int size, Color color)
        {
            var text = Area(parent, "Text", left, bottom, right, top).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.color = color; text.fontSize = size; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft; return text;
        }
        private static UnityEngine.UI.Button Button(Transform parent, string value, float left, float bottom, float right, float top,
            UnityEngine.Events.UnityAction click, bool primary = false)
        {
            var rect = Area(parent, value + " button", left, bottom, right, top);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = primary ? new Color(1, 0.55f, 0.2f) : new Color(0.10f, 0.22f, 0.31f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.8f, 1, 1);
            colors.selectedColor = new Color(0.8f, 1, 1); colors.pressedColor = new Color(0.65f, 0.75f, 0.85f);
            button.colors = colors;
            button.onClick.AddListener(click);
            var label = Text(rect, value, 0.08f, 0, 0.92f, 1, 27, primary ? new Color(0.08f, 0.1f, 0.12f) : Color.white);
            label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 27;
            return button;
        }
    }
}
