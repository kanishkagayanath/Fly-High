using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FlyHigh.PhoneController
{
    public enum CityTimeMode { Automatic, Morning, Evening, Night }

    /// <summary>Scene-owned day/night presentation. Imported assets are never edited at runtime.</summary>
    public sealed class CityDayNight : MonoBehaviour
    {
        private const string ModeKey="FlyHigh.TimeOfDay";
        private static readonly int Emission=Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionMap=Shader.PropertyToID("_EmissionMap");
        private static readonly int Zenith=Shader.PropertyToID("_Zenith"), Horizon=Shader.PropertyToID("_Horizon"), SunColor=Shader.PropertyToID("_SunColor");
        private static readonly int SunDirection=Shader.PropertyToID("_SunDirection"), MoonDirection=Shader.PropertyToID("_MoonDirection"), Night=Shader.PropertyToID("_Night");
        private static readonly int Shallow=Shader.PropertyToID("_ShallowColor"), Deep=Shader.PropertyToID("_DeepColor"), Foam=Shader.PropertyToID("_FoamColor"), WaterHorizon=Shader.PropertyToID("_HorizonColor");
        private static readonly int RippleBrightness=Shader.PropertyToID("_RippleBrightness");
        private readonly Dictionary<Material,Material> streets=new Dictionary<Material,Material>(),signals=new Dictionary<Material,Material>(),windows=new Dictionary<Material,Material>();
        private readonly List<Material> ownedMaterials=new List<Material>();
        private readonly List<Binding> bindings=new List<Binding>();
        private readonly List<Fixture> fixtures=new List<Fixture>();
        private readonly List<VehicleLights> vehicles=new List<VehicleLights>();
        private readonly List<Water> waters=new List<Water>();
        private Transform player;
        private WifiReceiver controls;
        private Light sun;
        private Material sky,oldSky;
        private Color oldSunColor,oldFog,oldAmbientSky,oldAmbientEquator,oldAmbientGround;
        private float oldSunIntensity,oldFogStart,oldFogEnd,oldReflection;
        private Quaternion oldSunRotation;
        private AmbientMode oldAmbientMode;
        private UniversalAdditionalCameraData cameraData;
        private bool oldPost,initialized;
        private VolumeProfile profile;
        private GameObject atmosphere;
        private Bloom bloom;
        private ColorAdjustments grade;
        private CityLightingSettings settings;
        private State current;
        private float hour;
        public CityTimeMode Mode { get; private set; }
        public float NightAmount => current.night;
        public float Hour => Mode==CityTimeMode.Automatic?hour:Mode==CityTimeMode.Morning?9:Mode==CityTimeMode.Evening?18:0;
        public int StreetLampCount => fixtures.Count;
        public int VehicleCount => vehicles.Count;
        private struct Binding { public Renderer renderer; public Material[] original; }
        private struct Fixture { public Light light; public float intensity; }
        private struct Water { public Material material; public Color shallow,deep,foam; }
        private struct State
        {
            public float hour,intensity,night,exposure,fogEnd;
            public Color sun,zenith,horizon,ambientSky,ambientEquator,ambientGround;
            public Quaternion rotation;
            public static State Mix(State a,State b,float t)
            {
                return new State{intensity=Mathf.Lerp(a.intensity,b.intensity,t),night=Mathf.Lerp(a.night,b.night,t),exposure=Mathf.Lerp(a.exposure,b.exposure,t),fogEnd=Mathf.Lerp(a.fogEnd,b.fogEnd,t),sun=Color.Lerp(a.sun,b.sun,t),zenith=Color.Lerp(a.zenith,b.zenith,t),horizon=Color.Lerp(a.horizon,b.horizon,t),ambientSky=Color.Lerp(a.ambientSky,b.ambientSky,t),ambientEquator=Color.Lerp(a.ambientEquator,b.ambientEquator,t),ambientGround=Color.Lerp(a.ambientGround,b.ambientGround,t),rotation=Quaternion.Slerp(a.rotation,b.rotation,t)};
            }
        }
        private static State NightState(float time)
        {return new State{hour=time,intensity=.16f,night=1,exposure=.25f,fogEnd=330,sun=new Color(.45f,.58f,1),zenith=new Color(.008f,.018f,.055f),horizon=new Color(.035f,.065f,.12f),ambientSky=new Color(.11f,.16f,.27f),ambientEquator=new Color(.07f,.10f,.17f),ambientGround=new Color(.045f,.055f,.08f),rotation=Quaternion.Euler(48,210,0)};}
        private static readonly State[] States={NightState(0),NightState(5),
            new State{hour=8,intensity=1.05f,night=0,exposure=.1f,fogEnd=620,sun=new Color(1,.89f,.72f),zenith=new Color(.19f,.47f,.70f),horizon=new Color(.78f,.84f,.85f),ambientSky=new Color(.65f,.78f,.88f),ambientEquator=new Color(.46f,.56f,.62f),ambientGround=new Color(.26f,.30f,.32f),rotation=Quaternion.Euler(30,120,0)},
            new State{hour=12,intensity=1.35f,night=0,exposure=.05f,fogEnd=620,sun=new Color(1,.98f,.92f),zenith=new Color(.15f,.44f,.72f),horizon=new Color(.68f,.82f,.88f),ambientSky=new Color(.72f,.84f,.95f),ambientEquator=new Color(.5f,.59f,.65f),ambientGround=new Color(.30f,.32f,.34f),rotation=Quaternion.Euler(58,150,0)},
            new State{hour=18,intensity=.68f,night=.22f,exposure=.15f,fogEnd=460,sun=new Color(1,.45f,.23f),zenith=new Color(.24f,.23f,.47f),horizon=new Color(.95f,.46f,.29f),ambientSky=new Color(.43f,.30f,.42f),ambientEquator=new Color(.32f,.22f,.26f),ambientGround=new Color(.16f,.13f,.17f),rotation=Quaternion.Euler(10,255,0)},NightState(21),NightState(24)};
        private static State At(float time)
        {
            for(int i=1;i<States.Length;i++)if(time<=States[i].hour)
            {float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(States[i-1].hour,States[i].hour,time));return State.Mix(States[i-1],States[i],t);}
            return States[0];
        }
        public void Initialize(Transform playerCar,WifiReceiver receiver)
        {
            if(initialized)return;
            player=playerCar;controls=receiver;settings=Resources.Load<CityLightingSettings>("FlyHighLighting");
            hour=settings!=null?settings.initialHour:9;Mode=(CityTimeMode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey,0),0,3);
            oldSky=RenderSettings.skybox;oldFog=RenderSettings.fogColor;oldFogStart=RenderSettings.fogStartDistance;oldFogEnd=RenderSettings.fogEndDistance;
            oldAmbientMode=RenderSettings.ambientMode;oldAmbientSky=RenderSettings.ambientSkyColor;oldAmbientEquator=RenderSettings.ambientEquatorColor;oldAmbientGround=RenderSettings.ambientGroundColor;oldReflection=RenderSettings.reflectionIntensity;
            sun=RenderSettings.sun;
            if(sun==null)foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){sun=light;break;}
            if(sun==null){Debug.LogError("[City lighting] A directional light is required.");return;}
            oldSunColor=sun.color;oldSunIntensity=sun.intensity;oldSunRotation=sun.transform.rotation;
            if(settings!=null && settings.skyMaterial!=null)sky=new Material(settings.skyMaterial);
            else {var shader=Shader.Find("FlyHigh/City Sky");if(shader!=null)sky=new Material(shader);}
            if(sky!=null){sky.name="City sky (runtime)";ownedMaterials.Add(sky);RenderSettings.skybox=sky;}
            SetupPostProcessing();BuildCityLights();
            current=At(Hour);initialized=true;Apply();StartCoroutine(AttachVehicleLights());
        }
        public void SetMode(CityTimeMode mode)
        {Mode=mode;PlayerPrefs.SetInt(ModeKey,(int)mode);PlayerPrefs.Save();}
        public void CycleMode(){SetMode((CityTimeMode)(((int)Mode+1)%4));}
        private void Update()
        {
            if(!initialized)return;
            if(Mode==CityTimeMode.Automatic)hour=Mathf.Repeat(hour+Time.deltaTime*24/((settings!=null?settings.cycleMinutes:12)*60),24);
            float duration=settings!=null?settings.transitionSeconds:5;
            float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*5/Mathf.Max(.1f,duration));
            State target=At(Hour);
            current=State.Mix(current,target,blend);
            if(Mathf.Abs(current.night-target.night)<.003f)current.night=target.night;
            Apply();
        }
        private void Apply()
        {
            sun.color=current.sun;sun.intensity=current.intensity;sun.transform.rotation=current.rotation;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=current.ambientSky;RenderSettings.ambientEquatorColor=current.ambientEquator;RenderSettings.ambientGroundColor=current.ambientGround;
            RenderSettings.fogColor=current.horizon;RenderSettings.fogStartDistance=Mathf.Lerp(oldFogStart,65,current.night);RenderSettings.fogEndDistance=current.fogEnd;RenderSettings.reflectionIntensity=Mathf.Lerp(oldReflection,.18f,current.night);
            if(sky!=null){sky.SetColor(Zenith,current.zenith);sky.SetColor(Horizon,current.horizon);sky.SetColor(SunColor,current.sun);sky.SetVector(SunDirection,-sun.transform.forward);sky.SetVector(MoonDirection,-sun.transform.forward);sky.SetFloat(Night,current.night);}
            foreach(var mat in streets.Values)mat.SetColor(Emission,Color.white*Mathf.Lerp(.02f,4.5f,current.night));
            foreach(var mat in signals.Values)mat.SetColor(Emission,Color.white*Mathf.Lerp(1.25f,4,current.night));
            foreach(var mat in windows.Values)mat.SetColor(Emission,new Color(.8f,1.05f,1.4f)*(current.night*.5f));
            foreach(var fixture in fixtures)
            {if(fixture.light==null)continue;float intensity=fixture.intensity*current.night;fixture.light.intensity=intensity;fixture.light.enabled=intensity>.01f;}
            foreach(var water in waters)
            {
                water.material.SetColor(Shallow,Color.Lerp(water.shallow,new Color(.018f,.085f,.13f),current.night));
                water.material.SetColor(Deep,Color.Lerp(water.deep,new Color(.008f,.025f,.065f),current.night));
                water.material.SetColor(Foam,Color.Lerp(water.foam,new Color(.18f,.30f,.42f),current.night));water.material.SetColor(WaterHorizon,current.horizon);
                water.material.SetFloat(RippleBrightness,Mathf.Lerp(.022f,.003f,current.night));
            }
            if(bloom!=null)bloom.intensity.value=Mathf.Lerp(.18f,.38f,current.night);
            if(grade!=null)grade.postExposure.value=current.exposure;
        }
        private void SetupPostProcessing()
        {
            var camera=Camera.main;if(camera!=null){cameraData=camera.GetComponent<UniversalAdditionalCameraData>();if(cameraData!=null){oldPost=cameraData.renderPostProcessing;cameraData.renderPostProcessing=true;}}
            var obj=new GameObject("Day-night atmosphere");obj.transform.SetParent(transform,false);atmosphere=obj;
            var volume=obj.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;
            if(settings!=null&&settings.atmosphereProfile!=null){volume.sharedProfile=settings.atmosphereProfile;profile=volume.profile;}
            else {profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;}
            profile.name="City lighting (runtime)";
            if(!profile.TryGet(out bloom))bloom=profile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);bloom.intensity.Override(.18f);bloom.scatter.Override(.55f);bloom.clamp.Override(12);
            if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);
            if(!profile.TryGet(out grade))grade=profile.Add<ColorAdjustments>(true);grade.contrast.Override(8);grade.saturation.Override(8);
        }
        private Material Emissive(Material source,Dictionary<Material,Material> cache)
        {
            if(cache.TryGetValue(source,out var clone))return clone;
            Material template=cache==windows&&settings!=null&&settings.windowGlowMaterial!=null?settings.windowGlowMaterial:source;
            clone=new Material(template){name=source.name+" (city lighting)"};clone.EnableKeyword("_EMISSION");
            if(clone.HasProperty(EmissionMap) && source.mainTexture!=null)clone.SetTexture(EmissionMap,source.mainTexture);
            ownedMaterials.Add(clone);cache.Add(source,clone);return clone;
        }
        private void BuildCityLights()
        {
            foreach(var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(renderer.gameObject.scene!=gameObject.scene)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                bool street=renderer.name.StartsWith("Spotlight_");bool signal=renderer.name.StartsWith("traffic_light_");bool building=renderer.name.StartsWith("Eco_Building")||renderer.name.StartsWith("Regular_Building");
                var original=renderer.sharedMaterials;var replacement=(Material[])original.Clone();bool changed=false;
                for(int i=0;i<original.Length;i++)
                {
                    var mat=original[i];if(mat==null)continue;
                    if((street||signal)&&mat.name=="Emissive")
                    {
                        replacement[i]=Emissive(mat,street?streets:signals);changed=true;
                        if(street && i<filter.sharedMesh.subMeshCount)
                        {
                            Bounds lens=filter.sharedMesh.GetSubMesh(i).bounds;
                            var obj=new GameObject("Street lamp light");obj.transform.SetParent(renderer.transform,false);obj.transform.localPosition=lens.center-Vector3.up*.2f;
                            obj.transform.localRotation=Quaternion.Euler(90,0,0);var lamp=obj.AddComponent<Light>();bool round=renderer.name=="Spotlight_02";
                            lamp.type=round?LightType.Point:LightType.Spot;lamp.color=new Color(1,.76f,.44f);lamp.range=round?13:17;lamp.spotAngle=115;lamp.innerSpotAngle=75;lamp.shadows=LightShadows.None;lamp.intensity=0;lamp.enabled=false;
                            fixtures.Add(new Fixture{light=lamp,intensity=round?65:155});
                        }
                    }
                    else if(building&&mat.name=="Glass"){replacement[i]=Emissive(mat,windows);changed=true;}
                    else if(mat.shader.name=="FlyHigh/Coastal Water")
                    {
                        var clone=new Material(mat){name=mat.name+" (day-night)"};ownedMaterials.Add(clone);replacement[i]=clone;changed=true;
                        waters.Add(new Water{material=clone,shallow=mat.GetColor(Shallow),deep=mat.GetColor(Deep),foam=mat.GetColor(Foam)});
                    }
                }
                if(changed){bindings.Add(new Binding{renderer=renderer,original=original});renderer.sharedMaterials=replacement;}
            }
        }
        private IEnumerator AttachVehicleLights()
        {
            // Traffic creates its models after one startup frame; discover fittings once.
            yield return null;yield return null;
            var seen=new HashSet<Transform>();
            foreach(var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(renderer.gameObject.scene!=gameObject.scene)continue;
                bool headlights=false;foreach(var material in renderer.sharedMaterials)if(material!=null&&(material.name=="Car_Headlights"||material.name=="Car_Taillights")){headlights=true;break;}
                if(!headlights)continue;
                var body=renderer.GetComponentInParent<Rigidbody>();Transform owner=body!=null?body.transform:renderer.transform;
                if(!seen.Add(owner))continue;
                if(owner.GetComponent<VehicleLights>()!=null)continue;
                var lighting=owner.gameObject.AddComponent<VehicleLights>();lighting.Initialize(this,renderer,body,owner==player?controls:null,owner==player);vehicles.Add(lighting);
            }
        }
        public void RefreshPlayerVehicleLights()
        {
            if(player==null)return;
            foreach(var old in player.GetComponents<VehicleLights>()){old.enabled=false;old.Release();vehicles.Remove(old);Destroy(old);}
            foreach(var renderer in player.GetComponentsInChildren<MeshRenderer>())
            {
                bool lenses=false;foreach(var material in renderer.sharedMaterials)if(material!=null && (material.name=="Car_Headlights" || material.name=="Car_Taillights"))lenses=true;
                if(!lenses)continue;
                var lighting=player.gameObject.AddComponent<VehicleLights>();lighting.Initialize(this,renderer,player.GetComponent<Rigidbody>(),controls,true);vehicles.Add(lighting);break;
            }
        }
        private void OnDestroy()
        {
            if(!initialized)return;
            foreach(var vehicle in vehicles)if(vehicle!=null)Destroy(vehicle);
            foreach(var fixture in fixtures)if(fixture.light!=null)Destroy(fixture.light.gameObject);
            foreach(var binding in bindings)if(binding.renderer!=null)binding.renderer.sharedMaterials=binding.original;
            if(sun!=null){sun.color=oldSunColor;sun.intensity=oldSunIntensity;sun.transform.rotation=oldSunRotation;}
            RenderSettings.skybox=oldSky;RenderSettings.fogColor=oldFog;RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;
            RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientSkyColor=oldAmbientSky;RenderSettings.ambientEquatorColor=oldAmbientEquator;RenderSettings.ambientGroundColor=oldAmbientGround;RenderSettings.reflectionIntensity=oldReflection;
            if(cameraData!=null)cameraData.renderPostProcessing=oldPost;
            foreach(var mat in ownedMaterials)if(mat!=null)Destroy(mat);
            if(profile!=null){foreach(var component in profile.components)if(component!=null)Destroy(component);Destroy(profile);}
            if(atmosphere!=null)Destroy(atmosphere);
        }
    }
}

