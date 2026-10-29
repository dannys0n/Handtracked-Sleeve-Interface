# Forearm Gesture Lab

Unity 6000.6.7f1, Meta XR SDK 207.0.1, Unity OpenXR 1.18.0, URP.

A minimal starting point for wrist-to-elbow interfaces on Meta Quest. Includes separate forearm-interface and gesture XR test scenes, two tracked hands, world-space drawing, and the old HandTracking project's X/T/O/M gesture templates and $P recognizer. No deprecated project packages or spell effects were imported.

## Asset organization

- `Assets/ForearmLab/Scenes/ForearmXR.unity`: forearm interface scene; `ForearmLab/Runtime` contains its anchor.
- `Assets/GestureLab/Scenes/`: GestureXR and GestureDesktop. GestureLab also owns drawing/input scripts, the inner-arm detection display, scene tools/checks, custom gesture Resources, and the imported $P recognizer under ThirdParty.
- `Assets/SharedXR/`: common XR startup, simulator/setup editor tools, and skeleton-aware joint lookup. Existing C# namespaces are retained to avoid unrelated API/reference changes.

Meta prefabs remain in Packages. TMP resources remain in their standard shared folder. Asset moves preserve Unity GUIDs; scene references and build/menu paths are updated.

## Try it without a headset

Open this folder in Unity Hub with **6000.6.7f1**. Let packages resolve and scripts compile.

1. Choose **Forearm Lab > Open desktop lab** and press Play.
2. Hold the left mouse button to draw. Scroll to change drawing depth.
3. Draw X, T, O or M. Release between strokes; press **Enter** to recognize the entire drawing, **C** to clear. Buttons are also available in the Game view.

Automatic recognition on release is optional and defaults off so multi-stroke X/T remain possible. Ink remains visible after recognition. `GestureDrawing.onRecognized` exposes accepted names for future interface actions. Scores are similarity scores, not calibrated probabilities.

## Desktop gesture recording

Choose **Gesture Lab > Open recording scene**, or open `Assets/GestureLab/Scenes/GestureRecording.unity`, then press Play. Hold the left mouse button inside the left drawing area; release between strokes. Enter a name and click **Save sample**. **Clear drawing** starts another sample; **Test recognition** compares the ink with the available templates. Enter and C also recognize/clear while the name field is not selected.

The right-hand list scrolls vertically with the mouse wheel or scrollbar. It lists every gesture XML in `Assets/GestureLab/Resources/CustomGestures`, including X/T/O/M. Save adds an XML asset to that same folder, and each **Delete** button removes its XML asset and Unity metadata. Multiple samples can share a name. All gesture lab scenes use this shared library; saving/deleting refreshes recognition immediately in the recorder. Record in Unity Play Mode: player builds load the packaged Resources library for recognition, while editing the project library requires the Unity Editor. New recordings are project assets available to Git and subsequent builds.

## Meta XR Simulator and webcam

