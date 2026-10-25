# Find PC: simple phone connection

The Android controller now has **Find PC**. On the same Wi-Fi, tap it and choose your computer from the list. The app fills the current address and session code and performs the existing acknowledged connection. If several games are running, select the intended one; no host is selected automatically. Games already controlled by another phone are marked **In use** and cannot be selected.

The PC receiver runs from the main menu as well as during driving. Open CityDrive or GameScene and enter Play mode (or launch an updated PC build) before searching. After connecting, hold the phone comfortably in landscape orientation and tap Calibrate Tilt.

## Install and fallback

Both the PC receiver and Android controller need this update for discovery. Existing manual connections remain compatible. The Android build menu is **Fly High > Build Android controller**, producing `Builds/Android/FlyHighController.apk`. The PC city build menu is **Fly High > Build PC city drive**. Editor Play uses the new receiver scripts immediately.

Search lasts six seconds and offers Search Again and Back. If no game appears, check that the devices share Wi-Fi, the network permits communication between clients, and Windows allows the game/Unity on the private network. Manual IPv4 address and six-digit code entry remains available if broadcasts are blocked. No camera permission, QR reader, cloud account or router port forwarding is required.

## Implementation

- ControllerDiscovery adds bounded request/reply messages on the existing UDP 47777 port. The phone uses a fresh nonce and an ephemeral reply port. The PC replies directly to the requester, at most five times per second. Discovery does not claim the driving session or affect input counters.
- PhoneSender broadcasts every 0.75 seconds during an explicit search, using limited broadcast and up to seven additional adapter-directed broadcasts. A session ID deduplicates replies from a PC with multiple adapters. At most eight hosts are retained. Replies must match the nonce and come from the controller port; names, code, state and packet lengths are validated.
- The selected address comes from the actual reply endpoint. Selecting a row closes discovery and uses the existing Connect/ACK flow. Busy replies omit the pairing code. Like the existing local pairing code, discovery is intended for a trusted local Wi-Fi network, without transport encryption.
- PhonePairingPanel adds an opaque, scrollable PC chooser; cached rows update when discovery results change. The existing manual controls, pedals and steering remain in place. Cancellation, pause, focus loss and disabling close the discovery socket. Late packets cannot populate a later search because its nonce changes.
- Unreachable-adapter ICMP connection-reset/refused messages are ignored during discovery so other targets can still reply. Other socket failures show a message and allow manual connection. The six-second deadline is checked independently of socket errors.

## Validation scope

Unity 6000.3.17f1 / URP 17.3.0. Live Editor compilation and Android conditional source compilation passed. Thirteen protocol assertions and 10,000 malformed-packet fuzz cases passed. Live local broadcast found the running receiver, deduplicated its replies, and actual row selection connected both sender and receiver. A second discovery client received Busy without a pairing code while the original phone connection remained active. UI capture verified the PC chooser and controls.

Repeated cancellation (four fresh searches) released each ephemeral socket for an exclusive rebind; disabling the sender also closed discovery. The no-PC timeout passed after the ICMP handling correction, and manual connection worked after discovery. A populated eight-PC UI test verified scrolling (646px content in a 374px viewport), nonzero row sizes, and a busy row refusing connection. Final Play-mode Console had zero errors; an existing Unity Account API warning was unrelated. Temporary test UI/captures were removed and the controller port was free after stopping Play mode.

Tests use a sender and receiver in the Editor process plus separate UDP query sockets. They do not replace installation and Wi-Fi testing on a physical Android phone; access-point broadcast isolation and device-specific network policies remain device checks.

## Built controller APK

On 2026-10-25 the existing PrototypeBuild.BuildAndroid workflow produced `Builds/Android/FlyHighController.apk` successfully in 511.2 seconds: 44,227,397 bytes, zero build errors. The report contains 972 warnings, dominated by existing Unity AI Inference ConvGeneric compute-shader variant warnings; these were recorded and not suppressed. No package changes were made for the build.

APK verification passed. Package ID is `com.flyhigh.phonecontroller`, version 0.1.0/code 1, ARM64 IL2CPP, minimum API 25 and target API 36. INTERNET permission is present; CAMERA is absent. Its signing certificate matches the previous local controller APK, allowing an in-place update. New discovery/picker types are present in the compiled IL2CPP metadata. SHA-256: `A5EE3DD4D98F001C9877E129E22AA1CC4EF27BB0999B11C7491BDFBC83CF7575`.

The Editor returned to CityDrive in Edit mode with its original Android build target. No Android device was connected, so installation and a physical phone-to-PC Wi-Fi test were not run. Install the updated APK, start PC Editor Play mode, then use Find PC and Calibrate Tilt. A standalone PC executable also needs rebuilding to include discovery.
