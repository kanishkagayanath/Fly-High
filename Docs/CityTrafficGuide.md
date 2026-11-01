# CityDrive NPC traffic

Updated 2026-10-25.

## Try it

Open CityDrive, enter Play mode, then choose Play in the game menu. Eight moving NPCs are distributed across the road network. Traffic follows longer routes between nine map districts, including the outer streets and central roads. The phone controller APK does not need rebuilding; rebuild the PC executable to distribute these changes.

## Movement and routing

- CityRoadGraph samples the static road colliders once. Only the approved asphalt material triangles are accepted; paving and kerb submeshes are excluded. Vehicle footprints and static obstacles validate the route.
- Destination selection cycles through nine districts. Weighted graph search favours continuing forwards and the right side of the road, with a penalty for sharp changes in direction. Route-node reservations guide planning; actual vehicle shapes determine safe movement, so adjacent lanes do not stop simply because they share a coarse nearest grid point.
- TrafficPath caches a polyline with rounded Bezier corners, arc distances and interpolated headings. Shortcutting preserves the selected lane instead of cutting across opposing traffic. Interpolated route poses are validated at 0.25-metre spacing before use.
- NPCs keep speed through intermediate waypoints. Curvature and upcoming obstacles set their speed, with gradual acceleration/braking. New routes start before the current one ends and preserve a forward lead-in.
- Kinematic Rigidbody movement uses MovePosition/MoveRotation in FixedUpdate with interpolation. Physics decisions use Rigidbody poses, rather than the delayed visible Transform. Wheel animation runs in LateUpdate with render-frame time.
- Obstacle checks follow the curved route ahead. Vehicles stop for blockers and resume once clear. Persistent conflicts trigger a new route; the yielding NPC can safely reverse along its path and retry, without teleporting or entering the pavement.
- NPCs pause with the main menu and resume with the game.

This is ambient prototype traffic. Right-side routing is a preference, not a complete authored lane/junction system. Traffic lights, formal intersection priority and overtaking are not implemented; vehicles can form temporary queues. Decorative parked demo cars remain separate from the moving NPC population.

## Settings

Select Assets/PhoneController/Resources/FlyHighTraffic.asset outside Play mode. Vehicle Count controls the moving population (default 8), Cruising Speed defaults to 7 world units/second with per-car variation, and Vehicles supplies the prefab models. Restart Play after changing settings.

## Files

- CityRoadGraph.cs: asphalt sampling, map districts, route search and static clearance.
- TrafficPath.cs: cached rounded routes, pose evaluation and curvature speed limits.
- NpcTrafficCar.cs: movement, sensing, recovery, reservations and wheel presentation.
- CityTrafficSystem.cs: map-wide spawn distribution and model/settings integration.
- TrafficReservations.cs: route-node ownership hints.

## Validation

Live Unity compilation passed. Android conditional source compilation passed with the existing PC-only PrototypeBootstrap field warnings.

A 165.7-game-second run with eight NPCs visited all 42 authored road tiles and all nine districts. Each vehicle travelled 748-932 metres and completed 2-5 district routes. At 672 fleet samples, full NPC footprints remained on asphalt (zero rejected poses). Temporary queues recovered; the longest stationary interval in that run was 12.3 game seconds. This is sampled validation of this scene and population, not a guarantee for arbitrary traffic counts or layouts.

A temporary obstacle test stopped an NPC with zero measured collision penetration; after removal it travelled another 11.62 metres over three seconds. Pause/resume produced zero NPC Rigidbody position drift. A 12-second URP render-pose sample collected 3,693 frames; the final movement path stayed interpolated with no direct per-frame Transform writes. No FPS improvement or comparative jitter percentage is claimed.

The current Unity Console contained zero errors. A Unity Account API connectivity/focus warning was unrelated to the traffic scripts. Temporary probes, obstacles and capture assets were removed after validation.

## Traffic jam recovery update — 2026-10-29

Reproduced a five-car junction queue in the original controller, including stationary intervals of 25-26 seconds. Its three-second blocked timer reset after every route retry, and its special reversing rule handled only head-on vehicles. Thus crossing queues could repeatedly select blocked routes without stronger recovery.

- TrafficReservations now also records each NPC's accepted future oriented body pose before PhysX applies MovePosition. Nearby vehicles check those volumes, preventing conflicting same-step moves. Node hints remain separate from the physical body claims.
- NPC liveness uses actual forward distance, so selecting another route does not reset the stall timer. Crossing conflicts can yield and safely reverse along the validated route. The next graph search can avoid the blocking vehicle's bounds.
- If a vehicle remains stalled for over 18 game seconds, it may relocate to a validated clear road point only when BOTH the old and new positions are outside the game camera (with a visibility margin) and over 35 metres from the player. This fallback never moves an on-screen vehicle. Routine traffic in the extended validation did not need it.
- Road footprint/collider checks, interpolated Rigidbody movement and menu pause remain mandatory. Cars still wait for a physically blocked lane. This remains ambient traffic rather than a full signal/right-of-way simulator.

Validation: the first updated 165.9-game-second run sampled all 42 road tiles and nine districts, with zero off-road poses and a maximum stationary interval of 4.9 seconds. A continued 250.25-game-second run recorded 3,232 fleet poses, zero off-asphalt footprints and zero measured car-to-car penetration; all eight drove another 1,262-1,340 metres. Maximum measured stop was 3.09 seconds, with 0-5 route/reversing recoveries per car and no automatic relocations. These are separate traffic runs, not a deterministic percentage performance comparison.

A parked kinematic blocker stopped a visible NPC with zero penetration; after removal it drove 24.51 m in six seconds. It did not overtake during the 16-second blockage. Direct fallback tests rejected a visible relocation, accepted an off-screen-to-off-screen clear-road relocation, and retained a valid footprint. 1,000 random pose comparisons included 382 actual overlapping box pairs with zero missed conflicts. Pending-pose, adjacent-lane and removal checks passed. Menu pause had zero Rigidbody drift. Live Editor and Windows/Android conditional source compiles passed, with existing opposite-platform Bootstrap warnings.

No new package, scene or controller APK is needed. Rebuild a standalone PC player before distributing the change.
