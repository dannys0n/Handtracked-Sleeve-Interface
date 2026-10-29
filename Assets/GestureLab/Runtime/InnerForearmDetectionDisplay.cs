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
        [Tooltip("Optional wrist transform. When empty, follows the Meta left wrist joint.")]
        public Transform wristAnchor;
        [Tooltip("Optional elbow transform. Leave empty for wrist-only simulation.")]
        public Transform elbowAnchor;
        public bool openXRWristAxes = true;
        public GestureDrawing drawing;
        public LabInput input;
        public Canvas display;
        public TMP_Text statusText;
        [Range(.04f, .25f)] public float distanceFromWrist = .11f;
        [Range(.01f, .1f)] public float surfaceOffset = .035f;
        public Vector3 rotationOffset;
        public bool TrackingAvailable { get; private set; }
        Transform metaWrist;

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
            bool useMetaWrist = !wristAnchor || wristAnchor == metaWrist;
            if (useMetaWrist)
            {
                metaWrist = HandJointLookup.Find(leftSkeleton, OVRSkeleton.BoneId.XRHand_Wrist, OVRSkeleton.BoneId.Hand_WristRoot);
                wristAnchor = metaWrist;
            }
            var wrist = wristAnchor;
            TrackingAvailable = wrist && (!useMetaWrist || (leftHand && leftHand.IsTracked && leftHand.IsDataValid));
            if (TrackingAvailable)
            {
                // Follow the persistent hand hierarchy, including late rig updates.
                // Do not parent to generated bones, which Meta can rebuild on reacquisition.
                if (useMetaWrist && transform.parent != leftSkeleton.transform) transform.SetParent(leftSkeleton.transform, true);
                bool openXR = useMetaWrist ? leftSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.XRHandLeft : openXRWristAxes;
                var local = WristLocalPose(openXR,
                    distanceFromWrist, surfaceOffset, rotationOffset);
                var rotation = ForearmAnchor.AttachmentRotation(wrist, elbowAnchor, openXR);
                transform.SetPositionAndRotation(wrist.position + rotation * local.position, rotation * local.rotation);
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
