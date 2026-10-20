# CityDrive NPC traffic

The PC city now includes a first version of automatic traffic using Car_06, Car_16, Car_19 and Van from the imported city package.

## Try it

1. Stop Play mode and allow Unity to finish compiling/importing.
2. Open CityDrive, press Play, then click Play in the game's menu.
3. Look for **NPC traffic: N vehicles** on the PC display. Up to eight vehicles are placed on clear road space near the player's starting area, with spacing from the player and each other.
4. Drive toward a traffic vehicle and check that it slows/stops. Move away and check that it continues.
5. Open the main menu with Escape: traffic should pause with the game.

The Android controller does not need rebuilding.

## Adjust traffic

After Unity imports the new scripts, the editor creates `Assets/PhoneController/Resources/FlyHighTraffic.asset`. Select it in the Project window to change Vehicle Count, Cruising Speed or vehicle prefabs. Make changes outside Play mode, then restart Play mode. Default count is 8; default speed is 7 world units/second, varied slightly per vehicle.

## Behaviour and limits

- Road routes are sampled once from the imported static road MeshColliders. Routes avoid the sampled pavement barriers and blocked space.
- Cars favour continuing straight, sometimes choose another connected road direction, and can turn around at route ends.
- Vehicles reserve road points and check obstacles ahead. They wait when blocked, rather than forcing through the player or teleporting away.
- Wheel animation supports the city package's named front/rear wheel objects.
- This is an initial ambient traffic system. It does not yet have authored traffic lanes, traffic lights, overtaking, or full junction priority rules. Congested or narrow routes can form queues.
- Existing parked demo vehicles remain in place; the system adds moving traffic vehicles.

## Validation

Editor and Android source paths compiled against the project's installed Unity libraries. Reservation checks passed for occupied-node exclusion, opposing-swap prevention, ownership-safe release, route handoff, cleanup and invalid requests. Live route generation, driving, visual placement and performance still need a Play mode check in the user's Editor.

If the display shows zero vehicles or reports missing traffic assets, inspect the Console's `[City traffic]` message. The system skips unsafe/unavailable spawn locations rather than spawning cars in buildings or on pavements.

## Asphalt-only NPC correction — 2026-10-19

Reported: moving NPCs spawn/drive on the grey pavements. Original CityRoadGraph accepted any triangle of a collider with road in its name and required road support only at the centre. Inspection of all seven imported road FBX files confirmed separate Color and Roads polygon material slots, so mesh-name filtering included non-asphalt geometry.

CityRoadGraph now caches approved triangle ranges per collider from Mesh.GetSubMesh metadata and matching renderer material slots. Only Roads and Asphalt_Dark_Gray materials are accepted; Color, Tile_1 and unknown materials are excluded. This does not read non-readable vertex buffers or modify model import settings. Ground checks consider the closest static surface, rejecting an upper pavement even when a road exists below.

Spawn nodes and route edges require centre/corners/edge-midpoint support on asphalt, with a curb margin. Each proposed NPC movement and turn repeats this footprint check using the model's normalized width. Invalid moves stop, and invalid spawns release reservations and are discarded. The PC status now says NPC traffic: N vehicles (asphalt only). Restart Play mode to rebuild routes and remove the old runtime NPC instances. Existing static demo cars remain unchanged.

Validation: Editor/Android source paths compiled. Plain-C# road-mask checks passed for asphalt ranges, rejected pavement ranges, boundary indices, allowed material names and unknown-material rejection. Raw FBX inspection confirmed the two material groups across all seven imported roads. Live Unity route/visual verification remains pending; observe corners, curb edges, spawn positions and turning after restarting Play. The stricter mask can reduce traffic count where no full vehicle footprint fits. No Android rebuild required.
