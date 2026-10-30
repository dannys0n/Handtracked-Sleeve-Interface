using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Interaction;

namespace ForearmLab
{
    // Adapter outside the sleeve prefab: Meta joint -> stable, OpenXR-oriented wrist transform.
    [DefaultExecutionOrder(900)]
    public class MetaSleeveWristBinding : MonoBehaviour
    {
        public OVRSkeleton skeleton;
        [Tooltip("Preferred raw Interaction SDK hand, in the rig's tracking-to-world frame. Skeleton is a fallback for older scenes.")]
        public MonoBehaviour handSource;
        [Tooltip("Visible SDK hand. Attachment uses its wrist position AND rotation as one pose; raw tracking is the fallback.")]
        public HandVisual handVisual;
        public WristSleeve sleeve;
        IHand Hand=>handSource as IHand;

        void OnEnable()
        {
            Application.onBeforeRender += UpdateWrist;
            if(Hand!=null)Hand.WhenHandUpdated+=UpdateWrist;
            if(handVisual)handVisual.WhenHandVisualUpdated+=UpdateWrist;
        }
        void OnDisable()
        {
            Application.onBeforeRender -= UpdateWrist;
            if(Hand!=null)Hand.WhenHandUpdated-=UpdateWrist;
            if(handVisual)handVisual.WhenHandVisualUpdated-=UpdateWrist;
            if (sleeve && sleeve.wristAnchor == transform) sleeve.wristAnchor = null;
        }
        void LateUpdate() => UpdateWrist();

        [BeforeRenderOrder(90)]
        void UpdateWrist()
        {
            if (!sleeve) return;
            if(handSource)
            {
                // GetJointPose is already in world space. Applying the old anchor hierarchy
                // or another tracking-origin transform here would offset it a second time.
                var hand=Hand;
                if(hand==null||!hand.IsConnected||!hand.IsTrackedDataValid||!hand.GetJointPose(HandJointId.HandWristRoot,out var pose))
                {sleeve.wristAnchor=null;return;}
                // The final visual may include the SDK's filtered/synthetic hand stages.
                // Use one complete pose. Combining a filtered visual position with a newer
                // raw rotation makes an arm-length attachment orbit the wrong wrist frame.
                if(handVisual&&handVisual.IsVisible&&handVisual.Joints.Count>(int)HandJointId.HandWristRoot)
                {
                    var renderedWrist=handVisual.GetTransformByHandJointId(HandJointId.HandWristRoot);
                    if(renderedWrist)pose=new Pose(renderedWrist.position,renderedWrist.rotation);
                }
                transform.SetPositionAndRotation(pose.position,pose.rotation);
                sleeve.wristAnchor=transform;return;
            }
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
