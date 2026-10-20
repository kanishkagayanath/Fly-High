using System;

namespace FlyHigh.PhoneController
{
    /// <summary>Approved asphalt index ranges, without reading imported mesh vertex buffers.</summary>
    public sealed class RoadTriangleMask
    {
        private readonly int[] first, count;
        private readonly bool[] allowed;
        public RoadTriangleMask(int[] firstTriangles, int[] triangleCounts, bool[] allowedMaterials)
        { first = firstTriangles; count = triangleCounts; allowed = allowedMaterials; }
        public bool Contains(int triangle)
        {
            if (triangle < 0) return false;
            for (int i = 0; i < first.Length; i++)
                if (allowed[i] && triangle >= first[i] && triangle - first[i] < count[i]) return true;
            return false;
        }
        public static bool AsphaltMaterial(string name)
        {
            if (name == null) return false;
            name = name.Replace(" (Instance)", "").Trim();
            return string.Equals(name, "Roads", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Asphalt_Dark_Gray", StringComparison.OrdinalIgnoreCase);
        }
    }
}
