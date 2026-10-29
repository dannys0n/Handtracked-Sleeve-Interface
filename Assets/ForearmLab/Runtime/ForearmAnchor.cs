using UnityEngine;

namespace ForearmLab
{
    // Wrist-only for hand simulation; an optional elbow supplies the forearm direction.
    [DefaultExecutionOrder(1000)]
    public class ForearmAnchor : MonoBehaviour
    {
        public OVRSkeleton leftSkeleton;
        [Tooltip("Optional wrist transform. When empty, follows the Meta left wrist joint.")]
        public Transform wristAnchor;
        [Tooltip("Optional elbow transform. Leave empty for wrist-only hand simulation.")]
        public Transform elbowAnchor;
        [Tooltip("Axis convention for an explicitly assigned wrist; Meta joints use their skeleton's convention automatically.")]
        public bool openXRWristAxes = true;
        public Transform preview;
        public bool desktop;
        public Vector3 wristLocalOffset = new Vector3(0, .045f, -.12f);
        public Vector3 rotationOffset;

        Transform metaWrist;

        void OnEnable() => Application.onBeforeRender += Follow;
        void OnDisable() => Application.onBeforeRender -= Follow;
        void LateUpdate() => Follow();

        [BeforeRenderOrder(100)]
        void Follow()
        {
            if (desktop) return;
            bool useMetaWrist = !wristAnchor || wristAnchor == metaWrist;
            if (useMetaWrist)
            {
                metaWrist = HandJointLookup.Find(leftSkeleton, OVRSkeleton.BoneId.XRHand_Wrist, OVRSkeleton.BoneId.Hand_WristRoot);
                wristAnchor = metaWrist;
            }
            var wrist = wristAnchor;
            if (preview) preview.gameObject.SetActive(wrist != null);
            if (!wrist) return;
            bool openXR = useMetaWrist ? leftSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.XRHandLeft : openXRWristAxes;
            var rotation = AttachmentRotation(wrist, elbowAnchor, openXR);
            transform.SetPositionAndRotation(wrist.position + rotation * wristLocalOffset, rotation * Quaternion.Euler(rotationOffset));
        }

        public static Quaternion AttachmentRotation(Transform wrist, Transform elbow, bool openXR)
        {
            if (!elbow || (wrist.position - elbow.position).sqrMagnitude < .000001f) return wrist.rotation;
            // Keep the wrist's roll while aligning the lengthwise axis with elbow -> wrist.
            var distal = wrist.rotation * (openXR ? Vector3.forward : Vector3.left);
            var direction = (wrist.position - elbow.position).normalized;
            var delta = Vector3.Dot(distal, direction) < -.9999f
                ? Quaternion.AngleAxis(180, wrist.rotation * (openXR ? Vector3.down : Vector3.up))
                : Quaternion.FromToRotation(distal, direction);
            return delta * wrist.rotation;
        }
    }
}
