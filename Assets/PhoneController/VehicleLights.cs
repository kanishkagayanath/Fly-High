using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlyHigh.PhoneController
{
    /// <summary>Lights attached to the imported lens fittings, independent of driving physics.</summary>
    public sealed class VehicleLights : MonoBehaviour
    {
        private static readonly int Emission=Shader.PropertyToID("_EmissionColor"),EmissionMap=Shader.PropertyToID("_EmissionMap");
        private CityDayNight cycle;
        private MeshRenderer bodyRenderer;
        private Material[] original;
        private readonly List<Material> frontMaterials=new List<Material>(),rearMaterials=new List<Material>();
        private readonly List<Light> headlights=new List<Light>();
        private GameObject lightRoot;
        private Light tailSpill;
        private Mesh fallbackLenses;
        private Rigidbody body;
        private WifiReceiver controls;
        private bool player;
        private float previousSpeed,brakeTarget,brakeGlow;

        public void Initialize(CityDayNight environment,MeshRenderer model,Rigidbody vehicle,WifiReceiver input,bool isPlayer)
        {
            cycle=environment;bodyRenderer=model;body=vehicle;controls=input;player=isPlayer;
            original=bodyRenderer.sharedMaterials;var replacement=(Material[])original.Clone();
            var mesh=bodyRenderer.GetComponent<MeshFilter>().sharedMesh;
            lightRoot=new GameObject("Vehicle light fittings");lightRoot.transform.SetParent(bodyRenderer.transform,false);
            for(int i=0;i<original.Length;i++)
            {
                var source=original[i];if(source==null)continue;
                bool front=source.name=="Car_Headlights",rear=source.name=="Car_Taillights";
                if(!front&&!rear)continue;
                var material=new Material(source){name=source.name+" (vehicle lights)"};material.EnableKeyword("_EMISSION");
                if(material.HasProperty(EmissionMap)&&source.mainTexture!=null)material.SetTexture(EmissionMap,source.mainTexture);
                replacement[i]=material;if(front)frontMaterials.Add(material);else rearMaterials.Add(material);
                if(i>=mesh.subMeshCount)continue;Bounds lens=mesh.GetSubMesh(i).bounds;
                if(front)
                {
                    for(int side=0;side<2;side++)
                    {
                        float x=side==0?Mathf.Lerp(lens.min.x,lens.max.x,.15f):Mathf.Lerp(lens.min.x,lens.max.x,.85f);
                        var lamp=Lamp("Headlight "+(side==0?"left":"right"),new Vector3(x,lens.center.y,lens.max.z+.04f),new Color(1,.94f,.82f),LightType.Spot);
                        lamp.transform.localRotation=Quaternion.Euler(5,0,0);lamp.spotAngle=62;lamp.innerSpotAngle=38;lamp.range=28;headlights.Add(lamp);
                    }
                }
                else
                {
                    tailSpill=Lamp("Tail light glow",new Vector3(lens.center.x,lens.center.y,lens.min.z-.08f),new Color(1,.025f,.008f),LightType.Point);tailSpill.range=3.5f;
                }
            }
            // The supplied vans put front lenses in the body palette rather than
            // a separate headlight material. Add small fittings on that front face.
            if(headlights.Count==0 && rearMaterials.Count>0)AddVanHeadlights(mesh.bounds,rearMaterials[0]);
            bodyRenderer.sharedMaterials=replacement;Apply();
        }
        private void AddVanHeadlights(Bounds bounds,Material source)
        {
            var material=new Material(source){name="Van headlight lenses"};
            material.SetTexture("_BaseMap",Texture2D.whiteTexture);material.SetTexture(EmissionMap,Texture2D.whiteTexture);material.SetColor("_BaseColor",Color.white);frontMaterials.Add(material);
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int side=0;side<2;side++)
            {
                float x=bounds.center.x+(side==0?-1:1)*bounds.extents.x*.62f;
                Vector3 p=new Vector3(x,bounds.min.y+bounds.size.y*.28f,bounds.max.z+.025f);
                var light=Lamp("Van headlight "+side,p,new Color(1,.94f,.82f),LightType.Spot);light.transform.localRotation=Quaternion.Euler(5,0,0);light.range=28;light.spotAngle=62;light.innerSpotAngle=38;headlights.Add(light);
                float w=bounds.size.x*.065f,h=bounds.size.y*.025f;int start=vertices.Count;
                vertices.Add(p+new Vector3(-w,-h,.003f));vertices.Add(p+new Vector3(w,-h,.003f));vertices.Add(p+new Vector3(w,h,.003f));vertices.Add(p+new Vector3(-w,h,.003f));
                indices.Add(start);indices.Add(start+1);indices.Add(start+2);indices.Add(start);indices.Add(start+2);indices.Add(start+3);
            }
            fallbackLenses=new Mesh{name="Van lamp lenses (runtime)"};fallbackLenses.SetVertices(vertices);fallbackLenses.SetTriangles(indices,0);fallbackLenses.RecalculateNormals();fallbackLenses.RecalculateBounds();
            var obj=new GameObject("Van headlight lenses",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(lightRoot.transform,false);obj.GetComponent<MeshFilter>().sharedMesh=fallbackLenses;
            var lensRenderer=obj.GetComponent<MeshRenderer>();lensRenderer.sharedMaterial=material;lensRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        private Light Lamp(string name,Vector3 local,Color tint,LightType type)
        {
            var obj=new GameObject(name);obj.transform.SetParent(lightRoot.transform,false);obj.transform.localPosition=local;
            var light=obj.AddComponent<Light>();light.type=type;light.color=tint;light.shadows=LightShadows.None;light.intensity=0;return light;
        }
        private void FixedUpdate()
        {
            float speed=body!=null?new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude:0;
            bool decelerating=previousSpeed-speed>Time.fixedDeltaTime*1.3f;
            bool pedal=player&&controls!=null&&controls.Connected&&controls.Brake>.05f;
            if(player&&controls!=null&&!controls.Connected&&Keyboard.current!=null)pedal|=Keyboard.current.sKey.isPressed;
            brakeTarget=pedal||decelerating?1:0;previousSpeed=speed;
        }
        private void LateUpdate()
        {
            if(cycle==null)return;
            // Keep the visual fade active in menus while the driving clock stays paused.
            brakeGlow=Mathf.MoveTowards(brakeGlow,brakeTarget,Time.unscaledDeltaTime*7);Apply();
        }
        private void Apply()
        {
            if(cycle==null)return;
            float night=cycle.NightAmount,local=night;
            foreach(var mat in frontMaterials)mat.SetColor(Emission,Color.white*Mathf.Lerp(.08f,3.5f,night));
            foreach(var mat in rearMaterials)mat.SetColor(Emission,new Color(1,.03f,.01f)*(night*1.2f+brakeGlow*2.2f));
            foreach(var light in headlights){light.intensity=local*(player?42:24);light.enabled=light.intensity>.01f;}
            if(tailSpill!=null){tailSpill.intensity=local*(.9f+brakeGlow*1.3f);tailSpill.enabled=tailSpill.intensity>.01f;}
        }
        private void OnDestroy()
        {
            if(bodyRenderer!=null&&original!=null)bodyRenderer.sharedMaterials=original;
            if(lightRoot!=null)Destroy(lightRoot);
            if(fallbackLenses!=null)Destroy(fallbackLenses);
            foreach(var mat in frontMaterials)if(mat!=null)Destroy(mat);
            foreach(var mat in rearMaterials)if(mat!=null)Destroy(mat);
        }
    }
}

