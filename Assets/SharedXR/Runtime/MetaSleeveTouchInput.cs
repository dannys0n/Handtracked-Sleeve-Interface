using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace ForearmLab
{
    // Reuse the tracked Meta index joint directly; no second hand rig or gesture detector.
    [DefaultExecutionOrder(-100)]
    public class MetaSleeveTouchInput : MonoBehaviour
    {
        public OVRSkeleton rightSkeleton;
        [Tooltip("Raw Interaction SDK hand used by the rig. Keeps touch in the same frame as visuals and the sleeve.")]
        public MonoBehaviour handSource;
        public SyntheticHand visualHand;
        public SleeveSliderFingerLock ActiveSlider { get; private set; }
        public SleevePressable ActivePressable { get; private set; }
        public bool TryClaimPressable(SleevePressable pressable)
        {
            if(ActiveSlider||(ActivePressable&&ActivePressable!=pressable))return false;
            if(manipulation&&manipulation.OwnsRightHand&&!ActivePressable)return false;
            ActivePressable=pressable;return true;
        }
        public void ReleasePressable(SleevePressable pressable){if(ActivePressable==pressable)ActivePressable=null;}
        public bool TryClaimSlider(SleeveSliderFingerLock slider)
        {if(ActivePressable||(ActiveSlider&&ActiveSlider!=slider))return false;ActiveSlider=slider;return true;}
        public void ReleaseSlider(SleeveSliderFingerLock slider){if(ActiveSlider==slider)ActiveSlider=null;}
        public Transform indexTip;
        public PokeInteractor interactor;
        [System.NonSerialized] public bool suppressForManipulation;
        [System.NonSerialized] public bool suppressForCamera;
        public SleeveManipulation manipulation;
        void OnEnable(){if(handSource is IHand hand)hand.WhenHandUpdated+=Update;}
        void Update()
        {
            if(!interactor)return;
            if(handSource)
            {
                var hand=handSource as IHand;
                bool valid=hand!=null&&hand.IsConnected&&hand.IsTrackedDataValid&&indexTip;
                if(valid&&hand.GetJointPose(HandJointId.HandIndexTip,out var pose))
                {indexTip.SetPositionAndRotation(pose.position,pose.rotation);interactor.InjectPointTransform(indexTip);}
                else valid=false;
                interactor.enabled=valid&&!suppressForCamera&&!suppressForManipulation&&(!manipulation||!manipulation.OwnsRightHand||ActiveSlider||ActivePressable);return;
            }
            var tip=HandJointLookup.Find(rightSkeleton,OVRSkeleton.BoneId.XRHand_IndexTip,OVRSkeleton.BoneId.Hand_IndexTip);
            if(tip)interactor.InjectPointTransform(tip);
            interactor.enabled=tip&&!suppressForCamera&&!suppressForManipulation&&(!manipulation||!manipulation.OwnsRightHand||ActiveSlider||ActivePressable);
        }
        void OnDisable()
        {
            if(ActiveSlider)ActiveSlider.Release();
            if(ActivePressable)ActivePressable.Release();
            if(handSource is IHand hand)hand.WhenHandUpdated-=Update;
            if(interactor)interactor.enabled=false;
        }
    }
}
