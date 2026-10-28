# Forearm tracking research - October 28, 2026

This is a design note, not a claim that body/elbow tracking is implemented or validated in this project.

## Hand tracking

Meta recommends Interaction SDK for hand interactions. OpenXR hand tracking exposes a 26-joint hand including wrist and palm, but no forearm or elbow. The older OVR hand skeleton has a forearm entry; that is not evidence of a separately tracked elbow. Use the active skeleton's named joints and coordinate conventions, not interchangeable integer IDs. [Meta hand setup](https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/)

Wrist and palm poses include rotation. Meta's OpenXR left-hand conventions in the installed SDK use distal +Z and palmar -Y; the legacy skeleton differs. Prefer tracked joint rotations and SDK hand-space constants over a normal reconstructed from articulating fingers. A hand aim/pointer pose is an interaction ray, not an anatomical palm/forearm attachment frame. [Meta hand-space directions](https://developers.meta.com/vr/documentation/unity/unity-isdk-openxr-custom-components/)

## Elbow and body tracking

The documented Meta path is Body Tracking / Movement SDK, with `OVRBody` providing poses in tracking space. IOBT is requested using High body-tracking fidelity. Availability is device/runtime dependent: the docs distinguish Quest 3 IOBT from Quest 2 and warn that low-fidelity fallback can lose elbow tracking even while a body skeleton is still returned. Tracking support, fidelity and permissions must be checked; a returned joint is not an accuracy guarantee. [Meta body tracking](https://developers.meta.com/vr/documentation/unity/move-body-tracking/)

The installed Core SDK names include `Body_LeftArmLower`, `Body_LeftHandWristTwist`, and `Body_LeftHandWrist`. Check the chosen body joint set and skeleton definitions when wiring these, rather than treating hand-joint IDs as body-joint IDs. Hand and body data must use the same tracking-space transform and compatible updates.

## Implications for this interface (project design inference)

A constant offset from the wrist is a useful wrist-mounted prototype. It cannot distinguish bending the wrist from changing forearm direction and cannot place a reliable wrist-to-elbow sleeve on its own. For the intended forearm surface, use valid elbow and wrist positions to define the arm segment; use wrist-twist/body orientation and calibrated palm-facing alignment to determine its roll. Two endpoint positions establish an axis, but not twist around it. Choose and test a physical forearm-length/radius calibration; hand tracking does not reconstruct the user's forearm skin surface.

Prefer raw anatomical tracking for attachment over a retargeted/synthetic hand that an interaction system can reposition to fit a virtual object. If elbow data is unavailable, explicitly use a wrist-only mode rather than claiming the forearm is tracked. Smoothing, if needed after correct coordinates/lifecycle are verified, trades jitter against lag and does not recover missing elbow information.

## Simulator limits

Webcam mode retargets 21 hand landmarks to 26 OpenXR joints, synthesizes unseen joints, estimates depth from palm width, and supplies head rotation. It does not document webcam-derived elbow tracking. [Meta camera tracking](https://developers.meta.com/vr/documentation/unity/xrsim-camera-tracking/)

Meta's separate body-simulation guide exercises a body sample with automatic motion. It is useful for integration, not proof that webcam input measures the user's elbow. [Meta body simulator guide](https://developers.meta.com/vr/documentation/unity/xrsim-body-tracking/)

Next recommended experiment: add a dedicated body-tracking diagnostic in ForearmLab that shows wrist, elbow and wrist-twist frames plus actual validity/fidelity on the target Quest. Evaluate wrist flexion, palm rotation, occlusion and reacquisition before building a wrapped UI surface. No body package, permission or scene behavior was added as part of this research/organization task.
