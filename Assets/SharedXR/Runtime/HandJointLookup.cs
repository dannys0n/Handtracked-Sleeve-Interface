using UnityEngine;

namespace ForearmLab
{
    public static class HandJointLookup
    {
        public static Transform Find(OVRSkeleton skeleton, OVRSkeleton.BoneId modern, OVRSkeleton.BoneId legacy)
        {
            if (!skeleton || !skeleton.IsDataValid) return null;
            // IDs overlap across formats. Never search for both in the same bone list.
            var type = skeleton.GetSkeletonType();
            bool openXR = type == OVRSkeleton.SkeletonType.XRHandLeft || type == OVRSkeleton.SkeletonType.XRHandRight;
            if (!openXR && type != OVRSkeleton.SkeletonType.HandLeft && type != OVRSkeleton.SkeletonType.HandRight) return null;
            var target = openXR ? modern : legacy;
            foreach (var bone in skeleton.Bones)
                if (bone.Id == target) return bone.Transform;
            return null;
        }
    }
}
