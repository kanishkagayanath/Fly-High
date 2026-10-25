using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    public sealed class NpcTrafficCar : MonoBehaviour
    {
        private CityRoadGraph graph;
        private Rigidbody body;
        private CarWheelVisuals wheels;
        private int current, target = -1, nextDistrict;
        private float cruise, speed, nextDecision, progress, blockedFor, wheelSteering;
        private TrafficPath path;
        private NpcTrafficCar blockingCar;
        private float retreatRemaining;
        private readonly List<int> route = new List<int>();
        private readonly Collider[] overlaps = new Collider[48];
        private Vector3 HalfSize = new Vector3(0.85f, 0.32f, 1.8f);
        public float DistanceTravelled { get; private set; }
        public int CompletedRoutes { get; private set; }
        public int VisitedDistricts { get; private set; }

        public bool Initialize(CityRoadGraph roadGraph, int start, GameObject prefab, float cruisingSpeed)
        {
            graph = roadGraph; current = start; cruise = cruisingSpeed;
            nextDistrict = (graph.District(graph.nodes[start].position)+4)%9;
            transform.position = graph.nodes[start].position;
            var shape = gameObject.AddComponent<BoxCollider>(); shape.size = HalfSize * 2;
            body = gameObject.AddComponent<Rigidbody>(); body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var model = Instantiate(prefab, transform, false);
            model.name = "Traffic vehicle visual";
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var physics in model.GetComponentsInChildren<Rigidbody>(true))
            { physics.isKinematic = true; physics.detectCollisions = false; Destroy(physics); }
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                model.transform.localScale *= 3.6f / Mathf.Max(0.01f, bounds.size.z);
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                HalfSize.x = Mathf.Max(HalfSize.x, bounds.size.x * 0.5f);
                shape.size = HalfSize * 2;
                model.transform.localPosition -= new Vector3(bounds.center.x - transform.position.x,
                    bounds.min.y - transform.position.y + 0.45f, bounds.center.z - transform.position.z);
            }
            wheels = gameObject.AddComponent<CarWheelVisuals>(); wheels.Initialize(model.transform, transform);
            // Spawn facing an actual open road edge before planning the first lead-in.
            Vector3 spawnHeading=Vector3.forward;
            foreach(int neighbour in graph.nodes[start].neighbours)
            {Vector3 direction=(graph.nodes[neighbour].position-body.position).normalized;if(graph.ClearSegment(body.position,body.position+direction*3,HalfSize)){spawnHeading=direction;break;}}
            body.rotation=Quaternion.LookRotation(spawnHeading);
            if (!PlanRoute(spawnHeading)) return false;
            path.Evaluate(0,out _,out var facing);body.rotation=facing;
            VisitedDistricts |= 1 << graph.District(body.position);
            return graph.ClearPose(body.position,body.rotation,HalfSize);
        }
        private bool PlanRoute(Vector3 heading)
        {
            Vector3 lead=body.position;
            float preferredLead=Mathf.Max(3,speed*speed/12+2);
            foreach(float length in new[]{preferredLead,3f,2f,1f})
                if(graph.ClearSegment(body.position,body.position+heading*length,HalfSize)){lead=body.position+heading*length;break;}
            if((lead-body.position).sqrMagnitude<0.1f)return false;
            int start=graph.NearestNode(lead);
            for(int attempt=0;attempt<9;attempt++)
            {
                if(!graph.PlanRoute(start,heading,ref nextDistrict,this,route))continue;
                var candidate=TrafficPath.Build(graph,body.position,lead,route,HalfSize);
                if(candidate==null)continue;
                path=candidate;progress=0;return true;
            }
            return false;
        }
        private bool Blocked(Vector3 position, Quaternion rotation, bool remember = false)
        {
            int count=Physics.OverlapBoxNonAlloc(position,HalfSize+new Vector3(0.06f,0.01f,0.06f),overlaps,rotation,~0,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length)return true;
            for(int i=0;i<count;i++)if(overlaps[i].attachedRigidbody!=body)
            {
                if(remember && overlaps[i].attachedRigidbody!=null)blockingCar=overlaps[i].attachedRigidbody.GetComponent<NpcTrafficCar>();
                return true;
            }
            return false;
        }
        private void Reserve(Vector3 position, Vector3 ahead)
        {
            int here=graph.NearestNode(position), next=graph.NearestNode(ahead);
            graph.Reservations.TryReserve(here,this);graph.Reservations.TryReserve(next,this);
            if(current!=here&&current!=next)graph.Reservations.Release(current,this);
            if(target!=here&&target!=next)graph.Reservations.Release(target,this);
            // Leases guide route planning. Actual vehicle volumes decide whether a
            // move is safe; adjacent lanes can share a nearest coarse grid sample.
            current=here;target=next;
        }
        private void FixedUpdate()
        {
            if(graph==null || body==null)return;
            float dt=Time.fixedDeltaTime;
            if(retreatRemaining>0)
            {
                float retreat=Mathf.Min(1.8f*dt,retreatRemaining);
                Vector3 p=body.position-body.rotation*Vector3.forward*retreat;Quaternion r=body.rotation;
                if(path!=null && progress>=retreat)path.Evaluate(progress-retreat,out p,out r);
                bool safe=!Blocked(p,r) && graph.RoadFootprint(p,r,HalfSize.x+0.12f,HalfSize.z+0.12f);
                if(safe){body.MovePosition(p);body.MoveRotation(r);progress=Mathf.Max(0,progress-retreat);retreatRemaining-=retreat;speed=-1.8f;}
                if(!safe || retreatRemaining<=0.001f)
                {retreatRemaining=0;speed=0;path=null;nextDecision=Time.time+0.7f;graph.Reservations.ReleaseAll(this);}
                return;
            }
            // Join the next district route before exhausting the current one. This
            // leaves room for a forward lead-in and keeps speed continuous at handoff.
            if(path!=null && path.Length-progress<8 && Time.time>=nextDecision)
            {
                if(PlanRoute(body.rotation*Vector3.forward))CompletedRoutes++;
                nextDecision=Time.time+0.5f;
            }
            if(path==null || progress>=path.Length-0.001f)
            {
                if(Time.time<nextDecision)return;
                if(path!=null)CompletedRoutes++;
                if(!PlanRoute(body.rotation*Vector3.forward))
                {
                    if(path!=null){retreatRemaining=4;speed=0;return;}
                    speed=Mathf.MoveTowards(speed,0,8*dt);blockedFor+=0.5f;
                    if(blockedFor>2){retreatRemaining=4;blockedFor=0;graph.Reservations.ReleaseAll(this);}
                    nextDecision=Time.time+0.5f;return;
                }
            }
            path.Evaluate(progress+3.5f,out _,out var futureFacing);
            float turn=Quaternion.Angle(body.rotation,futureFacing);
            float desiredSpeed=Mathf.Lerp(cruise,2,Mathf.Clamp01(turn/75));
            float lookAhead=Mathf.Max(2,speed*speed/12+0.8f),clearance=lookAhead;
            desiredSpeed=Mathf.Min(desiredSpeed,path.SpeedLimit(progress,lookAhead));
            blockingCar=null;
            // Follow the rounded route when looking ahead; a straight cast can see a
            // kerb across a bend and incorrectly stop an otherwise safe vehicle.
            for(float look=0.5f;look<=lookAhead;look+=0.75f)
            {
                path.Evaluate(progress+look,out var p,out var r);
                if(Blocked(p,r,true)){clearance=Mathf.Max(0,look-0.5f);break;}
            }
            path.Evaluate(progress+Mathf.Max(3,speed*0.6f),out var reservationPoint,out _);
            Reserve(body.position,reservationPoint);
            desiredSpeed=Mathf.Min(desiredSpeed,Mathf.Sqrt(12*Mathf.Max(0,clearance-0.3f)));
            speed=Mathf.MoveTowards(speed,desiredSpeed,(desiredSpeed<speed?6:3)*dt);
            float step=Mathf.Min(speed*dt,Mathf.Max(0,clearance-0.08f),path.Length-progress);
            path.Evaluate(progress+step,out var proposed,out var pathFacing);
            Quaternion facing=pathFacing;
            bool clear=step>0 && !Blocked(proposed,facing) && graph.RoadFootprint(proposed,facing,HalfSize.x+0.12f,HalfSize.z+0.12f);
            if(clear)
            {
                wheelSteering=Mathf.Clamp(Vector3.SignedAngle(body.rotation*Vector3.forward,facing*Vector3.forward,Vector3.up)/Mathf.Max(step,0.02f)*0.5f,-25,25);
                body.MovePosition(proposed);body.MoveRotation(facing);
                progress+=step;DistanceTravelled+=step;
                VisitedDistricts |= 1 << graph.District(proposed);
            }
            if(!clear || speed<0.15f)blockedFor+=dt;else blockedFor=0;
            if(blockedFor>3 && Time.time>=nextDecision)
            {
                if(blockingCar!=null && GetInstanceID()>blockingCar.GetInstanceID() &&
                    Vector3.Dot(body.rotation*Vector3.forward,blockingCar.body.rotation*Vector3.forward)<-0.2f)
                {retreatRemaining=5;blockedFor=0;return;}
                // Keep the current pose. Replan around occupied nodes instead of
                // teleporting, oscillating between neighbours or resetting speed.
                if(target!=current)graph.Reservations.Release(target,this);
                target=-1;PlanRoute(body.rotation*Vector3.forward);
                nextDecision=Time.time+2;blockedFor=0;
            }
        }
        private void LateUpdate()
        {
            if(wheels!=null && Time.deltaTime>0)wheels.Animate(speed,wheelSteering,Time.deltaTime);
        }
        private void OnDestroy() { graph?.Reservations.ReleaseAll(this); }
    }
}
