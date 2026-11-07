using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FlyHigh.PhoneController
{
    /// <summary>Skippable, local first-run help. It never connects a phone or starts gameplay.</summary>
    [DisallowMultipleComponent]
    public sealed class HowToPlayGuide : MonoBehaviour
    {
        public const string PcSeenKey="FlyHigh.Guide.PC.v1",PhoneSeenKey="FlyHigh.Guide.Phone.v1";
        private static readonly string[] Titles={"PC game + Android controller","Connect on the same Wi-Fi","Centre your steering","Hold the pedals to drive","Find missions. Earn coins."};
        private static readonly string[] Captions={"TWO SCREENS, ONE DRIVE","CONNECT YOUR DEVICES","CALIBRATE, THEN TILT","FORWARD / BRAKE / REVERSE","FOLLOW THE GOLD ROUTE"};
        private RectTransform root,progress;
        private TMP_Text heading,body,hint,counter,caption,nextLabel;
        private UnityEngine.UI.Button back,next;
        private GuideIllustration illustration;
        private MenuReveal reveal;
        private GameObject previousSelection;
        private Func<bool> gameplayStarted;
        private Func<string> connectionDetails;
        private bool phone,checkedFirstRun;
        private int openedFrame=-1,closedFrame=-1;
        public bool IsOpen => reveal != null && reveal.IsShown;
        public bool ConsumesKeyboardThisFrame=>IsOpen||closedFrame==Time.frameCount;
        public int Page {get;private set;}
        public RectTransform Root=>root;
        private string SeenKey=>phone?PhoneSeenKey:PcSeenKey;
        public void Initialize(RectTransform parent,bool onPhone,Func<bool> hasStarted=null,Func<string> pcConnectionDetails=null)
        {
            if(root!=null)return;phone=onPhone;gameplayStarted=hasStarted;connectionDetails=pcConnectionDetails;
            root=PhoneUi.Rect(parent,"How to Play guide",0,0,1,1);
            ViewportBackdrop.Create(root, new Color(.008f,.018f,.035f,.96f));
            var card=PhoneUi.Panel(root,"Guide card",.045f,.055f,.955f,.945f,PhoneUi.Surface,PhoneUi.Cyan).rectTransform;
            PhoneUi.Text(card,"Guide heading","HOW TO PLAY",.04f,.86f,.56f,.96f,32,PhoneUi.White,true).characterSpacing=2;
            counter=PhoneUi.Text(card,"Guide page","",.61f,.86f,.77f,.96f,20,PhoneUi.Muted,true,TextAlignmentOptions.Right);
            PhoneUi.Button(card,"Guide Skip","SKIP",.81f,.86f,.96f,.96f,()=>Close(true),PhoneUi.Ink,PhoneUi.Edge);
            PhoneUi.Panel(card,"Guide progress track",.04f,.823f,.96f,.831f,PhoneUi.Ink,PhoneUi.Ink);
            progress=PhoneUi.Panel(card,"Guide progress",.04f,.823f,.224f,.831f,PhoneUi.Cyan,PhoneUi.Cyan).rectTransform;
            var art=PhoneUi.Panel(card,"Guide illustration card",.045f,.245f,.375f,.78f,PhoneUi.Ink,PhoneUi.Edge).rectTransform;
            illustration=PhoneUi.Rect(art,"Guide illustration",.05f,.05f,.95f,.95f).gameObject.AddComponent<GuideIllustration>();illustration.raycastTarget=false;
            caption=PhoneUi.Text(card,"Guide illustration caption","",.045f,.18f,.375f,.245f,17,PhoneUi.Cyan,true,TextAlignmentOptions.Center);
            heading=PhoneUi.Text(card,"Guide step title","",.43f,.70f,.95f,.80f,30,PhoneUi.White,true);heading.textWrappingMode=TextWrappingModes.Normal;heading.fontSizeMin=20;
            body=PhoneUi.Text(card,"Guide instructions","",.43f,.38f,.95f,.69f,24,PhoneUi.White);body.textWrappingMode=TextWrappingModes.Normal;body.fontSizeMin=18;body.alignment=TextAlignmentOptions.TopLeft;
            var tip=PhoneUi.Panel(card,"Guide tip card",.43f,.18f,.95f,.355f,PhoneUi.Ink,PhoneUi.Edge).rectTransform;
            hint=PhoneUi.Text(tip,"Guide tip","",.045f,.10f,.955f,.90f,18,PhoneUi.Muted);hint.textWrappingMode=TextWrappingModes.Normal;hint.fontSizeMin=14;
            back=PhoneUi.Button(card,"Guide Back","BACK",.045f,.035f,.23f,.135f,Previous,PhoneUi.Ink,PhoneUi.Edge);
            next=PhoneUi.Button(card,"Guide Next","NEXT",.755f,.035f,.95f,.135f,Next,PhoneUi.Cyan,PhoneUi.Cyan,PhoneUi.Ink);nextLabel=next.GetComponentInChildren<TMP_Text>();
            root.gameObject.SetActive(false);
            reveal = MenuReveal.Attach(root.gameObject, card);
        }
        public void ShowFirstTime()
        {
            if(checkedFirstRun)return;checkedFirstRun=true;
            if(PlayerPrefs.GetInt(SeenKey,0)!=1)Open();
        }
        public void Open()
        {
            if(root==null||IsOpen)return;
            previousSelection=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            Page=0;root.SetAsLastSibling();openedFrame=Time.frameCount;reveal.SetVisible(true);Draw();
        }
        public void Close(bool remember)
        {
            if(!IsOpen)return;
            if(remember){PlayerPrefs.SetInt(SeenKey,1);PlayerPrefs.Save();}
            reveal.SetVisible(false);closedFrame=Time.frameCount;
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(previousSelection!=null&&previousSelection.activeInHierarchy?previousSelection:null);
            previousSelection=null;
        }
        private void Next(){if(Page==Titles.Length-1)Close(true);else{Page++;Draw();}}
        private void Previous(){if(Page>0){Page--;Draw();}}
        private void Draw()
        {
            heading.text=Titles[Page];caption.text=Captions[Page];counter.text=(Page+1)+" / "+Titles.Length;
            progress.anchorMax=new Vector2(Mathf.Lerp(.04f,.96f,(Page+1)/(float)Titles.Length),.831f);
            illustration.Page=Page;back.interactable=Page>0;nextLabel.text=Page==Titles.Length-1?"GOT IT":"NEXT";
            switch(Page)
            {
                case 0:
                    if (phone)
                    {
                        heading.text = "Choose where to play";
                        caption.text = "PHONE OR PC";
                        body.text = "Choose PLAY ON PHONE to drive the city on this screen with tilt and touch controls.\n\nChoose PLAY ON PC to use the phone as a controller for the PC game.";
                        hint.text = "Phone play works offline. PC play uses your local Wi-Fi network.";
                    }
                    else
                    {
                        body.text = "Fly-High runs the city on your PC. Your Android phone becomes the car controller.\n\nOpen the PC game, then open the Fly-High controller app on your phone.";
                        hint.text = "Leave this PC game open. You can start driving from your phone.";
                    }
                    break;
                case 1:
                    if (phone) heading.text = "Connect for PC play";
                    body.text="Connect your PC and phone to the same Wi-Fi network.\n\nOn your phone, tap PLAY ON PC and choose your PC from the list. Driving starts after the connection succeeds.";
                    hint.text=!phone&&connectionDetails!=null?connectionDetails():"No PC found? Try FIND PCs AGAIN. For MANUAL CONNECTION, use the current address and code shown on the PC's How to Play connection page.";break;
                case 2:
                    body.text="Once the driving controls appear, hold your phone in landscape in a comfortable driving position.\n\nKeep it centred and tap CALIBRATE. Tilt gently left or right to steer.";
                    hint.text="Recalibrate whenever you change how you hold the phone. Small tilts make gentle turns.";break;
                case 3:
                    body.text="Hold ACCELERATE to move forward. Hold REVERSE to back up. Hold BRAKE to slow down.\n\nRelease the driving pedal to come to a smooth stop.";
                    hint.text="Tap PAUSE for Resume, sound Settings and Home. Home returns to your menu.";break;
                default:
                    body.text="Drive to an M marker and slow down. Tap START MISSION on your phone when it appears.\n\nFollow the gold route on the round map, collect the floating coins and reach the coin sacks at the finish.";
                    hint.text="Spend earned coins in SHOP from Home. Your coins and owned vehicles save automatically.";break;
            }
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(next.gameObject);
        }
        private void Update()
        {
            if(!IsOpen)return;
            if(gameplayStarted!=null&&gameplayStarted()){Close(true);return;}
            if(Time.frameCount<=openedFrame||Keyboard.current==null)return;
            if(Keyboard.current.escapeKey.wasPressedThisFrame)Close(true);
            else if(Keyboard.current.rightArrowKey.wasPressedThisFrame)Next();
            else if(Keyboard.current.leftArrowKey.wasPressedThisFrame)Previous();
        }
        private void LateUpdate(){if(root!=null&&root.gameObject.activeSelf)root.SetAsLastSibling();}
        private void OnDisable(){Close(false);if(root!=null)root.gameObject.SetActive(false);}
        private void OnDestroy(){if(root!=null)Destroy(root.gameObject);}
    }
}
