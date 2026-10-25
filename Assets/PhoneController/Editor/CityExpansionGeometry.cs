using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlyHigh.PhoneController.Editor
{
    public sealed class ExpansionMesh
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> indices = new List<int>();
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = vertices.Count; vertices.AddRange(new[] { a,b,c,d });
            indices.AddRange(new[] { i,i+1,i+2,i,i+2,i+3 });
        }
        public void Strip(Vector3 a, Vector3 b, float width)
        {
            Vector3 r = Vector3.Cross(Vector3.up, (b-a).normalized) * (width*0.5f);
            Quad(a-r,b-r,b+r,a+r);
        }
        public void Box(Vector3 p, Vector3 direction, float length, float width, float height)
        {
            Vector3 d = direction.normalized * (length*0.5f), r = Vector3.Cross(Vector3.up,direction.normalized)*(width*0.5f), u=Vector3.up*height;
            Vector3 a=p-d-r,b=p+d-r,c=p+d+r,e=p-d+r;
            Quad(a+u,b+u,c+u,e+u); Quad(a,b,b+u,a+u); Quad(b,c,c+u,b+u); Quad(c,e,e+u,c+u); Quad(e,a,a+u,e+u);
        }
        public void Disc(Vector3 p, float radius)
        {
            for (int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12; int start=vertices.Count;
                vertices.AddRange(new[] {p,p+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius,p+new Vector3(Mathf.Sin(b),0,Mathf.Cos(b))*radius});
                indices.AddRange(new[] {start,start+1,start+2});
            }
        }
        public Mesh Build(string name)
        {
            var mesh=new Mesh {name=name,indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }

    public static class CityEntranceCut
    {
        private struct Vertex
        {
            public Vector3 p,n;
            public Vector2 uv,uv2;
            public Color color;
            public static Vertex Lerp(Vertex a,Vertex b,float t) => new Vertex
            {p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t),uv2=Vector2.Lerp(a.uv2,b.uv2,t),color=Color.Lerp(a.color,b.color,t)};
        }
        public static Mesh Cut(MeshFilter filter,Rect corridor,float ceiling)
        {
            Mesh source=filter.sharedMesh;
            using(var data=MeshUtility.AcquireReadOnlyMeshData(source))
            using(var positions=new NativeArray<Vector3>(source.vertexCount,Allocator.Temp))
            using(var normals=new NativeArray<Vector3>(source.vertexCount,Allocator.Temp))
            using(var uvs=new NativeArray<Vector2>(source.vertexCount,Allocator.Temp))
            using(var uv2s=new NativeArray<Vector2>(source.vertexCount,Allocator.Temp))
            using(var colors=new NativeArray<Color>(source.vertexCount,Allocator.Temp))
            {
                data[0].GetVertices(positions);
                bool hasNormals=source.HasVertexAttribute(VertexAttribute.Normal),hasUVs=source.HasVertexAttribute(VertexAttribute.TexCoord0),hasUV2=source.HasVertexAttribute(VertexAttribute.TexCoord1),hasColors=source.HasVertexAttribute(VertexAttribute.Color);
                if(hasNormals)data[0].GetNormals(normals);
                if(hasUVs)data[0].GetUVs(0,uvs);
                if(hasUV2)data[0].GetUVs(1,uv2s);
                if(hasColors)data[0].GetColors(colors);
                var output=new List<Vertex>(); var submeshes=new List<int[]>(); bool changed=false;
                for(int sub=0;sub<source.subMeshCount;sub++)
                {
                    var desc=source.GetSubMesh(sub);
                    if(desc.topology!=MeshTopology.Triangles)throw new InvalidOperationException("Non-triangle geometry at the road connection.");
                    var indices=new List<int>();
                    for(int i=0;i<desc.indexCount;i+=3)
                    {
                        var polygon=new List<Vertex>(3); bool high=false;
                        for(int c=0;c<3;c++)
                        {
                            int index=source.indexFormat==IndexFormat.UInt16 ? data[0].GetIndexData<ushort>()[desc.indexStart+i+c] : data[0].GetIndexData<int>()[desc.indexStart+i+c];
                            index+=desc.baseVertex;
                            var vertex=new Vertex {p=positions[index],n=hasNormals?normals[index]:Vector3.up,uv=hasUVs?uvs[index]:Vector2.zero,uv2=hasUV2?uv2s[index]:Vector2.zero,color=hasColors?colors[index]:Color.white};
                            polygon.Add(vertex); high|=filter.transform.TransformPoint(vertex.p).y>ceiling;
                        }
                        if(high){Emit(polygon,output,indices);continue;}
                        for(int plane=0;plane<4&&polygon.Count>0;plane++)
                        {
                            Split(polygon,filter.transform,corridor,plane,out var inside,out var outside);
                            Emit(outside,output,indices); polygon=inside;
                        }
                        changed|=polygon.Count>0;
                    }
                    submeshes.Add(indices.ToArray());
                }
                if(!changed)return null;
                var mesh=new Mesh {name=source.name+" connection patch",indexFormat=IndexFormat.UInt32,subMeshCount=source.subMeshCount};
                var p=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var uv2=new List<Vector2>();var color=new List<Color>();
                foreach(var vertex in output){p.Add(vertex.p);n.Add(vertex.n);uv.Add(vertex.uv);uv2.Add(vertex.uv2);color.Add(vertex.color);}
                mesh.SetVertices(p);mesh.SetNormals(n);mesh.SetUVs(0,uv);if(hasUV2)mesh.SetUVs(1,uv2);mesh.SetColors(color);
                for(int i=0;i<submeshes.Count;i++)mesh.SetTriangles(submeshes[i],i);
                mesh.RecalculateBounds();return mesh;
            }
        }
        private static float Distance(Vertex v,Transform transform,Rect rect,int plane)
        {
            Vector3 p=transform.TransformPoint(v.p);
            switch(plane){case 0:return p.x-rect.xMin;case 1:return rect.xMax-p.x;case 2:return p.z-rect.yMin;default:return rect.yMax-p.z;}
        }
        private static void Split(List<Vertex> polygon,Transform transform,Rect rect,int plane,out List<Vertex> inside,out List<Vertex> outside)
        {
            inside=new List<Vertex>();outside=new List<Vertex>();var previous=polygon[polygon.Count-1];float before=Distance(previous,transform,rect,plane);
            foreach(var vertex in polygon)
            {
                float after=Distance(vertex,transform,rect,plane);
                if((before>=0)!=(after>=0)){var cut=Vertex.Lerp(previous,vertex,before/(before-after));inside.Add(cut);outside.Add(cut);}
                if(after>=0)inside.Add(vertex);else outside.Add(vertex); previous=vertex;before=after;
            }
        }
        private static void Emit(List<Vertex> polygon,List<Vertex> output,List<int> indices)
        {
            if(polygon.Count<3)return;int start=output.Count;output.AddRange(polygon);
            for(int i=1;i<polygon.Count-1;i++)indices.AddRange(new[] {start,start+i,start+i+1});
        }
    }
}
