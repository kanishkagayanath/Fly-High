# Fly High phone controller prototype

The PC runs the game. An Android Unity build runs only the phone controls. Both builds use GameScene; the platform selects the role automatically. No cloud service or extra networking package is required.

## Try it

1. Open Fly-High with Unity 6000.3.17f1. Allow script compilation and the automatic non-interactive TMP essentials import to finish.
2. Open Assets/Scenes/GameScene.unity and press Play. The prototype adds a test ground, an orange car, and a following camera at runtime. Existing scene assets are preserved.
3. In the Unity menu choose **Fly High > Build Android controller**. The APK is written to Builds/Android/FlyHighController.apk. Android build support, SDK/JDK, and a working Unity license are required.
4. Install that APK on the Android phone. Connect phone and PC to the same Wi-Fi. Avoid guest Wi-Fi that isolates devices.
5. In the updated phone app tap **Find PC**, then tap your computer to connect without typing. Manual fallback: enter the PC's Wi-Fi IPv4 address and six-digit pairing code displayed by the running PC game. When multiple addresses appear, choose the Wi-Fi adapter's address. Both phone and PC need the discovery update; see Docs/QuickPhonePairing.md.
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

## PC main menu — 2026-10-19

DesktopMainMenu is created by PrototypeBootstrap in the PC/Editor branch of GameScene and CityDrive. The Android controller interface is unchanged. Opening Play Mode starts at the menu; Play hides it and starts physics. Escape pauses and returns to the menu, whose Play button becomes Resume. Escape in Options returns to the main menu. The network receiver continues updating using realtime timestamps while Time.timeScale is zero. Menu teardown restores the prior time scale.

Options provides master volume in 10% steps and fullscreen toggling. Volume and fullscreen choices persist via PlayerPrefs; fullscreen applies only to a standalone PC build. Exit stops Play Mode in Editor or quits a standalone PC build. Mouse and EventSystem keyboard navigation are supported, with initial selection on Play. The UI uses existing uGUI, Input System UI and TMP infrastructure; no scene serialization, imported assets or package dependencies changed.

Validation: Editor and Android source paths compiled against installed Unity libraries, with only existing unused-branch field warnings. Runtime/visual verification remains pending. Verify initial menu blocks driving, Play starts driving, Escape pauses/resumes, Options Back returns, volume persists, and Exit stops Play Mode. Verify fullscreen and real application exit in a standalone PC build. No Android rebuild is needed for this feature.

## Pavement and obstacle contacts — 2026-10-19

Reported: the car catches on pavements/obstacles and can mount sidewalks. Static evidence: driving and lateral grip forces were still applied into contact walls, and MoveRotation did not check penetration. The car used a default-friction sharp box collider. These are plausible contributors; original stuck contact has not been captured in a connected Editor.

Added CarContactGuard on the player's rigidbody GameObject so it receives collision callbacks. A runtime zero-friction material reduces catching; ground grip remains controlled by the existing lateral-force code. Recent horizontal contact normals filter inward velocity and combined drive/grip/brake force while preserving tangential movement and movement away from walls. Candidate steering rotations are rejected if they increase overlap with colliders, using the car's box and nonalloc overlap query. Contact state clears on reset.

City startup creates invisible nonconvex mesh barriers above static MeshColliders whose hierarchy identifies Sidewalk/Pavement/Set_B_Tiles. Each barrier stretches the existing mesh vertically to 1.2 world units, preserving its X/Z footprint and bottom. Near-flat meshes and tilted surfaces are skipped. Imported scenes, source prefabs, materials, project collision layers and the Android controller are unchanged. This is an arcade road-boundary treatment; intentionally driveable sidewalk ramps would need explicit exclusion in future.

