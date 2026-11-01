using TMPro;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    public sealed class PhoneMissionPanel : MonoBehaviour
    {
        private PhoneSender sender;
        private RectTransform root;
        private TMP_Text message, caption;
        private UnityEngine.UI.Button start;
        private string lastMessage;
        public void Initialize(RectTransform safeArea,PhoneSender connection)
        {
            sender=connection;
            root=MissionUi.Rect(safeArea,"Phone mission strip",new Vector2(.05f,.78f),new Vector2(.95f,.86f),Vector2.zero,Vector2.zero);
            MissionUi.Background(root,new Color(.025f,.075f,.11f,.96f),new Color(1,.76f,.21f));
            var label=MissionUi.Rect(root,"Phone mission instruction",new Vector2(.02f,0),new Vector2(.65f,1),Vector2.zero,Vector2.zero);
            message=label.gameObject.AddComponent<TextMeshProUGUI>();message.fontSize=19;message.color=Color.white;message.raycastTarget=false;message.alignment=TextAlignmentOptions.MidlineLeft;
            var button=MissionUi.Rect(root,"START MISSION",new Vector2(.67f,.08f),new Vector2(.99f,.92f),Vector2.zero,Vector2.zero);
            var background=MissionUi.Fill(button,new Color(1,.76f,.21f));background.raycastTarget=true;
            start=button.gameObject.AddComponent<UnityEngine.UI.Button>();start.targetGraphic=background;start.onClick.AddListener(sender.StartMission);
            var text=MissionUi.Rect(button,"Start mission caption",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            caption=text.gameObject.AddComponent<TextMeshProUGUI>();caption.fontSize=21;caption.fontStyle=FontStyles.Bold;caption.color=new Color(.02f,.07f,.1f);caption.alignment=TextAlignmentOptions.Center;caption.raycastTarget=false;
        }
        private void Update()
        {
            if(root==null)return;
            bool visible=sender.HasMissionState && sender.MissionState.phase!=CoinMissionPhase.Exploring;
            root.gameObject.SetActive(visible);if(!visible)return;
            var state=sender.MissionState;
            bool available=state.phase==CoinMissionPhase.Available;start.gameObject.SetActive(available);
            start.interactable=available && !sender.MissionStartPending;
            caption.text=sender.MissionStartPending?"STARTING…":"START MISSION";
            string value=available?"Mission nearby — ready to collect coins":state.phase==CoinMissionPhase.Collecting?
                "COIN RUN   "+state.collected+" / "+state.total+" coins  •  Follow the gold route":"MISSION COMPLETE   •   "+state.wallet+" total coins";
            if(value!=lastMessage){lastMessage=value;message.text=value;}
            message.rectTransform.anchorMax=new Vector2(available?.65f:.98f,1);
        }
        private void OnDestroy(){if(root!=null)Destroy(root.gameObject);}
    }
}
