using UnityEngine;

namespace FlyHigh.PhoneController
{
    [CreateAssetMenu(menuName = "Fly High/Menu Visual Settings")]
    public sealed class MenuVisualSettings : ScriptableObject
    {
        public Texture2D background;
        public Sprite playButton;
        public Sprite optionsButton;
        public Sprite exitButton;
    }
}
