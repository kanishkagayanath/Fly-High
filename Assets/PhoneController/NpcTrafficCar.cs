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
        private Collider blockingObstacle, avoidObstacle;
        private float stalledFor, forwardCheckpoint, nextRecovery, avoidUntil;
        private Transform player;
        private Camera view;
        private readonly Plane[] viewPlanes = new Plane[6];
        public int RecoveryCount { get; private set; }
        public int InvisibleRelocations { get; private set; }
        public float StalledSeconds => stalledFor;
        private readonly List<int> route = new List<int>();
        private readonly Collider[] overlaps = new Collider[48];
        private Vector3 HalfSize = new Vector3(0.85f, 0.32f, 1.8f);
        public float DistanceTravelled { get; private set; }
        public int CompletedRoutes { get; private set; }
        public int VisitedDistricts { get; private set; }

        public bool Initialize(CityRoadGraph roadGraph, int start, GameObject prefab, float cruisingSpeed, Transform playerCar = null)
        {
            graph = roadGraph; current = start; cruise = cruisingSpeed; player = playerCar; view = Camera.main;
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
            graph.Reservations.SetPose(this,body.position,body.rotation,HalfSize);
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
                Bounds? avoid=null;
                if(avoidObstacle!=null && Time.time<avoidUntil)
                {
                    Bounds bounds=avoidObstacle.bounds;bounds.Expand(new Vector3(HalfSize.x*2+.5f,2,HalfSize.x*2+.5f));avoid=bounds;
                }
                if(!graph.PlanRoute(start,heading,ref nextDistrict,this,route,avoid))continue;
                var candidate=TrafficPath.Build(graph,body.position,lead,route,HalfSize);
                if(candidate==null)continue;
                path=candidate;progress=0;return true;
            }
            return false;
        }
        private bool Blocked(Vector3 position, Quaternion rotation, bool remember = false)
        {
            int count=Physics.OverlapBoxNonAlloc(position,HalfSize+new Vector3(.06f,.01f,.06f),overlaps,rotation,~0,QueryTriggerInteraction.Ignore);
            bool blocked=count==overlaps.Length;float nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++)if(overlaps[i].attachedRigidbody!=body)
            {
                blocked=true;
                if(remember)
                {
                    float d=(overlaps[i].ClosestPoint(position)-position).sqrMagnitude;
                    if(d<nearest){nearest=d;blockingObstacle=overlaps[i];blockingCar=overlaps[i].attachedRigidbody!=null?overlaps[i].attachedRigidbody.GetComponent<NpcTrafficCar>():null;}
                }
            }
            if(graph.Reservations.PoseBlocked(this,position,rotation,HalfSize,out var owner))
            {
                blocked=true;
                if(remember && blockingObstacle==null && owner is NpcTrafficCar peer){blockingCar=peer;blockingObstacle=peer.GetComponent<Collider>();}
            }
            return blocked;
        }
        private void Commit(Vector3 position,Quaternion rotation)
        {
            // MovePosition is applied later by PhysX. Reserve its future volume now so
            // another car cannot choose the same space in this FixedUpdate step.
            graph.Reservations.SetPose(this,position,rotation,HalfSize);
            body.MovePosition(position);body.MoveRotation(rotation);
        }
        private bool Visible(Vector3 position)
        {
            if(view==null)return true;
            GeometryUtility.CalculateFrustumPlanes(view,viewPlanes);
            return GeometryUtility.TestPlanesAABB(viewPlanes,new Bounds(position,HalfSize*2+Vector3.one*6));
        }
        private bool RecoverOffscreen()
        {
            if(player==null || (body.position-player.position).sqrMagnitude<1225 || Visible(body.position))return false;
            int first=Random.Range(0,graph.nodes.Count);
            for(int n=0;n<graph.nodes.Count;n++)
            {
                int index=(first+n)%graph.nodes.Count;var node=graph.nodes[index];
                if(node.neighbours.Count<3 || (node.position-player.position).sqrMagnitude<1225 || (node.position-body.position).sqrMagnitude<400 || Visible(node.position))continue;
                foreach(int next in node.neighbours)
                {
                    Quaternion rotation=Quaternion.LookRotation(graph.nodes[next].position-node.position);
                    if(!graph.ClearPose(node.position,rotation,HalfSize) || Blocked(node.position,rotation))continue;
                    Vector3 oldPosition=body.position;Quaternion oldRotation=body.rotation;var oldPath=path;float oldProgress=progress,oldSpeed=speed;
                    body.position=node.position;body.rotation=rotation;speed=0;path=null;avoidObstacle=null;
                    if(!PlanRoute(rotation*Vector3.forward)){body.position=oldPosition;body.rotation=oldRotation;path=oldPath;progress=oldProgress;speed=oldSpeed;continue;}
                    graph.Reservations.ReleaseAll(this);graph.Reservations.SetPose(this,body.position,body.rotation,HalfSize);
                    retreatRemaining=0;blockedFor=stalledFor=0;forwardCheckpoint=DistanceTravelled;InvisibleRelocations++;RecoveryCount++;return true;
                }
            }
            return false;
        }
        private bool BeginRetreat()
        {
            Vector3 p=body.position-body.rotation*Vector3.forward*.4f;Quaternion r=body.rotation;
            if(path!=null && progress>.4f)path.Evaluate(progress-.4f,out p,out r);
            if(Blocked(p,r) || !graph.ClearPose(p,r,HalfSize))return false;
            retreatRemaining=4;speed=0;return true;
        }
        private void Recover()
        {
            if(retreatRemaining>0 || stalledFor<3.5f || Time.time<nextRecovery)return;
            nextRecovery=Time.time+1.5f+(Mathf.Abs(GetInstanceID())%7)*.12f;
            if(stalledFor>18 && RecoverOffscreen())return;
            if(blockingObstacle!=null){avoidObstacle=blockingObstacle;avoidUntil=Time.time+10;}
            // One crossing vehicle backs out first; following traffic never drives through it.
            bool yields=blockingCar!=null && GetInstanceID()>blockingCar.GetInstanceID();
            if(yields && BeginRetreat()){RecoveryCount++;return;}
            graph.Reservations.ReleaseAll(this);target=-1;
            if(PlanRoute(body.rotation*Vector3.forward)){RecoveryCount++;return;}
            if(BeginRetreat())RecoveryCount++;
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
            graph.Reservations.SetPose(this,body.position,body.rotation,HalfSize);
            if(DistanceTravelled-forwardCheckpoint>=.75f){forwardCheckpoint=DistanceTravelled;stalledFor=0;}else stalledFor+=dt;
            Recover();
            if(retreatRemaining>0)
            {
                float retreat=Mathf.Min(1.8f*dt,retreatRemaining);
                Vector3 p=body.position-body.rotation*Vector3.forward*retreat;Quaternion r=body.rotation;
                if(path!=null && progress>=retreat)path.Evaluate(progress-retreat,out p,out r);
                bool safe=!Blocked(p,r) && graph.RoadFootprint(p,r,HalfSize.x+0.12f,HalfSize.z+0.12f);
                if(safe){Commit(p,r);progress=Mathf.Max(0,progress-retreat);retreatRemaining-=retreat;speed=-1.8f;}
                if(!safe || retreatRemaining<=0.001f)
                {retreatRemaining=0;speed=0;path=null;nextDecision=Time.time+0.3f;nextRecovery=Time.time+1;graph.Reservations.ReleaseAll(this);}
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
            blockingCar=null;blockingObstacle=null;
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
                Commit(proposed,facing);
                progress+=step;DistanceTravelled+=step;
                VisitedDistricts |= 1 << graph.District(proposed);
            }
            if(!clear || speed<0.15f)blockedFor+=dt;else blockedFor=0;
        }
        private void LateUpdate()
        {
            if(wheels!=null && Time.deltaTime>0)wheels.Animate(speed,wheelSteering,Time.deltaTime);
        }
        private void OnDestroy() { graph?.Reservations.ReleaseAll(this); graph?.Reservations.ForgetPose(this); }
    }
}


