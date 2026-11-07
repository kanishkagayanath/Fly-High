using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlyHigh.PhoneController
{
    /// <summary>Owns mission world visuals and HUD; it never awards currency or accepts phone commands.</summary>
    public sealed class CoinMissionPresentation : MonoBehaviour
    {
        private CoinMissionSystem mission;
        private Transform world, coinsRoot, finish;
        private Transform[] coins, markers;
        private readonly List<TextMeshPro> worldLabels = new List<TextMeshPro>();
        private RectTransform hudRoot, card, wallet;
        private TMP_Text title, guidance, progress, walletText, buttonText;
        private UnityEngine.UI.Button startButton;
        private UnityEngine.UI.Image progressFill;
        private MissionMapOverlay map;
        private Material markerMaterial;
        private Camera view;
        private bool onPhone;
        private CoinMissionRoute shownRoute;
        private int lastVersion = -1, shownWallet = -1;
        private float nextGuidance, pickupFlash;
        
        private const float CoinFloatOffset = .40f;
        private const float CoinBobAmplitude = .08f;
        private static readonly Color Gold = new Color(1,0.76f,0.21f);
        private static readonly Color Mint = new Color(0.31f,1,0.81f);
        private static readonly Color Ink = new Color(0.025f,0.075f,0.11f,0.94f);

        public void Initialize(CoinMissionSystem owner, RectTransform hud, CityMinimap minimap, bool mobile = false)
        {
            mission = owner; view = Camera.main;
            onPhone = mobile;
            world = new GameObject("Coin mission world").transform; world.SetParent(transform,false);
            markerMaterial = new Material(mission.Settings.markerMaterial);
            markerMaterial.name = "Mission marker runtime"; markerMaterial.color = Gold;
            markers = new Transform[mission.Settings.routes.Length];
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i] = new GameObject("M - " + mission.Settings.routes[i].title).transform;
                markers[i].SetParent(world,false); markers[i].position = mission.Settings.routes[i].path[0] - Vector3.up*0.34f;
                Ring(markers[i],"Mission start ring",3,Gold);
                WorldLabel(markers[i],"M",new Vector3(0,2.6f,0),Gold);
            }
            hudRoot = MissionUi.Rect(hud,"Mission HUD",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            card = MissionUi.Rect(hudRoot,"Mission guidance",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0, onPhone ? -86 : -22), new Vector2(464, onPhone ? 144 : 132));
            card.pivot = new Vector2(.5f,1);
            MissionUi.Background(card,Ink,Gold);
            title = MissionUi.Text(card,"Mission title",new Vector2(16,-10),new Vector2(432,27),22,Gold);
            guidance = MissionUi.Text(card,"Mission instruction",new Vector2(16,-40),new Vector2(432,26),16,Color.white);
            progress = MissionUi.Text(card,"Mission progress",new Vector2(16,-73),new Vector2(235,24),15,Mint);
            var bar=MissionUi.Rect(card,"Coin progress track",Vector2.up,Vector2.up,new Vector2(16, onPhone ? -127 : -111),new Vector2(432,4));
            bar.pivot=Vector2.up; MissionUi.Fill(bar,new Color(.15f,.23f,.25f));
            var fill=MissionUi.Rect(bar,"Coin progress",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            progressFill=MissionUi.Fill(fill,Gold);
            var button=MissionUi.Rect(card,"Start Mission",Vector2.one,Vector2.one,new Vector2(-16,-76),new Vector2(165, onPhone ? 46 : 30));
            button.pivot=Vector2.one;
            var buttonVisual = PhoneUi.Rect(button, "Start button visual", 0, 0, 1, 1);
            var background=MissionUi.Fill(buttonVisual,Gold);
            startButton=button.gameObject.AddComponent<UnityEngine.UI.Button>();ButtonSound.Attach(startButton); startButton.targetGraphic=background;
            startButton.onClick.AddListener(()=>mission.TryStartMission());
            buttonText=MissionUi.Text(buttonVisual,"Start label",new Vector2(0,0),new Vector2(165, onPhone ? 46 : 30),15,Ink);
            buttonText.alignment=TextAlignmentOptions.Center;
            button.gameObject.AddComponent<MenuButtonMotion>().Initialize(buttonVisual);
            wallet=MissionUi.Rect(hudRoot,"Coin wallet",Vector2.one,Vector2.one,new Vector2(-20,-20),new Vector2(220,44));
            wallet.pivot=Vector2.one; MissionUi.Background(wallet,Ink,Gold);
            var icon=MissionUi.Disc(wallet,"Gold coin icon",26,Gold); icon.anchoredPosition=new Vector2(-85,-22);
            var symbol=MissionUi.Text(icon,"Coin symbol",new Vector2(-13,13),new Vector2(26,26),18,Ink); symbol.rectTransform.anchorMin=symbol.rectTransform.anchorMax=symbol.rectTransform.pivot=Vector2.one*.5f; symbol.rectTransform.anchoredPosition=Vector2.zero; symbol.text="$"; symbol.alignment=TextAlignmentOptions.Center;
            walletText=MissionUi.Text(wallet,"Collected coins",new Vector2(-164,-8),new Vector2(152,28),21,Color.white);
            walletText.rectTransform.anchorMin=walletText.rectTransform.anchorMax=Vector2.one;
            walletText.alignment=TextAlignmentOptions.MidlineRight;
            map=gameObject.AddComponent<MissionMapOverlay>(); map.Initialize(mission,minimap);
            card.gameObject.SetActive(false);
            MenuReveal.Attach(card.gameObject);
            Refresh();
        }

        public void Begin(CoinMissionRoute route)
        {
            shownRoute=route;
            ClearRun();
            coinsRoot=new GameObject("Floating mission coins").transform; coinsRoot.SetParent(world,false);
            coins=new Transform[route.coins.Length];
            for(int i=0;i<coins.Length;i++)
            {
                coins[i]=Model(mission.Settings.coinModel,coinsRoot,"Route coin "+(i+1),.92f);
                coins[i].position=route.coins[i]+Vector3.up*CoinFloatOffset;
            }
            finish=new GameObject("Coin sack finish").transform; finish.SetParent(world,false);
            finish.position=route.path[route.path.Length-1]-Vector3.up*.34f;
            Ring(finish,"Finish ring",2.8f,Gold);
            for(int i=0;i<3;i++)
            {
                var bag=Model(mission.Settings.sackModel,finish,"Finish coin sack "+(i+1),i==0?1.55f:1.05f);
                bag.localPosition=new Vector3(i==0?0:i==1?-1:1,i==0?1.75f:1.4f,i==0?0:.35f);
            }
            WorldLabel(finish,"COLLECT ALL COINS",new Vector3(0,3.8f,0),Gold);
            map.Begin(route); Refresh();
        }

        public void Pickup(int index)
        {
            if(coins!=null && index<coins.Length)coins[index].gameObject.SetActive(false);
            pickupFlash=1; map.Pickup(index);
        }
        public void Complete() { ClearRun(); map.ClearRun(); Refresh(); }

        private void ClearRun()
        {
            if(coinsRoot!=null)Destroy(coinsRoot.gameObject);
            if(finish!=null) { foreach(var label in finish.GetComponentsInChildren<TextMeshPro>())worldLabels.Remove(label); Destroy(finish.gameObject); }
            coins=null; finish=null;
        }

        private void LateUpdate()
        {
            if(mission==null || world==null)return;
            if(lastVersion!=mission.Version)Refresh();
            if(Time.timeScale<=0 || !hudRoot.gameObject.activeInHierarchy)return;
            float time=Time.time;
            for(int i=0;i<markers.Length;i++)markers[i].localScale=Vector3.one*(1+.035f*Mathf.Sin(time*2+i));
            foreach(var label in worldLabels)if(label!=null && view!=null)label.transform.rotation=view.transform.rotation;
            if(coins!=null)for(int i=0;i<coins.Length;i++)if(coins[i].gameObject.activeSelf)
            {
                coins[i].position=shownRoute.coins[i]+Vector3.up*(CoinFloatOffset+CoinBobAmplitude*Mathf.Sin(time*2.8f+i*.65f));
                coins[i].rotation=Quaternion.Euler(0,time*65+i*29,0);
            }
            if(finish!=null)
            {
                for(int i=1;i<=3;i++)
                {
                    var bag=finish.GetChild(i); bag.localPosition=new Vector3(i==1?0:i==2?-1:1,(i==1?1.75f:1.4f)+.14f*Mathf.Sin(time*2+i),i==1?0:.35f);
                    bag.localRotation=Quaternion.Euler(0,time*23+i*70,0);
                }
                var label=finish.GetComponentInChildren<TextMeshPro>();
                string text=mission.Collected==mission.Total?"FINISH  +"+mission.Settings.finishBonus:"COLLECT ALL COINS";
                if(label.text!=text)label.text=text;
            }
            pickupFlash=Mathf.MoveTowards(pickupFlash,0,Time.deltaTime*2.8f);
            wallet.localScale=Vector3.one*(1+.035f*pickupFlash);
            if(Time.time>=nextGuidance) {nextGuidance=Time.time+.2f; Guidance();}
        }

        private void Refresh()
        {
            lastVersion=mission.Version;
            for(int i=0;i<markers.Length;i++)markers[i].gameObject.SetActive(mission.Phase==CoinMissionPhase.Exploring || mission.Phase==CoinMissionPhase.Available);
            card.gameObject.SetActive(mission.Phase!=CoinMissionPhase.Exploring);
            startButton.gameObject.SetActive(mission.Phase==CoinMissionPhase.Available);
            if(shownWallet!=mission.Wallet){shownWallet=mission.Wallet;walletText.text=shownWallet.ToString("N0")+"  COINS";}
            switch(mission.Phase)
            {
                case CoinMissionPhase.Available:
                    var route=mission.Settings.routes[mission.OfferedIndex];
                    title.text=route.title.ToUpperInvariant();
                    progress.text=route.distance.ToString("F0")+" m   /   "+route.coins.Length+" coins";
                    progressFill.rectTransform.anchorMax=new Vector2(0,1);break;
                case CoinMissionPhase.Collecting:
                    title.text=shownRoute.title.ToUpperInvariant();
                    progress.text=mission.Collected+" / "+mission.Total+" COINS";
                    progressFill.rectTransform.anchorMax=new Vector2(mission.Collected/(float)mission.Total,1);break;
                case CoinMissionPhase.Complete:
                    title.text="MISSION COMPLETE";
                    guidance.text="All coins collected. Find another M to keep driving.";
                    progress.text="+"+mission.Settings.finishBonus+" COIN SACK BONUS";
                    progressFill.rectTransform.anchorMax=Vector2.one;break;
            }
            Guidance();
        }

        private void Guidance()
        {
            if(mission.Phase==CoinMissionPhase.Available)
            {
                bool ready=mission.CanStart;
                startButton.interactable=ready;
                buttonText.text = ready ? (onPhone ? "START MISSION" : "START MISSION  [E]") : "SLOW DOWN";
                guidance.text = ready ? (onPhone ? "Tap START MISSION to begin this coin run." : "Tap START MISSION on your phone, or click here.") : "Stop inside the M ring to start this coin run.";
            }
            else if(mission.Phase==CoinMissionPhase.Collecting)
                guidance.text=mission.Collected==mission.Total?"All coins collected! Drive into the floating coin sacks.":"Follow the gold route. Collect every coin, then the sacks.";
        }

        private Transform Model(GameObject prefab,Transform parent,string name,float size)
        {
            var holder=new GameObject(name).transform;holder.SetParent(parent,false);
            var model=Instantiate(prefab,holder,false);model.name="Imported model";
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var physics in model.GetComponentsInChildren<Rigidbody>(true)){physics.isKinematic=true;physics.detectCollisions=false;Destroy(physics);}
            var renderers=model.GetComponentsInChildren<Renderer>();
            var material=prefab==mission.Settings.coinModel?mission.Settings.coinMaterial:mission.Settings.sackMaterial;
            if(material!=null)foreach(var renderer in renderers)renderer.sharedMaterial=material;
            if(renderers.Length>0)
            {
                Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                model.transform.localScale*=size/Mathf.Max(.001f,Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z));
                bounds=renderers[0].bounds;foreach(var renderer in renderers){bounds.Encapsulate(renderer.bounds);renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
                model.transform.localPosition-=holder.InverseTransformPoint(bounds.center);
            }
            return holder;
        }
        private void Ring(Transform parent,string name,float radius,Color color)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(parent,false);
            line.sharedMaterial=markerMaterial;line.useWorldSpace=false;line.loop=true;line.widthMultiplier=.11f;
            line.positionCount=64;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,.03f,Mathf.Sin(a)*radius));}
        }
        private void WorldLabel(Transform parent,string text,Vector3 position,Color color)
        {
            var label=new GameObject(text,typeof(RectTransform)).AddComponent<TextMeshPro>();
            label.transform.SetParent(parent,false);label.transform.localPosition=position;label.rectTransform.sizeDelta=new Vector2(12,3);
            label.text=text;label.fontSize=text=="M"?20:5;label.color=color;label.fontStyle=FontStyles.Bold;
            label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;worldLabels.Add(label);
        }
        private void OnEnable(){if(world!=null)world.gameObject.SetActive(true);if(hudRoot!=null)hudRoot.gameObject.SetActive(true);if(map!=null)map.enabled=true;lastVersion=-1;}
        private void OnDisable(){if(world!=null)world.gameObject.SetActive(false);if(hudRoot!=null)hudRoot.gameObject.SetActive(false);if(map!=null)map.enabled=false;}
        private void OnDestroy(){if(world!=null)Destroy(world.gameObject);if(hudRoot!=null)Destroy(hudRoot.gameObject);if(markerMaterial!=null)Destroy(markerMaterial);if(map!=null)Destroy(map);}
    }

    internal static class MissionUi
    {
        public static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max,Vector2 position,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=min;rect.anchorMax=max;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        public static UnityEngine.UI.Image Fill(RectTransform rect,Color color)
        {var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;}
        public static void Background(RectTransform rect,Color color,Color accent)
        {
            Fill(rect,color);
            var edge=Rect(rect,"Gold accent",Vector2.up,Vector2.one,Vector2.zero,new Vector2(0,2));edge.pivot=Vector2.up;Fill(edge,accent);
        }
        public static TMP_Text Text(Transform parent,string name,Vector2 position,Vector2 size,int font,Color color)
        {
            var rect=Rect(parent,name,Vector2.up,Vector2.up,position,size);rect.pivot=Vector2.up;
            var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.fontSize=font;text.color=color;text.raycastTarget=false;
            text.alignment=TextAlignmentOptions.MidlineLeft;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;return text;
        }
        public static RectTransform Disc(Transform parent,string name,float size,Color color)
        {
            var rect=Rect(parent,name,new Vector2(.5f,1),new Vector2(.5f,1),Vector2.zero,new Vector2(size,size));
            rect.gameObject.AddComponent<MinimapGraphic>().SetCircle(color);return rect;
        }
    }
}

