# Pause menu and phone controls

The top-left PAUSE button (or Escape) opens a separate in-game pause menu. Resume continues the current drive. Settings adjusts saved master volume in 10% steps, from mute to 100%. Home returns to the existing main menu, preserving the current session, mission progress, earned coins and selected vehicle. The main menu's Play/Resume returns to that drive.

The updated Android controller has a PAUSE button beside its title and mirrors the PC pause menu, including Resume, Settings, Home, sound +/- and Back. Install `Builds/Android/FlyHighController.apk` over the previous controller. PC and phone share one authoritative desktop menu state; older controller builds still drive and can be paused using the PC.

## Implementation

- DesktopMainMenu owns gameplay pause, separate in-game panels, existing main menu transitions and saved `FlyHigh.MasterVolume`. It reuses the imported menu button sprites and restores the prior game time/audio pause state on Resume and teardown.
- PhonePausePanel mirrors the desktop state, blocks driving buttons while paused or at Home, and uses the existing paired PhoneSender socket. UI and networking use unscaled/realtime updates during pause.
- MenuPhoneProtocol adds optional FH1 UI status at 5 Hz and bounded menu command packets. A fresh 16-character token rotates on accepted changes. Retries reuse the original token, so repeated UDP copies cannot apply a volume change twice. WifiReceiver accepts commands only from the paired, currently live controller endpoint with the current PIN/token. Stale commands, wrong PINs and other endpoints have no effect.
- WifiReceiver neutralises gameplay input during menus while continuing controller acknowledgement and discovery. Resume requires a neutral throttle packet before accepting another drive press. PhoneSender clears held pedals when the menu state changes, while commands are pending and while driving controls are unavailable.
- No new packages or scene/prefab rewiring; PrototypeBootstrap creates the controls at runtime. Coin/shop saves are unchanged.

## Validation (2026-11-01)

- Live Unity compilation and Windows/Android conditional compilation passed.
- 16 protocol checks, including all actions, invalid ranges/PINs and 3,000 malformed packets, passed.
- 23 Play-mode checks over actual paired loopback UDP passed: both pause and resume directions, both Home buttons, shared sound settings, saved volume, repeat-packet deduplication, foreign endpoint/PIN rejection, held-pedal clearing, frozen player position, preserved 85-coin wallet and one EventSystem.
- 11 additional checks passed with raw legacy driving packets: held throttle blocked during/after pause until neutral, fresh press accepted, disconnect/reconnect retains pause, sound mute/full limits and unchanged pause during sound changes.
- PC HUD/pause previews inspected. Physical Android Wi-Fi/touch validation requires installing the updated APK; no device was attached during development.

Build result and final cleanup are recorded in the project context and Codex outputs/Pause-* artifacts.

Android controller build completed successfully in435.2s with0errors and973existing package/tooling warnings. The signed54,519,479-byte APK contains the new controls and uses the same signing certificate as the previous controller, so install it over the current app. Package com.flyhigh.phonecontroller0.1.0/code1; ARM64,minAPI25,target36. SHA256 D3DC0C207F3FA3ACAC18C00519B22A77F5CC3DD45A4C576EEF4D2F7970BD1E20. No physical device was available for installation testing. User coin/garage/volume preferences were restored unchanged after validation.
