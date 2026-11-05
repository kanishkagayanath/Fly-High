using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    public enum GarageActionResult { Purchased, Selected, Rejected }

    /// <summary>Single owner of earned currency, vehicle ownership and selected vehicle.</summary>
    public sealed class VehicleGarage : MonoBehaviour
    {
        public const string SaveKey="FlyHigh.Garage.v1",LegacyWalletKey="FlyHigh.Coins";
        [Serializable] private sealed class SaveData { public int version,coins;public string selected;public string[] owned; }
        private readonly HashSet<string> owned=new HashSet<string>(StringComparer.Ordinal);
        private string selectedId;
        private bool initialized,dirty,saveLocked;
        public VehicleShopCatalog Catalog {get;private set;}
        public int Coins {get;private set;}
        public int Version {get;private set;}
        public bool CanPurchase => !saveLocked && Catalog!=null;
        public ShopVehicle Selected => Catalog!=null && IsOwned(selectedId)?Catalog.Find(selectedId)??Catalog.Starter:Catalog?.Starter;
        public event Action Changed;
        public event Action<GarageActionResult> ActionCompleted;
        public event Action<ShopVehicle> SelectionChanged;

        public void Initialize()
        {
            if(initialized)return;initialized=true;
            Catalog=Resources.Load<VehicleShopCatalog>("FlyHighVehicleShop");
            Coins=Mathf.Max(0,PlayerPrefs.GetInt(LegacyWalletKey,0));
            if(Catalog==null || Catalog.Starter==null || Catalog.Starter.prefab==null){saveLocked=true;Debug.LogWarning("[Garage] Vehicle catalog unavailable.");return;}
            string raw=PlayerPrefs.GetString(SaveKey,"");
            if(raw.Length>0)
            {
                try
                {
                    var data=JsonUtility.FromJson<SaveData>(raw);
                    if(data==null || data.version!=1 || data.coins<0 || data.owned==null)throw new ArgumentException("Unsupported garage save");
                    Coins=data.coins;selectedId=data.selected;
                    foreach(string id in data.owned)if(!string.IsNullOrEmpty(id))owned.Add(id);
                }
                catch(ArgumentException)
                {
                    // Keep the original record intact. A failed read must never become a new purchase.
                    saveLocked=true;Debug.LogWarning("[Garage] Saved garage could not be read; purchases are disabled and the original record is preserved.");
                }
            }
            owned.Add(Catalog.Starter.id);
            if(string.IsNullOrEmpty(selectedId))selectedId=Catalog.Starter.id;
            if(raw.Length==0){dirty=true;Flush();}
        }
        public bool IsOwned(string id)=>!string.IsNullOrEmpty(id) && owned.Contains(id);
        public bool TryBuy(string id,out string message)
        {
            var entry=Catalog?.Find(id);
            if(!CanPurchase || entry==null || entry.prefab==null || entry.price<0){message="Vehicle unavailable";return Result(GarageActionResult.Rejected);}
            if(IsOwned(id)){message="Already owned";return Result(GarageActionResult.Rejected);}
            if(Coins<entry.price){message="Earn "+(entry.price-Coins).ToString("N0")+" more coins";return Result(GarageActionResult.Rejected);}
            Coins-=entry.price;owned.Add(id);selectedId=id;dirty=true;Flush();Version++;
            Changed?.Invoke();SelectionChanged?.Invoke(entry);message=entry.title+" purchased and selected";return Result(GarageActionResult.Purchased);
        }
        public bool TrySelect(string id,out string message)
        {
            var entry=Catalog?.Find(id);
            if(saveLocked || entry==null || entry.prefab==null || !IsOwned(id)){message="Buy this vehicle first";return Result(GarageActionResult.Rejected);}
            if(Selected==entry){message="Already selected";return Result(GarageActionResult.Rejected);}
            selectedId=id;dirty=true;Flush();Version++;Changed?.Invoke();SelectionChanged?.Invoke(entry);message=entry.title+" selected";return Result(GarageActionResult.Selected);
        }
        private bool Result(GarageActionResult result){ActionCompleted?.Invoke(result);return result!=GarageActionResult.Rejected;}
        public void Credit(int amount)
        {
            if(amount<=0)return;Coins=(int)Math.Min(int.MaxValue,(long)Coins+amount);dirty=true;Version++;Changed?.Invoke();
        }
        public void Flush()
        {
            if(!dirty)return;
            if(!saveLocked)
            {
                var ids=new string[owned.Count];owned.CopyTo(ids);Array.Sort(ids,StringComparer.Ordinal);
                PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new SaveData{version=1,coins=Coins,owned=ids,selected=selectedId}));
            }
            PlayerPrefs.SetInt(LegacyWalletKey,Coins);PlayerPrefs.Save();dirty=false;
        }
        private void OnApplicationPause(bool pause){if(pause)Flush();}
        private void OnDisable(){Flush();}
        private void OnApplicationQuit(){Flush();}
    }
}
