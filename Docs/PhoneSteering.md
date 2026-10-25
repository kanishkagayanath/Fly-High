# Smooth phone steering

The phone previously sent a linear steering value: tilt divided by 35 degrees, with no neutral dead zone. A settled five-degree tilt therefore requested 14.3% steering (about 8.83 degrees of heading change per second at 10 m/s before contact limits). Sensor filtering reduced noise over time but did not reduce sustained small-angle sensitivity.

`RacingPrototype` now shapes only connected-phone input on the PC:

- A 0.06 dead zone ignores approximately the first 2.1 degrees around the phone's calibrated centre.
- Remaining input is remapped continuously to 0–1 and raised to power 1.6. Small tilts are gentle; full tilt retains full steering authority. Left and right use the same curve.
- Steering eases with a 0.14-second SmoothDamp time, returning to centre with a 0.10-second time. Near-zero values settle exactly to zero. Reset clears steering and damping velocity.

The four serialized phone steering fields on `RacingPrototype` control this tuning. The original keyboard response, speed-dependent turn limit, acceleration, braking, collision guard, camera and wheel animation remain in their existing paths. The Wi-Fi packet and Android sender are unchanged, so the current phone APK is compatible. Rebuild the PC game for distribution; Editor Play uses the update immediately.

## Validation — 2026-10-25

Live Unity compilation passed. Deterministic checks invoked the actual steering method over 201 input levels: bounded/monotonic curve, left/right symmetry, neutral noise rejection, full steering, reversal, no overshoot at settled transitions, exact recentring, pause holding and unchanged keyboard response passed. Samples at 30/50/120 updates per second differed by only 0.000247 after 0.4 seconds. Full steering exceeded 99% after 0.5 seconds.

| Phone tilt | Previous settled steering | New settled steering |
| --- | ---: | ---: |
| 2 degrees | 5.71% | 0% |
| 5 degrees | 14.29% | 2.05% |
| 10 degrees | 28.57% | 10.20% |
| 20 degrees | 57.14% | 37.76% |
| 35 degrees | 100% | 100% |

Android conditional source compilation passed with only existing warnings about unused desktop bootstrap fields. No standalone executable or APK build was run.

A Play-mode loopback replay sent real FH1 packets at up to 30 Hz through WifiReceiver and the running car physics. Settled five-degree input produced +0.020544 and -0.020544 steering; the neutral phase settled to exactly zero. Connection, timeout/disconnect, and reset clearing the damping state all passed, with zero current Console errors. An initial wall-clock probe under-sampled the first turning phase during Editor startup; the final replay used simulation time for the phase durations.

## Check on the phone

1. Start CityDrive, connect the current phone app, and hold it comfortably in landscape orientation.
2. Tap Calibrate Tilt while the phone is centred. Hold Accelerate and tilt a few degrees each way: the car should make gentle corrections.
3. Increase the tilt gradually to make tighter turns, then return to centre: the car should ease back to straight driving.
4. Try reverse, brake, pause/resume and R reset. Reset should leave no leftover steering.

Synthetic tests cannot confirm a particular phone's sensor noise or the player's preferred feel; the device drive above is the final feel check.
