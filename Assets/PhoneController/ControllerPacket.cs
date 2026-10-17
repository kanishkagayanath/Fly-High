using System;
using System.Globalization;
using System.Text;

namespace FlyHigh.PhoneController
{
    public static class ControllerPacket
    {
        public const int Port = 47777;
        public static byte[] Encode(string code, uint sequence, float steer, float throttle, float brake)
        {
            return Encoding.ASCII.GetBytes(string.Format(CultureInfo.InvariantCulture,
                "FH1|{0}|{1}|{2:F4}|{3:F4}|{4:F4}", code, sequence, steer, throttle, brake));
        }

        public static bool TryDecode(byte[] bytes, string code, out uint sequence, out float steer, out float throttle, out float brake)
        {
            sequence = 0; steer = throttle = brake = 0;
            if (bytes == null || bytes.Length > 128) return false;
            string[] parts = Encoding.ASCII.GetString(bytes).Split('|');
            if (parts.Length != 6 || parts[0] != "FH1" || parts[1] != code) return false;
            if (!uint.TryParse(parts[2], out sequence) ||
                !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out steer) ||
                !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out throttle) ||
                !float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out brake)) return false;
            return Finite(steer) && Finite(throttle) && Finite(brake) &&
                steer >= -1 && steer <= 1 && throttle >= -1 && throttle <= 1 && brake >= 0 && brake <= 1;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool IsNewer(uint next, uint previous) => unchecked((int)(next - previous)) > 0;
    }
}
