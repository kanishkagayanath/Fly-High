using System;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    [Serializable]
    public sealed class ShopVehicle
    {
        public string id, title, description;
        [Min(0)] public int price;
        public GameObject prefab;
        public Sprite thumbnail;
    }
    [CreateAssetMenu(menuName="Fly High/Vehicle Shop Catalog")]
    public sealed class VehicleShopCatalog : ScriptableObject
    {
        public ShopVehicle[] vehicles;
        public ShopVehicle Find(string id)
        { if(vehicles!=null)foreach(var entry in vehicles)if(entry!=null && entry.id==id)return entry;return null; }
        public ShopVehicle Starter => vehicles!=null && vehicles.Length>0?vehicles[0]:null;
    }
}
