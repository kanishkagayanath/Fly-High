# Car speedometer

Added 2026-10-25 to the desktop Driving HUD.

- Bottom-right safe-area anchor, 220 x 220 reference UI units with 20-unit margins. Uses the existing Canvas scaling and matches the minimap's dark teal/mint styling.
- 0-200 km/h dial with a moving needle, large digital speed and D/R direction indicator. The numerical reading can exceed the dial maximum; the needle clamps to the end of the scale.
- Reads the player's Rigidbody horizontal velocity magnitude, converted from metres/second to km/h by multiplying by 3.6. Vertical motion is excluded and reverse speed stays positive.
- Needle and number share a displayed speed smoothed in LateUpdate with a 0.16-second SmoothDamp response. Low rest speeds settle to exact zero. Gear indication follows signed forward velocity, retaining the last direction at standstill.
- Geometry is created once using existing MinimapGraphic components. Runtime updates rotate the needle and only change text when the rounded speed/direction changes. No textures, cameras, input capture or new packages are required.
- The meter belongs to Driving HUD, so it hides and holds its value when the menu is open and returns on resume. It is created only in the desktop bootstrap branch; Android controller layout/protocol are unchanged.

Files: Assets/PhoneController/CarSpeedometer.cs and the one-line integration in PrototypeBootstrap.cs.

Validation: live Unity compilation passed and the Game-view capture verified placement/readability. Controlled Rigidbody checks settled at exactly 108 km/h for 30m/s forward, 54 km/h for 15m/s reverse with R, and zero for purely vertical velocity. Needle angle error was zero at each settled value, the upward transition was monotonic without overshoot, and all meter graphics had raycastTarget=false. Menu hide/resume and paused-value checks passed. Android conditional source compilation passed with existing PC-only field warnings. Current Console has no errors; the unrelated Unity Account API warning remains.

The temporary velocity probe and capture asset were removed after validation. The original player Rigidbody binding was restored. No phone APK rebuild is needed; rebuild the PC executable to distribute the HUD update.
