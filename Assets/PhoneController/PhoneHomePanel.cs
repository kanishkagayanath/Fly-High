using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Phone home/garage/settings presentation; PC remains authoritative for game progress.</summary>
    public sealed class PhoneHomePanel : MonoBehaviour
    {
        private sealed class Card {public HomeVehicleSnapshot entry;public TMP_Text title,price,caption;public UnityEngine.UI.Image preview;public UnityEngine.UI.Button action;}
        private PhoneSender sender;
        private PhonePairingPanel pairing;
        private HowToPlayGuide guide;
        private RectTransform root,volumeFill;
        private GameObject home,shop,options;
        private TMP_Text status,balance,shopBalance,shopMessage,volume,lighting,pageLabel;
        private UnityEngine.UI.Button mobilePlay,play,shopButton,optionsButton,minus,plus,lightButton,previous,next;
        private readonly Card[] cards=new Card[6];
        private VehicleShopCatalog catalog;
        private MenuVisualSettings visuals;
        private HomePhoneSnapshot displayed;
        private uint drawnRevision;
        private int shopPage;
        private DriveMenuAction? waitingAction;
        private bool actionReady,launchPending;
        private float intentDeadline;
        public bool IsVisible=>root!=null&&root.gameObject.activeSelf;
        public RectTransform Root=>root;
        public void Initialize(RectTransform parent,PhoneSender connection,PhonePairingPanel connectionPanel,HowToPlayGuide instructions=null, System.Action playOnPhone = null)
        {
            sender=connection;pairing=connectionPanel;guide=instructions;visuals=Resources.Load<MenuVisualSettings>("FlyHighMenu");catalog=Resources.Load<VehicleShopCatalog>("FlyHighVehicleShop");
            root = PhoneUi.Rect(parent, "Phone home menu", 0, 0, 1, 1);
            ViewportBackdrop.Create(root, PhoneUi.Ink, visuals != null ? visuals.background : null);
            var frame=PhoneUi.Rect(root,"Phone menu artwork",0,0,1,1);var fit=frame.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=visuals!=null&&visuals.background!=null?(float)visuals.background.width/visuals.background.height:16f/9;
            if (visuals == null || visuals.background == null)
                PhoneUi.Text(frame,"Home title","FLY HIGH",.35f,.78f,.93f,.96f,60,PhoneUi.White,true);
            home=PhoneUi.Rect(frame,"Phone home actions",0,0,1,1).gameObject;
            if (playOnPhone != null)
            {
                mobilePlay = FrameButton(home.transform, "Play on phone", "PLAY ON PHONE", .058f, .55f, .35f, .71f,
                    () => playOnPhone(), visuals != null ? visuals.playButton : null, 30);
                play = FrameButton(home.transform, "Phone home Play", "PLAY ON PC", .635f, .325f, .945f, .47f,
                    Play, visuals != null ? visuals.optionsButton : null, 26);
            }
            else
                play = FrameButton(home.transform, "Phone home Play", "PLAY", .058f, .55f, .35f, .71f,
                    Play, visuals != null ? visuals.playButton : null, 36);
            shopButton=FrameButton(home.transform,"Phone home Shop",playOnPhone != null ? "PC SHOP" : "SHOP",.058f,.40f,.35f,.535f,()=>OpenPage(DriveMenuAction.Shop),visuals!=null?visuals.optionsButton:null);
            optionsButton=FrameButton(home.transform,"Phone home Options",playOnPhone != null ? "PC OPTIONS" : "OPTIONS",.058f,.25f,.35f,.385f,()=>OpenPage(DriveMenuAction.Options),visuals!=null?visuals.optionsButton:null);
            FrameButton(home.transform,"Phone home Exit","EXIT",.058f,.10f,.35f,.235f,Exit,visuals!=null?visuals.exitButton:null);
            if(guide!=null)
            {
                var help=FrameButton(home.transform,"Phone How to Play","HOW TO PLAY",0,0,1,1,guide.Open,visuals!=null?visuals.optionsButton:null,18);
                var helpRect=help.GetComponent<RectTransform>();helpRect.anchorMin=helpRect.anchorMax=helpRect.pivot=Vector2.one;helpRect.anchoredPosition=new Vector2(-24,-24);helpRect.sizeDelta=new Vector2(190,52);
            }
            var connectionCard=PhoneUi.Panel(home.transform,"Home PC status",.635f,.075f,.945f,.22f,PhoneUi.Surface,PhoneUi.Cyan);
            status=PhoneUi.Text(connectionCard.transform,"PC link","NOT CONNECTED",.06f,.12f,.94f,.88f,22,PhoneUi.White,true,TextAlignmentOptions.Center);
            balance=PhoneUi.Text(home.transform,"Home garage balance","",.635f,.235f,.945f,.30f,18,PhoneUi.Amber);
            BuildShop(frame);BuildOptions(frame);
            pairing.ConnectionCompleted+=Connected;pairing.Cancelled+=Cancelled;
            shop.SetActive(false);options.SetActive(false);
            MenuReveal.Attach(home);
            MenuReveal.Attach(shop);
            MenuReveal.Attach(options);
        }
        public void Play(){waitingAction=DriveMenuAction.Play;actionReady=launchPending=false;intentDeadline=0;pairing.Open();}
        private void OpenPage(DriveMenuAction action)
        {
            if(sender.HasMenuState){sender.RequestMenu(action);return;}
            waitingAction=action;actionReady=launchPending=false;intentDeadline=0;pairing.Open();
        }
        private void Connected(){if(waitingAction.HasValue){actionReady=true;intentDeadline=Time.realtimeSinceStartup+4;}}
        private void Cancelled(){waitingAction=null;actionReady=launchPending=false;intentDeadline=0;}
        private void Back(){sender.RequestMenu(DriveMenuAction.Back);}
        private void Exit()
        {
            sender.Disconnect();Cancelled();
#if !UNITY_EDITOR
            Application.Quit();
#endif
        }
        private void BuildShop(Transform parent)
        {
            var area=PhoneUi.Panel(parent,"Phone vehicle shop",.045f,.055f,.955f,.735f,PhoneUi.Ink,PhoneUi.Amber).rectTransform;shop=area.gameObject;
            PhoneUi.Text(area,"Shop heading","VEHICLE SHOP",.03f,.90f,.62f,.98f,30,PhoneUi.White,true);
            shopBalance=PhoneUi.Text(area,"Shop coins","",.66f,.90f,.97f,.98f,24,PhoneUi.Amber,true,TextAlignmentOptions.Right);
            shopMessage=PhoneUi.Text(area,"Shop action feedback","",.03f,.84f,.97f,.90f,16,PhoneUi.Muted);
            for(int i=0;i<cards.Length;i++)
            {
                int slot=i;float left=.03f+(i%3)*.315f,top=.83f-(i/3)*.335f;
                var card=PhoneUi.Panel(area,"Phone vehicle card "+i,left,top-.315f,left+.31f,top,PhoneUi.Surface,PhoneUi.Edge).rectTransform;
                var row=new Card();row.title=PhoneUi.Text(card,"Vehicle name","",.045f,.80f,.96f,.985f,19,PhoneUi.White,true);
                row.preview=PhoneUi.Rect(card,"Vehicle preview",.02f,.37f,.98f,.81f).gameObject.AddComponent<UnityEngine.UI.Image>();row.preview.preserveAspect=true;row.preview.raycastTarget=false;
                row.price=PhoneUi.Text(card,"Vehicle price","",.045f,.035f,.43f,.32f,17,PhoneUi.Amber);
                row.action=FrameButton(card,"Phone vehicle action "+i,"",.45f,0,.965f,0,()=>Activate(slot),visuals!=null?visuals.playButton:null,17);var r=row.action.GetComponent<RectTransform>();r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(0,5);r.sizeDelta=new Vector2(0,50);
                row.caption=row.action.GetComponentInChildren<TMP_Text>();cards[i]=row;
            }
            var back=FrameButton(area,"Phone shop Back","BACK",.03f,0,.235f,0,Back,visuals!=null?visuals.optionsButton:null,21);FooterButton(back);
            previous=FrameButton(area,"Phone shop Previous","<",.39f,0,.50f,0,()=>{shopPage--;DrawShop();},visuals!=null?visuals.optionsButton:null,20);
            next=FrameButton(area,"Phone shop Next",">",.69f,0,.80f,0,()=>{shopPage++;DrawShop();},visuals!=null?visuals.optionsButton:null,20);
            FooterButton(previous);FooterButton(next);
            pageLabel=PhoneUi.Text(area,"Shop page","",.51f,.02f,.68f,.13f,17,PhoneUi.White,true,TextAlignmentOptions.Center);
        }
        private void BuildOptions(Transform parent)
        {
            var area=PhoneUi.Panel(parent,"Phone home options",.055f,.105f,.465f,.69f,PhoneUi.Ink,PhoneUi.Cyan).rectTransform;options=area.gameObject;
            PhoneUi.Text(area,"Options heading","OPTIONS",.055f,.845f,.94f,.97f,32,PhoneUi.White,true);
            volume=PhoneUi.Text(area,"Home volume","",.055f,.65f,.64f,.81f,21,PhoneUi.White);
            minus=PhoneUi.Button(area,"Phone home Volume down","-",.67f,.655f,.79f,.795f,()=>sender.RequestMenu(DriveMenuAction.VolumeDown),PhoneUi.Surface,PhoneUi.Edge);
            plus=PhoneUi.Button(area,"Phone home Volume up","+",.82f,.655f,.94f,.795f,()=>sender.RequestMenu(DriveMenuAction.VolumeUp),PhoneUi.Surface,PhoneUi.Edge);
            PhoneUi.Panel(area,"Home volume track",.06f,.59f,.94f,.605f,PhoneUi.Edge,PhoneUi.Edge);volumeFill=PhoneUi.Panel(area,"Home volume fill",.06f,.59f,.94f,.605f,PhoneUi.Cyan,PhoneUi.Cyan).rectTransform;
            PhoneUi.Text(area,"Home city lighting","CITY LIGHTING",.06f,.38f,.40f,.53f,17,PhoneUi.Muted);
            lightButton=FrameButton(area,"Phone home Lighting","",.42f,.375f,.94f,.535f,()=>sender.RequestMenu(DriveMenuAction.CycleLighting),visuals!=null?visuals.playButton:null,20);lighting=lightButton.GetComponentInChildren<TMP_Text>();
            FrameButton(area,"Phone options Back","BACK",.055f,.075f,.51f,.245f,Back,visuals!=null?visuals.optionsButton:null);
        }
        private void Activate(int slot)
        {
            var entry=cards[slot].entry;if(entry==null||!sender.HasHomeState||sender.MenuPending)return;
            sender.RequestVehicle(entry.id,!entry.owned);
        }
        private void DrawShop()
        {
            if(displayed==null)return;var entries=displayed.vehicles;int pages=Mathf.Max(1,(entries.Length+5)/6);shopPage=Mathf.Clamp(shopPage,0,pages-1);
            shopBalance.text=displayed.coins.ToString("N0")+" COINS";shopMessage.text=displayed.message??"";
            previous.gameObject.SetActive(pages>1);next.gameObject.SetActive(pages>1);pageLabel.gameObject.SetActive(pages>1);previous.interactable=shopPage>0;next.interactable=shopPage<pages-1;pageLabel.text=(shopPage+1)+" / "+pages;
            for(int i=0;i<cards.Length;i++)
            {
                int index=shopPage*6+i;var card=cards[i];card.title.transform.parent.gameObject.SetActive(index<entries.Length);card.entry=index<entries.Length?entries[index]:null;if(card.entry==null)continue;
                var entry=card.entry;var local=catalog!=null?catalog.Find(entry.id):null;card.preview.sprite=local!=null?local.thumbnail:null;card.preview.enabled=card.preview.sprite!=null;
                card.title.text=entry.title.ToUpperInvariant();bool selected=entry.owned&&displayed.selected==entry.id;
                card.price.text=entry.owned?(selected?"SELECTED":"OWNED"):entry.price.ToString("N0")+" COINS";
                card.caption.text=selected?"DRIVING":entry.owned?"SELECT":displayed.coins>=entry.price?"BUY":(entry.price-displayed.coins).ToString("N0")+" MORE";
            }
        }
        private void Update()
        {
            if(root==null)return;
            if(actionReady&&waitingAction.HasValue&&sender.HasMenuState&&!sender.MenuPending&&!pairing.IsOpen)
            {
                var action=waitingAction.Value;sender.RequestMenu(action);waitingAction=null;actionReady=false;launchPending=action==DriveMenuAction.Play;intentDeadline=Time.realtimeSinceStartup+3;
            }
            if(launchPending&&sender.HasMenuState&&sender.MenuState.view==DriveMenuView.Driving&&!sender.MenuPending)launchPending=false;
            if(intentDeadline>0&&Time.realtimeSinceStartup>intentDeadline){waitingAction=null;actionReady=launchPending=false;intentDeadline=0;}
            bool visible=!sender.HasMenuState||sender.MenuState.view==DriveMenuView.Home||waitingAction.HasValue||launchPending||pairing.IsOpen;
            root.gameObject.SetActive(visible);if(!visible)return;if(!pairing.IsVisible)root.SetAsLastSibling();
            bool fresh=sender.HasHomeState;
            if(!sender.Connected)displayed=null;
            if(fresh&&(displayed==null||drawnRevision!=sender.HomeState.revision))
            {displayed=sender.HomeState;drawnRevision=displayed.revision;DrawShop();}
            var page=displayed!=null?displayed.page:PhoneHomePage.Home;
            home.SetActive(page==PhoneHomePage.Home);shop.SetActive(page==PhoneHomePage.Shop);options.SetActive(page==PhoneHomePage.Options);
            bool busy=waitingAction.HasValue||launchPending||sender.MenuPending;
            play.interactable=shopButton.interactable=optionsButton.interactable=!busy;
            if (mobilePlay != null) mobilePlay.interactable = !busy;
            status.text=sender.Connected?(sender.HasMenuState?"PC CONNECTED":"SYNCING PC…"):"NOT CONNECTED";
            balance.text=displayed!=null?displayed.coins.ToString("N0")+" COINS   /   "+SelectedTitle():"";
            if(displayed!=null)
            {
                volume.text="MASTER VOLUME   "+sender.MenuState.volume+"%";volumeFill.anchorMax=new Vector2(Mathf.Lerp(.06f,.94f,sender.MenuState.volume/100f),.605f);
                lighting.text=displayed.lighting<0?"UNAVAILABLE":displayed.lighting==0?"AUTO  >":((CityTimeMode)displayed.lighting).ToString().ToUpperInvariant()+"  >";
                minus.interactable=plus.interactable=fresh&&!busy;lightButton.interactable=fresh&&!busy&&displayed.lighting>=0;
                foreach(var c in cards)if(c.entry!=null)c.action.interactable=fresh&&!busy&&displayed.canPurchase&&displayed.selected!=c.entry.id&&(c.entry.owned||displayed.coins>=c.entry.price);
            }
        }
        private string SelectedTitle(){foreach(var v in displayed.vehicles)if(v.id==displayed.selected)return v.title;return "";}
        private static void FooterButton(UnityEngine.UI.Button button){var r=button.GetComponent<RectTransform>();r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(0,5);r.sizeDelta=new Vector2(0,50);}
        private static UnityEngine.UI.Button FrameButton(Transform parent,string name,string value,float x0,float y0,float x1,float y1,UnityEngine.Events.UnityAction action,Sprite sprite,int size=27)
        {
            var rect=PhoneUi.Rect(parent,name,x0,y0,x1,y1);var visual=PhoneUi.Rect(rect,"Button visual",0,0,1,1);var image=visual.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.color=Color.white;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();ButtonSound.Attach(button);button.targetGraphic=image;button.onClick.AddListener(action);var colors=button.colors;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.65f,.8f,.9f);colors.disabledColor=new Color(.4f,.45f,.5f,.8f);button.colors=colors;
            var nav=button.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=nav;
            PhoneUi.Text(visual,"Caption",value,.10f,.12f,.90f,.88f,size,PhoneUi.White,true,TextAlignmentOptions.Center);rect.gameObject.AddComponent<MenuButtonMotion>().Initialize(visual);return button;
        }
        private void OnDestroy(){if(pairing!=null){pairing.ConnectionCompleted-=Connected;pairing.Cancelled-=Cancelled;}if(root!=null)Destroy(root.gameObject);}
    }
}
