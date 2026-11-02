# Phone controller dashboard

The Android controller now uses a landscape dashboard with a midnight background, cyan drive accents, amber mission/connection details, a violet Reverse pedal and warm red Brake pedal. A large Accelerate pedal sits under the right thumb; Reverse and Brake are stacked on the left. The centre steering wheel and position marker follow the existing smoothed phone steering. Calibrate remains directly below the wheel and briefly confirms centring.

Connection and Pause are in the top bar. The mission strip below it shows free-roam guidance, nearby Start Mission, collection progress and the earned-coin balance. Connection fields are in a separate two-column panel: nearby PCs on the left, manual IPv4/PIN on the right. The app now opens Home; tapping Play opens this panel and searches for PCs. Pairing starts/resumes the PC drive automatically. See PhoneHomeMenu.md for the full home/shop/options flow. Reopening it while connected keeps that connection; Find PCs Again deliberately starts a new search. Pedals release and disable while the panel is open. Invalid manual details leave the panel open with the connection status shown.

Pause, Resume, Home and sound settings use the same visual style. The pause controls retain desktop-authoritative state and the existing saved volume. All interactive controls use the existing uGUI EventSystem, with no additional packages or imported art.

## Code ownership

- `PhoneControllerView` builds and updates phone presentation; PrototypeBootstrap's Android branch creates it with the existing PhoneSender.
- `PhoneUi` provides shared phone-only typography, panels, buttons and inputs. PhonePanelGraphic draws gradient bevelled frames; PhoneWheelGraphic draws the steering wheel. Both explicitly require CanvasRenderer. The aspect-fitted wheel is inside its own bounded centre container.
- PhonePairingPanel retains discovery/manual connection ownership and now owns the manual fields. PhoneMissionPanel retains mission presentation/actions; PhonePausePanel mirrors authoritative menu state.
- HoldPedal checks button interactability, tracks each finger independently, releases on pointer exit, and exposes Release for menu/disconnect cancellation. Another finger cannot release the held pedal. Touch feedback scales only the visual; the input target remains stationary.
- Background fills the complete screen; layout follows the existing safe area and CanvasScaler. Root/overlays/graphics clean up with their owner. Geometry arrays are cached; no runtime background textures, render cameras, shaders or new material assets were added.

## Validation (2026-11-01)

Live Editor and Android/Windows conditional source compilation passed. The known opposite-platform Bootstrap unused-field warnings remain.

Twenty-four live interaction checks passed with the real controller socket and UI actions: discovered-PC connection, manual invalid/valid reconnect, both driving directions, two simultaneous touches, unrelated-finger release refusal, slide-off release, pause clearing held reverse, Resume, Home, volume, connection overlay keeping the link while cancelling pedals, and an actual Start Mission request/collecting status.

Twenty layout combinations (drive/connection/pause/sound at 1280x720,1600x720,1920x864,1024x768,2400x1080 with simulated safe-area insets) passed765checks. Buttons stayed within the safe area, did not overlap, had nonzero readable text and touch targets at least44layout units high/70wide. The steering gauge remained inside its centre region. Six real GraphicRaycaster probes hit the correct pedal/header/calibration buttons. Game-view screenshots of all four screens were inspected.

Original 85coins, garage JSON and volume were backed up/restored after tests. Editor probes and temporary capture assets were removed. Build/package details follow in the project context. Physical phone touch/sensor/keyboard and Wi-Fi validation still require installing the new APK; no Android device was attached.

Android build succeeded in250.5s with0errors and973existing package/tooling warnings. The new54,536,375-byte APK at Builds/Android/FlyHighController.apk is signed with the same certificate as the previous app and retains the new UI classes in IL2CPP metadata. SHA256 1FF99BB52F0BFBC05B474F94C80EF2BE93BAC41A4434725F0C092AF0FC05377B. Install it over the existing controller, then use the connection panel. Original coins/ownership/volume are preserved. Final CityDrive scene is clean/Edit mode, controller port free, with no test UI objects or temporary imported-asset/settings changes left behind.

## Pause layout cleanup — 2026-11-01

Removed the pause brand, decorative subtitle and saved-coins hint marked in the user's screenshot. The title and three equally wide Resume, Settings and Back to Home buttons now form a centred column. Sound controls follow the simpler heading. Other dashboard/connection/mission content remains as designed. The revised phone layout passed760checks over20cases and all three pause controls passed touch-raycast/navigation checks. Updated APK required.

Cleanup APK build succeeded (0errors; existing package warnings) and its signature matches the installed controller's prior build. Install Builds/Android/FlyHighController.apk over the current app. Centred pause controls, navigation and safe-area layouts passed in Unity; physical installation remains to verify. Original137coins, ownership and saved volume remain intact.
