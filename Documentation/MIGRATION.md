# Migration from HandTracking.zip

Source: user's `UnityProjects/HandTracking.zip`, inspected without executing the old project or importing its packages.

Retained:

- Six SDK-independent C# files from `Assets/PDollar/Scripts/PDollarGestureRecognizer`.
- Four user templates from `Assets/Resources/CustomGestures`: X, T, O, M.
- The concepts from `DrawLine`, `LineManager`, and `LineGestureRecognizer`: pinch-held capture, separate strokes, 3D line rendering, 2D projection, similarity threshold, named recognition events, and clearing.

Replaced:

- Old `HandPinchOffset` and scene/prefab dependencies with current Meta `OVRHand` / `OVRSkeleton` input and an explicit desktop provider.
- Exact pinch-strength equality with press/release hysteresis.
- Per-point live screen projection with a fixed view-plane basis captured on the first stroke, reducing camera-motion distortion.
- Spell spawning and singleton debug dependencies with a small status display and a serializable UnityEvent.
- Single-stroke automatic recognition with explicit multi-stroke recognition by default.

The original archive's `HandPoseManager` was an event-wrapper registry rather than a stand-alone trained pose classifier. This lab restores index/middle pinch detection and exposes drawing recognition; old custom pose prefab configurations, archived Interaction SDK sample scenes, spell VFX, raycast spawning and hand-pose authoring were not migrated. Open/pinch display is a diagnostic, not a general learned pose recognizer.

Robustness: minimum point spacing, bounded point count, tracking-loss stroke termination, stationary/short-input rejection, invariant-culture XML parsing, and zero-length normalization guards. $P source attribution and BSD terms remain in the original file headers. Imported code is third-party code under those terms; this repository does not relicense it or Meta's SDK. Package Manager retrieves Meta packages under Meta's own terms.

Limits: template self-recognition is a wiring regression check, not user accuracy measurement. Editor Play Mode tests do not validate Quest rendering, thermal performance, calibrated forearm placement, or headset interaction quality.
