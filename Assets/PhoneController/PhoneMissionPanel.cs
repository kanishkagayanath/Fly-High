using TMPro;
using UnityEngine;
namespace FlyHigh.PhoneController
{
    public sealed class PhoneMissionPanel : MonoBehaviour
    {
        private PhoneSender sender;
        private RectTransform root;
        private TMP_Text title,message,wallet,caption;
        private UnityEngine.UI.Button start;
        public void Initialize(RectTransform safeArea,PhoneSender connection)
        {
            sender=connection;root=PhoneUi.Panel(safeArea,"Phone mission strip",.04f,.725f,.96f,.825f,PhoneUi.Surface,PhoneUi.Edge).rectTransform;
            var badge=PhoneUi.Panel(root,"Mission badge",.012f,.15f,.068f,.85f,new Color(.20f,.16f,.08f),PhoneUi.Amber);
            PhoneUi.Text(badge.transform,"Mission symbol","M",0,0,1,1,27,PhoneUi.Amber,true,TextAlignmentOptions.Center);
            title=PhoneUi.Text(root,"Mission title","FREE ROAM",.086f,.48f,.68f,.91f,22,PhoneUi.White,true);
            message=PhoneUi.Text(root,"Mission guidance","Your city. Your drive.",.086f,.08f,.68f,.49f,16,PhoneUi.Muted);
            start=PhoneUi.Button(root,"START MISSION","START MISSION",.72f,.10f,.985f,.90f,sender.StartMission,PhoneUi.Amber,PhoneUi.Amber,PhoneUi.Ink);caption=start.GetComponentInChildren<TMP_Text>();
            wallet=PhoneUi.Text(root,"Phone wallet","",.70f,.16f,.975f,.84f,24,PhoneUi.Amber,true,TextAlignmentOptions.Right);
        }
        private void Update()
        {
            if(root==null)return;bool fresh=sender.HasMissionState;var state=sender.MissionState;
            bool available=fresh&&state.phase==CoinMissionPhase.Available&&sender.DrivingControlsAvailable;
            start.gameObject.SetActive(available);wallet.gameObject.SetActive(!available);
            start.interactable=available&&!sender.MissionStartPending;caption.text=sender.MissionStartPending?"STARTING…":"START MISSION";
            wallet.text=fresh?state.wallet.ToString("N0")+" COINS":"COIN MISSIONS";
            title.text=!fresh||state.phase==CoinMissionPhase.Exploring?"FREE ROAM":state.phase==CoinMissionPhase.Available?"MISSION READY":state.phase==CoinMissionPhase.Collecting?"COIN RUN  /  "+state.collected+" OF "+state.total:"MISSION COMPLETE";
            message.text=available?"Start your next coin run here":fresh&&state.phase==CoinMissionPhase.Collecting?"Follow the gold route on your PC":fresh&&state.phase==CoinMissionPhase.Complete?"Coins added. Ready for your next drive.":"Drive to an M marker to find a mission.";
        }
        private void OnDestroy(){if(root!=null)Destroy(root.gameObject);}
    }
}
