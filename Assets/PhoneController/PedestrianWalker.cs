using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FlyHigh.PhoneController
{
    public sealed class PedestrianWalker : MonoBehaviour
    {
        private static readonly int WalkOne=Animator.StringToHash("Walk 1"),WalkTwo=Animator.StringToHash("Walk 2");
        private CityPedestrianSystem owner;
        private NavMeshAgent agent;
        private Animator animator;
        private List<Vector3> goals;
        private NavMeshPath path;
        private Vector3 lastSafe,lastProgress;
        private float waitUntil,nextDecision,nextSafety,lastProgressTime;
        private bool moving,secondGait;
        public int Region { get; private set; }
        public bool StartedAtBeach { get; private set; }
        public float DistanceWalked { get; private set; }
        public bool Walking => moving;
        public void Initialize(CityPedestrianSystem system,GameObject prefab,List<Vector3> destinations,int region,bool beach,float scale,float speed,int index)
        {
            owner=system;goals=destinations;Region=region;StartedAtBeach=beach;path=new NavMeshPath();
            var visual=Instantiate(prefab,transform,false);visual.name="Character visual";visual.transform.localScale=Vector3.one*scale;
            foreach(var collider in visual.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var body in visual.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.useGravity=false;}
            foreach(var a in visual.GetComponentsInChildren<Animator>(true)){a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.CullUpdateTransforms;if(a.isHuman)animator=a;}
            secondGait=(index%2)==1;
            agent=gameObject.AddComponent<NavMeshAgent>();agent.agentTypeID=0;
            agent.radius=.32f;agent.height=1.8f;agent.baseOffset=.025f;agent.speed=speed;agent.acceleration=2.5f;agent.angularSpeed=160;
            agent.stoppingDistance=.20f;agent.autoBraking=true;agent.autoTraverseOffMeshLink=false;agent.areaMask=1;agent.updateRotation=false;
            agent.obstacleAvoidanceType=ObstacleAvoidanceType.MedQualityObstacleAvoidance;agent.avoidancePriority=25+(index*7)%50;
            lastSafe=lastProgress=transform.position;lastProgressTime=Time.time;waitUntil=Time.time+Random.Range(.2f,2f);nextSafety=Time.time;
        }
        private void Update()
        {
            if(agent==null||!agent.isOnNavMesh||Time.deltaTime<=0)return;
            Vector3 position=transform.position;DistanceWalked+=Vector3.Distance(lastProgress,position);lastProgress=position;
            if(Time.time>=nextSafety)
            {
                nextSafety=Time.time+.2f;
                if(owner.Surfaces.Contains(position,out _))lastSafe=position;
                else {agent.Warp(lastSafe);agent.ResetPath();owner.RoadSafetyCorrections++;waitUntil=Time.time+.5f;}
            }
            bool walking=agent.velocity.sqrMagnitude>.015f;
            if(walking!=moving){moving=walking;if(animator!=null){animator.SetBool(WalkOne,moving&&!secondGait);animator.SetBool(WalkTwo,moving&&secondGait);}}
            if(moving)
            {
                Vector3 heading=agent.velocity;heading.y=0;
                if(heading.sqrMagnitude>.002f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(heading),160*Time.deltaTime);
                if(animator!=null)animator.speed=Mathf.Lerp(.65f,1.2f,Mathf.Clamp01(agent.velocity.magnitude/1.4f));
                lastProgressTime=Time.time;
            }
            else if(animator!=null)animator.speed=1;
            if(agent.pathPending||Time.time<nextDecision)return;
            nextDecision=Time.time+.3f;
            if(!agent.hasPath||agent.remainingDistance<=agent.stoppingDistance+.12f)
            {
                if(agent.hasPath){agent.ResetPath();waitUntil=Time.time+Random.Range(.8f,3f);}
                if(Time.time>=waitUntil)ChooseDestination();
            }
            else if(Time.time-lastProgressTime>5){agent.ResetPath();waitUntil=Time.time+Random.Range(.3f,1.1f);lastProgressTime=Time.time;}
        }
        private void ChooseDestination()
        {
            for(int i=0;i<12;i++)
            {
                Vector3 destination=goals[Random.Range(0,goals.Count)];float distance=(destination-transform.position).sqrMagnitude;
                if(distance<36||distance>4900)continue;
                if(!NavMesh.CalculatePath(transform.position,destination,new NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=1},path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                agent.SetPath(path);lastProgressTime=Time.time;return;
            }
            waitUntil=Time.time+1;
        }
    }
}


