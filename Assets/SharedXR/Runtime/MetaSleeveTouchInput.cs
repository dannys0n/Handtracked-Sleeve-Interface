using Oculus.Interaction;
using UnityEngine;

namespace ForearmLab
{
    // Reuse the tracked Meta index joint directly; no second hand rig or gesture detector.
    [DefaultExecutionOrder(-100)]
    public class MetaSleeveTouchInput : MonoBehaviour
    {
        public OVRSkeleton rightSkeleton;
        public PokeInteractor interactor;
        void Update()
        {
            if(!interactor)return;
            var tip=HandJointLookup.Find(rightSkeleton,OVRSkeleton.BoneId.XRHand_IndexTip,OVRSkeleton.BoneId.Hand_IndexTip);
            if(tip)interactor.InjectPointTransform(tip);
            interactor.enabled=tip;
        }
        void OnDisable(){if(interactor)interactor.enabled=false;}
    }
}
