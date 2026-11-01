using System;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    [Serializable]
    public sealed class CoinMissionRoute
    {
        public string title;
        public Vector3[] path;
        public Vector3[] coins;
        public float distance;
    }

    [CreateAssetMenu(menuName = "Fly High/Coin Mission Settings")]
    public sealed class CoinMissionSettings : ScriptableObject
    {
        public GameObject coinModel;
        public GameObject sackModel;
        public Material markerMaterial;
        public Material coinMaterial, sackMaterial;
        public CoinMissionRoute[] routes;
        [Min(1)] public int finishBonus = 25;
        [Min(1)] public float startRadius = 6;
        [Min(0.5f)] public float collectRadius = 2.3f;
    }
}
