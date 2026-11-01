# City pedestrians

CityDrive now has a desktop pedestrian system using all ten imported Hodaart Casual Character Collection 3 prefabs. Default population is 20 city walkers and 12 beach walkers. Each uses the pack's walking/idle controller, varied speeds and gait choices, and short idle pauses between destinations. Native NavMeshAgent acceleration, avoidance and smooth heading changes handle movement.

## Pavement and beach routes

The baked navigation includes approved Tile/Color pavement faces from the widened city tiles and road kerbs, and the upper dry-sand strip on the island shore. Asphalt, parking asphalt, grass beds, buildings, props, rooftops and submerged beach sections are excluded. Static render geometry blocks obstacles even when props lack colliders; conservative building blockers prevent interior spawns.

There are no road-crossing/off-mesh links. Four usable disconnected walking regions are populated separately. Tiny isolated fragments are excluded from spawn/destination selection. Walkers choose complete paths within their own region; they cannot use a road to reach another block. A shared runtime surface whitelist also checks walker positions. The NavMesh has 0.32m pedestrian-radius clearance, and agents use the same radius and a 1.8m height.

## Configuration and rebuilding

- `Assets/PhoneController/Resources/FlyHighPedestrians.asset`: prefab references, city/beach counts, speed range, visual scale and baked destinations.
- `Assets/PhoneController/Pedestrians/City pedestrian routes.asset`: baked NavMeshData.
- `Fly High > City > Bake pedestrian routes`: rebuild after changing pavements, roads, buildings or beach geometry. Run in Edit mode with CityDrive open. The tool updates existing assets without saving or modifying the scene, source models or imported animation controllers.

PrototypeBootstrap creates CityPedestrianSystem only in the desktop CityDrive branch. It registers the owned NavMesh data, distributes walkers, then removes actors/data when the runtime system is destroyed. The Android controller, vehicle driving, vehicle traffic and network protocol are unchanged. A new phone APK is not required; rebuild the PC game to distribute the feature.

## Validation — 2026-10-27

- AI Navigation 2.0.12 was already installed. Baseline contained no NavMesh surfaces, agents, obstacles or links. Humanoid agent type 0 is used with per-bake/per-agent size overrides; global navigation settings were not modified.
- Live Editor compilation passed. Windows and Android conditional source compilations passed with existing opposite-platform bootstrap-field warnings only.
- Route bake produced 2,390 destinations (1,393 on beach) in four usable regions. All destinations passed the approved-ground check. 120 complete native paths and 23,841 path-position samples had zero asphalt hits.
- Startup created 32 agents on the NavMesh: 20 city and 12 beach, with beach spawns present on every side and all four walking regions populated.
- The first live movement attempt revealed a NavMeshPath allocation in a MonoBehaviour field initializer; this was corrected by creating the path during explicit Initialize. That failure triggered Editor Error Pause. A subsequent restart was needed to release a controller UDP handle held by the Editor after stopping the failed session. The saved CityDrive scene remained clean.

After the user reopened Unity, the corrected initialization passed live startup with zero Console errors. A 45.0-second run sampled 5,664 walker poses: zero invalid ground samples, zero road-safety corrections, and all 32 actors observed walking. Each travelled 45.70–59.47m during that run. Both imported walking states were verified active, and city-pavement/beach animation previews were visually inspected.

Menu pause produced zero position drift and zero animation-time drift across all walkers. After resume the fleet travelled 37.54m in the test interval; eight NPC vehicles remained active. Stopping Play removed all 32 walkers, all NavMeshAgents and the registered navigation data (zero triangulation vertices remaining); CityDrive remained clean and UDP 47777 was free. Temporary observer-camera/HUD changes and capture assets were removed. No physical-device test, standalone PC build or new phone APK was needed or run for this desktop feature.

