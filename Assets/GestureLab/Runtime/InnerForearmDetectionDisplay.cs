using TMPro;
using UnityEngine;

namespace ForearmLab
{
    // Fixed pose in the tracked wrist frame; finger articulation must not move the UI.
    [DefaultExecutionOrder(1000)]
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

        void OnEnable() => Application.onBeforeRender += BeforeRender;
        void OnDisable() => Application.onBeforeRender -= BeforeRender;

        [BeforeRenderOrder(100)]
        void BeforeRender() => FollowWrist();

        void LateUpdate()
        {
            FollowWrist();
            RefreshText();
        }

        public static Pose WristLocalPose(bool openXR, float distance, float clearance, Vector3 eulerOffset)
        {
            // Meta OpenXRHandPrimitives / OVRHandPrimitives: left distal and palmar.
            var distal = openXR ? Vector3.forward : Vector3.left;
            var palmar = openXR ? Vector3.down : Vector3.up;
            return new Pose(-distal * distance + palmar * clearance,
                Quaternion.LookRotation(-palmar, distal) * Quaternion.Euler(eulerOffset));
        }

        void FollowWrist()
        {
            var wrist = HandJointLookup.Find(leftSkeleton, OVRSkeleton.BoneId.XRHand_Wrist, OVRSkeleton.BoneId.Hand_WristRoot);
            TrackingAvailable = leftHand && leftHand.IsTracked && leftHand.IsDataValid && wrist;
            if (TrackingAvailable)
            {
                // Follow the persistent hand hierarchy, including late rig updates.
                // Do not parent to generated bones, which Meta can rebuild on reacquisition.
                if (transform.parent != leftSkeleton.transform) transform.SetParent(leftSkeleton.transform, true);
                var local = WristLocalPose(leftSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.XRHandLeft,
                    distanceFromWrist, surfaceOffset, rotationOffset);
                transform.SetPositionAndRotation(wrist.position + wrist.rotation * local.position, wrist.rotation * local.rotation);
            }
            if (display) display.gameObject.SetActive(TrackingAvailable);
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
