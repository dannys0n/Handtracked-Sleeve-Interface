using UnityEngine;
using UnityEngine.InputSystem;

namespace ForearmLab
{
    public class LabInput : MonoBehaviour
    {
        public GestureDrawing drawing;
        public bool desktop;
        public OVRHand rightHand;
        public OVRHand leftHand;
        public OVRSkeleton rightSkeleton;
        public float desktopDepth = .7f;
        public bool recognizeOnRelease;
        public bool showOverlay = true;
        public RectTransform desktopDrawArea;
        bool desktopStrokeAllowed;
        bool wasPinching;
        bool wasConfirming;
        string pose = "Waiting for hand tracking";
        public string PoseStatus => pose;

        public static Transform Bone(OVRSkeleton skeleton, OVRSkeleton.BoneId modern, OVRSkeleton.BoneId legacy)
        {
            return HandJointLookup.Find(skeleton, modern, legacy);
        }

        void Update()
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            bool editingName = events && events.currentSelectedGameObject && events.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>();
            var kb = Keyboard.current;
            if (kb != null && !editingName)
            {
                if (kb.cKey.wasPressedThisFrame) { drawing.Clear(); wasPinching = false; }
                if (kb.enterKey.wasPressedThisFrame) drawing.Recognize();
            }
            Vector3 position = default;
            bool pinching = false;
            if (desktop && Mouse.current != null)
            {
                var mouse = Mouse.current.position.ReadValue();
                bool inside = desktopDrawArea ? RectTransformUtility.RectangleContainsScreenPoint(desktopDrawArea, mouse) : !(mouse.x < 440 && mouse.y > Screen.height - 230);
                if (inside) desktopDepth = Mathf.Clamp(desktopDepth + Mouse.current.scroll.ReadValue().y * .0005f, .25f, 2);
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    // The recording scene has an explicit drawing region. EventSystem's
                    // cached pointer target can still describe the previous frame's sidebar.
                    desktopStrokeAllowed = inside && (desktopDrawArea || !events || !events.IsPointerOverGameObject());
                if (!Mouse.current.leftButton.isPressed) desktopStrokeAllowed = false;
                position = drawing.view.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, desktopDepth));
                // Leave the controls overlay clickable without producing ink.
                pinching = Mouse.current.leftButton.isPressed && desktopStrokeAllowed && inside;
                pose = "Desktop mouse";
            }
            else if (!desktop)
            {
                bool confirming = leftHand && leftHand.IsTracked && leftHand.IsDataValid && leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
                if (confirming && !wasConfirming) drawing.Recognize();
                wasConfirming = confirming;
                var tip = Bone(rightSkeleton, OVRSkeleton.BoneId.XRHand_IndexTip, OVRSkeleton.BoneId.Hand_IndexTip);
                bool tracked = rightHand && rightHand.IsTracked && rightHand.IsDataValid && tip;
                if (tracked)
                {
                    position = tip.position;
                    // Hysteresis avoids demanding the old project's exact strength == 1.
                    var strength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
                    pinching = strength >= (wasPinching ? .55f : .8f);
                    pose = pinching ? "Index pinch" : rightHand.GetFingerIsPinching(OVRHand.HandFinger.Middle) ? "Middle pinch" : "Open / pointing";
                    if (rightHand.GetFingerIsPinching(OVRHand.HandFinger.Middle)) { drawing.Clear(); pinching = false; }
                }
                else pose = "Tracking lost / waiting for right hand";
            }
            if (pinching && !wasPinching) drawing.BeginStroke(position);
            else if (pinching) drawing.AddPoint(position);
            else if (wasPinching)
            {
                drawing.EndStroke();
                if (recognizeOnRelease) drawing.Recognize();
            }
            wasPinching = pinching;
        }

        void OnDisable() { if (drawing) drawing.EndStroke(); wasPinching = false; }

        void OnGUI()
        {
            if (!showOverlay) return;
            GUILayout.BeginArea(new Rect(12, 12, 420, 210), GUI.skin.box);
            GUILayout.Label("FOREARM LAB  /  " + (desktop ? "DESKTOP" : "META XR"));
            GUILayout.Label(pose);
            GUILayout.Label(desktop ? "Hold left mouse: draw  |  Wheel: depth" : "Right index: draw | Right middle: clear | Left index: recognize");
            GUILayout.Label("Multiple strokes supported. Enter: recognize  |  C: clear");
            GUILayout.Label(drawing.Status + $"  [{drawing.PointCount} points]");
            recognizeOnRelease = GUILayout.Toggle(recognizeOnRelease, "Recognize after each stroke (off for X / T)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Recognize")) drawing.Recognize();
            if (GUILayout.Button("Clear")) drawing.Clear();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
