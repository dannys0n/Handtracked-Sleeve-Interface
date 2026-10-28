using TMPro;
using UnityEngine;

namespace ForearmLab
{
    // Left palm basis avoids depending on the different legacy/OpenXR wrist axes.
    public class InnerForearmDetectionDisplay : MonoBehaviour
    {
        public OVRHand leftHand;
        public OVRSkeleton leftSkeleton;
        public GestureDrawing drawing;
        public LabInput input;
        public Canvas display;
        public TMP_Text statusText;
        [Range(.04f, .25f)] public float distanceFromWrist = .11f;
        [Range(.01f, .1f)] public float surfaceOffset = .035f;
        public Vector3 rotationOffset;
        public bool TrackingAvailable { get; private set; }

        void LateUpdate()
        {
            var wrist = LabInput.Bone(leftSkeleton, OVRSkeleton.BoneId.XRHand_Wrist, OVRSkeleton.BoneId.Hand_WristRoot);
            var index = LabInput.Bone(leftSkeleton, OVRSkeleton.BoneId.XRHand_IndexMetacarpal, OVRSkeleton.BoneId.Hand_Index1);
            var little = LabInput.Bone(leftSkeleton, OVRSkeleton.BoneId.XRHand_LittleMetacarpal, OVRSkeleton.BoneId.Hand_Pinky1);
            TrackingAvailable = leftHand && leftHand.IsTracked && leftHand.IsDataValid && wrist && index && little;
            if (TrackingAvailable)
            {
                var fingers = ((index.position + little.position) * .5f - wrist.position).normalized;
                var across = (index.position - little.position).normalized;
                var palmNormal = Vector3.Cross(fingers, across).normalized;
                TrackingAvailable = palmNormal.sqrMagnitude > .5f;
                if (TrackingAvailable)
                    transform.SetPositionAndRotation(wrist.position - fingers * distanceFromWrist + palmNormal * surfaceOffset,
                        Quaternion.LookRotation(-palmNormal, fingers) * Quaternion.Euler(rotationOffset));
            }
            if (display) display.gameObject.SetActive(TrackingAvailable);
            RefreshText();
        }

        public void RefreshText()
        {
            if (!statusText || !drawing || !input) return;
            statusText.text = "GESTURE XR\n" + input.PoseStatus + "\n" +
                (drawing.IsDrawing ? "Drawing" : "Ready") + $"  |  {drawing.PointCount} points\n\n" +
                drawing.Status + "\n\nRight index: draw\nRight middle: clear\nLeft index: recognize";
        }
    }
}
