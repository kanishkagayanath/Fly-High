using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FlyHigh.PhoneController
{
    /// <summary>Desktop presentation and menu state; controller networking remains live while paused.</summary>
    public sealed class DesktopMainMenu : MonoBehaviour
    {
        private const string VolumeKey = "FlyHigh.MasterVolume";
        private const string FullscreenKey = "FlyHigh.Fullscreen";
        private static readonly Color Mint = new Color(.40f, .96f, 1);
        private static readonly Color SoftWhite = new Color(.79f, .85f, .93f);
        private GameObject overlay, home, options, hud;
        private TMP_Text volumeLabel, fullscreenLabel, timeLabel, playCaption, phoneLabel, phoneHint;
        private UnityEngine.UI.Image phoneIndicator;
        private RectTransform volumeFill, revealPanel;
        private CanvasGroup revealGroup;
        private CityDayNight environment;
        private WifiReceiver receiver;
        private MenuVisualSettings visuals;
        private VehicleGarage garage;
        private VehicleShopPanel shop;
        private TMP_Text garageLabel;
        private GameObject connectionCard;
        private UnityEngine.UI.Button playButton, backButton;
        private bool started, showingOptions, paused;
        private bool? lastConnected;
        private float savedTimeScale, revealProgress;

        public void Initialize(Transform uiParent, GameObject drivingHud, CityDayNight cityEnvironment = null, WifiReceiver phoneReceiver = null, VehicleGarage playerGarage = null)
        {
            environment = cityEnvironment; receiver = phoneReceiver; hud = drivingHud; garage = playerGarage;
            visuals = Resources.Load<MenuVisualSettings>("FlyHighMenu");
            savedTimeScale = Time.timeScale;
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1));
#if !UNITY_EDITOR
            if (PlayerPrefs.HasKey(FullscreenKey)) Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) == 1;
