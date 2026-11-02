using System;
using System.Globalization;
using System.Text;

namespace FlyHigh.PhoneController
{
    public enum DriveMenuView { Home, Driving, Paused, Settings }
    public enum DriveMenuAction { Pause, Resume, Settings, Back, Home, VolumeDown, VolumeUp, Play, Shop, Options, CycleLighting }
    public struct MenuPhoneSnapshot
    {
        public uint revision;
        public DriveMenuView view;
        public int volume;
        public string token;
    }

    /// <summary>Optional paired menu channel. A fresh token makes UDP retries idempotent.</summary>
    public static class MenuPhoneProtocol
    {
        public static byte[] Status(string code, MenuPhoneSnapshot state) => Encoding.ASCII.GetBytes(
            string.Format(CultureInfo.InvariantCulture, "FH1|UI|{0}|{1}|{2}|{3}|{4}", code,
                state.revision, (int)state.view, state.volume, state.token));
        public static byte[] Request(string code, string token, DriveMenuAction action) => Encoding.ASCII.GetBytes(
            "FH1|MENU|" + code + "|" + token + "|" + ((int)action).ToString(CultureInfo.InvariantCulture));
        public static bool TryRequest(byte[] bytes, string code, out string token, out DriveMenuAction action)
        {
            token = null; action = default;
            if (bytes == null || bytes.Length > 96) return false;
            var p = Encoding.ASCII.GetString(bytes).Split('|');
            if (p.Length != 5 || p[0] != "FH1" || p[1] != "MENU" || p[2] != code || !ValidToken(p[3]) ||
                !int.TryParse(p[4], NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value < 0 || value > 10) return false;
            token = p[3]; action = (DriveMenuAction)value; return true;
        }
        public static bool TryStatus(byte[] bytes, string code, out MenuPhoneSnapshot state)
        {
            state = default;
            if (bytes == null || bytes.Length > 128) return false;
            var p = Encoding.ASCII.GetString(bytes).Split('|');
            if (p.Length != 7 || p[0] != "FH1" || p[1] != "UI" || p[2] != code ||
                !uint.TryParse(p[3], NumberStyles.None, CultureInfo.InvariantCulture, out uint revision) ||
                !int.TryParse(p[4], NumberStyles.None, CultureInfo.InvariantCulture, out int view) || view < 0 || view > 3 ||
                !int.TryParse(p[5], NumberStyles.None, CultureInfo.InvariantCulture, out int volume) || volume < 0 || volume > 100 || !ValidToken(p[6])) return false;
            state = new MenuPhoneSnapshot { revision = revision, view = (DriveMenuView)view, volume = volume, token = p[6] }; return true;
        }
        internal static bool ValidToken(string value)
        {
            if (value == null || value.Length != 16) return false;
            foreach (char c in value) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            return true;
        }
    }
}
