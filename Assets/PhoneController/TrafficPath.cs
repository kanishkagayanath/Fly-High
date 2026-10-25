using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>A cached, rounded driving path. Planning never changes a vehicle's live pose.</summary>
    public sealed class TrafficPath
    {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Quaternion> rotations = new List<Quaternion>();
        private readonly List<float> distances = new List<float>();
        public float Length => distances.Count == 0 ? 0 : distances[distances.Count - 1];

        public static TrafficPath Build(CityRoadGraph graph, Vector3 start, Vector3 lead, List<int> route, Vector3 halfSize)
        {
            if (route.Count < 2) return null;
            var raw = new List<Vector3> { start, lead };
            for (int i = 1; i < route.Count; i++) raw.Add(graph.nodes[route[i]].position);
            var corners = new List<Vector3> { start, lead };
            int from = 1;
            while (from < raw.Count - 1)
            {
                int next = from + 1;
                for (int candidate = Mathf.Min(raw.Count - 1, from + 12); candidate > next; candidate--)
                {
                    Vector3 delta=raw[candidate]-raw[from];bool straight=true;
                    for(int between=from+1;between<candidate;between++)
                    {
                        float t=Mathf.Clamp01(Vector3.Dot(raw[between]-raw[from],delta)/Mathf.Max(0.0001f,delta.sqrMagnitude));
                        if(Vector3.Distance(raw[between],raw[from]+delta*t)>0.35f){straight=false;break;}
                    }
                    // Do not shortcut across the opposing lane, even if the asphalt is clear.
                    if (straight && graph.ClearSegment(raw[from], raw[candidate], halfSize)) { next = candidate; break; }
                }
                corners.Add(raw[next]); from = next;
            }
            var path = new TrafficPath();
            Vector3 end = start;
            for (int i = 1; i < corners.Count - 1; i++)
            {
                Vector3 incoming = corners[i] - corners[i-1], outgoing = corners[i+1] - corners[i];
                if (Vector3.Angle(incoming, outgoing) < 1) continue;
                float cut = Mathf.Min(4, incoming.magnitude * 0.45f, outgoing.magnitude * 0.45f);
                bool rounded = false;
                for (int attempt = 0; attempt < 4; attempt++, cut *= 0.5f)
                {
                    Vector3 a = corners[i] - incoming.normalized * cut;
                    Vector3 b = corners[i] + outgoing.normalized * cut;
                    if (!graph.ClearSegment(end, a, halfSize)) continue;
                    bool clear = true;
                    for (int sample = 0; sample <= 12; sample++)
                    {
                        float t = sample / 12f;
                        Vector3 p = Bezier(a, corners[i], b, t);
                        Vector3 tangent = Vector3.Lerp(corners[i]-a, b-corners[i], t);
                        if (tangent.sqrMagnitude < 0.0001f || !graph.ClearPose(p, Quaternion.LookRotation(tangent), halfSize)) { clear = false; break; }
                    }
                    if (!clear) continue;
                    path.Line(end, a);
                    for (int sample = 0; sample <= 12; sample++)
                    {
                        float t = sample / 12f;
                        path.Add(Bezier(a,corners[i],b,t), Quaternion.LookRotation(Vector3.Lerp(corners[i]-a,b-corners[i],t)));
                    }
                    end = b; rounded = true; break;
                }
                if (!rounded) return null;
            }
            Vector3 finish = corners[corners.Count - 1];
            if (!graph.ClearSegment(end, finish, halfSize)) return null;
            path.Line(end, finish);
            if(path.Length<=2)return null;
            // Validate the interpolated poses as well as Bezier control samples.
            for(float d=0;d<=path.Length;d+=0.25f)
            {path.Evaluate(d,out var p,out var r);if(!graph.ClearPose(p,r,halfSize))return null;}
            return path;
        }
        private static Vector3 Bezier(Vector3 a, Vector3 corner, Vector3 b, float t)
        { float u = 1-t; return u*u*a + 2*u*t*corner + t*t*b; }
        private void Line(Vector3 a, Vector3 b)
        {
            if ((b-a).sqrMagnitude < 0.00001f) return;
            Quaternion facing = Quaternion.LookRotation(b-a);
            int steps = Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/0.75f));
            for (int i = 0; i <= steps; i++) Add(Vector3.Lerp(a,b,i/(float)steps),facing);
        }
        private void Add(Vector3 position, Quaternion rotation)
        {
            float step = positions.Count == 0 ? 0 : Vector3.Distance(positions[positions.Count-1],position);
            if (positions.Count > 0 && step < 0.0001f) { rotations[rotations.Count-1] = rotation; return; }
            distances.Add(Length+step);positions.Add(position);rotations.Add(rotation);
        }
        public void Evaluate(float distance, out Vector3 position, out Quaternion rotation)
        {
            distance = Mathf.Clamp(distance,0,Length);
            int lo = 0, hi = distances.Count-1;
            while(lo < hi) {int mid=(lo+hi)/2;if(distances[mid]<distance)lo=mid+1;else hi=mid;}
            int a=Mathf.Max(0,lo-1), b=lo;
            float t=b==a?0:Mathf.InverseLerp(distances[a],distances[b],distance);
            position=Vector3.Lerp(positions[a],positions[b],t);rotation=Quaternion.Slerp(rotations[a],rotations[b],t);
        }
        public float SpeedLimit(float distance, float ahead)
        {
            float limit=float.PositiveInfinity;
            for(int i=1;i<distances.Count;i++)
            {
                if(distances[i]<distance || distances[i-1]>distance+ahead)continue;
                float step=distances[i]-distances[i-1];
                float curvature=Quaternion.Angle(rotations[i-1],rotations[i])/Mathf.Max(0.001f,step);
                if(curvature>0.01f)limit=Mathf.Min(limit,100/curvature,Mathf.Sqrt(3.5f/(curvature*Mathf.Deg2Rad)));
            }
            return limit;
        }
    }
}
