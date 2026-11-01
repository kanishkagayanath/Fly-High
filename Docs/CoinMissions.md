# Coin missions

CityDrive now starts three road-only coin missions automatically through PrototypeBootstrap. No scene wiring or phone pairing changes are needed.

## Playing

- Gold **M** markers appear on the minimap, including clamped markers at its edge for distant starts. World-space rings and floating M letters identify each starting point.
- Approach within six metres and slow below 3 m/s. The top-centre mission card and the phone's gold **START MISSION** button become available. The PC button or **E** also starts the same mission.
- Follow the gold minimap route and floating, spinning coins centred in the driving lane. Each pickup gives one coin. All coins must be collected before driving into the three floating coin sacks at the finish, which award 25 bonus coins.
- The top-right wallet sits above the minimap. The mission card shows collection progress and instructions. The minimap highlights the nearest remaining coin, so a missed coin can be recovered.
- Runs have no timer. They can be repeated after completion; leaving the previous starting point clears its replay lock. Escape pauses the mission, pickups, coin animation and driving along with the existing menu.
- Wallet balance is stored locally as PlayerPrefs `FlyHigh.Coins`, saved on completion, disable and application pause. The active run resets on a new session. The phone only displays desktop state and cannot award coins.

## Phone update

Install the newly built `Builds/Android/FlyHighController.apk` over the existing controller. Manual pairing and Find PC still work. The mission strip occupies the gap under the controller heading, above the IP/code fields, without covering the pedals. It hides when the phone disconnects, the PC pauses, or no mission interaction is available.

The FH1 driving packet, ACK, discovery protocol and UDP port 47777 are unchanged. Optional `MS` status messages run at 5 Hz, with revisions and a current offer token. `START` requests retry for 1.5 seconds and are accepted only from the currently paired endpoint, with the current code/token, while near the marker, sufficiently slow and unpaused. Duplicate and stale starts cannot restart a run. Older controller APKs still drive; they simply lack the mission button.

## Assets and authoring

- `CoinMissionSettings` / Resources/FlyHighMissions.asset references the imported Assets/Assets/Coin.fbx and Assets/Assets/Coin_sack/goldbag.fbx, owned mission materials, and cached routes.
- Original imported files and materials remain untouched. Mission materials add a soft gold emission so pickups remain readable at night. Coins are normalised to 0.92 m, with their centres about 0.85 m above the asphalt and an 0.08 m bob. The finish uses three differently sized sacks.
- `CoinMissionSystem` owns offers, collection and wallet. FixedUpdate uses horizontal swept pickup checks to prevent high-speed misses, and rejects long teleport sweeps. Completion requires every pickup and reaching the finish.
- `CoinMissionPresentation` owns the world visuals and top-centre/top-right HUD. `MissionMapOverlay` adds cached route geometry and coin markers to the existing circular map; no extra camera or RenderTexture is used.
- `MissionPhoneProtocol`, the receiver/sender additions and `PhoneMissionPanel` provide the optional paired phone interaction.
- **Fly High > City > Bake coin missions** rebuilds routes from the current CityDrive road graph in Edit mode. It reuses asphalt-only footprint checks, static clearance and rounded TrafficPath paths. CoinLanePlacement then samples asphalt edges and the actual road atlas lane markings to centre the route in its nearest driving lane. It blends neighbouring lane positions through open junctions and keeps the authored start/finish points. Re-bake after road geometry changes; it may select new roads and endpoints. No imported asset or scene modifications occur.
- Resources/MissionGold.mat, MissionCoin.mat and MissionSack.mat retain material/shader build dependencies. Runtime models, rings, text, overlays and the marker material clone are removed on teardown.

## Validation

Current saved routes are 199.79 m / 27 coins, 205.57 m / 28 coins, and 209.33 m / 28 coins after lane alignment. All 612 sampled route poses passed asphalt/car footprint checks. Live controlled route replay collected all 83 coins and awarded three 25-coin bonuses; desktop and phone totals both reached 158. The real phone-panel button over local UDP and the PC button both started missions. Start guards, incomplete finishes, pause, swept high-speed pickup, duplicate pickup and disconnect/reconnect were exercised. Five protocol assertions and 5,000 malformed messages passed.

The Android APK built successfully in 429.9 seconds with zero errors. It is 54,725,711 bytes, ARM64 IL2CPP, Android API 25 minimum / 36 target, package com.flyhigh.phonecontroller version 0.1.0 (code 1). Its signature verifies and matches the previous APK. The 973 build warnings come from existing inference shaders, tooling configuration notices and the imported character pack's obsolete API usage. No mission compilation errors remain.

After the build, a fresh CityDrive session confirmed one system, three starts, correct menu pause and preserved mission material emission. Stopping removed all owned runtime objects and freed the controller port. Test wallet/time preferences were restored. The Editor is left in Edit mode with CityDrive clean, Android target unchanged, and no changes to scenes, packages or Project Settings.

Validation outputs and previews are in this Codex chat's outputs folder. No Android device was connected, so installation, physical Wi-Fi behavior and touch layout on the user's phone still need a device check. No standalone Windows player was built for this feature.



The vehicle-shop update routes all mission currency through the shared VehicleGarage. The versioned FlyHigh.Garage.v1 snapshot now stores coins/owned vehicles/selection together; existing FlyHigh.Coins is migrated and mirrored. Mission pickup and finish rewards use that shared balance, so buying a vehicle during a paused run cannot later be overwritten by a stale mission wallet. See VehicleShop.md.
