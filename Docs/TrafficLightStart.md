# Fixed traffic-light car start

2026-10-25.

CityDrive now contains one saved CityCarSpawn marker named "Player start - traffic lights". It is positioned at (10, 0.7, 79.5), yaw 270 degrees, on the north road's westbound approach to the traffic-light junction. This approach is near the overhead traffic_light_002 at approximately (4.92, 0, 84). The car starts before the signal, facing into the junction and clear of the pavement.

Previously no marker existed, so RacingPrototype selected the first clear road returned by an unordered collider search. The existing marker path now supplies the fixed start position/heading; its existing ResetCar method returns to the same point. No movement or networking logic was changed. The marker's cyan editor gizmo is hidden in Play mode.

Validation: twelve nearby candidate poses passed asphalt-footprint/static-clearance checks with the complete pavement barriers active. Two fresh Play sessions started exactly at the saved marker with zero position/heading error and exactly one spawn marker. Moving/turning the runtime car and calling its existing reset returned exactly to the marker with zero velocity. The game-view preview verified the traffic lights ahead and no visible marker wireframe. After settling, the car retained the marker's X/Z and heading and remained on asphalt. Live source compilation passed and the current Console had zero errors.

Scene save comparison contained only additions for the new marker object/component/transform/root entry (45 extra lines, no removed lines). Backup: Codex chat workspace work/traffic-light-spawn-backup. To move the start later, select "Player start - traffic lights" outside Play mode, move/rotate it over a clear road, and save CityDrive.

No phone APK rebuild is needed. Rebuild the desktop game to distribute the new scene start.
