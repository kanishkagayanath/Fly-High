# Phone home menu and PC connection flow

The Android app opens on Home with Play, Shop, Options and Exit, using the existing main-menu artwork and button sprites. The removed decorative captions/footer hints stay removed.

- Play opens the PC connection panel. Select a discovered PC or enter its address/code. After pairing, the phone requests Play and the PC starts/resumes driving automatically. When already connected, Play still opens the panel and Continue starts/resumes that PC. Back cancels an unfinished connection and returns to Home.
- The pause menu's Home button returns to the full phone home and PC main menu. The current drive/mission is retained for Play/Resume.
- Shop shows the PC's current wallet, vehicle prices, ownership, selection and purchase feedback, with local preview sprites. Buy and Select are validated/saved by the PC's VehicleGarage. The phone has no independent wallet or ownership save. Six vehicles fit per page, with pagination for larger bounded catalogues.
- Options controls PC master volume and city lighting. Shop/Options open connection when needed, then open the requested page. Exit closes the phone controller; the PC remains available.

The PC game/server must already be running (Unity Play mode or an updated standalone player). The phone cannot discover or launch a stopped PC application. No manual click on the PC game's own Play menu button is needed after pairing. Use the rebuilt Builds/Android/FlyHighController.apk; legacy phone apps keep existing driving/pause controls but do not gain the new home actions.

## Ownership and wire protocol

PhoneHomePanel owns phone home presentation, pending connection intents and page rendering. PhoneControllerView gates driving input while Home/connection is visible. Pairing emits completed/cancelled events and provides Continue for an existing link. PhonePausePanel visibility yields to Home/connection, preserving the cleaned pause layout.

Existing FH1 driving/ACK/discovery/mission and menu IDs0-6 remain unchanged. Menu IDs7-10 add Play, Shop, Options and CycleLighting. HomePhoneProtocol adds bounded HS snapshots (version1, revision/token, page, wallet, prices/ownership/selection, lighting, result) and GARAGE requests containing only ID/Buy-or-Select. The receiver requires the current live paired endpoint, PIN and menu token. The PC validates that the shop is open, then uses existing VehicleGarage purchase rules. Coins/prices/ownership never come from phone requests. Accepted/rejected actions publish fresh tokens; stale duplicate packets cannot charge twice. New home state is sent only while PC menus are open, at2.5Hz and immediately after changes. Unrelated driving coin pickups do not rotate menu tokens; the latest wallet is published when returning Home.

The phone renders only fresh, token-matched snapshots and disables purchases while syncing/pending. Requests retry for1.5seconds; cancelled connection intentions cannot later start the game. Disconnect returns the phone to Home and neutralises pedals. Application focus/pause retains the sender's existing safety behaviour. No new packages, scenes or prefab rewiring.

## Validation (2026-11-01)

Live Editor, Android and Windows conditional source compilation passed.16protocol checks and3000malformed byte messages passed.30live paired UDP/UI checks passed: initial Home/no popup, Play popup/cancel, discovery and automatic remote Play, Home, connected Continue, six server-backed vehicles, purchase137->37exactly once, duplicate/foreign endpoint refusal, free owned selection, server refusal of unaffordable request, shop result, Options/volume/lighting, refusal of garage commands while driving, disconnect/manual reconnect and saved purchase. Original137coins/starter-only ownership/selected vehicle/volume/time preference were restored after tests.

Fifteen home/shop/options layouts across5sizes (including notched/small/wide cases) passed595checks for bounds, overlap, text and minimum44x44touch areas. Fifteen actual GraphicRaycaster probes reached main/shop/options controls. Home, connection, shop and Options Game-view previews were inspected. A Unity test sender losing Editor focus was pinned active only by a temporary Editor probe; the Android focus-loss safety code was unchanged. An initial API call crossing Play-mode domain reload produced a Pipeline ThreadAbort error; a fresh session had0Consoleerrors. One protocol probe initially omitted an out argument; that test-only call was corrected before passing.

Physical Android installation, touch/sensors/keyboard and application exit remain device checks; no device was attached. Build/signature and final state are recorded below and in Docs/AI/UnityProjectContext.md.

Android package completed on2026-11-02 in383.0s with0errors/3existing tooling/import warnings. The54,546,563-byte ARM64APK has the same signing certificate as the previous controller and retains the new home/garage protocol/UI types. SHA256 1BE201A50AD5099D2FC9D600F8265995F91BDBE27972368F660E84A1919B0D89. Install Builds/Android/FlyHighController.apk over the current app. The updated PC source must be running in Unity Play mode (or exported again for standalone). Original137coins/starter ownership/selection/volume/time and project settings are preserved. Editor returned clean/Edit mode with controller port free. Physical device validation remains.
