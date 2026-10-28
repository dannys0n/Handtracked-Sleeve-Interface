# Validation - October 28, 2026

Verified on this workstation with Unity 6000.6.7f1:

- Project compiles after removing deprecated simulator package 81.0.0, which conflicted with Meta SDK 207.0.1.
- Generated `ForearmXR` and `GestureDesktop` scenes and configured Standalone/Android OpenXR settings.
- Both generated scenes were checked for missing MonoBehaviour scripts.
- Original X, T, O and M templates each recognize themselves with score >= 0.99.
- Desktop Play Mode: stationary pinch input is discarded safely; multi-stroke capture, recognition, and clearing pass. Synthetic X test recognized X at 0.82.
- Official Meta XR Simulator 207.1 MSI downloaded; Authenticode status Valid, signer Meta Platforms, Inc.
- Standard installer failed for insufficient administrator privileges. A per-user attempt also rolled back on its mandatory HKLM registry entries. Its Windows App Runtime dependency installations reported success.
- Administrative extraction to the ignored `Artifacts/SimulatorPortable` directory completed with exit code 0.
- Local runtime selected for the Unity process. Unity identified simulated Meta Quest 3 and loaded `OpenXRLoader`; an XR display subsystem reported **running: True**. The test stopped Play Mode afterward.

Machine-local evidence is in `Artifacts/setup-result.txt`, `desktop-test-result.txt`, `xr-test-result.txt` and Unity's `Logs/Editor.log`. These files, simulator binaries and installer logs are intentionally excluded from Git.

The SDK emits warnings for simulator capabilities and some unsupported controller binding paths. The XR display started despite these messages. Desktop audio falls back to the default audio device. No claim is made that every Meta SDK feature is supported by this simulator.

Not verified: webcam camera permission/streaming, hand motion accuracy, new-user recognition accuracy, real Quest deployment, measured elbow tracking, or forearm interface usability. Webcam testing needs visible hands and optional OS permission; physical-device testing remains necessary.
