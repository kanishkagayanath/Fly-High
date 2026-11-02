using System;
using System.Globalization;
using System.Text;
using UnityEngine;
namespace FlyHigh.PhoneController
{
    public enum PhoneHomePage { Home, Shop, Options }
    [Serializable] public sealed class HomeVehicleSnapshot { public string id,title;public int price;public bool owned; }
    [Serializable] public sealed class HomePhoneSnapshot
    {
        public int version=1;
        public uint revision;
        public string token,selected,message;
        public PhoneHomePage page;
        public int coins,lighting;
        public bool canPurchase;
        public HomeVehicleSnapshot[] vehicles;
    }
    /// <summary>PC-authoritative garage/settings snapshots; currency never comes from the phone.</summary>
    public static class HomePhoneProtocol
    {
        public const int MaximumVehicles=24,MaximumBytes=8192;
        public static byte[] Status(string code,HomePhoneSnapshot state)=>Encoding.UTF8.GetBytes("FH1|HS|"+code+"|"+JsonUtility.ToJson(state));
        public static byte[] Request(string code,string token,string id,bool buy)=>Encoding.ASCII.GetBytes("FH1|GARAGE|"+code+"|"+token+"|"+(buy?"B":"S")+"|"+id);
        public static bool TryRequest(byte[] bytes,string code,out string token,out string id,out bool buy)
        {
            token=id=null;buy=false;if(bytes==null||bytes.Length>128)return false;
            var p=Encoding.ASCII.GetString(bytes).Split('|');
            if(p.Length!=6||p[0]!="FH1"||p[1]!="GARAGE"||p[2]!=code||!MenuPhoneProtocol.ValidToken(p[3])||!(p[4]=="B"||p[4]=="S")||!ValidId(p[5]))return false;
            token=p[3];id=p[5];buy=p[4]=="B";return true;
        }
        public static bool TryStatus(byte[] bytes,string code,out HomePhoneSnapshot state)
        {
            state=null;if(bytes==null||bytes.Length>MaximumBytes)return false;
            var p=Encoding.UTF8.GetString(bytes).Split(new[]{'|'},4);
            if(p.Length!=4||p[0]!="FH1"||p[1]!="HS"||p[2]!=code)return false;
            try
            {
                var s=JsonUtility.FromJson<HomePhoneSnapshot>(p[3]);
                if(s==null||s.version!=1||!MenuPhoneProtocol.ValidToken(s.token)||(int)s.page<0||(int)s.page>2||s.coins<0||s.lighting < -1||s.lighting>3||s.vehicles==null||s.vehicles.Length>MaximumVehicles||!ValidId(s.selected)||!SafeText(s.message,160))return false;
                var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach(var v in s.vehicles)if(v==null||!ValidId(v.id)||!SafeText(v.title,64)||string.IsNullOrWhiteSpace(v.title)||v.price<0||!ids.Add(v.id))return false;
                state=s;return true;
            }
            catch(ArgumentException){return false;}
        }
        public static bool ValidId(string value)
        {
            if(string.IsNullOrEmpty(value)||value.Length>32)return false;
            foreach(char c in value)if(!(c>='a'&&c<='z')&&!(c>='A'&&c<='Z')&&!(c>='0'&&c<='9')&&c!='_'&&c!='-')return false;
            return true;
        }
        private static bool SafeText(string value,int maximum)
        {if(value==null)return true;if(value.Length>maximum)return false;foreach(char c in value)if(char.IsControl(c))return false;return true;}
    }
}
