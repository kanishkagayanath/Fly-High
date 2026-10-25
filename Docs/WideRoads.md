# Wider city roads

Saved in `Assets/Scenes/CityDrive.unity` on 2026-10-25.

Street corridors are now 1.8 times their original width. A representative straight asphalt section measures approximately 13.1 m, compared with roughly 7.3 m before. Junctions and curved road tiles follow the same continuous coordinate mapping. The original six buildings, street props and vehicles retain their dimensions; blocks and furniture move outward to keep space around the streets.

Eighty road/pavement mesh copies and matching MeshCollider references are stored in `Assets/WideRoadsGenerated`. Source package models and prefabs are unchanged. UVs, vertex colours, material submeshes and normals are preserved or transformed appropriately. The four-sided island beach was rebuilt around the enlarged 138 by 168 m city footprint.

Authoring tools:
- `Assets/PhoneController/Editor/CityRoadWidening.cs`: `Fly High > Widen City Roads`; a scene marker prevents accidentally widening the layout twice. It targets the original island layout and guards against rotated/scaled roots or separately authored expansion districts.
- `Assets/PhoneController/Editor/CoastalBeachBuilder.cs`: added an explicit beach rebuild entry point for layout changes.

Validation:
- Live Unity compilation passed; Console reported zero errors.
- All six building bounds retained their original dimensions.
- 315 straight-road positions passed continuous asphalt checks across tile joins, including a 4 m wide centre corridor.
- 100 car-footprint checks passed around all four outer bends.
- Traffic sampling found 515 nodes and 1,390 links in one connected network.
- Beach ground was checked outside all four enlarged city edges.
- A Play-mode acceleration/release-braking test travelled 9.18 m on a widened road; all 204 footprint samples stayed on asphalt.
- Repeated authoring did not duplicate or enlarge the layout again.
- Returned to Edit mode and saved the scene.

Pre-change live scene and beach-builder backup:
`C:/Users/Kanishka/Documents/Codex/2026-10-15/i/work/road-widening-backup/`

Preview: `WideRoads-preview.png`.

No Android controller APK rebuild is needed. Rebuild the PC executable to include the wider roads in a standalone game.
