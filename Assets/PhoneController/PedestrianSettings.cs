using UnityEngine;
using UnityEngine.AI;

namespace FlyHigh.PhoneController
{
    [System.Serializable]
    public struct PedestrianWaypoint
    {
        public Vector3 position;
        public int region;
        public bool beach;
    }
    [CreateAssetMenu(menuName="Fly High/Pedestrian Settings")]
    public sealed class PedestrianSettings : ScriptableObject
    {
        public GameObject[] characters;
        public NavMeshData navigation;
        public PedestrianWaypoint[] waypoints;
        [Min(0)] public int cityPopulation=20;
        [Min(0)] public int beachPopulation=12;
        public Vector2 walkSpeed=new Vector2(1.05f,1.4f);
        [Min(.1f)] public float characterScale=.9f;
    }
}
