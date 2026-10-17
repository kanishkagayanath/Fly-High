using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyHigh.PhoneController
{
    public sealed class HoldPedal : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action<bool> Changed;
        private int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId; Changed?.Invoke(true);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            pointer = int.MinValue; Changed?.Invoke(false);
        }
        private void OnDisable() { pointer = int.MinValue; Changed?.Invoke(false); }
    }
}
