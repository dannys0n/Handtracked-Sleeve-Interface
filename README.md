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

## Quest build

Install Unity's Android Build Support, SDK/NDK and OpenJDK modules. Switch the build profile to Android. OpenXR is configured for Android with the Meta feature, hands/controllers, ARM64, IL2CPP and Vulkan. `ForearmXR` is the enabled build scene. Android initializes XR at launch; the desktop editor starts it only in the XR scene. Developer mode and a connected Quest are required for Build And Run.

## Forearm interface foundation

`Left forearm UI anchor (estimated)` follows the left wrist with configurable position and rotation offsets. Its small slab is a placement placeholder, not a wrapped or interactive interface. Adjust `ForearmAnchor.wristLocalOffset` and `rotationOffset` against actual tracking. Hand tracking alone does not provide a measured elbow. Future work: calibrated elbow/body tracking, curved UI surface, opposite-hand touch/pinch, and physical-device UX testing.

## GestureXR inner-forearm display

`GestureXR` contains the migrated drawing and detection logic. Its world-space TextMeshPro display sits on the palm-facing side of the left forearm, using the tracked wrist pose and the palm axes for the active Meta skeleton format. It shows the right-hand state, drawing state, point count, and latest recognition result/score. Raise and turn your left palm toward you to read it. The display hides on lost left-hand tracking and returns on reacquisition; it does not float at a stale pose.

Select `Left inner forearm detection display` to adjust `Distance From Wrist`, `Surface Offset`, and `Rotation Offset`. The arm position is estimated from the hand; no measured elbow is implied. In edit mode the panel is visible at a preview position for layout. `ForearmXR` retains the rig and interface placeholder without gesture drawing logic; `GestureDesktop` retains mouse tests.

## Recognition migration

Points are stored in 3D and projected to a view plane fixed at the first stroke, then compared using the original $P recognizer. This preserves the concept of spatial drawing recognized as a 2D symbol; it is not a volumetric 3D recognizer. The threshold defaults to the original 0.8. Imported templates need evaluation with fresh tracked drawings.

**Forearm Lab > Rebuild test scenes** recreates all three lab scenes and their settings; it replaces edits to those scenes. **Verify recognition** checks the four template classes. Keep custom scenes under a different name before rebuilding.

Unity caches, logs, builds, simulator binaries, local credentials and user settings are excluded from Git. The old ZIP is read-only and is not part of this repository.
