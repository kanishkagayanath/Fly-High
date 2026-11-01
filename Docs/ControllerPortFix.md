# Controller port recovery and cleanup

Recovered Fly-High's Wi-Fi controller listener on 2026-10-25.

Evidence: UDP port 47777 was held by the Unity Editor process even in Edit mode with no WifiReceiver or PrototypeBootstrap objects remaining. A forced garbage collection did not release it. This confirmed an orphaned socket within the Editor, rather than a competing standalone game. The precise original retention trigger was not established.

The active CityDrive scene was saved and backed up, and the same Unity project was restarted to clear the orphaned native handle. An early lifecycle experiment during recovery also failed; after a clean restart the final checks below passed repeatedly.

Changes:
- `Assets/PhoneController/WifiReceiver.cs`: retain the active socket independently of the Unity object's lifetime; close it on disable, destruction, application quit, Play-mode exit and before assembly reload; reset session state; dispose a socket even when binding fails; disable duplicate receivers; give a clear occupied-port message.
- `Assets/PhoneController/PrototypeBootstrap.cs`: prevent duplicate bootstrap instances and reuse an existing enabled receiver.
- The protocol, pairing flow and UDP port remain unchanged. Port sharing was not enabled.

Validation after recovery:
- Live Unity Editor compilation passed.
- Android conditional source compilation passed, with existing unused platform-field warnings; no APK/device build was run.
- Real paired controller packets received the expected PC acknowledgement.
- Disabling the receiver released port 47777, and enabling it again successfully rebound the listener and acknowledged another packet.
- Duplicate receiver and bootstrap probes were disabled while the original listener remained active.
- Three subsequent Play sessions started successfully; the port was explicitly rebound by an independent exclusive socket check after each stop, including a session with no test phone traffic.
- Final Console check reported zero errors. Returned to Edit mode with port 47777 available.

Backup: `C:/Users/Kanishka/Documents/Codex/2026-10-15/i/work/receiver-fix-backup/`.

Press Play and enter the newly displayed pairing code in the phone app. The existing phone APK remains compatible. Rebuild the standalone PC executable to include the cleanup changes there.

2026-10-29 follow-up: a stopped Editor session retained UDP47777 during final Exit validation. An explicit close while the receiver was alive released it. WifiReceiver now registers an instance-owned ExitingPlayMode callback on enable and removes it on disable/destruction, closing the live socket before runtime teardown. Existing static cleanup remains as a backstop. Three repeated Play/stop or actual menu Exit cycles passed, including a paired UDP input/ACK. The original stale handle was cleared by safely reopening the same project through EditorApplication.OpenProject; no network protocol or phone APK update was necessary.
