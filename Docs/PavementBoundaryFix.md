# Player road and pavement boundary fix

2026-10-25, CityDrive / PC player.

The previous runtime protection only stretched named sidewalk meshes vertically. It skipped 11 zero-height paving meshes and did not cover the non-asphalt kerb/pavement submeshes within the 42 road tiles. A low original kerb therefore remained the only obstacle in those areas.

CarContactGuard now builds invisible closed collision prisms from the upward-facing triangles of all 38 paving meshes and the non-asphalt submeshes of all 42 road tiles. The prisms extend from 0.5 metres below each surface to 1.6 metres above it, preserving the real curved and concave outlines. Entire paving tiles are excluded, including their decorative parking/grass areas. Asphalt within actual road tiles stays open. No visible city meshes or scene assets are changed.

The player guard owns the generated meshes and barrier objects and releases them on destruction. Repeated generation skips existing barriers. Barriers use the car's zero-friction sliding material; the existing impact heading hold, continuous collision detection, smooth camera and pedal release remain in use.

Validation:
- Live Unity script compilation passed.
- All 635 sampled exposed asphalt-to-pavement transitions were blocked by a static obstacle at car height. These are samples, not a proof covering every geometric point.
- Eight controlled Play-mode physics scenarios covered inner/outer straight kerbs, 55 m/s impacts, a curved kerb, a junction and reverse. No impact heading change occurred. Straight impacts showed zero measured penetration; the curved contact showed up to 0.017 m transient solver penetration. The first curved test started overlapped and was replaced by a verified clear start.
- Reversing/moving away from a stopped kerb contact cleared 4.80 metres.
- NPC traffic still spawned eight vehicles; all 513 route nodes belong to one connected graph with 1,375 links. Two previous edge-clearance nodes are now excluded by the more complete barriers.
- Minimap tracking remained correct. Current Console has no errors; the existing Unity Account API network/focus warning was recorded separately.

Restart Play mode to generate the barriers. The Android phone APK does not need rebuilding; rebuild the PC executable to distribute this fix.

Final restart check: compilation completed, all 635 sampled boundaries remained blocked, repeated generation kept exactly 80 barriers, and the current Console had zero errors/warnings. After stopping Play, zero generated barriers/meshes remained and UDP 47777 was free.
