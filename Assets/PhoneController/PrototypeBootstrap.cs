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
        private bool mobile, loading;
        private WifiReceiver receiver;
        private RacingPrototype driving;
        private string addresses;
        private RectTransform safeArea;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        private void Awake()
        {
            if (instance != null && instance != this) { enabled = false; return; }
            instance = this;
        }
        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void WatchSceneLoads()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || (scene.name != "GameScene" && scene.name != "CityDrive")) return;
            if (instance != null && instance.gameObject.scene == scene) return;
            var bootstrap = new GameObject("Fly-High game session").AddComponent<PrototypeBootstrap>();
            bootstrap.mobile = Application.platform == RuntimePlatform.Android;
        }

        private void Start()
        {
            Application.runInBackground = !mobile;
            if (mobile)
            {
                Screen.orientation = ScreenOrientation.LandscapeLeft;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Application.targetFrameRate = 30;
            }
            if (EventSystem.current == null)
            {
                var events = new GameObject("Controller events", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            var canvas = new GameObject("Controller UI", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.transform.SetParent(transform);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            safeArea = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeArea.SetParent(canvas.transform, false);
            bool city = gameObject.scene.name == "CityDrive";
            if (mobile && !city)
            {
                var sender = gameObject.AddComponent<PhoneSender>();
                gameObject.AddComponent<PhoneControllerView>().Initialize(safeArea, sender, () => LoadMobileScene("CityDrive"));
                return;
            }
            StartDrivingSession(city);
        }

        private void StartDrivingSession(bool city)
        {
            MobileDriveControls localControls = null;
            if (mobile)
                localControls = gameObject.AddComponent<MobileDriveControls>();
            else
            {
                foreach (var existing in FindObjectsByType<WifiReceiver>(FindObjectsSortMode.None))
                    if (existing.isActiveAndEnabled) { receiver = existing; break; }
                if (receiver == null) receiver = gameObject.AddComponent<WifiReceiver>();
            }

            var garage = gameObject.AddComponent<VehicleGarage>();
            garage.Initialize();
            driving = gameObject.AddComponent<RacingPrototype>();
            driving.Initialize(receiver, city, garage, localControls);
            CityDayNight environment = null;
            if (city)
            {
                var traffic = gameObject.AddComponent<CityTrafficSystem>();
                traffic.Initialize(driving.PlayerCar, mobile ? 4 : int.MaxValue);
                gameObject.AddComponent<CityPedestrianSystem>().Initialize(driving.PlayerCar, mobile);
                environment = gameObject.AddComponent<CityDayNight>();
                environment.Initialize(driving.PlayerCar, receiver, mobile);
            }

            var drivingHud = PhoneUi.Rect(safeArea, "Driving HUD", 0, 0, 1, 1);
            if (receiver != null)
            {
                addresses = "";
                foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)) addresses += ip + "  ";
                if (!city)
                {
                    status = Label("", 0.02f, 0.59f, 0.96f, 0.39f, 24);
                    status.transform.SetParent(drivingHud, false);
                }
            }

            CityMinimap minimap = null;
            CoinMissionSystem missions = null;
            if (city)
            {
                minimap = gameObject.AddComponent<CityMinimap>();
                minimap.Initialize(driving.PlayerCar, drivingHud, mobile);
                missions = gameObject.AddComponent<CoinMissionSystem>();
                missions.Initialize(driving.PlayerCar, receiver, drivingHud, minimap, garage, mobile);
            }
            gameObject.AddComponent<CarSpeedometer>().Initialize(driving.PlayerCar, drivingHud, mobile ? minimap?.Frame : null);
            var menu = gameObject.AddComponent<DesktopMainMenu>();
            System.Action controller = mobile ? () => LoadMobileScene("GameScene") : null;
            menu.Initialize(safeArea, drivingHud.gameObject, environment, receiver, garage, addresses, controller,
                mobile ? driving.ResetCar : null);
            if (localControls != null) localControls.Initialize(drivingHud, menu);
            gameObject.AddComponent<GameAudio>().InitializeDesktop(driving, menu, environment, missions, garage, receiver, mobile);
        }

        private void LoadMobileScene(string scene)
        {
            if (loading) return;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("[Fly-High] This build is missing " + scene + ".");
                return;
            }

            loading = true;
            var sender = GetComponent<PhoneSender>();
            if (sender != null) sender.Disconnect();
            var blocker = PhoneUi.Rect(safeArea, "Loading screen", 0, 0, 1, 1);
            ViewportBackdrop.Create(blocker, PhoneUi.Ink);
            PhoneUi.Text(blocker, "Loading message", "LOADING FLY-HIGH", .18f, .40f, .82f, .60f,
                34, PhoneUi.White, true, TextAlignmentOptions.Center);
            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
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
            if (receiver != null) status.text = "FLY HIGH — Wi-Fi controller test\nPC address: " + addresses +
                "\nPairing code: " + receiver.PairingCode + "   |   " + receiver.Status +
                "\n" + receiver.Diagnostics +
                "\n" + driving.CollisionDiagnostics + " | Input steer: " + driving.RawSteering.ToString("F2") +
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
