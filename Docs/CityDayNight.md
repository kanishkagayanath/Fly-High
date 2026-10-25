# City time and vehicle lighting

CityDrive now starts at 09:00 with an automatic 12-minute full-day cycle. Morning, daytime, warm evening and cool night change continuously: sun, procedural sky, moon/stars, ambient light, fog, ocean tint and bloom blend together. The clock pauses with the main menu. Visual transitions continue while Options is open so a selected appearance can be previewed.

Press Esc, open Options, and click **Time of day** to cycle through Automatic, Morning, Evening and Night. The choice is remembered. Fixed choices blend over approximately five seconds; Automatic resumes its session clock. A new Automatic session starts at 09:00.

## Lighting

- All 19 existing city lamp fittings gain warm local lights and glowing lenses that fade in toward night. Traffic-signal lenses retain their existing red/amber/green atlas colors with stronger night glow. This feature does not add signal timing or traffic-rule logic.
- Building glass gains a subtle night glow. Fog and ocean colors follow the sky, with lower distant water-ripple brightness to avoid a distracting bright horizon pattern at night.
- The player and eight current NPC vehicles receive paired headlights, red rear lamps and brighter brake lighting. Vans without a dedicated headlamp material receive small front lens fittings. Brake glow follows deceleration; the player's brake input also lights the rear lamps.
- The existing Forward+ renderer supports the scene's additional lights. New local lights have no shadows; there is one shared atmosphere Volume and no additional cameras.

## Authoring

`Assets/PhoneController/Resources/FlyHighLighting.asset` stores cycle length, initial hour, transition duration and persistent material/profile references. Its references under `Assets/PhoneController/Lighting` preserve the sky shader, emissive window material and URP post-processing overrides for builds.

`CityDayNight` is created only by the desktop CityDrive bootstrap. It discovers the existing lamp/signal/building material slots once, then attaches `VehicleLights` after the traffic models spawn. `DesktopMainMenu` exposes the time choice. Imported source materials remain unchanged: runtime clones, light objects, fallback lens meshes and the Volume profile are owned and cleaned up. Camera post-processing and scene lighting settings are restored on destruction. Android controller UI and the Wi-Fi protocol are unchanged.

## Validation — 2026-10-25

- Live Unity compilation passed with no current Console errors. City Sky and Coastal Water shaders returned no compiler messages.
- Isolated six-second appearance transitions were smooth and monotonic. Morning settled to zero night light; evening to 0.22; night to 1.0. All 19 lamps and 18 headlights across nine vehicles were enabled for evening/night and disabled in morning.
- Actual Options-button clicks cycled through all four modes with the correct labels. Game-view captures verified the night road/headlight/rear-light appearance and Options layout.
- The clock stayed fixed while paused. After resuming near midnight it wrapped correctly: 0.0234 hours, within 0.000002 hours of the expected value.
- Runtime Volume profile and all three override components were distinct from their source assets. Camera post-processing was enabled during play.
- Fresh Play-mode restart created exactly one time controller, one atmosphere, 19 lamps and nine vehicle-light components. Stop restored the original sky, ambient mode, sun intensity, fog, reflection intensity and camera post-processing. No owned lighting components, lamp objects or city-light material clones remained; controller UDP port was free. Automatic was restored as the saved mode. Final current Console had zero errors and zero warnings.
- Android conditional source compilation passed; only existing warnings for unused desktop bootstrap fields remained. No phone APK rebuild is needed for this feature.

No standalone PC build or comparative frame-rate benchmark was run. Rebuild the PC game before distributing these changes.
