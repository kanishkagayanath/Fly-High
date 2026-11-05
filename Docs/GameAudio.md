# Fly-High audio

Open **Fly High > Audio > Open audio settings** in Unity. This selects `Assets/PhoneController/Resources/FlyHighAudio.asset` in the Inspector. Assign imported AudioClips outside Play mode; the runtime loads this asset automatically. There are no scene or prefab assignments to maintain. Empty fields and empty playlists are safe and silent.

| Inspector group | Assign |
| --- | --- |
| Music | Add MP3s to Menu Music and Driving Music arrays. Tracks play in order, skip empty entries and repeat the playlist. Menu/driving music fades on transitions. |
| Interface | Button Click, Connected and Disconnected. Clicks work on both PC and phone, including keyboard submit and pedal touch-down. |
| Player vehicle | Engine Start (one shot); Engine Idle Loop and Engine Driving Loop (seamless loops); Tyre Skid Loop, Collision (one shot), Reverse Loop. If only one engine loop is assigned it covers the whole speed range. |
| City | City Day Loop, City Night Loop, Beach Waves Loop and Traffic Engine Loop. Day/night and beach proximity blend the ambience. NPC engines are spatial and fade with distance. |
| Missions and rewards | Mission Available, Mission Start, Coin Collect, Mission Complete, Vehicle Purchased, Vehicle Selected, Action Denied. |

Music, interface, engine, ambience, traffic and effects have separate Inspector gain controls. **Master Volume** in the existing PC/phone settings scales every channel, including button clicks; zero mutes everything. Do not normalize very loud MP3s by setting every gain to maximum.

The PC plays world/music/reward audio. The phone plays its own interface and connection feedback so its buttons respond immediately. Phone Home Music is optional and off by default to avoid competing music from two devices. Once connected, phone feedback follows the PC's master volume. Connection and button feedback remain available during pause, while world sources pause. Driving music lowers during pause; Home fades to menu music.

The optional mixer routing fields accept existing AudioMixerGroups if you later create a mixer. Leaving these empty uses the normal listener/master volume; no mixer package or global audio-setting changes are required.

## Import and build

1. Import the MP3/WAV files anywhere inside Assets (for example `Assets/Audio`).
2. Assign the AudioClips to `FlyHighAudio` and save the asset outside Play mode.
3. For long music and ambience clips, use Streaming, compression and Load In Background in each clip's Audio Import Settings. For short clicks/rewards, Decompress On Load is appropriate; use short trimmed files. Use mono audio for NPC engines and collision sounds. Loop files should have matching starts/ends; WAV/OGG often provide cleaner loop boundaries than padded MP3s.
4. Enter Play mode and tune the category gains and engine pitch range. Save your desired gains outside Play mode.
5. Rebuild **Fly High > Build Android controller** after assigning/changing clips and install the new APK. Rebuild the PC city player too if using an exported PC game. Unity Play mode reads assignments directly. Previously exported builds do not acquire new audio automatically.

The original files are never replaced, synthesized or automatically assigned. No audio assets were supplied for this change, so the delivered settings have empty clip slots. The existing APK remains the prior build until you assign your sounds and rebuild.

The sea uses the current island bounds, initially read from the ocean material. After resizing the island, open CityDrive and use **Fly High > Audio > Read island bounds** to refresh them. This edits only the audio settings asset.

## Integration

`GameAudio` is owned by the existing bootstrap (desktop) or phone controller view (Android). It subscribes to mission/garage outcomes, reads the actual car speed/braking state, and attaches bounded spatial traffic voices at spawn. `VehicleImpactSound` filters out road-support contacts and uses a cooldown. `ButtonSound` is attached by the five existing button factories and releases listeners on disable. No global sound singleton or UI layout changes are needed.

One-shot effects use fixed voice pools; rapid coins/clicks/impacts have short retrigger limits. Scene teardown releases owned sources and event subscriptions. Unassigned clips never block gameplay.
