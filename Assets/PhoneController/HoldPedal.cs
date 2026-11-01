using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyHigh.PhoneController
{
    public sealed class HoldPedal : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action<bool> Changed;
        private UnityEngine.UI.Selectable target;
        private void Awake(){target=GetComponent<UnityEngine.UI.Selectable>();}
        private int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue || (target!=null && !target.IsInteractable())) return;
            pointer = e.pointerId; Changed?.Invoke(true);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            pointer = int.MinValue; Changed?.Invoke(false);
        }
        public void Release(){pointer=int.MinValue;Changed?.Invoke(false);}
        public void OnPointerExit(PointerEventData e){if(pointer==e.pointerId)Release();}
        private void OnDisable(){Release();}
    }
}
