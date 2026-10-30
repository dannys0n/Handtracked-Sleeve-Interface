# Forearm Gesture Lab

**A wearable spatial interface for Meta Quest, controlled with your hands.**

Forearm Gesture Lab explores the space between the wrist and elbow as a personal workspace in VR and mixed reality. A flexible interface wraps around the left forearm, placing controls, images, and interactive objects within reach of the opposite hand.

The goal is to explore familiar physical actions—touching, scrolling, grasping, and rolling a sleeve—as ways to manage spatial interfaces. This is an experimental Unity prototype focused on interaction design and reusable XR components.

<!-- Replace this placeholder with a hero GIF or image. -->
> **Showcase preview** · GIF / image placeholder

## Interaction showcase

### Scroll around the arm

Drag around the cuff with two fingers to browse elements across the sleeve’s full circumference. The interface stays attached to the arm while its contents move around it.

**Potential use:** browse tools, media, or shortcuts without filling the surrounding space with floating panels.

<!-- Add the sleeve scrolling GIF here. -->
> **Scrolling demo** · GIF / image placeholder

### Roll the workspace away

Grasp the cuff and move along the forearm to fold the interface closed or reopen it. Separate thresholds assist the end of each motion. Closing leaves a visible cuff to grab again, while a shared diamond-fold deformation affects the sleeve and its contents.

**Potential use:** put controls away when they are no longer needed, with a physical cue for bringing them back.

<!-- Add the sleeve rolling GIF here. -->
> **Rolling demo** · GIF / image placeholder

### Touch buttons and toggles

Single-action button and toggle tiles fill their elements. Raised faces depress over a short travel distance, activate at a configurable press depth, and use cyan pressed feedback. The showcase includes a passthrough toggle and a drawing-mode toggle.

**Potential use:** keep frequently used actions and mode switches on the body, within direct reach.

<!-- Add the button and toggle interaction GIF here. -->
> **Button demo** · GIF / image placeholder

### Adjust continuous controls

The volume slider runs along the forearm, from elbow toward wrist. During interaction, the displayed finger follows the slider handle, helping communicate which control has been acquired.

**Potential use:** adjust volume or other continuous values without opening a separate settings window.

<!-- Add the slider interaction GIF here. -->
> **Slider demo** · GIF / image placeholder

### Capture, resize, and store an image

Form an L with each hand to frame a region of the scene, then close both thumbs to capture it. Grab the resulting image card with either hand, or use both hands to resize it.

Bringing the card near the sleeve opens a narrow storage slot that turns toward the approaching image. Contact aligns the card for insertion; pushing it into the slot stores it on the sleeve. The holding hand hides during insertion. Releasing beyond halfway completes storage, while an early release or withdrawal cancels it.

**Potential use:** collect visual references in a personal workspace, then retrieve them as part of a larger spatial workflow.

<!-- Add the end-to-end capture, resize, and insertion GIF here. -->
> **Image workflow demo** · GIF / image placeholder

### Draw and recognize gestures

Enable **DRAW** on the sleeve to draw persistent strokes in space. Right thumb/index pinch draws, right thumb/middle pinch clears, and left thumb/index pinch recognizes. On-arm buttons also provide recognition and clear actions. The front display shows the recognition result.

The prototype recognizes X, T, O, and M using a shared template library. Drawings remain visible for the current session when drawing mode is disabled, so they can be included in a scene capture.

**Potential use:** sketch a reference or explore drawn symbols as shortcuts for interface actions.

<!-- Add the drawing and recognition GIF here. -->
> **Drawing demo** · GIF / image placeholder

## Technical focus

- **Reusable sleeve architecture:** a baseline prefab accepts a wrist transform and an optional elbow transform, with adjustable radius and length. A demo prefab variant supplies example content.
- **Composable UI:** standard Unity uGUI controls, RectTransforms, layout groups, and UnityEvents sit inside generic sleeve elements. Elements can contain UI or 3D content, expand into focus, and project outward from the sleeve.
- **Curved presentation:** UI geometry wraps around a cylindrical workspace. Projection distance, grid layout, and the closing deformation are separate controls.
- **Meta interaction integration:** hand tracking, poke interactions, and object grabs use the Meta XR SDK. Interaction ownership coordinates drawing, cuff manipulation, touch, and two-hand capture. Visual hand constraints remain separate from raw tracking input.
- **Gesture tooling:** a desktop recorder saves custom XML samples into the same library used by the XR recognizer. Spatial strokes are projected onto a drawing plane and classified with the $P point-cloud recognizer.

## Run the prototype

**Stack:** Unity **6000.6.7f1**, Meta XR SDK **207.0.1**, OpenXR **1.18.0**, Universal Render Pipeline.

1. Open the repository in Unity Hub with the editor version above and let packages resolve.
2. Connect a Meta Quest through Link or Air Link with hand tracking enabled. Deactivate Meta XR Simulator when using a headset.
3. Choose **Forearm Lab → Open showcase**, then press **Play**.

The showcase starts in VR. Use the sleeve’s passthrough toggle or press **P** to switch modes. Press **R** to recenter the recognition display and **F8** to save a Game-view snapshot.

For headset-free drawing, choose **Forearm Lab → Open desktop lab**. Hold the left mouse button to draw, press **Enter** to recognize, and **C** to clear. **Gesture Lab → Open recording scene** opens the template recorder.

## Project layout

| Location | Purpose |
| --- | --- |
| `Assets/ForearmLab/Scenes/ForearmShowcase.unity` | Main interaction showcase |
| `Assets/ForearmLab/Scenes/ForearmXR.unity` | Forearm interaction lab |
| `Assets/ForearmLab/Prefabs/` | Sleeve, element, and captured-image prefabs |
| `Assets/GestureLab/` | Drawing, recognition, desktop recording, and gesture templates |
| `Assets/SharedXR/` | Tracking adapters, interaction coordination, and environment modes |

## Prototype scope

This is a standalone Unity application exploring forearm interaction, rather than a modification of Horizon OS. Gesture tolerances, ergonomics, and tracking reliability remain areas for iteration.

Scene captures contain Unity-rendered content; real passthrough imagery is excluded. Elbow attachment uses body data when the runtime supplies it and falls back to wrist-only attachment otherwise; it does not provide a measured Quest 2 elbow. Drawing recognition compares projected 2D shapes, rather than recognizing volumetric 3D gestures.
