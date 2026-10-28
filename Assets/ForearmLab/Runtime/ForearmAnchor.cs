using UnityEngine;

namespace ForearmLab
{
    // Wrist-relative placeholder for later wrapped UI. Hand tracking alone does not measure the elbow.
    public class ForearmAnchor : MonoBehaviour
    {
        public OVRSkeleton leftSkeleton;
        public Transform preview;
        public bool desktop;
        public Vector3 wristLocalOffset = new Vector3(0, .045f, -.12f);
        public Vector3 rotationOffset;

        void LateUpdate()
        {
            if (desktop) return;
            var wrist = LabInput.Bone(leftSkeleton, OVRSkeleton.BoneId.XRHand_Wrist, OVRSkeleton.BoneId.Hand_WristRoot);
            preview.gameObject.SetActive(wrist != null);
            if (!wrist) return;
            transform.SetPositionAndRotation(wrist.TransformPoint(wristLocalOffset), wrist.rotation * Quaternion.Euler(rotationOffset));
        }
    }
}
