# Fly High phone controller prototype

The PC runs the game. An Android Unity build runs only the phone controls. Both builds use GameScene; the platform selects the role automatically. No cloud service or extra networking package is required.

## Try it

1. Open Fly-High with Unity 6000.3.17f1. Allow script compilation and the automatic non-interactive TMP essentials import to finish.
2. Open Assets/Scenes/GameScene.unity and press Play. The prototype adds a test ground, an orange car, and a following camera at runtime. Existing scene assets are preserved.
3. In the Unity menu choose **Fly High > Build Android controller**. The APK is written to Builds/Android/FlyHighController.apk. Android build support, SDK/JDK, and a working Unity license are required.
4. Install that APK on the Android phone. Connect phone and PC to the same Wi-Fi. Avoid guest Wi-Fi that isolates devices.
5. In the phone app enter the PC's Wi-Fi IPv4 address and six-digit pairing code displayed by the running PC game. When multiple addresses appear, choose the Wi-Fi adapter's address.
6. Tap Connect. Hold the phone landscape in a comfortable neutral position and tap Calibrate Tilt. Tilt left/right to steer; hold Accelerate or Brake. Both pedals support simultaneous touch.
7. If Windows asks about network access, allow the game/Editor on your private network. The receiver uses UDP port 47777. No router port forwarding is needed.

Choose **Fly High > Build PC prototype** for Builds/PC/FlyHigh.exe. The custom build commands select GameScene explicitly, avoiding the existing stale SampleScene build entry. They restore temporary player settings after each build. Platform switching may cause Unity to reimport assets.

Without a phone, use W/A/S/D while the PC receiver is disconnected. S brakes. The prototype is a simple driving test rather than a complete wheel-based racing simulation; it resets the car at the edge of the test area.

## Connection behaviour

Inputs are sent at 30 Hz. Steering is smoothed and calibrated relative to the current grip; approximately 35 degrees gives full steering. PC physics owns the car movement. The first valid phone endpoint owns the connection until it times out. A six-digit per-session code prevents accidental pairing; it is not encryption or internet-grade authentication.

The PC rejects malformed, non-finite, out-of-range and old input messages. No input for 0.5 seconds releases throttle and applies the brake. The phone shows Connected only after an acknowledgement arrives from the PC. Losing focus or pausing releases the pedals and stops sending until resumed. Socket handles are closed on disable. Restart PC Play Mode to rotate the pairing code.

## Validation and current limits

The scripts were compiled separately against this project's Unity libraries for Editor and Android paths. Packet parsing and sequence ordering were checked independently. Unity batch startup failed before project compilation with licensing IPC and system-initialization errors, so Unity runtime, visual layout, physical sensor direction, Wi-Fi latency, APK build, and real-device installation remain unverified. Open Unity normally through Hub with an active license, then test these steps on the phone.

Files live under Assets/PhoneController. Delete that feature folder to remove the prototype; no existing scene was rewritten. Unity generates runtime test objects afresh on each play session.

## CityDrive — added 2026-10-18

Open Assets/Scenes/CityDrive.unity and press Play. The PC now runs the receiver and test car in the imported city, without spawning the test ground or enforcing its square boundary. The existing Android controller works without rebuilding. Connect using the current PC pairing code. A clear road MeshCollider is selected as a provisional start; placement and camera behaviour require Play Mode verification.

To choose the start manually, stop Play, create an empty GameObject named CarStart, add the CityCarSpawn component, and position it about 0.8 metres above a clear road. Set its Y rotation toward the driving direction. Save the scene. Only one active spawn marker should exist. R on the PC resets the car to its starting point. City roads/buildings need colliders for physical contact; imported road prefabs inspected already contain MeshColliders. Existing imported assets were not modified.

The car remains the orange block with simple prototype physics. Detailed vehicle art and wheel physics are future work. Fly High > Build PC city drive builds CityDrive; the existing PC prototype build still uses GameScene. Both Editor and Android code paths compiled against installed Unity libraries; city runtime and visual checks remain pending in the user's Editor.

## Wheel animation and handling — 2026-10-18

Car_13's four named wheel transforms are cached once from the runtime visual instance. All wheels roll from signed forward rigidbody speed and measured wheel radius; front wheels also steer. Original import rotations are preserved. This is visual animation, not WheelCollider suspension. Imported vehicle assets are unchanged.

The controller smooths steering at 3 input units/second. Full-input yaw rate decreases from 70 to 25 degrees/second as speed approaches the existing 55-unit/second speed limit. Lateral grip increases from 10 to 14 per second with speed; acceleration remains 28. Visual front-wheel steering decreases from 28 to 8 degrees. R resets the car, steering and wheel poses; the reset key is sampled in Update so render/physics timing cannot miss it.

Validation: Editor and Android preprocessor paths compiled against installed Unity libraries. Visual direction and road handling require in-Editor/device verification. Test straight acceleration, release/brake, left/right turns at low/high speed, stopping wheels at rest, and R reset. Existing phone APK remains compatible.

## Reverse control — 2026-10-18

The updated Android UI has hold-to-use Reverse, Brake and Accelerate pedals. Rebuild via Fly High > Build Android controller and install Builds/Android/FlyHighController.apk over the old controller app. Restart PC Play mode after compilation. For a standalone PC game, rebuild it too.

Reverse is signed throttle (-1) in the existing packet format. Forward-only old APKs remain usable with the updated PC, but the new reverse APK requires the updated PC receiver. Forward and reverse together cancel drive input; Brake overrides drive. Opposite-direction input brakes first until near standstill. Reverse has gentler acceleration and a 12-unit/second limit; forward keeps 28 acceleration / 55 limit. Focus loss, pause, disconnect and receiver timeout release drive input. The PC Down Arrow can test reverse while no phone is connected; S remains brake.

Validation: Editor and Android source paths compiled against installed Unity libraries; forward/neutral/reverse packet round-trips and malformed-input rejection passed. No new APK was produced from this chat: the live Editor remains unreachable. Device UI, reverse steering/wheel animation and driving need a Play Mode/phone test after rebuilding.
