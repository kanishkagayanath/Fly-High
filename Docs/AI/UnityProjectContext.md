# Fly-High project context

Analyzed 2026-10-17 from local files. Root: C:/Users/Kanishka/Documents/UNITY-DEV/Fly-High.

- Confirmed Unity 6000.3.17f1, URP 17.3.0, Input System 1.19.0 with activeInputHandler 1; uGUI 2.0.0, Test Framework 1.6.0.
- Original first-party scene: Assets/Scenes/GameScene.unity, containing camera and directional light. Existing build settings refer to disabled SampleScene; custom prototype builds explicitly select GameScene.
- Original scripts were tutorial readme/editor scripts. No existing car framework, runtime networking or custom test assemblies were found.
- Added Assets/PhoneController: UDP input protocol, receiver, Android sender, uGUI/TMP runtime controller, simple rigidbody test car, and editor build menu. No new package dependencies. Default Assembly-CSharp / editor assembly boundaries.
- Startup: PrototypeBootstrap starts only for GameScene. Android player builds get phone UI; Editor and PC get receiver plus driving demo. PC owns physics, phone supplies input.
- Existing repository has user changes/untracked content; preserve it. No serialized scene assets were edited for this implementation.
- Android and Windows build modules installed. No connected Unity Pipeline Editor detected. Unity batch startup failed with licensing IPC/system initialization errors; full runtime/build validation remains pending.
- See Docs/PhoneControllerPrototype.md for setup and validation limits.
- 2026-10-18: CityDrive scene uses imported ithappy Cartoon_City_Free city. Bootstrap supports CityDrive, reuses road collisions, selects a provisional road start or optional CityCarSpawn, removes test ground/boundary in city mode, adds PC city build option. Code compiled; runtime placement pending.
- 2026-10-18: CarWheelVisuals adds procedural four-wheel spin and front-wheel steering to Car_13; RacingPrototype smooths steering, reduces high-speed yaw, increases lateral grip. Compilation checked; visual/runtime verification pending.
- 2026-10-18: Reverse pedal uses signed throttle; PC decoder accepts [-1,1], car brakes before direction changes and limits reverse to 12. Android APK rebuild required. Source compilation and packet checks passed; device test pending.
