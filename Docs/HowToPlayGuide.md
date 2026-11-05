# First-time How to Play guide

The PC game and Android controller now each show a five-page guide on first launch:

1. **Setup:** the PC displays the city and the Android phone is the controller.
2. **Connection:** both devices use the same Wi-Fi; phone Play opens the existing PC picker. The PC guide displays the current local addresses and pairing code for manual connection.
3. **Calibration:** hold the phone in landscape, calibrate from the driving controls, then tilt gently to steer.
4. **Driving:** hold Accelerate / Reverse / Brake; release the pedal for smooth stopping; Pause offers Resume, Settings and Home.
5. **Missions:** approach an M marker, slow down, start from the phone, follow the gold minimap route, collect coins and reach the finish sacks; spend rewards in Shop.

**Skip** dismisses the guide at any page. **Back / Next / Got It** navigate it. A **How to Play** button at the top right of each Home screen reopens it from page one. The guide does not connect devices or start the car. Starting the PC game from the connected phone dismisses any open PC guide automatically, so the PC overlay cannot block driving.

The guide uses the existing Canvas, safe area, palette, button feedback and click sounds. Animated vector illustrations need no new image assets or cameras. The calibration illustration shows a landscape phone. The modal blocks clicks on the menu beneath it. Phone pedals remain disabled while help is open; the existing Home pause state owns PC simulation. All guide timing uses unscaled time.

Separate local preferences (`FlyHigh.Guide.PC.v1`, `FlyHigh.Guide.Phone.v1`) remember dismissal. Skip, Got It or starting gameplay while help is open marks it seen. Opening help manually is always available. These preferences are independent of coins, vehicle ownership, audio and time-of-day settings. No network message, package, scene or prefab change is required.

Keyboard: Left/Right navigate pages, Enter submits the selected Next button, and Escape dismisses help. The menu consumes the dismissal frame to prevent Escape from also resuming driving. The automated virtual Escape event was not delivered by this Editor test environment, so physical keyboard behavior still needs a manual check. Actual click, submit, UI raycast, phone touch paths and close-frame input guard were checked.

## Validation

- Unity Editor compilation and Windows/Android conditional source compilation.
- 16 PC checks: first run, code display, page navigation, modal hit testing, skip/completion persistence, returning-player behavior, replay and close-frame input guard.
- 23 phone/paired local UDP checks: first run, disabled pedals, blocked underlying touches, five pages, skip/replay, existing Play/pairing/Continue, PC guide auto-dismissal, Pause/Home and unchanged 137-coin wallet.
- 50 guide layout cases (PC/phone, all five pages, five screen sizes including a safe-area simulation): 1,350 bounds, touch size, overlap and text checks.
- 10 Home layouts: 215 checks for the new help button alongside existing Home actions.
- PC guide and phone landscape-calibration Game-view previews inspected.

The guide is included in the rebuilt Android controller. Install the new `Builds/Android/FlyHighController.apk`; existing Android apps do not obtain UI changes from source edits. PC Unity Play mode uses the updated source; rebuild an exported PC city player when needed. Physical phone installation and physical keyboard checks remain manual.

If connection discovery is empty, check both devices' network and allow the PC game through Windows Firewall on the intended trusted network. Manual connection uses the PC Wi-Fi IPv4 address and the current code; choose the address on the same subnet as the phone. Never turn off the firewall as a setup step.
