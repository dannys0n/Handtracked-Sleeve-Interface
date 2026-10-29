using UnityEngine;

namespace ForearmLab
{
    // Adapter outside the sleeve prefab: Meta joint -> stable, OpenXR-oriented wrist transform.
    [DefaultExecutionOrder(900)]
    public class MetaSleeveWristBinding : MonoBehaviour
    {
        public OVRSkeleton skeleton;
        public WristSleeve sleeve;

        void OnEnable() => Application.onBeforeRender += UpdateWrist;
        void OnDisable()
        {
            Application.onBeforeRender -= UpdateWrist;
            if (sleeve && sleeve.wristAnchor == transform) sleeve.wristAnchor = null;
        }
        void LateUpdate() => UpdateWrist();

        [BeforeRenderOrder(90)]
        void UpdateWrist()
        {
            if (!sleeve) return;
            var wrist = HandJointLookup.Find(skeleton, OVRSkeleton.BoneId.XRHand_Wrist, OVRSkeleton.BoneId.Hand_WristRoot);
            if (!wrist) { sleeve.wristAnchor = null; return; }
            bool openXR = skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.XRHandLeft;
            var rotation = openXR ? wrist.rotation :
                wrist.rotation * Quaternion.LookRotation(Vector3.left, Vector3.down);
            transform.SetPositionAndRotation(wrist.position, rotation);
            sleeve.wristAnchor = transform;
        }
    }
}
