using System.Collections.Generic;

namespace FlyHigh.PhoneController.Editor
{
    public static class CityExpansionPlan
    {
        public struct Junction
        {
            public float x, z;
            public Junction(float x, float z) { this.x = x; this.z = z; }
        }
        public struct Street
        {
            public int a, b;
            public Street(int a, int b) { this.a = a; this.b = b; }
        }
        public static Junction[] Junctions()
        {
            var result = new List<Junction>();
            for (int x = 0; x < 3; x++) for (int z = -1; z <= 1; z++) result.Add(new Junction(x * 64, z * 64));
            result.AddRange(new[] { new Junction(192,104), new Junction(248,64), new Junction(248,-64), new Junction(192,-104), new Junction(248,0) });
            return result.ToArray();
        }
        public static Street[] Streets()
        {
            var result = new List<Street>();
            for (int x = 0; x < 3; x++) for (int z = 0; z < 3; z++)
            {
                int i = x * 3 + z;
                if (x < 2) result.Add(new Street(i, i + 3));
                if (z < 2) result.Add(new Street(i, i + 1));
            }
            result.AddRange(new[] { new Street(8,9), new Street(9,10), new Street(10,13), new Street(13,11), new Street(11,12), new Street(12,6), new Street(7,13) });
            return result.ToArray();
        }
    }
}
