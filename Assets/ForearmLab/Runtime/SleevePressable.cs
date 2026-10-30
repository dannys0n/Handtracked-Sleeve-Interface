using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.UI;

namespace ForearmLab
{
    // A raised visual face over a fixed activation plane. Meta owns click/toggle events.
    [ExecuteAlways,DefaultExecutionOrder(1050)]
    public class SleevePressable : MonoBehaviour
    {
        public SleeveElement element;
        public Selectable control;
        public MetaSleeveTouchInput touchInput;
        [Min(0)] public float restHeight=.003f;
        [Min(.0001f)] public float pressTravel=.002f;
        [Min(.0001f)] public float activationTravel=.0015f;
        [Min(.001f)] public float returnSpeed=.04f;
        public float CurrentTravel { get; private set; }
        public bool IsTouching { get; private set; }
        public bool ThresholdReached { get; private set; }
        public float ActivationOffset=>restHeight-Mathf.Min(pressTravel,activationTravel);
        public bool UsesRaisedFace=>isActiveAndEnabled&&singleControl&&control;
        bool singleControl,limiting,disabledGrab;
        SyntheticHand visual;
        IHand Raw=>touchInput?touchInput.handSource as IHand:null;

        void OnEnable()
        {
            if(!element)element=GetComponent<SleeveElement>();
            singleControl=element&&element.contentRoot&&element.contentRoot.GetComponentsInChildren<Selectable>(true).Length==1;
            if(element)element.pressOffset=UsesRaisedFace?restHeight:0;
            Application.onBeforeRender+=LimitFinger;
        }
        void Start(){if(Application.isPlaying&&!touchInput)touchInput=FindAnyObjectByType<MetaSleeveTouchInput>();}
        void Update()
        {
            if(!UsesRaisedFace||!element||!element.Sleeve){Release();if(element)element.pressOffset=0;return;}
            if(!Application.isPlaying){CurrentTravel=0;element.pressOffset=restHeight;return;}
            var hand=Raw;
            bool available=element.panel&&element.panel.enabled&&element.Sleeve.wristAnchor&&element.Sleeve.closeAmount<.01f&&
                control.IsInteractable()&&touchInput&&!touchInput.ActiveSlider&&!touchInput.suppressForCamera&&
                !touchInput.suppressForManipulation&&hand!=null&&hand.IsConnected&&hand.IsTrackedDataValid&&hand.IsHighConfidence;
            if(!available||!hand.GetJointPose(HandJointId.HandIndexTip,out var tip)||!Inside(tip.position))
            {Release();ReturnFace();return;}
            float depth=RawDepth(tip.position);
            if(IsTouching&&depth<-.001f){Release();ReturnFace();return;}
            if(!IsTouching)
            {
                if(depth<0||depth>pressTravel+.015f||!touchInput.TryClaimPressable(this)){ReturnFace();return;}
                IsTouching=true;ThresholdReached=false;
                var grab=touchInput.manipulation?touchInput.manipulation.rightGrab:null;
                if(grab&&grab.enabled){grab.enabled=false;disabledGrab=true;}
            }
            CurrentTravel=Mathf.Clamp(depth,0,pressTravel);
            element.pressOffset=restHeight-CurrentTravel;
            if(depth>=Mathf.Min(pressTravel,activationTravel))ThresholdReached=true;
            if(ThresholdReached)FreeVisual();
        }
        float RawDepth(Vector3 tip)
        {
            var p=element.Sleeve.transform.InverseTransformPoint(tip);
            float radius=new Vector2(p.x,p.y).magnitude;
            float fingerRadius=touchInput&&touchInput.interactor?touchInput.interactor.Radius:0;
            return element.Sleeve.radius+element.BaseLift+restHeight-(radius-fingerRadius);
        }
        bool Inside(Vector3 tip)
        {
            var surface=element.panel.GetComponent<SleevePokeSurface>();
            var p=element.Sleeve.transform.InverseTransformPoint(tip);var r=element.Region;
            float u=surface.UnwrappedU(p),v=(p.z-element.AxialShift)/element.Sleeve.EffectiveLength;
            if(u<r.x||u>r.z||v<r.y||v>r.w)return false;
            var canvas=surface.CanvasPoint(tip);
            var flatWorld=element.panel.transform.TransformPoint(new Vector3(canvas.x,canvas.y,0));
            var rect=(RectTransform)control.transform;
            return rect.rect.Contains(rect.InverseTransformPoint(flatWorld));
        }
        void ReturnFace()
        {
            CurrentTravel=Mathf.MoveTowards(CurrentTravel,0,Time.unscaledDeltaTime*returnSpeed);
            element.pressOffset=restHeight-CurrentTravel;
        }
        void LateUpdate()=>LimitFinger();
        [BeforeRenderOrder(145)]
        void LimitFinger()
        {
            if(!Application.isPlaying||!IsTouching||ThresholdReached||!touchInput)return;
            var hand=Raw;visual=touchInput.visualHand;
            if(!visual||!visual.Started||hand==null||!hand.GetRootPose(out var wrist)||
                !hand.GetJointPose(HandJointId.HandIndexTip,out var tip))return;
            var local=element.Sleeve.transform.InverseTransformPoint(tip.position);
            var normal=element.Sleeve.transform.TransformDirection(new Vector3(local.x,local.y,0).normalized);
            float radius=new Vector2(local.x,local.y).magnitude;
            float face=element.Sleeve.radius+element.BaseLift+restHeight-CurrentTravel;
            float padRadius=touchInput.interactor?touchInput.interactor.Radius:0;
            var position=wrist.position+normal*Mathf.Max(0,face+padRadius-radius);
            visual.LockWristPose(new Pose(position,wrist.rotation),1,SyntheticHand.WristLockMode.Position,true,true);
            visual.MarkInputDataRequiresUpdate();limiting=true;
        }
        void FreeVisual()
        {
            if(limiting&&visual){visual.FreeWrist(SyntheticHand.WristLockMode.Position);visual.MarkInputDataRequiresUpdate();}
            limiting=false;
        }
        public void Release()
        {
            IsTouching=false;ThresholdReached=false;FreeVisual();
            if(touchInput)touchInput.ReleasePressable(this);
            if(disabledGrab&&touchInput&&touchInput.manipulation&&touchInput.manipulation.rightGrab)
                touchInput.manipulation.rightGrab.enabled=true;
            disabledGrab=false;
        }
        void OnDisable(){Application.onBeforeRender-=LimitFinger;Release();if(element)element.pressOffset=0;}
    }
}
