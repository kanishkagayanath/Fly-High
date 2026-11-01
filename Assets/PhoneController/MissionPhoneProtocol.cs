using System;
using System.Globalization;
using System.Text;

namespace FlyHigh.PhoneController
{
    public enum CoinMissionPhase { Exploring, Available, Collecting, Complete }

    public struct MissionPhoneSnapshot
    {
        public uint revision;
        public CoinMissionPhase phase;
        public string token;
        public int collected, total, wallet;
    }

    /// <summary>Optional side channel on the paired driving socket; FH1 driving and ACK stay compatible.</summary>
    public static class MissionPhoneProtocol
    {
        public static byte[] Status(string code, MissionPhoneSnapshot state) => Encoding.ASCII.GetBytes(
            string.Format(CultureInfo.InvariantCulture, "FH1|MS|{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                code, state.revision, (int)state.phase, state.token ?? "-", state.collected, state.total, state.wallet));

        public static byte[] Start(string code, string token) => Encoding.ASCII.GetBytes("FH1|START|" + code + "|" + token);

        public static bool TryStart(byte[] bytes, string code, out string token)
        {
            token = null;
            if (bytes == null || bytes.Length > 96) return false;
            string[] p = Encoding.ASCII.GetString(bytes).Split('|');
            if (p.Length != 4 || p[0] != "FH1" || p[1] != "START" || p[2] != code || !ValidToken(p[3])) return false;
            token = p[3]; return true;
        }

        public static bool TryStatus(byte[] bytes, string code, out MissionPhoneSnapshot state)
        {
            state = default;
            if (bytes == null || bytes.Length > 160) return false;
            string[] p = Encoding.ASCII.GetString(bytes).Split('|');
            if (p.Length != 9 || p[0] != "FH1" || p[1] != "MS" || p[2] != code ||
                !uint.TryParse(p[3], NumberStyles.None, CultureInfo.InvariantCulture, out uint revision) ||
                !int.TryParse(p[4], out int phase) || phase < 0 || phase > 3 ||
                !(p[5] == "-" || ValidToken(p[5])) ||
                !int.TryParse(p[6], out int collected) || !int.TryParse(p[7], out int total) ||
                !int.TryParse(p[8], out int wallet) || collected < 0 || total < collected || total > 256 || wallet < 0 ||
                (phase == 1 && !ValidToken(p[5]))) return false;
            state = new MissionPhoneSnapshot {revision = revision, phase = (CoinMissionPhase)phase, token = p[5], collected = collected, total = total, wallet = wallet};
            return true;
        }

        private static bool ValidToken(string value)
        {
            if (value == null || value.Length != 16) return false;
            foreach (char c in value) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            return true;
        }
    }
}
