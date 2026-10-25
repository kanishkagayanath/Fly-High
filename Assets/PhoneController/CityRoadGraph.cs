using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Bounded, startup-only road sampling. No NavMesh/package or scene edits required.</summary>
    public sealed class CityRoadGraph
    {
        public sealed class Node
        {
            public Vector3 position;
            public readonly List<int> neighbours = new List<int>(4);
        }
        public readonly List<Node> nodes = new List<Node>();
        public TrafficReservations Reservations { get; private set; }
        private readonly Dictionary<MeshCollider, RoadTriangleMask> roads = new Dictionary<MeshCollider, RoadTriangleMask>();
        private readonly Dictionary<Vector2Int, int> grid = new Dictionary<Vector2Int, int>();
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        private const float Spacing = 4;
        private readonly Collider[] clearanceHits = new Collider[48];
        private readonly List<int>[] districts = new List<int>[9];
        private int[] destinations;
        private Bounds mapBounds;
        private readonly Dictionary<long,float> laneCosts = new Dictionary<long,float>();
        private static readonly Vector2Int[] Directions =
            { Vector2Int.right, Vector2Int.up, new Vector2Int(1, 1), new Vector2Int(-1, 1) };

        public void Build()
        {
            var surfaces = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
            foreach (var surface in surfaces)
            {
                if (!surface.enabled || surface.isTrigger || surface.sharedMesh == null || surface.attachedRigidbody != null ||
                    !(surface.name + surface.sharedMesh.name).ToLowerInvariant().Contains("road")) continue;
                var renderer = surface.GetComponent<MeshRenderer>();
                var filter = surface.GetComponent<MeshFilter>();
                if (renderer == null || filter == null || filter.sharedMesh != surface.sharedMesh) continue;
                Mesh mesh = surface.sharedMesh;
                var materials = renderer.sharedMaterials;
                var first = new int[mesh.subMeshCount]; var counts = new int[first.Length]; var allowed = new bool[first.Length];
                bool hasAsphalt = false;
                for (int i = 0; i < first.Length; i++)
                {
                    var descriptor = mesh.GetSubMesh(i);
                    first[i] = descriptor.indexStart / 3;
                    counts[i] = descriptor.indexCount / 3;
                    allowed[i] = descriptor.topology == MeshTopology.Triangles && i < materials.Length &&
                        materials[i] != null && RoadTriangleMask.AsphaltMaterial(materials[i].name);
                    hasAsphalt |= allowed[i];
                }
                if (hasAsphalt) roads.Add(surface, new RoadTriangleMask(first, counts, allowed));
            }
            int attempts = 0;
            foreach (var entry in roads)
            {
                var road = entry.Key;
                Bounds b = road.bounds;
                for (int x = Mathf.CeilToInt(b.min.x / Spacing); x <= Mathf.FloorToInt(b.max.x / Spacing); x++)
                    for (int z = Mathf.CeilToInt(b.min.z / Spacing); z <= Mathf.FloorToInt(b.max.z / Spacing); z++)
                    {
                        if (++attempts > 30000 || nodes.Count >= 6000) goto SamplingFinished;
                        var key = new Vector2Int(x, z);
                        if (grid.ContainsKey(key)) continue;
                        var ray = new Ray(new Vector3(x * Spacing, b.max.y + 2, z * Spacing), Vector3.down);
                        if (!road.Raycast(ray, out RaycastHit hit, b.size.y + 4) || hit.normal.y < 0.95f || !entry.Value.Contains(hit.triangleIndex)) continue;
                        Vector3 position = hit.point + Vector3.up * 0.45f;
                        if (!RoadFootprint(position, Quaternion.identity) || Physics.CheckBox(position, new Vector3(1, 0.33f, 2), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                        grid.Add(key, nodes.Count); nodes.Add(new Node { position = position });
                    }
            }
            SamplingFinished:
            foreach (var cell in grid)
                foreach (var direction in Directions)
                {
                    if (!grid.TryGetValue(cell.Key + direction, out int other)) continue;
                    int from = cell.Value;
                    Vector3 a = nodes[from].position, b = nodes[other].position;
                    if (Mathf.Abs(a.y - b.y) > 0.35f) continue;
                    Vector3 delta = b - a;
                    Quaternion rotation = Quaternion.LookRotation(delta);
                    bool clear = true;
                    int steps = Mathf.Max(4, Mathf.CeilToInt(delta.magnitude));
                    for (int step = 0; step <= steps; step++)
                    {
                        Vector3 p = Vector3.Lerp(a, b, step / (float)steps);
                        if (!RoadFootprint(p, rotation) || Physics.CheckBox(p, new Vector3(0.95f, 0.33f, 1.9f), rotation, ~0, QueryTriggerInteraction.Ignore))
                        { clear = false; break; }
                    }
                    if (!clear) continue;
                    nodes[from].neighbours.Add(other); nodes[other].neighbours.Add(from);
                }
            Reservations = new TrafficReservations(nodes.Count);
            destinations = new int[nodes.Count];
            for (int i = 0; i < districts.Length; i++) districts[i] = new List<int>();
            if (nodes.Count > 0)
            {
                mapBounds = new Bounds(nodes[0].position,Vector3.zero);
                foreach (var node in nodes) mapBounds.Encapsulate(node.position);
                for (int i = 0; i < nodes.Count; i++) if (nodes[i].neighbours.Count >= 2) districts[District(nodes[i].position)].Add(i);
                for (int i = 0; i < nodes.Count; i++) foreach (int next in nodes[i].neighbours)
                {
                    Vector3 direction=(nodes[next].position-nodes[i].position).normalized;
                    Vector3 right=Vector3.Cross(Vector3.up,direction);
                    Vector3 centre=(nodes[i].position+nodes[next].position)*0.5f;
                    // Prefer the right side of each street, reducing opposing traffic
                    // meeting head-on in the same strip of sampled road points.
                    laneCosts[((long)i<<32)|(uint)next]=HasRoadBelow(centre+right*4.5f)?Vector3.Distance(nodes[i].position,nodes[next].position)*4:0;
                }
            }
        }

        public int District(Vector3 position)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt(Mathf.InverseLerp(mapBounds.min.x,mapBounds.max.x,position.x)*3),0,2);
            int z = Mathf.Clamp(Mathf.FloorToInt(Mathf.InverseLerp(mapBounds.min.z,mapBounds.max.z,position.z)*3),0,2);
            return z*3+x;
        }
        public int NearestNode(Vector3 position)
        {
            int best=-1;float distance=float.PositiveInfinity;
            for(int i=0;i<nodes.Count;i++) {float d=(nodes[i].position-position).sqrMagnitude;if(d<distance){best=i;distance=d;}}
            return best;
        }
        public bool ClearPose(Vector3 position, Quaternion rotation, Vector3 halfSize)
        {
            if (!RoadFootprint(position,rotation,halfSize.x+0.12f,halfSize.z+0.12f)) return false;
            int count=Physics.OverlapBoxNonAlloc(position,halfSize+new Vector3(0.06f,0.01f,0.06f),clearanceHits,rotation,~0,QueryTriggerInteraction.Ignore);
            if(count==clearanceHits.Length)return false;
            for(int i=0;i<count;i++)if(clearanceHits[i].attachedRigidbody==null)return false;
            return true;
        }
        public bool ClearSegment(Vector3 a, Vector3 b, Vector3 halfSize)
        {
            if((b-a).sqrMagnitude<0.0001f)return true;
            Quaternion facing=Quaternion.LookRotation(b-a);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/0.75f));
            for(int i=0;i<=steps;i++)if(!ClearPose(Vector3.Lerp(a,b,i/(float)steps),facing,halfSize))return false;
            return true;
        }
        public bool PlanRoute(int start, Vector3 heading, ref int district, object owner, List<int> route)
        {
            route.Clear();
            if(start<0 || start>=nodes.Count)return false;
            // Cycle through all nine map districts, rather than making a random local turn.
            var candidates=districts[district];
            int goal=-1;float best=float.NegativeInfinity;
            foreach(int candidate in candidates)
            {
                if(nodes[candidate].neighbours.Count<6)continue;
                float distance=Vector3.Distance(nodes[start].position,nodes[candidate].position);
                if(distance<24 || !Reservations.Available(candidate,owner))continue;
                float score=distance-destinations[candidate]*20+Random.Range(0f,6f);
                if(score>best){best=score;goal=candidate;}
            }
            district=(district+4)%9;
            if(goal<0)return false;
            int count=nodes.Count;var costs=new float[count];var parents=new int[count];var closed=new bool[count];
            for(int i=0;i<count;i++){costs[i]=float.PositiveInfinity;parents[i]=-1;}
            costs[start]=0;
            for(int iteration=0;iteration<count;iteration++)
            {
                int current=-1;float shortest=float.PositiveInfinity;
                for(int i=0;i<count;i++)if(!closed[i])
                {float score=costs[i]+Vector3.Distance(nodes[i].position,nodes[goal].position);if(score<shortest){shortest=score;current=i;}}
                if(current<0)break;
                if(current==goal)
                {
                    for(int node=goal;node>=0;node=parents[node])route.Add(node);
                    route.Reverse();destinations[goal]++;return route.Count>1;
                }
                closed[current]=true;
                foreach(int next in nodes[current].neighbours)
                {
                    if(closed[next] || !Reservations.Available(next,owner))continue;
                    Vector3 delta=nodes[next].position-nodes[current].position;
                    Vector3 previous=parents[current]>=0?nodes[current].position-nodes[parents[current]].position:heading;
                    float dot=Vector3.Dot(previous.normalized,delta.normalized);
                    if(current==start && dot < -0.1f)continue;
                    float cost=costs[current]+delta.magnitude+(1-dot)*5+laneCosts[((long)current<<32)|(uint)next];
                    if(cost<costs[next]){costs[next]=cost;parents[next]=current;}
                }
            }
            return false;
        }

        private bool HasRoadBelow(Vector3 position)
        {
            int count = Physics.RaycastNonAlloc(position + Vector3.up * 0.5f, Vector3.down, groundHits, 1.5f, ~0, QueryTriggerInteraction.Ignore);
            if (count == groundHits.Length) return false;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
                if (groundHits[i].collider.attachedRigidbody == null) nearest = Mathf.Min(nearest, groundHits[i].distance);
            bool asphalt = false;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider.attachedRigidbody != null || hit.distance > nearest + 0.01f) continue;
                // Reject the topmost pavement even if a road collider exists underneath it.
                if (!(hit.collider is MeshCollider mesh) || !roads.TryGetValue(mesh, out var mask) ||
                    hit.normal.y < 0.95f || !mask.Contains(hit.triangleIndex) || Mathf.Abs(hit.point.y - (position.y - 0.45f)) > 0.15f)
                    return false;
                asphalt = true;
            }
            return asphalt;
        }

        public bool RoadFootprint(Vector3 position, Quaternion rotation, float halfWidth = 1.05f, float halfLength = 2f)
        {
            // Centre, corners and edge midpoints must all sit on asphalt with a curb margin.
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    if (!HasRoadBelow(position + rotation * new Vector3(x * halfWidth, 0, z * halfLength))) return false;
            return true;
        }

        public bool PickNext(int current, int previous, Vector3 heading, object owner, out int next)
        {
            next = -1;
            float best = float.NegativeInfinity;
            foreach (int candidate in nodes[current].neighbours)
            {
                if (!Reservations.Available(candidate, owner)) continue;
                Vector3 delta = (nodes[candidate].position - nodes[current].position).normalized;
                float dot = Vector3.Dot(heading, delta);
                float score = dot * 2 + Random.Range(0f, 0.6f);
                // Normally continue down the street; occasionally turn to explore another block.
                if (dot > 0.9f && Random.value < 0.08f) score -= 2.5f;
                if (candidate == previous) score -= 5;
                if (score > best) { best = score; next = candidate; }
            }
            return next >= 0 && Reservations.TryReserve(next, owner);
        }
    }
}
