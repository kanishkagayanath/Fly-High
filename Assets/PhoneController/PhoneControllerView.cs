using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Landscape controller presentation. Driving, pairing and pause keep their existing owners.</summary>
    public sealed class PhoneControllerView : MonoBehaviour
    {
        private PhoneSender sender;
        private RectTransform root, wheel, marker;
        private GameObject background;
        private TMP_Text connectionLabel, guidance, steeringLabel;
        private PhonePanelGraphic connectionBadge;
        private PhonePairingPanel pairing;
        private PhoneHomePanel home;
        private HowToPlayGuide guide;
        private readonly UnityEngine.UI.Button[] pedals=new UnityEngine.UI.Button[3];
        private readonly HoldPedal[] holds=new HoldPedal[3];
        private bool? controlsReady;
        private float calibratedUntil;
        public RectTransform Root=>root;
        public void Initialize(RectTransform safeArea,PhoneSender connection)
        {
            sender=connection;
            var audio=GetComponent<GameAudio>();if(audio==null)audio=gameObject.AddComponent<GameAudio>();audio.InitializePhone(sender);
            var full=PhoneUi.Rect(safeArea.parent,"Phone screen background",0,0,1,1);full.SetAsFirstSibling();background=full.gameObject;
            full.gameObject.AddComponent<UnityEngine.UI.Image>().color=PhoneUi.Ink;
            root=PhoneUi.Rect(safeArea,"Phone dashboard",0,0,1,1);
            PhoneUi.Panel(root,"Dashboard inset",.018f,.024f,.982f,.976f,new Color(.035f,.065f,.105f),PhoneUi.Edge);
            PhoneUi.Text(root,"Brand","FLY HIGH",.042f,.888f,.31f,.962f,35,PhoneUi.White,true).characterSpacing=5;
            PhoneUi.Text(root,"Brand caption","PHONE CONTROLLER",.043f,.858f,.31f,.89f,13,PhoneUi.Cyan,true).characterSpacing=3;
            connectionBadge=PhoneUi.Panel(root,"Connection badge",.355f,.884f,.64f,.96f,PhoneUi.Surface,PhoneUi.Edge);
            connectionLabel=PhoneUi.Text(connectionBadge.transform,"Connection state","OFFLINE",.05f,.12f,.95f,.88f,20,PhoneUi.Amber,true,TextAlignmentOptions.Center);
            var link=PhoneUi.Button(root,"Connection button","CONNECTION",.675f,.88f,.817f,.96f,()=>pairing.Open(),PhoneUi.Surface,PhoneUi.Edge);
            PhoneUi.Panel(root,"Header line",.04f,.842f,.96f,.845f,PhoneUi.Edge,PhoneUi.Edge);
            pedals[0]=Pedal("Reverse pedal","REVERSE","HOLD TO BACK UP",.04f,.445f,.28f,.68f,new Color(.20f,.16f,.36f),new Color(.59f,.48f,.9f),false);
            pedals[1]=Pedal("Brake pedal","BRAKE","HOLD TO SLOW DOWN",.04f,.16f,.28f,.42f,new Color(.78f,.24f,.16f),new Color(1,.53f,.32f),false);
            pedals[2]=Pedal("Accelerate pedal","ACCELERATE","HOLD TO DRIVE",.72f,.16f,.96f,.68f,new Color(.12f,.73f,.73f),PhoneUi.Cyan,true);
            holds[0].Changed=held=>sender.Reverse=held?1:0;holds[1].Changed=held=>sender.Brake=held?1:0;holds[2].Changed=held=>sender.Throttle=held?1:0;
            PhoneUi.Text(root,"Steering title","TILT TO STEER",.33f,.65f,.67f,.70f,16,PhoneUi.Muted,true,TextAlignmentOptions.Center).characterSpacing=3;
            var wheelBounds=PhoneUi.Rect(root,"Steering bounds",.365f,.285f,.635f,.64f);
            var wheelArea=PhoneUi.Rect(wheelBounds,"Steering gauge",0,0,1,1);
            var fit=wheelArea.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;
            wheel=PhoneUi.Rect(wheelArea,"Live steering wheel",0,0,1,1);wheel.gameObject.AddComponent<PhoneWheelGraphic>().raycastTarget=false;
            PhoneUi.Text(wheel,"Wheel centre","FH",.37f,.37f,.63f,.63f,22,PhoneUi.White,true,TextAlignmentOptions.Center);
            PhoneUi.Panel(root,"Steer track",.36f,.258f,.64f,.266f,PhoneUi.Edge,PhoneUi.Edge);
            marker=PhoneUi.Panel(root,"Steering position",.494f,.249f,.506f,.275f,PhoneUi.Cyan,PhoneUi.Cyan).rectTransform;
            steeringLabel=PhoneUi.Text(root,"Steer direction","CENTRED",.35f,.20f,.65f,.246f,14,PhoneUi.Muted,true,TextAlignmentOptions.Center);
            PhoneUi.Button(root,"Calibrate button","CALIBRATE",.385f,.095f,.615f,.175f,Calibrate,PhoneUi.Surface,PhoneUi.Cyan);
            guidance=PhoneUi.Text(root,"Controller guidance","Connect your phone to begin",.04f,.045f,.96f,.092f,15,PhoneUi.Muted,false,TextAlignmentOptions.Center);
            pairing=gameObject.AddComponent<PhonePairingPanel>();pairing.Initialize(root,sender);
            gameObject.AddComponent<PhoneMissionPanel>().Initialize(root,sender);
            guide=gameObject.AddComponent<HowToPlayGuide>();guide.Initialize(root,true,()=>sender.HasMenuState&&sender.MenuState.view==DriveMenuView.Driving);
            home=gameObject.AddComponent<PhoneHomePanel>();home.Initialize(root,sender,pairing,guide);
            gameObject.AddComponent<PhonePausePanel>().Initialize(root,sender,()=>!home.IsVisible&&!pairing.IsOpen&&!guide.IsOpen);
            guide.ShowFirstTime();
        }
        private UnityEngine.UI.Button Pedal(string name,string title,string subtitle,float x0,float y0,float x1,float y1,Color fill,Color edge,bool tall)
        {
            var button=PhoneUi.Button(root,name,"",x0,y0,x1,y1,null,fill,edge);var visual=button.targetGraphic.transform;
            var ink=tall?PhoneUi.Ink:PhoneUi.White;
            PhoneUi.Text(visual,"Pedal title",title,.08f,tall?.25f:.42f,.92f,tall?.39f:.70f,30,ink,true,TextAlignmentOptions.Center);
            PhoneUi.Text(visual,"Pedal instruction",subtitle,.06f,tall?.12f:.17f,.94f,tall?.23f:.37f,14,ink,false,TextAlignmentOptions.Center);
            if(tall){for(int i=0;i<4;i++)PhoneUi.Panel(visual,"Pedal grip "+i,.29f,.53f+i*.065f,.71f,.55f+i*.065f,new Color(.03f,.28f,.33f,.75f),Color.clear);}
            holds[tall?2:name=="Reverse pedal"?0:1]=button.gameObject.AddComponent<HoldPedal>();return button;
        }
        private void Calibrate(){sender.Calibrate();calibratedUntil=Time.realtimeSinceStartup+2;}
        private void Update()
        {
            if(root==null)return;
            bool ready=sender.DrivingControlsAvailable&&!pairing.IsOpen&&!home.IsVisible&&!guide.IsOpen;
            if(controlsReady!=ready)
            {
                controlsReady=ready;
                for(int i=0;i<pedals.Length;i++){pedals[i].interactable=ready;holds[i].Release();}
            }
            if(!ready)sender.Throttle=sender.Reverse=sender.Brake=0;
            float steer=sender.Connected?sender.Steering:0;
            wheel.localRotation=Quaternion.Euler(0,0,-steer*42);
            marker.anchorMin=new Vector2(.494f+steer*.134f,.249f);marker.anchorMax=new Vector2(.506f+steer*.134f,.275f);
            steeringLabel.text=Mathf.Abs(steer)<.04f?"CENTRED":steer<0?"STEERING LEFT":"STEERING RIGHT";
            bool connected=sender.Connected;
            connectionLabel.text=connected?sender.HasMenuState&&sender.MenuState.view!=DriveMenuView.Driving?"CONNECTED / MENU":"CONNECTED / READY":sender.Searching?"SEARCHING…":"OFFLINE";
            connectionLabel.color=connected?PhoneUi.Cyan:PhoneUi.Amber;
            guidance.text=Time.realtimeSinceStartup<calibratedUntil?"Steering centred. You're ready to drive.":!connected?sender.Status:
                sender.HasMenuState&&sender.MenuState.view==DriveMenuView.Home?"Tap PLAY on the phone home screen to begin":"Hold landscape. Tilt gently to steer. Release pedals to slow down.";
        }
        private void OnDestroy(){if(root!=null)Destroy(root.gameObject);if(background!=null)Destroy(background);}
    }
}
