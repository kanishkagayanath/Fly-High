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
            gameObject.AddComponent<PhoneControllerView>().Initialize(safeArea,sender);
#else
            foreach (var existing in FindObjectsByType<WifiReceiver>(FindObjectsSortMode.None))
                if (existing.isActiveAndEnabled) { receiver = existing; break; }
            if (receiver == null) receiver = gameObject.AddComponent<WifiReceiver>();
            var garage=gameObject.AddComponent<VehicleGarage>(); garage.Initialize();
            driving = gameObject.AddComponent<RacingPrototype>();
            driving.Initialize(receiver, SceneManager.GetActiveScene().name == "CityDrive",garage);
            CityDayNight environment = null;
            if (SceneManager.GetActiveScene().name == "CityDrive")
            {
                traffic = gameObject.AddComponent<CityTrafficSystem>();
                traffic.Initialize(driving.PlayerCar);
                gameObject.AddComponent<CityPedestrianSystem>().Initialize(driving.PlayerCar);
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
            {
                var minimap=gameObject.AddComponent<CityMinimap>(); minimap.Initialize(driving.PlayerCar, drivingHud);
                gameObject.AddComponent<CoinMissionSystem>().Initialize(driving.PlayerCar,receiver,drivingHud,minimap,garage);
                status.fontSize=13;
                var diagnostics=status.rectTransform; diagnostics.anchorMin=diagnostics.anchorMax=diagnostics.pivot=Vector2.zero;
                diagnostics.anchoredPosition=new Vector2(20,16);diagnostics.sizeDelta=new Vector2(780,105);
            }
            gameObject.AddComponent<CarSpeedometer>().Initialize(driving.PlayerCar, drivingHud);
            gameObject.AddComponent<DesktopMainMenu>().Initialize(safeArea, drivingHud.gameObject,environment,receiver,garage);
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
    }
}
