using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace FlyHigh.PhoneController
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private static PrototypeBootstrap instance;
        private TMP_Text status;
        private PhoneSender sender;
        private WifiReceiver receiver;
        private RacingPrototype driving;
        private CityTrafficSystem traffic;
        private string addresses;
        private RectTransform safeArea;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { instance = null; }
        private void Awake()
        {
            if (instance != null && instance != this) { enabled = false; return; }
            instance = this;
        }
        private void OnDestroy() { if (instance == this) instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene != "GameScene" && scene != "CityDrive") return;
            foreach (var existing in FindObjectsByType<PrototypeBootstrap>(FindObjectsSortMode.None))
                if (existing.isActiveAndEnabled) return;
            new GameObject("Phone controller prototype").AddComponent<PrototypeBootstrap>();
        }

        private void Start()
        {
            Application.runInBackground = true;
            if (EventSystem.current == null)
                new GameObject("Controller events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = new GameObject("Controller UI", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.transform.SetParent(transform);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            safeArea = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeArea.SetParent(canvas.transform, false);
#if UNITY_ANDROID && !UNITY_EDITOR
            sender = gameObject.AddComponent<PhoneSender>();
            Label("FLY HIGH / PHONE CONTROLLER", 0.05f, 0.82f, 0.9f, 0.12f, 36);
            var ip = Input("PC Wi-Fi IPv4 address", "", 0.06f, 0.64f, 0.48f);
            var code = Input("Six-digit pairing code", "", 0.57f, 0.64f, 0.36f);
            Button("CONNECT", 0.36f, 0.47f, 0.27f, 0.12f, () => sender.Connect(ip.text, code.text));
            Button("CALIBRATE TILT", 0.66f, 0.47f, 0.27f, 0.12f, sender.Calibrate);
            var reverse = Button("REVERSE", 0.05f, 0.05f, 0.28f, 0.28f, null).gameObject.AddComponent<HoldPedal>();
            reverse.Changed = held => sender.Reverse = held ? 1 : 0;
            var stop = Button("BRAKE", 0.36f, 0.05f, 0.28f, 0.28f, null).gameObject.AddComponent<HoldPedal>();
            stop.Changed = held => sender.Brake = held ? 1 : 0;
            var gas = Button("ACCELERATE", 0.67f, 0.05f, 0.28f, 0.28f, null).gameObject.AddComponent<HoldPedal>();
            gas.Changed = held => sender.Throttle = held ? 1 : 0;
            status = Label("", 0.05f, 0.34f, 0.9f, 0.1f, 22);
            gameObject.AddComponent<PhonePairingPanel>().Initialize(safeArea, sender, ip, code);
#else
            foreach (var existing in FindObjectsByType<WifiReceiver>(FindObjectsSortMode.None))
                if (existing.isActiveAndEnabled) { receiver = existing; break; }
            if (receiver == null) receiver = gameObject.AddComponent<WifiReceiver>();
            driving = gameObject.AddComponent<RacingPrototype>();
            driving.Initialize(receiver, SceneManager.GetActiveScene().name == "CityDrive");
            CityDayNight environment = null;
            if (SceneManager.GetActiveScene().name == "CityDrive")
            {
                traffic = gameObject.AddComponent<CityTrafficSystem>();
                traffic.Initialize(driving.PlayerCar);
                environment = gameObject.AddComponent<CityDayNight>();
                environment.Initialize(driving.PlayerCar,receiver);
            }
            addresses = "";
            foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)) addresses += ip + "  ";
            status = Label("", 0.02f, 0.59f, 0.96f, 0.39f, 24);
            var drivingHud = new GameObject("Driving HUD", typeof(RectTransform)).GetComponent<RectTransform>();
            drivingHud.SetParent(safeArea, false);
            drivingHud.anchorMin = Vector2.zero; drivingHud.anchorMax = Vector2.one;
            drivingHud.offsetMin = drivingHud.offsetMax = Vector2.zero;
            status.transform.SetParent(drivingHud, false);
            if (SceneManager.GetActiveScene().name == "CityDrive")
                gameObject.AddComponent<CityMinimap>().Initialize(driving.PlayerCar, drivingHud);
            gameObject.AddComponent<CarSpeedometer>().Initialize(driving.PlayerCar, drivingHud);
            gameObject.AddComponent<DesktopMainMenu>().Initialize(safeArea, drivingHud.gameObject,environment,receiver);
#endif
        }
        private void Update()
        {
            if (safeArea != null)
            {
                Rect safe = Screen.safeArea;
                safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
                safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            }
            if (status == null) return;
            if (sender != null) status.text = sender.Status + "  |  Steering: " + sender.Steering.ToString("F2");
            if (receiver != null) status.text = "FLY HIGH — Wi-Fi controller test\nPC address: " + addresses +
                "\nPairing code: " + receiver.PairingCode + "   |   " + receiver.Status +
                "\n" + receiver.Diagnostics +
                "\n" + driving.CollisionDiagnostics + " | Input steer: " + driving.RawSteering.ToString("F2") +
                (traffic != null ? "\n" + traffic.Status : "") +
                "\nKeyboard: W forward, Down Arrow reverse, S brake, A/D steer, R reset";
        }
        private RectTransform Rect(string title, float x, float y, float w, float h)
        {
            var rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(safeArea, false); rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(x + w, y + h);
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private TMP_Text Label(string text, float x, float y, float w, float h, int size)
        {
            var label = Rect("Label", x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text; label.fontSize = size; label.color = Color.white; label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft; return label;
        }
        private UnityEngine.UI.Button Button(string title, float x, float y, float w, float h, UnityEngine.Events.UnityAction clicked)
        {
            var rect = Rect(title, x, y, w, h);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(0.1f, 0.34f, 0.48f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            if (clicked != null) button.onClick.AddListener(clicked);
            var label = Label(title, x, y, w, h, 30); label.alignment = TextAlignmentOptions.Center;
            return button;
        }
        private TMP_InputField Input(string hint, string value, float x, float y, float w)
        {
            var rect = Rect(hint, x, y, w, 0.13f);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(0.08f, 0.13f, 0.2f);
            var field = rect.gameObject.AddComponent<TMP_InputField>(); field.targetGraphic = image;
            var textRect = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            textRect.SetParent(rect, false); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 4); textRect.offsetMax = new Vector2(-12, -4);
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>(); text.fontSize = 28; text.color = Color.white;
            var placeholder = Instantiate(text, rect); placeholder.text = hint; placeholder.color = Color.gray;
            field.textViewport = rect; field.textComponent = text; field.placeholder = placeholder;
            field.lineType = TMP_InputField.LineType.SingleLine; field.text = value; return field;
        }
    }
}
