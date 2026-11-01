using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlyHigh.PhoneController.Editor
{
    /// <summary>Edit-time placement against the actual asphalt edges and painted lane dividers.</summary>
    public sealed class CoinLanePlacement : IDisposable
    {
        private sealed class Surface { public MeshCollider collider; public Material[] materials; public int[] first, count; }
        private readonly Dictionary<MeshCollider,Surface> surfaces=new Dictionary<MeshCollider,Surface>();
        private readonly Dictionary<Texture,Texture2D> textures=new Dictionary<Texture,Texture2D>();
        private readonly RaycastHit[] hits=new RaycastHit[32];
        private readonly CityRoadGraph graph;
        public int AlignedSamples {get;private set;}
        public int JunctionSamples {get;private set;}

        public CoinLanePlacement(CityRoadGraph roads)
        {
            graph=roads;
            foreach(var collider in UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                if(!collider.enabled || collider.isTrigger || collider.attachedRigidbody!=null || collider.sharedMesh==null)continue;
                var renderer=collider.GetComponent<MeshRenderer>();if(renderer==null)continue;
                var materials=renderer.sharedMaterials;bool asphalt=false;
                foreach(var material in materials)if(material!=null && RoadTriangleMask.AsphaltMaterial(material.name))asphalt=true;
                if(!asphalt)continue;
                var mesh=collider.sharedMesh;var first=new int[mesh.subMeshCount];var count=new int[first.Length];
                for(int i=0;i<first.Length;i++){var sub=mesh.GetSubMesh(i);first[i]=sub.indexStart/3;count[i]=sub.indexCount/3;}
                surfaces.Add(collider,new Surface{collider=collider,materials=materials,first=first,count=count});
            }
        }

        private bool Sample(Vector3 p,out RaycastHit hit,out Material material)
        {
            hit=default;material=null;int count=Physics.RaycastNonAlloc(p+Vector3.up*.6f,Vector3.down,hits,1.6f,~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return false;
            float nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++)if(hits[i].collider.attachedRigidbody==null && hits[i].distance<nearest){nearest=hits[i].distance;hit=hits[i];}
            if(!(hit.collider is MeshCollider mesh) || !surfaces.TryGetValue(mesh,out var surface) || hit.normal.y<.95f)return false;
            for(int i=0;i<surface.first.Length;i++)if(hit.triangleIndex>=surface.first[i] && hit.triangleIndex<surface.first[i]+surface.count[i])
            {material=i<surface.materials.Length?surface.materials[i]:null;return material!=null && RoadTriangleMask.AsphaltMaterial(material.name);}
            return false;
        }
        private bool Asphalt(Vector3 p)=>Sample(p,out _,out _);
        private float Edge(Vector3 p,Vector3 side)
        {
            float inside=0;
            for(float d=.25f;d<=24;d+=.25f)
            {
                if(Asphalt(p+side*d)){inside=d;continue;}
                float outside=d;
                for(int i=0;i<5;i++){float mid=(inside+outside)*.5f;if(Asphalt(p+side*mid))inside=mid;else outside=mid;}
                return (inside+outside)*.5f;
            }
            return 24;
        }
        private Texture2D Readable(Texture texture)
        {
            if(textures.TryGetValue(texture,out var copy))return copy;
            var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var previous=RenderTexture.active;
            try
            {
                Graphics.Blit(texture,rt);RenderTexture.active=rt;
                copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false,true);
                copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);copy.Apply();textures.Add(texture,copy);return copy;
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        }
        private bool Paint(Vector3 p)
        {
            if(!Sample(p,out var hit,out var material) || material.mainTexture==null)return false;
            Vector2 uv=Vector2.Scale(hit.textureCoord,material.mainTextureScale)+material.mainTextureOffset;
            var color=Readable(material.mainTexture).GetPixelBilinear(Mathf.Repeat(uv.x,1),Mathf.Repeat(uv.y,1));
            return Mathf.Min(color.r,color.g,color.b)>.48f;
        }
        public bool LaneOffset(Vector3 p,Vector3 forward,out Vector3 shift)
        {
            shift=Vector3.zero;forward.y=0;forward.Normalize();if(forward.sqrMagnitude<.5f || !Asphalt(p))return false;
            Vector3 side=Vector3.Cross(Vector3.up,forward);float left=Edge(p,-side),right=Edge(p,side),width=left+right;
            // An open junction is not a very wide lane. Interpolate its neighbouring lane anchors instead.
            if(width<7 || width>21 || left>=24 || right>=24)return false;
            var dividers=new List<float>{-left};float run=float.NaN,last=0;
            for(float cross=-left+.15f;cross<right-.1f;cross+=.1f)
            {
                int white=0;
                for(int along=-2;along<=2;along++)if(Paint(p+side*cross+forward*(along*2)))white++;
                if(white>=2){if(float.IsNaN(run))run=cross;last=cross;}
                else if(!float.IsNaN(run))
                {if(last-run<=.85f)dividers.Add((last+run)*.5f);run=float.NaN;}
            }
            if(!float.IsNaN(run) && last-run<=.85f)dividers.Add((last+run)*.5f);
            dividers.Add(right);dividers.Sort();
            // Treat the close pair of solid centre lines as a single lane boundary.
            for(int i=1;i<dividers.Count-1;i++)if(dividers[i]-dividers[i-1]<.85f && i>1)
            {dividers[i-1]=(dividers[i-1]+dividers[i])*.5f;dividers.RemoveAt(i);i--;}
            float best=float.PositiveInfinity,offset=0;
            for(int i=1;i<dividers.Count;i++)
            {
                float a=dividers[i-1],b=dividers[i],lane=b-a,centre=(a+b)*.5f;
                if(lane<2.7f || lane>8 || a>.3f || b<-.3f)continue;
                if(Mathf.Abs(centre)<best){best=Mathf.Abs(centre);offset=centre;}
            }
            if(float.IsPositiveInfinity(best))
            {
                int lanes=width>16?4:2;float lane=width/lanes;
                for(int i=0;i<lanes;i++){float centre=-left+(i+.5f)*lane;if(Mathf.Abs(centre)<best){best=Mathf.Abs(centre);offset=centre;}}
            }
            if(best>3.1f)return false;
            shift=side*offset;return true;
        }
        public CoinMissionRoute Align(CoinMissionRoute source)
        {
            int count=source.path.Length;var shifts=new Vector3[count];var valid=new bool[count];
            for(int i=0;i<count;i++)
            {
                Vector3 forward=source.path[Mathf.Min(count-1,i+2)]-source.path[Mathf.Max(0,i-2)];
                valid[i]=LaneOffset(source.path[i],forward,out shifts[i]);
                if(valid[i])AlignedSamples++;else JunctionSamples++;
            }
            for(int i=0;i<count;i++)if(!valid[i])
            {
                int a=i-1,b=i+1;while(a>=0 && i-a<=16 && !valid[a])a--;while(b<count && b-i<=16 && !valid[b])b++;
                bool before=a>=0 && i-a<=16,after=b<count && b-i<=16;
                if(before && after)shifts[i]=Vector3.Lerp(shifts[a],shifts[b],(i-a)/(float)(b-a));
                else if(before)shifts[i]=shifts[a];else if(after)shifts[i]=shifts[b];
            }
            var points=new Vector3[count];
            for(int i=0;i<count;i++)
            {
                Vector3 smooth=Vector3.zero;float weights=0;
                for(int j=-2;j<=2;j++){float weight=3-Mathf.Abs(j);smooth+=shifts[Mathf.Clamp(i+j,0,count-1)]*weight;weights+=weight;}
                float fade=Mathf.Min(1,i/5f,(count-1-i)/5f);points[i]=source.path[i]+smooth/weights*fade;
            }
            // Preserve authored starts/finishes and never pull the car footprint onto a kerb.
            for(int pass=0;pass<3;pass++)for(int i=1;i<count-1;i++)
            {
                var heading=Quaternion.LookRotation(points[i+1]-points[i-1]);
                if(!graph.ClearPose(points[i],heading,new Vector3(.92f,.32f,1.8f)))points[i]=Vector3.Lerp(source.path[i],points[i],.5f);
            }
            var lengths=new float[count];for(int i=1;i<count;i++)lengths[i]=lengths[i-1]+Vector3.Distance(points[i-1],points[i]);
            var coins=new Vector3[source.coins.Length];int segment=1;
            for(int i=0;i<coins.Length;i++)
            {
                float distance=(i+1)*lengths[count-1]/(coins.Length+1);
                while(segment<count-1 && lengths[segment]<distance)segment++;
                coins[i]=Vector3.Lerp(points[segment-1],points[segment],Mathf.InverseLerp(lengths[segment-1],lengths[segment],distance));
            }
            return new CoinMissionRoute{title=source.title,path=points,coins=coins,distance=lengths[count-1]};
        }
        public void Dispose(){foreach(var copy in textures.Values)UnityEngine.Object.DestroyImmediate(copy);textures.Clear();}
    }
}
