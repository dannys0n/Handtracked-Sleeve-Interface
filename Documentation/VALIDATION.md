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

## GestureXR scene and inner-forearm detection display

Added a dedicated `GestureXR` scene by duplicating the existing Meta rig and gesture setup, then removing the gesture logic from `ForearmXR`. `GestureDesktop` remains the mouse test scene. The world-space TextMeshPro panel uses a fixed offset in the actual left-wrist frame and palm axes for the active skeleton format. Its offsets are configurable; the elbow is not measured.

Verified: new scripts compile; GestureXR has no missing scripts; imported M points produce `Recognized: M (1.00)` and that result appears in the panel text in a ticking Play Mode session. When left-hand tracking is unavailable, the canvas hides. Imported TMP essentials fixed the missing text shader. Render inspection caught and fixed UI text sizing and wrapping; the preview contains the detection status and controls within the panel bounds.

Tracked palm alignment and reading comfort still need a headset/webcam check with the user's arm. The synthetic recognition and rendered preview do not establish physical placement accuracy.

## Wrist anchoring correction

Fixed the shared bone resolver accepting overlapping IDs from two skeleton formats: legacy wrist ID 0 could select OpenXR palm ID 0 instead of wrist ID 1. The resolver now selects the active skeleton convention before searching. This also corrects index-tip lookup used by drawing.

Replaced finger-position cross products with a fixed wrist-local pose. OpenXR left-hand distal is +Z and palmar is -Y, per the installed Meta SDK constants; legacy left-hand directions are handled separately. The canvas front (-Z) faces palmar and text up follows distal. The display follows the persistent hand hierarchy and refreshes in LateUpdate and before rendering, avoiding a stale world transform after late rig updates.

Verified compilation, 144 synthetic rotation checks for invariant wrist offset/palmar-facing front/text up across both skeleton conventions, and a joint-ID regression with initialized OpenXR bones and temporary validity flags restored immediately afterward. At the final live sample tracking was unavailable, so these checks do not establish physical-device stability or eliminate webcam tracking noise. Re-test with a tracked hand after this correction.
