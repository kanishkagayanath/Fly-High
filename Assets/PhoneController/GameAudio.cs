using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace FlyHigh.PhoneController
{
    /// <summary>Scene-owned audio. The PC plays the world; the phone plays local interface feedback.</summary>
    [DisallowMultipleComponent]
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] private GameAudioSettings settings;
        private sealed class TrafficVoice { public Transform car;public AudioSource source;public Vector3 previous;public float speed; }
        private readonly List<TrafficVoice> traffic=new List<TrafficVoice>();
        private readonly List<AudioSource> sources=new List<AudioSource>();
        private AudioSource menuMusic,driveMusic,idle,engine,skid,reverse,cityDay,cityNight,sea;
        private AudioSource[] uiVoices,effectVoices;
        private Transform root,vehiclePoint;
        private RacingPrototype driving;
        private Rigidbody body;
        private DesktopMainMenu menu;
        private PhoneSender phone;
        private WifiReceiver receiver;
        private CityDayNight environment;
        private CoinMissionSystem missions;
        private VehicleGarage garage;
        private VehicleImpactSound impact;
        private bool initialized,subscribed,lastConnected,suspended;
        private int menuTrack,driveTrack,uiVoice,effectVoice,lastPhoneVolume=-1;
        private float nextClick,nextCoin,nextImpact,nextOffer;
        private DriveMenuView lastView=DriveMenuView.Home;
        public GameAudioSettings Settings=>settings;
        public void InitializeDesktop(RacingPrototype car,DesktopMainMenu navigation,CityDayNight city,CoinMissionSystem coinMissions,VehicleGarage progress,WifiReceiver connection)
        {
            if(initialized)return;driving=car;menu=navigation;environment=city;missions=coinMissions;garage=progress;receiver=connection;
            if(car!=null&&car.PlayerCar!=null)body=car.PlayerCar.GetComponent<Rigidbody>();
            InitializeSources();
            if(body!=null){impact=body.gameObject.AddComponent<VehicleImpactSound>();impact.Initialize(this);}
            lastConnected=receiver!=null&&receiver.Connected;Subscribe();
        }
        public void InitializePhone(PhoneSender connection)
        {
            if(initialized)return;phone=connection;InitializeSources();lastConnected=phone!=null&&phone.Connected;
        }
        private void InitializeSources()
        {
            if(settings==null)settings=Resources.Load<GameAudioSettings>("FlyHighAudio");
            initialized=true;if(settings==null)return;
            root=new GameObject("Fly High audio").transform;root.SetParent(transform,false);
            menuMusic=Source(root,"Menu music",true,settings.musicOutput,32);
            driveMusic=Source(root,"Driving music",true,settings.musicOutput,32);
            uiVoices=new AudioSource[4];effectVoices=new AudioSource[6];
            for(int i=0;i<uiVoices.Length;i++)uiVoices[i]=Source(root,"Interface voice "+i,true,settings.interfaceOutput,16);
            for(int i=0;i<effectVoices.Length;i++)effectVoices[i]=Source(root,"Reward / impact voice "+i,false,settings.effectsOutput,48);
            if(phone!=null)return;
            vehiclePoint=new GameObject("Player vehicle audio").transform;vehiclePoint.SetParent(root,false);
            idle=Source(vehiclePoint,"Engine idle",false,settings.vehicleOutput,64);
            engine=Source(vehiclePoint,"Engine driving",false,settings.vehicleOutput,64);
            skid=Source(vehiclePoint,"Tyres",false,settings.vehicleOutput,80);
            reverse=Source(vehiclePoint,"Reverse",false,settings.vehicleOutput,80);
            cityDay=Source(root,"City day ambience",false,settings.ambienceOutput,180);
            cityNight=Source(root,"City night ambience",false,settings.ambienceOutput,180);
            sea=Source(root,"Beach waves",false,settings.ambienceOutput,180);
        }
        private AudioSource Source(Transform parent,string name,bool menus,AudioMixerGroup output,int priority)
        {
            var node=new GameObject(name);node.transform.SetParent(parent,false);var source=node.AddComponent<AudioSource>();
            source.playOnAwake=false;source.volume=0;source.ignoreListenerPause=menus;source.outputAudioMixerGroup=output;
            source.dopplerLevel=0;source.priority=priority;sources.Add(source);return source;
        }
        private void OnEnable(){if(initialized)Subscribe();}
        private void Subscribe()
        {
            if(subscribed||phone!=null)return;subscribed=true;
            if(missions!=null){missions.PhaseChanged+=MissionPhase;missions.CoinCollected+=Coin;}
            if(garage!=null)garage.ActionCompleted+=GarageAction;
        }
        private void Unsubscribe()
        {
            if(!subscribed)return;subscribed=false;
            if(missions!=null){missions.PhaseChanged-=MissionPhase;missions.CoinCollected-=Coin;}
            if(garage!=null)garage.ActionCompleted-=GarageAction;
        }
        private void Update()
        {
            if(!initialized||settings==null||suspended)return;
            DriveMenuView view=phone!=null?(phone.HasMenuState?phone.MenuState.view:DriveMenuView.Home):menu!=null?menu.View:DriveMenuView.Home;
            bool connected=phone!=null?phone.Connected:receiver!=null&&receiver.Connected;
            if(connected!=lastConnected){Interface(connected?settings.connected:settings.disconnected);lastConnected=connected;}
            if(phone!=null&&phone.HasMenuState&&lastPhoneVolume!=phone.MenuState.volume)
            {lastPhoneVolume=phone.MenuState.volume;AudioListener.volume=lastPhoneVolume/100f;}
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            bool home=view==DriveMenuView.Home;
            Music(menuMusic,settings.menuMusic,ref menuTrack,home&&(phone==null||settings.phoneHomeMusic)?settings.musicVolume:0,dt);
            Music(driveMusic,settings.drivingMusic,ref driveTrack,!home&&phone==null?settings.musicVolume*(view==DriveMenuView.Driving?1:settings.pausedMusicMultiplier):0,dt);
            if(phone==null)
            {
                if(view==DriveMenuView.Driving&&lastView==DriveMenuView.Home)Effect(settings.engineStart,.65f);
                if(!AudioListener.pause)UpdateWorld(view==DriveMenuView.Driving&&Time.timeScale>0,dt);
            }
            lastView=view;
        }
        private void Music(AudioSource source,AudioClip[] playlist,ref int track,float target,float dt)
        {
            if(source==null)return;
            bool assigned=false;if(playlist!=null)foreach(var clip in playlist)if(clip!=null){assigned=true;break;}
            if(!assigned){source.Stop();source.clip=null;source.volume=0;return;}
            source.volume=Ease(source.volume,target,dt,settings.musicFadeSeconds);
            if(target<=0&&source.volume<.001f){source.Stop();source.volume=0;return;}
            if(source.isPlaying||target<=0)return;
            for(int i=0;i<playlist.Length;i++)
            {
                var clip=playlist[track%playlist.Length];track=(track+1)%playlist.Length;
                if(clip==null)continue;source.clip=clip;source.loop=false;source.Play();break;
            }
        }
        private static float Ease(float value,float target,float dt,float seconds)
        {return Mathf.Lerp(value,target,1-Mathf.Exp(-4*dt/Mathf.Max(.05f,seconds)));}
        private static void Loop(AudioSource source,AudioClip clip,float target,float pitch,float dt)
        {
            if(source==null)return;
            if(source.clip!=clip){source.Stop();source.clip=clip;}
            source.loop=true;source.pitch=Mathf.Clamp(pitch,.25f,3);source.volume=Ease(source.volume,clip!=null?Mathf.Clamp01(target):0,dt,.25f);
            if(clip==null||source.volume<.001f&&target<=0){source.Stop();return;}
            if(!source.isPlaying&&!AudioListener.pause)source.Play();
        }
        private void UpdateWorld(bool active,float dt)
        {
            float speed=body!=null?new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude:0;
            float fraction=driving!=null?Mathf.Clamp01(speed/Mathf.Max(1,driving.MaximumSpeed)):0;
            float load=driving!=null?Mathf.Max(fraction,Mathf.Abs(driving.AppliedThrottle)*.35f):fraction;
            float pitch=Mathf.Lerp(settings.lowEnginePitch,settings.highEnginePitch,load);
            float gain=active?settings.engineVolume:0;
            if(body!=null)vehiclePoint.position=body.transform.position;
            bool dual=settings.engineIdleLoop!=null&&settings.engineDrivingLoop!=null;
            Loop(idle,settings.engineIdleLoop,gain*(dual?1-load*.9f:1),dual?Mathf.Lerp(.9f,1.15f,load):pitch,dt);
            Loop(engine,settings.engineDrivingLoop,gain*(dual?Mathf.Lerp(.05f,1,load):1),pitch,dt);
            bool sliding=active&&driving!=null&&speed>settings.skidMinimumSpeed&&(driving.AppliedBrake>.15f||Mathf.Abs(driving.RawSteering)>.65f&&speed>12);
            Loop(skid,settings.tyreSkidLoop,sliding?settings.effectsVolume*.65f:0,1,dt);
            bool backing=active&&body!=null&&Vector3.Dot(body.linearVelocity,body.transform.forward)<-.4f;
            Loop(reverse,settings.reverseLoop,backing?settings.effectsVolume*.35f:0,1,dt);
            float night=environment!=null?Mathf.Clamp01(environment.NightAmount):0;
            float beach=0;
            if(body!=null&&environment!=null)
            {
                Vector3 p=body.position;float edge=Mathf.Min(settings.islandHalfSize.x-Mathf.Abs(p.x-settings.islandCentre.x),settings.islandHalfSize.y-Mathf.Abs(p.z-settings.islandCentre.y));
                beach=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/Mathf.Max(1,settings.beachFadeDistance)));
            }
            float ambience=active&&environment!=null?settings.ambienceVolume:0;
            bool both=settings.cityDayLoop!=null&&settings.cityNightLoop!=null;
            Loop(cityDay,settings.cityDayLoop,ambience*(both?1-night:1)*(1-beach*.65f),1,dt);
            Loop(cityNight,settings.cityNightLoop,ambience*(both?night:1)*(1-beach*.65f),1,dt);
            Loop(sea,settings.beachWavesLoop,ambience*beach,1,dt);
            foreach(var voice in traffic)
            {
                if(voice.car==null||voice.source==null)continue;
                float travelled=Vector3.Distance(voice.car.position,voice.previous);voice.previous=voice.car.position;
                voice.speed=Ease(voice.speed,Time.deltaTime>0&&travelled<10?travelled/Time.deltaTime:0,dt,.4f);
                bool near=body!=null&&(voice.car.position-body.position).sqrMagnitude<2500;
                Loop(voice.source,settings.trafficEngineLoop,active&&near?settings.trafficVolume:0,Mathf.Lerp(.8f,1.4f,Mathf.Clamp01(voice.speed/12)),dt);
            }
        }
        public void AttachTraffic(Transform car)
        {
            if(settings==null||phone!=null||car==null||traffic.Count>=16)return;
            foreach(var voice in traffic)if(voice.car==car)return;
            var source=Source(car,"Traffic engine audio",false,settings.vehicleOutput,220);source.spatialBlend=1;
            source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=3;source.maxDistance=30;
            traffic.Add(new TrafficVoice{car=car,source=source,previous=car.position});
        }
        public void PlayButton()
        {
            if(settings==null||!isActiveAndEnabled||Time.realtimeSinceStartup<nextClick)return;
            nextClick=Time.realtimeSinceStartup+.035f;Interface(settings.buttonClick);
        }
        private void Interface(AudioClip clip){Voice(uiVoices,ref uiVoice,clip,settings!=null?settings.interfaceVolume:0,false,Vector3.zero,1);}
        private void Effect(AudioClip clip,float gain=1){Voice(effectVoices,ref effectVoice,clip,settings!=null?settings.effectsVolume*gain:0,false,Vector3.zero,1);}
        private void Voice(AudioSource[] pool,ref int index,AudioClip clip,float volume,bool spatial,Vector3 position,float pitch)
        {
            if(clip==null||pool==null||pool.Length==0||!isActiveAndEnabled||suspended||volume<=0)return;
            var source=pool[index];index=(index+1)%pool.Length;if(source==null)return;
            source.Stop();source.spatialBlend=spatial?1:0;source.transform.position=position;source.minDistance=8;source.maxDistance=45;
            source.clip=clip;source.volume=Mathf.Clamp01(volume);source.pitch=pitch;source.loop=false;source.Play();
        }
        public void PlayImpact(Vector3 position,float speed)
        {
            if(settings==null||phone!=null||speed<settings.minimumImpactSpeed||Time.timeScale<=0||AudioListener.pause||Time.realtimeSinceStartup<nextImpact)return;
            nextImpact=Time.realtimeSinceStartup+.25f;
            Voice(effectVoices,ref effectVoice,settings.collision,settings.effectsVolume*Mathf.Lerp(.25f,1,Mathf.Clamp01(speed/18)),true,position,1);
        }
        private void MissionPhase(CoinMissionPhase phase)
        {
            if(settings==null)return;
            if(phase==CoinMissionPhase.Available&&Time.realtimeSinceStartup>=nextOffer){nextOffer=Time.realtimeSinceStartup+3;Effect(settings.missionAvailable,.7f);}
            if(phase==CoinMissionPhase.Collecting)Effect(settings.missionStart);
            if(phase==CoinMissionPhase.Complete)Effect(settings.missionComplete);
        }
        private void Coin()
        {
            if(settings==null||Time.realtimeSinceStartup<nextCoin)return;nextCoin=Time.realtimeSinceStartup+.055f;Effect(settings.coinCollect,.7f);
        }
        private void GarageAction(GarageActionResult result)
        {
            if(settings==null)return;
            // Shop feedback must still play while world audio is paused.
            Interface(result==GarageActionResult.Purchased?settings.vehiclePurchased:result==GarageActionResult.Selected?settings.vehicleSelected:settings.actionDenied);
        }
        private void OnApplicationPause(bool paused)
        {
            if(phone==null)return;suspended=paused;
            foreach(var source in sources)if(source!=null){if(paused)source.Pause();else source.UnPause();}
        }
        private void OnDisable(){Unsubscribe();foreach(var source in sources)if(source!=null)source.Stop();}
        private void OnDestroy()
        {
            Unsubscribe();if(impact!=null)Destroy(impact);
            foreach(var voice in traffic)if(voice.source!=null)Destroy(voice.source.gameObject);
            if(root!=null)Destroy(root.gameObject);
        }
    }
}
