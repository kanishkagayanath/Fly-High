using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController
{
    /// <summary>Shared pavement/sand whitelist. Asphalt, grass, roofs and submerged sand are excluded.</summary>
    public sealed class PedestrianSurfaces
    {
        private sealed class Floor { public RoadTriangleMask mask; public bool beach; }
        private readonly Dictionary<Collider,Floor> floors=new Dictionary<Collider,Floor>();
        private readonly RaycastHit[] hits=new RaycastHit[32];
        public static bool IsBeach(MeshCollider collider) => collider.name=="Island shoreline";
        public static bool IsPavement(MeshCollider collider)
        {
            string name=collider.name.ToLowerInvariant();
            return name.StartsWith("road_")||name.StartsWith("set_b_tiles")||name.Contains("sidewalk")||name.Contains("pavement");
        }
        public static bool AllowedMaterial(string name,bool beach)
        {
            if(name==null)return false;
            if(beach)return name.StartsWith("Warm sand");
            name=name.Replace(" (Instance)","");
            return name=="Color"||name.StartsWith("Tile_")||name.ToLowerInvariant().Contains("sidewalk");
        }
        public PedestrianSurfaces()
        {
            foreach(var collider in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                if(!collider.enabled||collider.isTrigger||collider.attachedRigidbody!=null||collider.sharedMesh==null||(!IsBeach(collider)&&!IsPavement(collider)))continue;
                var renderer=collider.GetComponent<MeshRenderer>();if(renderer==null)continue;
                var mesh=collider.sharedMesh;var materials=renderer.sharedMaterials;
                var first=new int[mesh.subMeshCount];var count=new int[first.Length];var allowed=new bool[first.Length];
                bool beach=IsBeach(collider);
                for(int i=0;i<first.Length;i++){var descriptor=mesh.GetSubMesh(i);first[i]=descriptor.indexStart/3;count[i]=descriptor.indexCount/3;allowed[i]=i<materials.Length&&materials[i]!=null&&AllowedMaterial(materials[i].name,beach);}
                floors.Add(collider,new Floor{mask=new RoadTriangleMask(first,count,allowed),beach=beach});
            }
        }
        public bool Contains(Vector3 position,out bool beach)
        {
            beach=false;
            int count=Physics.RaycastNonAlloc(position+Vector3.up*.65f,Vector3.down,hits,1.3f,~0,QueryTriggerInteraction.Ignore);
            float nearest=float.MaxValue;Floor found=null;RaycastHit ground=default;
            for(int i=0;i<count;i++)
            {
                if(hits[i].normal.y<.8f||!floors.TryGetValue(hits[i].collider,out var floor)||!floor.mask.Contains(hits[i].triangleIndex))continue;
                if(floor.beach&&hits[i].point.y<-.36f)continue;
                float distance=Mathf.Abs(hits[i].point.y-position.y);if(distance>nearest)continue;
                nearest=distance;found=floor;ground=hits[i];
            }
            if(found==null||Mathf.Abs(ground.point.y-position.y)>.3f)return false;
            beach=found.beach;return true;
        }
    }
}
