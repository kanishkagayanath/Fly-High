using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Reserve both ends of a move so two cars cannot swap through each other.</summary>
    public sealed class TrafficReservations
    {
        private readonly object[] owners;
        private sealed class Pose { public object owner; public Vector3 position, half; public Quaternion rotation; }
        private readonly List<Pose> poses = new List<Pose>(12);
        public void SetPose(object owner, Vector3 position, Quaternion rotation, Vector3 half)
        {
            foreach (var pose in poses) if (ReferenceEquals(pose.owner, owner))
            { pose.position=position;pose.rotation=rotation;pose.half=half;return; }
            poses.Add(new Pose{owner=owner,position=position,rotation=rotation,half=half});
        }
        public void ForgetPose(object owner) { for(int i=poses.Count-1;i>=0;i--)if(ReferenceEquals(poses[i].owner,owner))poses.RemoveAt(i); }
        public bool PoseBlocked(object owner,Vector3 position,Quaternion rotation,Vector3 half,out object blocker)
        {
            blocker=null;
            foreach(var other in poses)
            {
                if(ReferenceEquals(other.owner,owner) || Mathf.Abs(position.y-other.position.y)>half.y+other.half.y+.02f)continue;
                Vector3 delta=other.position-position;
                Vector3 right=rotation*Vector3.right,forward=rotation*Vector3.forward;
                Vector3 otherRight=other.rotation*Vector3.right,otherForward=other.rotation*Vector3.forward;
                if(Separated(delta,right,right,forward,half,otherRight,otherForward,other.half) ||
                    Separated(delta,forward,right,forward,half,otherRight,otherForward,other.half) ||
                    Separated(delta,otherRight,right,forward,half,otherRight,otherForward,other.half) ||
                    Separated(delta,otherForward,right,forward,half,otherRight,otherForward,other.half))continue;
                blocker=other.owner;return true;
            }
            return false;
        }
        private static bool Separated(Vector3 delta,Vector3 axis,Vector3 right,Vector3 forward,Vector3 half,Vector3 otherRight,Vector3 otherForward,Vector3 otherHalf)
        {
            float reach=Mathf.Abs(Vector3.Dot(axis,right))*half.x+Mathf.Abs(Vector3.Dot(axis,forward))*half.z+
                Mathf.Abs(Vector3.Dot(axis,otherRight))*otherHalf.x+Mathf.Abs(Vector3.Dot(axis,otherForward))*otherHalf.z+.06f;
            return Mathf.Abs(Vector3.Dot(delta,axis))>reach;
        }
        public TrafficReservations(int capacity) { owners = new object[capacity]; }
        public bool Available(int node, object owner) => node >= 0 && node < owners.Length &&
            (owners[node] == null || ReferenceEquals(owners[node], owner));
        public bool TryReserve(int node, object owner)
        {
            if (owner == null || !Available(node, owner)) return false;
            owners[node] = owner; return true;
        }
        public void Release(int node, object owner)
        {
            if (node >= 0 && node < owners.Length && ReferenceEquals(owners[node], owner)) owners[node] = null;
        }
        public void ReleaseAll(object owner)
        {
            for (int i = 0; i < owners.Length; i++) if (ReferenceEquals(owners[i], owner)) owners[i] = null;
        }
    }
}