Install the **standalone Meta XR Simulator 207.1 or newer** from [Meta's Windows download](https://developers.meta.com/vr/downloads/package/meta-xr-simulator-windows/). The old `com.meta.xr.simulator` Unity package is deprecated and conflicts with SDK 207; it has been removed.

1. In Unity choose **Forearm Lab > Open Gesture XR lab** for drawing/recognition, or **Open Meta XR lab** for the separate forearm-interface foundation.
2. Select **Window > Meta > Meta XR Simulator > Activate**. Press Play.
3. In the simulator Inputs panel select **Meta Quest 3**, and set both left and right input to **Hand**.
4. For webcam input, enable **Inputs > Camera tracking > Enable camera**. Wait for **Streaming** and put both hands within view. Desktop camera access must already be permitted in Windows.
5. Right index pinch draws at the index fingertip; release ends a stroke. Right middle pinch clears. Left index pinch recognizes the drawing. You can also press Enter in Unity's Game view or enable recognition after each stroke.

Without webcam input, use the simulator's built-in hand poses and movement controls. Inspect its Input bindings panel for current key mappings. Stop Play before switching device profile. Deactivate the simulator before using a physical headset through Link.

### Local runtime on this workstation

The signed 207.1 MSI is downloaded under the ignored `Artifacts` folder. Windows rejected its system registry registration without administrator privileges. Its files were extracted successfully to `Artifacts/SimulatorPortable/PFiles/MetaXRSimulator/v207.1`; Windows App Runtime dependencies were installed by the per-user attempt before rollback.

To try that local extraction, launch `MetaXRSimulator.exe` from that folder, then choose **Forearm Lab > Simulator > Use local extracted runtime**, open the Gesture XR lab and press Play. This sets OpenXR environment variables for the Unity editor process only. **Restore previous runtime** undoes that selection; restarting Unity also clears it. This optional path and its binaries are machine-local and are not included in the Git repository. For a fresh clone, use the official administrator installer above.

Webcam tracking arrives through normal XR hand data; this project does not run a second webcam/ML pipeline. Single-camera depth and occluded joints are estimates. Head rotation is tracked but head translation is not. [Meta camera tracking guide](https://developers.meta.com/vr/documentation/unity/xrsim-camera-tracking/) · [Simulator setup](https://developers.meta.com/vr/documentation/unity/xrsim-getting-started/)

## VR and passthrough modes

`ForearmXR` supports both modes in the same scene. In Play Mode, use the **VR**/**Passthrough** buttons in the Game view, press **P**, or press **A** on the right Quest controller. The `Environment mode` object's `LabEnvironmentMode` component also exposes Play Mode context-menu actions and a `Startup Mode` setting (system recommendation, VR, or passthrough). The default follows Meta's system recommendation. Passthrough is optional; unsupported runtimes, initialization failures, and readiness timeouts retain an opaque VR background with a status message.

The controller owns one `OVRPassthroughLayer` underlay and changes the camera background to transparent black only after the layer reports that it has been presented. VR restores the original camera background and disables passthrough. An optional `VR Environment` reference can hide virtual scenery in MR; leave the rig, hands, and sleeve outside that container. Sleeve and hand tracking use the same components in either mode.

Quest 2 passthrough is grayscale. Meta XR Simulator uses a synthetic room rather than your webcam image as the background: choose a synthetic environment in the simulator and restart Play Mode if required. Webcam input still supplies hand poses. Headset testing is required for real-world sleeve alignment, transparency, and comfort. This feature does not add environment-depth occlusion or raw passthrough-camera access.

## Quest build

Install Unity's Android Build Support, SDK/NDK and OpenJDK modules. Switch the build profile to Android. OpenXR is configured for Android with the Meta feature, hands/controllers, ARM64, IL2CPP and Vulkan. `ForearmXR` is the enabled build scene. Android initializes XR at launch; the desktop editor starts it only in the XR scene. Developer mode and a connected Quest are required for Build And Run.

## Forearm interface foundation

The bottom-left **Scroll Around Sleeve** debug dial scrolls all elements around the circumference. Drag the dial or use the mouse wheel while hovering it; the angle wraps through 360 degrees. **Reset scroll** restores zero. This changes `SleeveGridLayout.Scroll Angle` independently of projection distance, wrist orientation, and closing deformation.

The reusable `WristSleeve.prefab` contains one blank full-wrap element, automatically hidden whenever active elements are present and restored when the last one is removed. `WristSleeveDemo.prefab` is a true prefab variant used by ForearmXR. It contains nine examples: app-volume slider, passthrough toggle, time/power readout, projection button, image, 3D hologram, touch pad, A–Z keyboard, and detachable image. Battery is reported on standalone Android; Link displays N/A rather than the PC battery. Elements are prefab variants of `Prefabs/Elements/SleeveElement.prefab`: normal uGUI controls, RectTransform anchors, Layout Groups, UnityEvents, and optional 3D content are composed inside a generic grid element. Put arbitrary UI under `Content`, or assign a 3D hierarchy through `Volume Content`. `SleeveGridLayout.AddElement` / `RemoveElement` manage runtime placement and fallback visibility; `SleeveImageContent.SetTexture` accepts an image or RenderTexture supplied by a future capture flow. The detachable example toggles a floating copy of its image; hand grabbing, two-hand capture/scaling, persistence, and drop-to-store are not implemented yet.

The grid wraps 360 degrees with configurable row/column spans. Projection defaults to zero plus a 0.004 m surface offset. Debug controls retain the projection-distance slider, radial scroll dial, and independent cylinder/line toggles (both off by default). Controls resize with their cell. Standard Image/RawImage geometry and TMP glyphs are mapped onto the cylinder and use the shared fold shader. The sample cube retains its fixed mount, separate spin pivot, 2x size, and rotational clearance.

Right-index touch uses Meta `PokeInteractor`, `PokeInteractable`, and `PointableCanvasModule`, with project-specific `ISurfacePatch` and `PointableCanvas` adapters for cylinder-to-uGUI coordinates. The wrist input and hand visuals remain separate from the reusable sleeve. With Quest Link connected, open ForearmXR and press Play; touch PROJECT with the right index finger, then try volume, passthrough, keyboard, and touch-pad controls. Touch is cancelled while folding (Close Amount >= 0.01), because folded hit geometry is not implemented. Automated tests cover synthetic fingertip hover/select/release with one actual uGUI button click, placeholder add/remove, 27 resize/scroll mappings, keyboard events, app-volume binding, and the detachable image example. Physical headset comfort, occlusion, and passthrough readiness still need on-device testing.

`Close Amount` is a continuous shader deformation, independent of logical workspace dimensions. One `SleeveDeformation.hlsl` mapping folds the shell, projected tiles, SDF glyphs, sample 3D content, and tethers into staggered planar triangles and compresses them together toward the elbow. Closing does not change opacity, grid placement, or stored sizes. The **Collapse threshold** slider defaults to 75% folded; between that threshold and fully closed, the shared shader smoothly shrinks the entire presentation into the elbow-end point. At fully closed its geometry has zero area and disappears. This is parameter-driven and reversible, with no separate animation state. Reopening is the inverse of the same parameter. Each sleeve supplies its own transform and fold uniforms, preserving independent instances and avoiding changes to shared asset materials. Future content shaders should include this deformation function. The bundled text shader provides basic SDF rendering rather than all TMP outline/underlay features. This is a visual Yoshimura approximation, not a rigid-folding or cloth solver.

`Assets/ForearmLab/Prefabs/WristSleeve.prefab` is a self-contained sleeve with two anchor slots: **Wrist Anchor** and optional **Elbow Anchor**. The `WristSleeve` component uses only Unity transforms. Its `Radius` and `Length` are in metres (defaults: 0.03 m radius, 0.22 m length). With only a wrist, it follows the wrist and uses the configured length. With an elbow, it points toward the elbow and optionally fits the length to the wrist-to-elbow distance; turn off `Fit Length To Elbow` to keep a manual length. Wrist orientation provides the roll reference. The input convention is OpenXR wrist axes (+Z toward fingers, +Y toward the back of the hand); `Wrist Rotation Offset` adapts other conventions. Local sleeve +Z runs toward the elbow and +Y faces the palm side. Keep the prefab root and its parent at unit scale for metre-based sizing.

The prefab owns an open-ended tube mesh with UVs and a double-sided, translucent white URP Unlit material (`Assets/ForearmLab/Materials/SleeveWhiteTransparent.mat`). Change its Base Color and alpha for tint/opacity. A separate surface child scales with radius and length, keeping the root at unit scale for future UI. The mesh is shared; resizing does not rebuild geometry or allocate a new material. Touch uses an analytic cylinder surface rather than a MeshCollider.

`ForearmXR` contains the demo prefab variant. `MetaSleeveWristBinding`, outside the prefab under the left hand, supplies a stable wrist transform and handles Meta skeleton-axis differences. The elbow stays empty for hand simulation. Tracking loss clears that input; the sleeve surface hides until it returns. The wrist pose source remains independent of the sleeve geometry; the element library currently includes the Meta poke adapter. Hand tracking alone does not provide a measured elbow. GestureXR's detection panel remains a separate gesture-test interface with its existing optional wrist/elbow attachment.

## GestureXR inner-forearm display

`GestureXR` contains the migrated drawing and detection logic. Its world-space TextMeshPro display sits on the palm-facing side of the left forearm, using the tracked wrist pose and the palm axes for the active Meta skeleton format. It shows the right-hand state, drawing state, point count, and latest recognition result/score. Raise and turn your left palm toward you to read it. The display hides on lost left-hand tracking and returns on reacquisition; it does not float at a stale pose.

Select `Left inner forearm detection display` to adjust `Distance From Wrist`, `Surface Offset`, and `Rotation Offset`. The arm position is estimated from the hand; no measured elbow is implied. In edit mode the panel is visible at a preview position for layout. `ForearmXR` contains the sleeve prefab without gesture drawing logic; `GestureDesktop` retains mouse tests.

## Recognition migration

Points are stored in 3D and projected to a view plane fixed at the first stroke, then compared using the original $P recognizer. This preserves the concept of spatial drawing recognized as a 2D symbol; it is not a volumetric 3D recognizer. The threshold defaults to the original 0.8. Imported templates need evaluation with fresh tracked drawings.

The lab scenes and prefab assets are saved directly in the project; temporary generation and automated Play Mode helpers are not included.

Unity caches, logs, builds, simulator binaries, local credentials and user settings are excluded from Git. The old ZIP is read-only and is not part of this repository.
