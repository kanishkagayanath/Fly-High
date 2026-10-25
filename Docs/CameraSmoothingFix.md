# Driving camera smoothing

Fixed `Assets/PhoneController/RacingPrototype.cs` on 2026-10-25.

The camera previously combined the car's raw fixed-step Rigidbody position with its interpolated rendered rotation, then aimed directly at the raw position each render frame. In a live driving probe, the raw physics position averaged about 0.40 m ahead of the rendered car. That mismatch produced visible aim vibration.

The camera now samples the rendered Transform consistently in LateUpdate, with separate damping for follow position, heading and aim. It keeps a level horizon, holds still while paused, and snaps immediately after resetting or teleporting the car. Existing acceleration, steering, collision handling and controller inputs were preserved.

Validation:
- Live Unity Editor compilation completed without errors.
- Matched before/after driving probes used the actual player controller and interpolated Rigidbody on a temporary flat test platform, targeting 120 FPS and Unity's existing physics timestep. Test input included acceleration and steering; test objects, input and temporary frame-rate settings were removed/restored afterwards.
- During the straight-driving window, vertical frame-to-frame curvature fell from 10.05 to 0.19 pixels RMS at a normalized 1080-pixel image height, approximately a 98% reduction. This is a controlled test metric, not a guarantee for every frame rate or road surface.
- Pause check recorded zero camera movement and rotation.
- First-render reset check recorded zero follow-position error after a deliberately displaced camera.
- Returned to Edit mode. No scene assets or project settings were changed.

An editor connection request was interrupted during Play-mode domain reload and generated a Unity Pipeline “Thread was being aborted” log. It was identified in Console as a tooling error; no camera or gameplay exception was found. The original Console messages were retained.

Default smoothing times: position 0.12 s, aim 0.06 s, heading 0.14 s. The Android controller APK does not need rebuilding. Rebuild the PC game to include the fix in its standalone executable.

Original script backup: `C:/Users/Kanishka/Documents/Codex/2026-10-15/i/work/camera-backup/RacingPrototype.cs`.
