using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyHigh.PhoneController
{
    /// <summary>One click for an accepted button action, including keyboard submit and pedal touch-down.</summary>
    [DisallowMultipleComponent]
    public sealed class ButtonSound : MonoBehaviour, IPointerDownHandler
    {
        private UnityEngine.UI.Button button;
        private GameAudio gameAudio;
        public static void Attach(UnityEngine.UI.Button target)
        {
            if (target.GetComponent<ButtonSound>() != null) return;
            var sound = target.gameObject.AddComponent<ButtonSound>();
            sound.button = target;
            // Register before the action so hiding the clicked panel cannot swallow its cue.
            target.onClick.AddListener(sound.Clicked);
        }
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Clicked);
        }
        private void Clicked(){if(GetComponent<HoldPedal>()==null)Play();}
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button==PointerEventData.InputButton.Left&&GetComponent<HoldPedal>()!=null)Play();
        }
        private void Play()
        {
            if(!isActiveAndEnabled||button==null||!button.IsActive()||!button.IsInteractable())return;
            if(gameAudio==null)gameAudio=GetComponentInParent<GameAudio>();
            if(gameAudio!=null)gameAudio.PlayButton();
        }
    }
}