#endif
            var background = Area(uiParent, "Main menu", 0, 0, 1, 1);
            overlay = background.gameObject;
            background.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.018f, .027f, .055f);
            // Fit the entire authored title/car composition, including on ultrawide and 4:3 screens.
            var composition = Area(background, "Menu artwork composition", 0, 0, 1, 1);
            var fit = composition.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = visuals != null && visuals.background != null ? (float)visuals.background.width / visuals.background.height : 16f / 9;
            var artwork = Area(composition, "City sunset artwork", 0, 0, 1, 1).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            artwork.texture = visuals != null ? visuals.background : null; artwork.raycastTarget = false;
            if (artwork.texture == null) { artwork.color = Color.clear; Text(composition,"FLY HIGH",.33f,.78f,.95f,.95f,68,Color.white); }
            var shade = Area(composition, "Left readability gradient", 0, 0, .53f, .77f).gameObject.AddComponent<MenuGradient>();
            shade.Initialize(new Color(.014f,.027f,.065f,.86f),new Color(.014f,.027f,.065f,0));

            home = Area(composition, "Home", 0, 0, 1, 1).gameObject;
            Text(home.transform, "FREE ROAM  /  COASTAL CITY", .065f,.72f,.43f,.765f,17,Mint).characterSpacing = 2;
            Text(home.transform, "Your city. Your drive.", .065f,.665f,.43f,.72f,24,Color.white);
            playButton = Button(home.transform,"PLAY",.058f,.50f,.35f,.66f,StartDriving,visuals!=null?visuals.playButton:null,true);
            var playVisual = playButton.transform.GetChild(0);
            playCaption = Text(playVisual,"ENTER THE CITY",.10f,.18f,.90f,.37f,14,SoftWhite);
            playCaption.alignment = TextAlignmentOptions.Center; playCaption.characterSpacing = 3;
            if(garage!=null) Button(home.transform,"SHOP",.058f,.355f,.35f,.49f,ShowShop,visuals!=null?visuals.optionsButton:null);
            Button(home.transform,"OPTIONS",.058f,.21f,.35f,.345f,ShowOptions,visuals!=null?visuals.optionsButton:null);
            Button(home.transform,"EXIT",.058f,.065f,.35f,.20f,Exit,visuals!=null?visuals.exitButton:null);

            options = Area(composition,"Options",.055f,.105f,.465f,.69f).gameObject;
            Panel(options.transform,"Options glass",0,0,1,1,new Color(.025f,.045f,.095f,.96f));
            Panel(options.transform,"Options accent",0,.987f,1,1,new Color(.65f,.47f,1));
            Text(options.transform,"OPTIONS",.055f,.845f,.94f,.97f,32,Color.white).characterSpacing=3;
            volumeLabel = Text(options.transform,"",.055f,.65f,.64f,.81f,21,SoftWhite);
            Button(options.transform,"−",.67f,.665f,.79f,.795f,()=>ChangeVolume(-.1f),null);
            Button(options.transform,"+",.82f,.665f,.94f,.795f,()=>ChangeVolume(.1f),null);
            Panel(options.transform,"Volume track",.06f,.62f,.94f,.635f,new Color(.16f,.21f,.30f));
            volumeFill = Panel(options.transform,"Volume fill",.06f,.62f,.94f,.635f,Mint);
            Text(options.transform,"DISPLAY",.06f,.44f,.4f,.57f,17,SoftWhite);
            var fullscreen = Button(options.transform,"",.42f,.425f,.94f,.585f,ToggleFullscreen,visuals!=null?visuals.optionsButton:null);
            fullscreenLabel = fullscreen.GetComponentInChildren<TMP_Text>();
            if(environment!=null)
            {
                Text(options.transform,"CITY LIGHTING",.06f,.245f,.42f,.39f,17,SoftWhite);
                var time = Button(options.transform,"",.42f,.23f,.94f,.39f,()=>{environment.CycleMode();RefreshOptions();},visuals!=null?visuals.playButton:null);
                timeLabel = time.GetComponentInChildren<TMP_Text>();
            }
            backButton = Button(options.transform,"BACK",.055f,.045f,.51f,.205f,ShowHome,visuals!=null?visuals.optionsButton:null);

            var connection = Panel(composition,"Phone connection card",.635f,.075f,.945f,.248f,new Color(.02f,.045f,.08f,.93f));
            connectionCard=connection.gameObject;
            Panel(connection,"Phone card accent",0,.975f,1,1,Mint);
            Text(connection,"PHONE CONTROLLER",.06f,.68f,.95f,.9f,14,Mint).characterSpacing=2;
            phoneIndicator = Panel(connection,"Connection dot",.06f,.44f,.085f,.54f,Color.white).GetComponent<UnityEngine.UI.Image>();
            phoneLabel = Text(connection,"",.115f,.34f,.95f,.64f,24,Color.white);
            phoneHint = Text(connection,"",.06f,.10f,.96f,.32f,16,SoftWhite);
            Text(composition,"ENTER  SELECT    /    ESC  MENU & BACK",.065f,.015f,.60f,.052f,13,SoftWhite).characterSpacing=1;
            Text(composition,"EXPLORE AT YOUR OWN PACE",.65f,.015f,.945f,.052f,13,SoftWhite).alignment=TextAlignmentOptions.Right;
            if(garage!=null)
            {
                garageLabel=Text(composition,"",.635f,.255f,.945f,.3f,17,new Color(1,.8f,.35f));
                shop=gameObject.AddComponent<VehicleShopPanel>();shop.Initialize(composition,garage,visuals,ShowHome);
                garage.Changed+=RefreshGarage;RefreshGarage();
            }
            options.SetActive(false);
            Pause(); ShowHome(); UpdatePhoneCard();
        }

        private void Update()
        {
            if(overlay == null)return;
            if(overlay.activeSelf)
            {
                UpdatePhoneCard();
                if(revealGroup!=null && revealProgress<1)
                {
                    revealProgress=Mathf.Min(1,revealProgress+Time.unscaledDeltaTime/.28f);
                    float ease=1-Mathf.Pow(1-revealProgress,3);
                    revealGroup.alpha=ease;revealPanel.anchoredPosition=new Vector2(0,Mathf.Lerp(-10,0,ease));
                }
            }
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (showingOptions || (shop!=null && shop.IsOpen)) ShowHome();
            else if (overlay.activeSelf && started) StartDriving();
            else if (!overlay.activeSelf) { Pause(); ShowHome(); }
        }
        private void RefreshGarage(){if(garageLabel!=null)garageLabel.text=garage.Coins.ToString("N0")+" COINS   /   "+(garage.Selected!=null?garage.Selected.title:"");}
        private void ShowShop()
        {
            showingOptions=false;home.SetActive(false);options.SetActive(false);if(connectionCard!=null)connectionCard.SetActive(false);if(garageLabel!=null)garageLabel.gameObject.SetActive(false);shop.Show();Reveal(shop.Root.gameObject);Select(shop.BackButton);
        }
        private void OnDestroy(){if(garage!=null)garage.Changed-=RefreshGarage;if(shop!=null)Destroy(shop);}
        private void UpdatePhoneCard()
        {
            bool connected=receiver!=null&&receiver.Connected;
            if(lastConnected==connected)return;
            lastConnected=connected;
            phoneIndicator.color=connected?new Color(.4f,1,.66f):new Color(1,.69f,.32f);
            phoneLabel.text=connected?"Ready to drive":"Connect your phone";
            phoneHint.text=connected?"Connected  /  Tilt to steer":"Same Wi-Fi  /  Phone app > FIND PC";
        }
        private void Reveal(GameObject panel)
        {
            revealPanel=panel.GetComponent<RectTransform>();
            revealGroup=panel.GetComponent<CanvasGroup>();if(revealGroup==null)revealGroup=panel.AddComponent<CanvasGroup>();
            revealProgress=0;revealGroup.alpha=0;revealPanel.anchoredPosition=new Vector2(0,-10);
        }
        private void Pause()
        {
            if (!paused) { savedTimeScale = Time.timeScale; paused = true; }
            Time.timeScale = 0;
            overlay.SetActive(true); hud.SetActive(false);
        }
        private void StartDriving()
        {
            started = true; showingOptions = false; if(shop!=null)shop.Hide();
            playButton.GetComponentInChildren<TMP_Text>().text = "RESUME";
            playCaption.text="BACK TO YOUR DRIVE";
            overlay.SetActive(false); hud.SetActive(true);
            Time.timeScale = savedTimeScale; paused = false;
        }
        private void ShowOptions()
        {
            showingOptions = true; if(shop!=null)shop.Hide(); home.SetActive(false); options.SetActive(true);
            RefreshOptions(); Reveal(options); Select(backButton);
        }
        private void ShowHome()
        {
            showingOptions = false; if(shop!=null)shop.Hide(); if(connectionCard!=null)connectionCard.SetActive(true);if(garageLabel!=null)garageLabel.gameObject.SetActive(true); options.SetActive(false); home.SetActive(true); Reveal(home); Select(playButton);
        }
        private void ChangeVolume(float amount)
        {
            AudioListener.volume = Mathf.Clamp01(Mathf.Round((AudioListener.volume + amount) * 10) / 10);
            PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume); PlayerPrefs.Save(); RefreshOptions();
        }
        private void ToggleFullscreen()
        {
#if !UNITY_EDITOR
            bool enabled = !Screen.fullScreen;
            Screen.fullScreen = enabled;
            PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0); PlayerPrefs.Save();
