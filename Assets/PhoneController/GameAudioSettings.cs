using UnityEngine;
using UnityEngine.Audio;

namespace FlyHigh.PhoneController
{
    [CreateAssetMenu(menuName="Fly High/Audio settings",fileName="FlyHighAudio")]
    public sealed class GameAudioSettings : ScriptableObject
    {
        [Header("Music — add MP3s to each playlist")]
        [Tooltip("Plays on the PC Home, Shop and Options screens. Null entries are skipped.")]
        public AudioClip[] menuMusic = new AudioClip[0];
        [Tooltip("Plays while driving. Pausing lowers its volume; returning Home fades to menu music.")]
        public AudioClip[] drivingMusic = new AudioClip[0];
        [Range(0,1)] public float musicVolume=.4f;
        [Range(0,1)] public float drivingMusicMultiplier=1;
        [Min(.05f)] public float musicFadeSeconds=1.2f;
        [Tooltip("Play quieter menu music on the controller home screen until it pairs with a PC.")]
        public bool phoneHomeMusic;
        [Range(0,1)] public float pausedMusicMultiplier=.3f;

        [Header("Interface — PC and phone")]
        public AudioClip buttonClick;
        public AudioClip connected;
        public AudioClip disconnected;
        [Range(0,1)] public float interfaceVolume=.65f;
        [Range(0,1)] public float controllerInterfaceMultiplier=1;

        [Header("Player vehicle — use short seamless loops")]
        public AudioClip engineStart;
        public AudioClip engineIdleLoop;
        public AudioClip engineDrivingLoop;
        [Tooltip("Loop used for hard braking and fast, sharp turns.")]
        public AudioClip tyreSkidLoop;
        [Tooltip("One impact sound, throttled so resting on a wall never repeats it.")]
        public AudioClip collision;
        public AudioClip reverseLoop;
        [Range(0,1)] public float engineVolume=.55f;
        [Range(.25f,3)] public float lowEnginePitch=.8f;
        [Range(.25f,3)] public float highEnginePitch=2.1f;
        [Min(1)] public float skidMinimumSpeed=7;
        [Min(.1f)] public float minimumImpactSpeed=1.5f;

        [Header("City — seamless ambience loops")]
        public AudioClip cityDayLoop;
        public AudioClip cityNightLoop;
        public AudioClip beachWavesLoop;
        public AudioClip trafficEngineLoop;
        [Range(0,1)] public float ambienceVolume=.25f;
        [Range(0,1)] public float cityNightVolumeMultiplier=1;
        [Range(0,1)] public float trafficVolume=.16f;
        [Tooltip("Island bounds used to fade the sea in near every city edge. Refresh using Fly High > Audio > Read island bounds.")]
        public Vector2 islandCentre=Vector2.zero;
        public Vector2 islandHalfSize=new Vector2(69,84);
        [Min(1)] public float beachFadeDistance=25;

        [Header("Missions and rewards")]
        public AudioClip missionAvailable;
        public AudioClip missionStart;
        public AudioClip coinCollect;
        public AudioClip missionComplete;
        public AudioClip vehiclePurchased;
        public AudioClip vehicleSelected;
        public AudioClip actionDenied;
        [Range(0,1)] public float effectsVolume=.7f;

        [Header("Optional mixer routing — leave empty to use existing master volume")]
        public AudioMixerGroup musicOutput;
        public AudioMixerGroup interfaceOutput;
        public AudioMixerGroup vehicleOutput;
        public AudioMixerGroup ambienceOutput;
        public AudioMixerGroup effectsOutput;
    }
}
