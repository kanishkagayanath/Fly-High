using TMPro;
using UnityEngine;
namespace FlyHigh.PhoneController
{
    public sealed class PhonePausePanel : MonoBehaviour
    {
        private PhoneSender sender;
        private RectTransform root,volumeFill;
        private GameObject actions,settings;
        private TMP_Text title,volume,pauseLabel;
        private UnityEngine.UI.Button pause;
        private System.Func<bool> canShow;
        private readonly System.Collections.Generic.List<UnityEngine.UI.Button> controls=new System.Collections.Generic.List<UnityEngine.UI.Button>();
        public void Initialize(RectTransform parent,PhoneSender connection,System.Func<bool> allowed=null)
        {
            sender=connection;canShow=allowed;pause=Command(parent,"Phone PAUSE","PAUSE",.835f,.88f,.96f,.96f,DriveMenuAction.Pause,PhoneUi.Surface,PhoneUi.Cyan);pauseLabel=pause.GetComponentInChildren<TMP_Text>();
            root=PhoneUi.Rect(parent,"Phone pause menu",0,0,1,1);root.gameObject.AddComponent<UnityEngine.UI.Image>().color=PhoneUi.Ink;
            title=PhoneUi.Text(root,"Pause title","DRIVE PAUSED",.20f,.79f,.80f,.94f,43,PhoneUi.White,true,TextAlignmentOptions.Center);
            actions=PhoneUi.Rect(root,"Phone pause actions",0,0,1,1).gameObject;
            controls.Add(Command(actions.transform,"Phone RESUME","RESUME",.25f,.56f,.75f,.73f,DriveMenuAction.Resume,PhoneUi.Cyan,PhoneUi.Cyan,PhoneUi.Ink));
            controls.Add(Command(actions.transform,"Phone SETTINGS","SETTINGS",.25f,.35f,.75f,.52f,DriveMenuAction.Settings,PhoneUi.Surface,PhoneUi.Edge));
            controls.Add(Command(actions.transform,"Phone HOME","BACK TO HOME",.25f,.14f,.75f,.31f,DriveMenuAction.Home,new Color(.36f,.17f,.10f),PhoneUi.Amber));
            settings=PhoneUi.Rect(root,"Phone sound settings",0,0,1,1).gameObject;
            var sound=PhoneUi.Panel(settings.transform,"Volume card",.06f,.31f,.94f,.68f,PhoneUi.Surface,PhoneUi.Edge).rectTransform;
            PhoneUi.Text(sound,"Volume caption","MASTER VOLUME",.04f,.65f,.70f,.94f,18,PhoneUi.Muted,true);
            volume=PhoneUi.Text(sound,"Volume amount","100%",.70f,.65f,.96f,.94f,27,PhoneUi.White,true,TextAlignmentOptions.Right);
            controls.Add(Command(sound,"Phone −","-",.04f,.13f,.16f,.53f,DriveMenuAction.VolumeDown,PhoneUi.Ink,PhoneUi.Edge));
            controls.Add(Command(sound,"Phone +","+",.84f,.13f,.96f,.53f,DriveMenuAction.VolumeUp,PhoneUi.Ink,PhoneUi.Edge));
            PhoneUi.Panel(sound,"Volume track",.21f,.29f,.79f,.37f,PhoneUi.Ink,PhoneUi.Ink);volumeFill=PhoneUi.Panel(sound,"Volume fill",.21f,.29f,.79f,.37f,PhoneUi.Cyan,PhoneUi.Cyan).rectTransform;
            controls.Add(Command(settings.transform,"Phone BACK","BACK",.06f,.10f,.40f,.245f,DriveMenuAction.Back,PhoneUi.Surface,PhoneUi.Edge));
            PhoneUi.Text(settings.transform,"Sound hint","Changes the game audio on your PC.",.45f,.10f,.94f,.245f,17,PhoneUi.Muted);
            root.gameObject.SetActive(false);
        }
        private void Update()
        {
            if(root==null)return;bool ready=sender.HasMenuState;var view=sender.MenuState.view;
            bool paused=ready&&(canShow==null||canShow())&&(view==DriveMenuView.Paused||view==DriveMenuView.Settings);root.gameObject.SetActive(paused);
            if(paused)
            {
                root.SetAsLastSibling();bool sound=view==DriveMenuView.Settings;actions.SetActive(!sound);settings.SetActive(sound);title.text=sound?"SOUND SETTINGS":"DRIVE PAUSED";
                volume.text=sender.MenuState.volume+"%";volumeFill.anchorMax=new Vector2(Mathf.Lerp(.21f,.79f,sender.MenuState.volume/100f),.37f);
                foreach(var button in controls)button.interactable=!sender.MenuPending;
            }
            pause.interactable=ready&&view==DriveMenuView.Driving&&!sender.MenuPending;pauseLabel.text=sender.MenuPending?"WAIT…":"PAUSE";
        }
        private UnityEngine.UI.Button Command(Transform parent,string name,string label,float x0,float y0,float x1,float y1,DriveMenuAction action,Color color,Color edge,Color? textColor=null)
        {return PhoneUi.Button(parent,name,label,x0,y0,x1,y1,()=>sender.RequestMenu(action),color,edge,textColor);}
        private void OnDestroy(){if(root!=null)Destroy(root.gameObject);if(pause!=null)Destroy(pause.gameObject);}
    }
}
