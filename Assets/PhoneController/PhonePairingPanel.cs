using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    public sealed class PhonePairingPanel : MonoBehaviour
    {
        private PhoneSender sender;
        private TMP_InputField address, code;
        private RectTransform overlay;
        private MenuReveal reveal;
        private TMP_Text searchStatus,emptyStatus;
        private readonly UnityEngine.UI.Button[] rows=new UnityEngine.UI.Button[ControllerDiscovery.MaximumGames];
        private readonly TMP_Text[] labels=new TMP_Text[ControllerDiscovery.MaximumGames];
        private int revision=-1;
        private bool connecting;
        private UnityEngine.UI.Button continueButton;
        public event System.Action ConnectionCompleted;
        public event System.Action Cancelled;
        public bool IsOpen => reveal != null && reveal.IsShown;
        internal bool IsVisible => overlay != null && overlay.gameObject.activeSelf;
        public void Initialize(RectTransform parent,PhoneSender phone)
        {
            sender=phone;overlay=PhoneUi.Rect(parent,"Choose PC",0,0,1,1);
            ViewportBackdrop.Create(overlay, PhoneUi.Ink);
            var content = PhoneUi.Rect(overlay, "Pairing content", 0, 0, 1, 1);
            PhoneUi.Text(content,"Connection heading","LET'S CONNECT",.04f,.84f,.72f,.96f,40,PhoneUi.White,true);
            PhoneUi.Text(content,"Connection hint","Your phone and PC must use the same Wi-Fi.",.04f,.76f,.80f,.84f,20,PhoneUi.Muted);
            PhoneUi.Button(content,"Connection back","BACK",.84f,.85f,.96f,.95f,Close,PhoneUi.Surface,PhoneUi.Edge);
            continueButton=PhoneUi.Button(content,"Connection continue","CONTINUE",.65f,.85f,.81f,.95f,CompleteConnection,PhoneUi.Cyan,PhoneUi.Cyan,PhoneUi.Ink);
            continueButton.gameObject.SetActive(false);
            var nearby=PhoneUi.Panel(content,"Nearby PC card",.04f,.185f,.58f,.735f,PhoneUi.Surface,PhoneUi.Edge).rectTransform;
            PhoneUi.Text(nearby,"Nearby title","NEARBY PCs",.04f,.83f,.96f,.96f,22,PhoneUi.Cyan,true);
            searchStatus=PhoneUi.Text(nearby,"Search status","",.04f,.64f,.96f,.83f,18,PhoneUi.Muted);searchStatus.textWrappingMode=TextWrappingModes.Normal;
            var scrollRoot=PhoneUi.Rect(nearby,"PC list",.035f,.035f,.965f,.63f);var scroll=scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport=PhoneUi.Rect(scrollRoot,"Viewport",0,0,1,1);viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.white;
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
            var listContent=PhoneUi.Rect(viewport,"Content",0,1,1,1);listContent.pivot=new Vector2(.5f,1);
            var layout=listContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=10;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            listContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=listContent;scroll.horizontal=false;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            for(int i=0;i<rows.Length;i++)
            {
                int index=i;rows[i]=PhoneUi.Button(listContent,"PC "+i,"",0,0,1,1,()=>Choose(index),new Color(.08f,.18f,.24f),PhoneUi.Edge);
                rows[i].gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=82;
                labels[i]=rows[i].GetComponentInChildren<TMP_Text>();labels[i].fontSizeMax=22;labels[i].textWrappingMode=TextWrappingModes.Normal;
                rows[i].gameObject.SetActive(false);
            }
            emptyStatus=PhoneUi.Text(nearby,"Empty discovery list","",.06f,.12f,.94f,.54f,22,PhoneUi.Muted,false,TextAlignmentOptions.Center);emptyStatus.textWrappingMode=TextWrappingModes.Normal;
            var manual=PhoneUi.Panel(content,"Manual connection card",.61f,.185f,.96f,.735f,PhoneUi.Surface,PhoneUi.Edge).rectTransform;
            PhoneUi.Text(manual,"Manual title","MANUAL CONNECTION",.07f,.83f,.93f,.97f,21,PhoneUi.White,true);
            PhoneUi.Text(manual,"Address caption","PC ADDRESS",.07f,.73f,.93f,.83f,13,PhoneUi.Muted,true);address=PhoneUi.Input(manual,"PC address","e.g. 192.168.1.7",.58f,false);
            PhoneUi.Text(manual,"Code caption","PAIRING CODE",.07f,.43f,.93f,.56f,13,PhoneUi.Muted,true);code=PhoneUi.Input(manual,"Pairing code","6-digit code",.29f,true);
            PhoneUi.Button(manual,"Connect button","CONNECT",.07f,.065f,.93f,.235f,ConnectManual,PhoneUi.Cyan,PhoneUi.Cyan,PhoneUi.Ink);
            PhoneUi.Button(content,"Find PCs button","FIND PCs AGAIN",.04f,.05f,.35f,.145f,Search,PhoneUi.Cyan,PhoneUi.Cyan,PhoneUi.Ink);
            PhoneUi.Text(content,"Connect footer","Open Fly-High on your PC, then choose it here.",.39f,.05f,.96f,.145f,17,PhoneUi.Muted);
            overlay.gameObject.SetActive(false);
            reveal = MenuReveal.Attach(overlay.gameObject, content);
        }
        public void Open(){overlay.SetAsLastSibling();reveal.SetVisible(true);sender.Throttle=sender.Reverse=sender.Brake=0;connecting=false;if(!sender.Connected)Search();}
        private void Search(){connecting=false;sender.FindPCs();revision=-1;}
        private void Close(){if(connecting&&!sender.Connected)sender.Disconnect();else sender.CancelDiscovery();reveal.SetVisible(false);connecting=false;Cancelled?.Invoke();}
        private void CompleteConnection(){if(!sender.Connected)return;sender.CancelDiscovery();reveal.SetVisible(false);connecting=false;ConnectionCompleted?.Invoke();}
        private void ConnectManual(){sender.Connect(address.text,code.text);connecting=true;}
        private void Choose(int index)
        {
            if(index>=sender.Games.Count||sender.Games[index].Busy)return;
            var pc=sender.Games[index];address.text=pc.Address;code.text=pc.PairingCode;sender.Connect(pc.Address,pc.PairingCode);connecting=true;
        }
        private void Update()
        {
            if(!IsOpen)return;
            continueButton.gameObject.SetActive(sender.Connected);
            if(connecting&&sender.Connected){CompleteConnection();return;}
            if(searchStatus.text!=sender.Status)searchStatus.text=sender.Status;
            emptyStatus.gameObject.SetActive(sender.Games.Count==0);emptyStatus.text=sender.Searching?"Looking for your PC…\nIt will appear here.":"No PC found yet.\nTry Find PCs Again or connect manually.";
            if(revision==sender.DiscoveryVersion)return;revision=sender.DiscoveryVersion;
            for(int i=0;i<rows.Length;i++)
            {
                bool visible=i<sender.Games.Count;rows[i].gameObject.SetActive(visible);if(!visible)continue;
                var pc=sender.Games[i];labels[i].text=pc.Name+"  /  "+(pc.Busy?"IN USE":"TAP TO CONNECT")+"\n"+pc.Address;rows[i].interactable=!pc.Busy;
            }
        }
        private void OnDisable(){if(sender!=null)sender.CancelDiscovery();if(overlay!=null)overlay.gameObject.SetActive(false);}
        private void OnDestroy(){if(overlay!=null)Destroy(overlay.gameObject);}
    }
}
