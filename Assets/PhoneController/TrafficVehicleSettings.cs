using UnityEngine;

namespace FlyHigh.PhoneController
{
    public sealed class TrafficVehicleSettings : ScriptableObject
    {
        public GameObject[] vehicles;
        [Range(1, 20)] public int vehicleCount = 8;
        [Min(1)] public float cruisingSpeed = 7;
    }
}
