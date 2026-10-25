using System;
using System.Text;

namespace FlyHigh.PhoneController
{
    public sealed class DiscoveredPC
    {
        public string Id { get; internal set; }
        public string Address { get; internal set; }
        public string Name { get; internal set; }
        public string PairingCode { get; internal set; }
        public bool Busy { get; internal set; }
    }

    /// <summary>Small, nonce-matched LAN discovery messages on the existing controller port.</summary>
    public static class ControllerDiscovery
    {
        public const int MaximumGames = 8;
        public static byte[] Request(string nonce) => Encoding.ASCII.GetBytes("FH1|FIND|" + nonce);
        public static bool TryReadRequest(byte[] packet, out string nonce)
        {
            nonce = null;
            if (packet == null || packet.Length != 41) return false;
            string text = Encoding.ASCII.GetString(packet);
            if (!text.StartsWith("FH1|FIND|", StringComparison.Ordinal)) return false;
            string candidate = text.Substring(9);
            if (!Guid.TryParseExact(candidate, "N", out _)) return false;
            nonce = candidate; return true;
        }
        public static byte[] Reply(string nonce, string hostId, string code, string name, bool busy)
        {
            name = CleanName(name);
            return Encoding.ASCII.GetBytes("FH1|HOST|" + nonce + "|" + hostId + "|" + (busy ? "-" : code)
                + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(name)) + "|" + (busy ? "BUSY" : "READY"));
        }
        public static bool TryReadReply(byte[] packet, string nonce, out DiscoveredPC pc)
        {
            pc = null;
            if (packet == null || packet.Length > 256 || nonce == null) return false;
            string[] fields = Encoding.ASCII.GetString(packet).Split('|');
            if (fields.Length != 7 || fields[0] != "FH1" || fields[1] != "HOST" || fields[2] != nonce
                || !Guid.TryParseExact(fields[3], "N", out _)) return false;
            bool busy = fields[6] == "BUSY";
            if (!busy && fields[6] != "READY") return false;
            if (busy ? fields[4] != "-" : !ValidCode(fields[4])) return false;
            string name;
            try { name = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(fields[5])); }
            catch (FormatException) { return false; }
            catch (DecoderFallbackException) { return false; }
            if (name.Length == 0 || name.Length > 32 || name != CleanName(name)) return false;
            pc = new DiscoveredPC { Id = fields[3], Name = name, PairingCode = busy ? null : fields[4], Busy = busy };
            return true;
        }
        private static bool ValidCode(string code)
        {
            if (code.Length != 6) return false;
            foreach (char digit in code) if (digit < '0' || digit > '9') return false;
            return true;
        }
        private static string CleanName(string name)
        {
            var clean = new StringBuilder();
            foreach (char c in name ?? "")
            {
                if (!char.IsControl(c) && !char.IsSurrogate(c) && c != '<' && c != '>') clean.Append(c);
                if (clean.Length == 32) break;
            }
            return clean.Length == 0 ? "Fly High PC" : clean.ToString();
        }
    }
}