#endif
            RefreshOptions();
        }
        private void RefreshOptions()
        {
            volumeLabel.text = "MASTER VOLUME   " + Mathf.RoundToInt(AudioListener.volume * 100) + "%";
            volumeFill.anchorMax=new Vector2(Mathf.Lerp(.06f,.94f,AudioListener.volume),.635f);
            if(timeLabel!=null)timeLabel.text=(environment.Mode==CityTimeMode.Automatic?"AUTO":environment.Mode.ToString().ToUpperInvariant())+"  >";
#if UNITY_EDITOR
            fullscreenLabel.text = "PC BUILD ONLY";
#else
            fullscreenLabel.text = Screen.fullScreen?"FULLSCREEN":"WINDOWED";
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
        private static RectTransform Panel(Transform parent,string name,float left,float bottom,float right,float top,Color color)
        {
            var rect=Area(parent,name,left,bottom,right,top);
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return rect;
        }
        private static TMP_Text Text(Transform parent, string value, float left, float bottom, float right, float top, int size, Color color)
        {
            var text = Area(parent, "Text", left, bottom, right, top).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.color = color; text.fontSize = size; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=size;return text;
        }
        private static UnityEngine.UI.Button Button(Transform parent,string value,float left,float bottom,float right,float top,
            UnityEngine.Events.UnityAction click,Sprite sprite,bool primary=false)
        {
            var rect=Area(parent,value+" button",left,bottom,right,top);
            var visual=Area(rect,"Button visual",0,0,1,1);
            var image=visual.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;
            if(sprite!=null){image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=2.6f;image.color=Color.white;}
            else image.color=new Color(.10f,.22f,.34f);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=new Color(.85f,.88f,.94f);colors.highlightedColor=Color.white;colors.selectedColor=Color.white;colors.pressedColor=new Color(.66f,.77f,.88f);colors.fadeDuration=.12f;button.colors=colors;
            button.onClick.AddListener(click);
            var label=Text(visual,value,.12f,primary ? .35f : .15f,.88f,primary ? .78f : .85f,primary ? 38 : 27,Color.white);
            label.alignment=TextAlignmentOptions.Center;label.fontStyle=FontStyles.Bold;label.characterSpacing=3;
            var shadow=label.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(.01f,.02f,.05f,.8f);shadow.effectDistance=new Vector2(0,-2);
            rect.gameObject.AddComponent<MenuButtonMotion>().Initialize(visual);
            return button;
        }
    }
}


