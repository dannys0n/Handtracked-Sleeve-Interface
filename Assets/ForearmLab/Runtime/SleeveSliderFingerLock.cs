using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForearmLab
{
    [DefaultExecutionOrder(1300)]
    public class SleeveSliderFingerLock : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEndDragHandler
    {
        public Slider slider;
        public SleeveElement element;
        public MetaSleeveTouchInput touchInput;
        [Min(.005f)] public float releaseDistance=.04f;
        public bool IsLocked { get; private set; }
        public Vector3 HandlePoint { get; private set; }
        SyntheticHand visual;
        IHand Raw=>touchInput?touchInput.handSource as IHand:null;
        Vector3 tipInWrist;
        Quaternion wristInSleeve;
        bool disabledGrab;
        void Start()
        {
            if(!slider)slider=GetComponent<Slider>();
            if(!element)element=GetComponentInParent<SleeveElement>();
            if(!touchInput)touchInput=FindAnyObjectByType<MetaSleeveTouchInput>();
        }
        void OnEnable()=>Application.onBeforeRender+=Refresh;
        public Vector3 GetHandlePoint()
        {
            var canvas=(RectTransform)element.panel.transform;
            var point=canvas.InverseTransformPoint(slider.handleRect.TransformPoint(slider.handleRect.rect.center));
            return element.panel.GetComponent<SleevePokeSurface>().CanvasToWorld(point,.001f);
        }
        public void OnPointerDown(PointerEventData data)
        {
            if(data.button!=PointerEventData.InputButton.Left||IsLocked||!slider||!slider.IsInteractable()||
                !element||!element.Sleeve||!touchInput||touchInput.suppressForCamera||touchInput.suppressForManipulation)return;
            visual=touchInput.visualHand;
            var hand=Raw;
            if(!visual||!visual.Started||hand==null||!hand.IsConnected||!hand.IsTrackedDataValid||!hand.IsHighConfidence||
                !hand.GetRootPose(out var wrist)||!hand.GetJointPose(HandJointId.HandIndexTip,out var tip))return;
            if(!touchInput.TryClaimSlider(this))return;
            visual.GetRootPose(out _);visual.LockFingerAtCurrent(HandFinger.Index);
            tipInWrist=Quaternion.Inverse(wrist.rotation)*(tip.position-wrist.position);
            wristInSleeve=Quaternion.Inverse(element.Sleeve.transform.rotation)*wrist.rotation;
            IsLocked=true;
            var grab=touchInput.manipulation?touchInput.manipulation.rightGrab:null;
            if(grab&&grab.enabled){grab.enabled=false;disabledGrab=true;}
            Refresh();
        }
        public void OnPointerUp(PointerEventData data)=>Release();
        public void OnEndDrag(PointerEventData data)=>Release();
        void LateUpdate()=>Refresh();
        [BeforeRenderOrder(145)]
        void Refresh()
        {
            if(!IsLocked)return;
            var hand=Raw;
            if(!element||!element.Sleeve||!element.Sleeve.wristAnchor||element.Sleeve.closeAmount>=.01f||
                !element.panel.enabled||!slider.IsInteractable()||hand==null||!hand.IsConnected||!hand.IsTrackedDataValid||
                !hand.IsHighConfidence||!hand.GetJointPose(HandJointId.HandIndexTip,out var tip))
            {Cancel();return;}
            HandlePoint=GetHandlePoint();
            if(Vector3.Distance(tip.position,HandlePoint)>releaseDistance){Cancel();return;}
            var rotation=element.Sleeve.transform.rotation*wristInSleeve;
            var wrist=new Pose(HandlePoint-rotation*tipInWrist,rotation);
            visual.SetFingerFreedom(HandFinger.Index,JointFreedom.Locked,true);
            visual.LockWristPose(wrist,1,SyntheticHand.WristLockMode.Full,true,true);
            visual.MarkInputDataRequiresUpdate();
        }
        void Cancel()
        {
            Release();
            if(touchInput&&touchInput.interactor)touchInput.interactor.enabled=false;
        }
        public void Release()
        {
            if(!IsLocked)return;
            IsLocked=false;
            if(touchInput)touchInput.ReleaseSlider(this);
            if(visual)
            {
                visual.SetFingerFreedom(HandFinger.Index,JointFreedom.Free);
                visual.FreeWrist();visual.MarkInputDataRequiresUpdate();
            }
            if(disabledGrab&&touchInput&&touchInput.manipulation&&touchInput.manipulation.rightGrab)
                touchInput.manipulation.rightGrab.enabled=true;
            disabledGrab=false;
        }
        void OnDisable(){Application.onBeforeRender-=Refresh;Release();}
    }
}
