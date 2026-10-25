using UnityEngine;

namespace FlyHigh.PhoneController
{
    [CreateAssetMenu(menuName="Fly High/City Lighting Settings")]
    public sealed class CityLightingSettings : ScriptableObject
    {
        public Material skyMaterial;
        public Material windowGlowMaterial;
        public UnityEngine.Rendering.VolumeProfile atmosphereProfile;
        [Min(1)] public float cycleMinutes=12;
        [Range(0,24)] public float initialHour=9;
        [Min(0.1f)] public float transitionSeconds=5;
    }
}
