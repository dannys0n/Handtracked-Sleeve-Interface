using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using UnityEngine;
using UnityEngine.Events;

namespace ForearmLab
{
    // Application gestures use raw IHand data. Synthetic grab poses are presentation only.
    public class SleeveManipulation : MonoBehaviour
    {
        public SleeveGridLayout grid;
        public SleeveContactHandLock contactHandLock;
        [SerializeField] MonoBehaviour leftHandSource,rightHandSource;
        public MetaSleeveTouchInput touchInput;
        public HandGrabInteractor rightGrab;
        public HandGrabInteractor leftGrab;
        public FingerFeatureStateProvider leftFingerStates,rightFingerStates;
        [Header("Cuff interaction volume (metres)")]
        [Min(.02f)] public float cuffInwardWidth=.09f;
        [Min(.005f)] public float cuffOutsideWidth=.09f;
        [Min(.005f)] public float contactTolerance=.018f;
        [Min(.01f)] public float gripTolerance=.055f;
        [Min(.02f)] public float rollTravel=.16f;
        [Header("Deliberate roll grasp")]
        [Range(0,60)] public float maxFingerSpreadAngle=22;
        [Range(0,90)] public float minGraspAngleToArm=35;
        [Range(0,90)] public float maxPalmAngleToArm=55;
        [Range(.05f,.8f)] public float rollPoseHoldTime=.25f;
        [Range(.001f,.03f)] public float rollStartTravel=.012f;
        [Header("Gesture targeting")]
        [Min(.005f)] public float pinchTargetDistance=.025f;
        [Min(.1f)] public float poseHoldTime=.12f;
        [Min(.2f)] public float transitionTimeout=2;
        public UnityEvent<SleeveElement> onElementAcquired=new UnityEvent<SleeveElement>();
        public UnityEvent onManipulationStarted=new UnityEvent();
        public UnityEvent onManipulationEnded=new UnityEvent();
        public string Status {get;private set;}="Ready";
        public bool CameraOwnsHands {get;set;}
        public bool OwnsRightHand => mode!=Mode.None||(touchInput&&touchInput.ActiveSlider&&touchInput.ActiveSlider.IsLocked)||
            (touchInput&&touchInput.ActivePressable&&touchInput.ActivePressable.IsTouching)||
            (rightGrab&&rightGrab.State==InteractorState.Select);
        enum Mode{None,Scroll,Roll}
        Mode mode;
        IHand Left=>leftHandSource as IHand;
        IHand Right=>rightHandSource as IHand;
        static readonly HandJointId[] tips={HandJointId.HandThumbTip,HandJointId.HandIndexTip,HandJointId.HandMiddleTip,HandJointId.HandRingTip,HandJointId.HandPinkyTip};
        SleeveElement gestureTarget;
        float lastAngle,startRollZ,startClose,gestureDeadline,stableSince;
        bool previousPinch,needsPinchRelease,waitingForPinch;
        bool disabledGrab;
        float rollCandidateSince,rollCandidateZ;
        bool rollNeedsRelease;
        bool rollFinishing;
        public void SetHandSources(MonoBehaviour left,MonoBehaviour right){leftHandSource=left;rightHandSource=right;}
        bool Valid(IHand hand)=>hand!=null&&hand.IsConnected&&hand.IsTrackedDataValid&&hand.IsHighConfidence;
        bool Joint(IHand hand,HandJointId id,out Vector3 point)
        {point=default;if(hand==null||!hand.GetJointPose(id,out var pose))return false;point=pose.position;return true;}
        bool FingerState(FingerFeatureStateProvider provider,HandFinger finger,string state)
            =>provider&&provider.GetCurrentState(finger,FingerFeature.Curl,out var current)&&current==state;
        bool BentFinger(FingerFeatureStateProvider provider,HandFinger finger)
            =>provider&&provider.GetCurrentState(finger,FingerFeature.Curl,out var current)&&(current=="1"||current=="2");
        bool Open(IHand hand)
        {
            if(!Valid(hand)||hand.GetIndexFingerIsPinching())return false;
            for(int i=1;i<5;i++)if(!FingerState(leftFingerStates,(HandFinger)i,"0"))return false;
            return true;
        }
        bool RollGrasp(IHand hand,bool maintaining,out Vector3 palm)
        {
            palm=Vector3.zero;
            if(hand.GetIndexFingerIsPinching())return false;
            if(!Joint(hand,HandJointId.HandIndex1,out var indexBase)||!Joint(hand,HandJointId.HandMiddle1,out var middleBase)||
               !Joint(hand,HandJointId.HandRing1,out var ringBase)||!Joint(hand,HandJointId.HandPinky1,out var pinkyBase)||
               !Joint(hand,HandJointId.HandIndexTip,out var indexTip)||!Joint(hand,HandJointId.HandMiddleTip,out var middleTip)||
               !Joint(hand,HandJointId.HandRingTip,out var ringTip)||!Joint(hand,HandJointId.HandPinkyTip,out var pinkyTip)||
               !hand.GetJointPose(HandJointId.HandWristRoot,out var wrist))return false;
            palm=(indexBase+middleBase+ringBase+pinkyBase)*.25f;
            // All four fingers must form a relaxed, closed-together cup, not just any fist.
            for(int i=1;i<5;i++)
            {
                if(!BentFinger(rightFingerStates,(HandFinger)i))return false;
            }
            float spread=maxFingerSpreadAngle+(maintaining?10:0);
            if(Vector3.Angle(indexTip-indexBase,middleTip-middleBase)>spread||
               Vector3.Angle(middleTip-middleBase,ringTip-ringBase)>spread||
               Vector3.Angle(ringTip-ringBase,pinkyTip-pinkyBase)>spread)return false;
            // Finger directions can be parallel but spread apart; also constrain tip separation.
            if(Vector3.Distance(indexTip,middleTip)>Vector3.Distance(indexBase,middleBase)*1.4f||
               Vector3.Distance(middleTip,ringTip)>Vector3.Distance(middleBase,ringBase)*1.4f||
               Vector3.Distance(ringTip,pinkyTip)>Vector3.Distance(ringBase,pinkyBase)*1.4f)return false;
            var arm=grid.sleeve.transform.forward;
            var across=(indexTip+middleTip+ringTip+pinkyTip)*.25f-palm;
            float angle=Vector3.Angle(across,arm);angle=Mathf.Min(angle,180-angle);
            if(angle<Mathf.Max(0,minGraspAngleToArm-(maintaining?12:0)))return false;
            var local=grid.sleeve.transform.InverseTransformPoint(palm);
            var inward=-grid.sleeve.transform.TransformDirection(new Vector3(local.x,local.y,0).normalized);
            var normal=wrist.rotation*Constants.RightPalmar;
            if(Vector3.Angle(normal,inward)>maxPalmAngleToArm+(maintaining?15:0))return false;
            return maintaining||InCuff(palm,gripTolerance);
        }
        float CuffZ
        {
            get
            {
                return grid.sleeve.CuffEdgeZ;
            }
        }
        public float CuffContactMinZ => CuffZ - cuffOutsideWidth;
        public float CuffContactMaxZ => Mathf.Min(grid.sleeve.EffectiveLength + CuffZ*.1f, CuffZ + cuffInwardWidth);
        bool InCuff(Vector3 world,float tolerance)
        {
            var s=grid.sleeve;var p=s.transform.InverseTransformPoint(world);
            return p.z>=CuffContactMinZ&&p.z<=CuffContactMaxZ&&
                Mathf.Abs(new Vector2(p.x,p.y).magnitude-s.radius)<=tolerance;
        }
        void Update()
        {
            if(!grid||!grid.sleeve)return;
            if(CameraOwnsHands){EndRight();gestureTarget=null;needsPinchRelease=true;Status="Camera frame";return;}
            UpdateRight();UpdateLeft();
        }
        void UpdateRight()
        {
            if(touchInput&&touchInput.ActiveSlider&&touchInput.ActiveSlider.IsLocked){EndRight();Status="Slider";return;}
            if(touchInput&&touchInput.ActivePressable&&touchInput.ActivePressable.IsTouching){EndRight();Status="Button";return;}
            var hand=Right;
            if(!Valid(hand)||!grid.sleeve.wristAnchor){EndRight();return;}
            if(rightGrab&&rightGrab.State==InteractorState.Select){EndRight();Status="Object grab";return;}
            // Once acquired, hold the captured pose until the tracked contact leaves
            // the constrained path. Finger recognition only acquires the interaction.
            if (mode != Mode.None && contactHandLock && contactHandLock.IsLocked)
            {
                if (!contactHandLock.TryUpdateContact(out var rawContact)) { EndRight(); return; }
                if (mode == Mode.Roll)
                {
                    if (!rollFinishing)
                    {
                        float z = grid.sleeve.transform.InverseTransformPoint(rawContact).z;
                        grid.sleeve.SetRollAmount(startClose + (z-startRollZ)/Mathf.Max(.02f,rollTravel));
                        if (grid.sleeve.IsRollAssisting) { rollFinishing = true; rollNeedsRelease = true; }
                    }
                }
                else
                {
                    float nextAngle = Angle(rawContact);
                    float delta = Mathf.DeltaAngle(lastAngle, nextAngle); lastAngle = nextAngle;
                    if (Mathf.Abs(delta) < 45) grid.ScrollByDegrees(delta);
                }
                return;
            }
            if(!Joint(hand,HandJointId.HandIndexTip,out var index)||!Joint(hand,HandJointId.HandMiddleTip,out var middle))
            {EndRight();return;}
            var contact=(index+middle)*.5f;
            float tolerance=contactTolerance*(mode==Mode.Scroll?1.5f:1);
            bool twoFingerPose=FingerState(rightFingerStates,HandFinger.Index,"0")&&FingerState(rightFingerStates,HandFinger.Middle,"0")&&
                BentFinger(rightFingerStates,HandFinger.Ring)&&BentFinger(rightFingerStates,HandFinger.Pinky);
            bool twoFingers=twoFingerPose&&
                !hand.GetIndexFingerIsPinching()&&InCuff(index,tolerance)&&InCuff(middle,tolerance);
            var palm=contact;
            bool wrapped=!twoFingerPose&&RollGrasp(hand,mode==Mode.Roll,out palm);
            if (contactHandLock) contactHandLock.SetCandidate(wrapped ? palm : contact);
            if (!wrapped) rollNeedsRelease = false;
            if (grid.sleeve.IsRollAssisting) { EndRight(); return; }
            int near=0;foreach(var id in tips)
                if(Joint(hand,id,out var p)&&InCuff(p,gripTolerance))near++;
            var local=grid.sleeve.transform.InverseTransformPoint(palm);
            float angle=Angle(contact);
            if(mode==Mode.None)
            {
                // Scrolling wins immediately. Roll cannot claim the hand until a stable angled
                // grasp is held AND a small intentional movement along the arm has occurred.
                if(twoFingers){rollCandidateSince=0;mode=Mode.Scroll;lastAngle=angle;BeginRight("Scroll cuff",contact);}
                else if(wrapped&&near>=3&&!rollNeedsRelease)
                {
                    if(rollCandidateSince==0){rollCandidateSince=Time.unscaledTime;rollCandidateZ=local.z;}
                    if(Time.unscaledTime-rollCandidateSince>=rollPoseHoldTime&&Mathf.Abs(local.z-rollCandidateZ)>=rollStartTravel)
                    {mode=Mode.Roll;startRollZ=local.z;startClose=grid.sleeve.closeAmount;BeginRight("Roll cuff",palm);}
                }
                else rollCandidateSince=0;
            }
            else if(mode==Mode.Roll)
            {
                if(twoFingerPose||!wrapped){EndRight();return;}
                grid.sleeve.SetRollAmount(startClose+(local.z-startRollZ)/Mathf.Max(.02f,rollTravel));
                if (grid.sleeve.IsRollAssisting) { rollNeedsRelease = true; EndRight(); }
            }
            else
            {
                if(!twoFingers){EndRight();return;}
                float delta=Mathf.DeltaAngle(lastAngle,angle);lastAngle=angle;
                if(Mathf.Abs(delta)<45)grid.ScrollByDegrees(delta);
            }
        }
        float Angle(Vector3 world){var p=grid.sleeve.transform.InverseTransformPoint(world);return Mathf.Atan2(p.y,p.x)*Mathf.Rad2Deg;}
        void BeginRight(string status, Vector3 contact)
        {
            Status=status;if(touchInput)touchInput.suppressForManipulation=true;
            if(rightGrab&&rightGrab.enabled){rightGrab.enabled=false;disabledGrab=true;}
            rollFinishing = false;
            if (contactHandLock && !contactHandLock.Begin(contact, mode == Mode.Roll)) { EndRight(); return; }
            onManipulationStarted.Invoke();
        }
        void EndRight()
        {
            bool active=mode!=Mode.None;mode=Mode.None;
            if (contactHandLock) contactHandLock.Release();
            rollFinishing = false;
            rollCandidateSince=0;
            if(touchInput)touchInput.suppressForManipulation=false;
            if(disabledGrab){if(rightGrab)rightGrab.enabled=true;disabledGrab=false;}
            if(active){Status="Ready";onManipulationEnded.Invoke();}
        }
        void UpdateLeft()
        {
            var hand=Left;
            if(leftGrab&&leftGrab.State==InteractorState.Select){gestureTarget=null;stableSince=0;needsPinchRelease=true;return;}
            if(!Valid(hand)||!grid.sleeve.wristAnchor)
            {gestureTarget=null;previousPinch=false;needsPinchRelease=true;stableSince=0;return;}
            bool pinch=hand.GetIndexFingerIsPinching();
            if(!hand.GetFingerIsHighConfidence(HandFinger.Index)||!hand.GetFingerIsHighConfidence(HandFinger.Thumb))
            {gestureTarget=null;needsPinchRelease=true;return;}
            if(needsPinchRelease){if(!pinch)needsPinchRelease=false;previousPinch=pinch;return;}
            bool began=pinch&&!previousPinch;previousPinch=pinch;
            if(!gestureTarget)
            {
                if(!began||grid.sleeve.closeAmount>=.01f)return;
                if(!Joint(hand,HandJointId.HandIndexTip,out var index)||!Joint(hand,HandJointId.HandThumbTip,out var thumb))return;
                var found=FindPinchTarget((index+thumb)*.5f,out _);
                if(!found)return;
                gestureTarget=found;waitingForPinch=false;gestureDeadline=Time.unscaledTime+transitionTimeout;stableSince=0;
                Status="Element acquired: open left hand";onElementAcquired.Invoke(found);return;
            }
            if(!gestureTarget.isActiveAndEnabled||Time.unscaledTime>gestureDeadline||grid.sleeve.closeAmount>=.01f)
            {gestureTarget=null;stableSince=0;Status="Ready";return;}
            if(!waitingForPinch)
            {
                if(!Open(hand)){stableSince=0;return;}
                if(stableSince==0)stableSince=Time.unscaledTime;
                if(Time.unscaledTime-stableSince<poseHoldTime)return;
                grid.Focus(gestureTarget);waitingForPinch=true;stableSince=0;gestureDeadline=Time.unscaledTime+transitionTimeout;
                Status="Expanded: pinch left hand to restore";
            }
            else if(began)
            {if(grid.FocusedElement==gestureTarget)grid.ClearFocus();gestureTarget=null;needsPinchRelease=true;Status="Ready";}
        }
        // Debug and selection share the exact pinch point and surface-distance query.
        public bool TryGetLeftContact(out Vector3 point, out Vector3 surfacePoint, out bool hasTarget)
        {
            point=surfacePoint=default;hasTarget=false;
            if(!Valid(Left)||!Joint(Left,HandJointId.HandIndexTip,out var index)||
                !Joint(Left,HandJointId.HandThumbTip,out var thumb))return false;
            point=(index+thumb)*.5f;
            hasTarget=FindPinchTarget(point,out surfacePoint);
            return true;
        }
        SleeveElement FindPinchTarget(Vector3 point,out Vector3 surfacePoint)
        {
            surfacePoint=point;
            if(!grid||!grid.sleeve||grid.sleeve.closeAmount>=.01f)return null;
            float best=pinchTargetDistance;SleeveElement found=null;
            foreach(var e in grid.GetComponentsInChildren<SleeveElement>())
            {
                if(e.isEmptyPlaceholder||!e.panel||!e.panel.enabled)continue;
                var surface=e.panel.GetComponent<SleevePokeSurface>();
                if(surface&&surface.ClosestSurfacePoint(point,out var hit,best))
                {best=hit.Distance;found=e;surfacePoint=hit.Point;}
            }
            return found;
        }
        void OnDisable(){EndRight();gestureTarget=null;needsPinchRelease=true;}
    }
}