Validation: Editor/Android code paths compiled against installed Unity libraries. No runtime reproduction, contact capture or visual verification was available. Manual check: restart Play, drive slowly and quickly diagonally against a pavement, verify sliding along it and no climbing; drive head-on into a building, release throttle and reverse away; test steering near an obstacle, street corners, ordinary road seams, reset, pause and reconnect. A direct head-on impact should stop the car: use Reverse to back away. No automatic teleport/recovery is applied. No Android rebuild needed.

## Controlled arcade collision heading — 2026-10-19

Reported regression/remaining issue: obstacle and pavement contact rotates the player car. Source inspection confirmed the rigidbody froze only X/Z rotation, leaving Y available to off-centre contact impulses. Steering also used MoveRotation on the dynamic body. Runtime collision angular velocity was not captured because the Editor connection remains unavailable.

Targeted change in RacingPrototype: FreezeRotation now locks physics rotation on all axes. Steering still computes its speed-sensitive desired heading and uses CarContactGuard.CanTurn, but assigns the approved Rigidbody.rotation explicitly. Heading is owned by steering; collisions retain positional response, contact sliding and stopping. Existing pavement barriers, acceleration, braking, reverse steering and wheel visuals remain in place. This is a controlled arcade handling choice, not a recreation of a proprietary racing game's physics. No physics rotation from other forces will apply to this prototype; future jumps/rollovers would require a separate design.

Validation: Editor and Android source paths compiled against installed Unity libraries. No live Editor available for physical reproduction. Manual regression: with zero phone steering, hit a wall front-corner-first and verify heading stays steady; scrape a pavement at a shallow angle and verify the car slides without collision spin; steer clear at low/high speed, test reversing away, R reset and pause/resume. No Android rebuild required.

## Contact heading hold and diagnostics — 2026-10-19

The user reported rotation continuing after the all-axis rotation lock. The prior report did not establish whether raw steering was neutral. Further source tracing showed that the explicit steering rotation setter continued updating heading during wall contact whenever the overlap gate accepted a small turn. Physics constraints cannot block these scripted assignments. This is a confirmed code path, but it has not yet been confirmed as the complete cause of the user's observed spin.

CarContactGuard now owns the approved heading. ApplySteering accepts new headings only outside obstacle contact (with a 0.12-second grace after the last horizontal contact), subject to existing overlap checks. Contact callbacks and each driving step restore that approved heading and clear angular velocity. Reverse/forward translation away from a wall remains allowed; steering resumes after leaving contact. Ground-only contacts do not start a hold. Reset updates the approved heading and clears contact history.

The PC HUD now shows Contact guard v2: HEADING HELD / free steering plus raw steering input. This identifies the loaded controller revision and whether contact detection engages. Use this to distinguish remaining solver, collision-normal, held-input, or camera/visual issues. Source paths compiled for Editor and Android; original runtime behaviour is still awaiting user verification. No APK change required. If using a standalone PC game, rebuild PC city drive to include these changes.


## Hold-to-drive stop — 2026-10-19
The PC car now zeros horizontal velocity at the next physics step after neutral drive input arrives (absolute signed throttle below 0.01). Releasing either driving pedal stops coasting; vertical velocity is preserved for gravity. Steering speed is computed after the stop so neutral input cannot keep turning the car. The Android app already sends neutral on release; no APK rebuild needed. Existing forward/reverse acceleration, direction-change braking and collision heading hold are preserved. Editor/Android code paths compiled; manual check forward/reverse release at low/high speed, gravity, reconnect and obstacle contact in Play Mode.



## Smooth pedal-release stop — 2026-10-19
Replaced the abrupt neutral-input horizontal velocity reset with fixed-step Vector3.SmoothDamp toward rest (0.25-second smoothing parameter, not a fixed stop duration). Residual horizontal speed below 0.05 units/second settles to zero. Smoothing state clears on throttle/reverse and reset. Gravity, collision filtering and the brake button are preserved. Source compiled for Editor/Android; verify release from forward/reverse and immediate re-acceleration in Play Mode. No Android rebuild required.

