using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForearmLab
{
    [DefaultExecutionOrder(-200)]
    public class TwoHandCameraGesture : MonoBehaviour
    {
        public Camera view;
        public MonoBehaviour leftHandSource, rightHandSource;
        public FingerFeatureStateProvider leftFingerStates, rightFingerStates;
        public SleeveManipulation manipulation;
        public MetaSleeveTouchInput touchInput;
        public HandGrabInteractor leftGrab, rightGrab;
        public CapturedImageCard cardPrefab;
        public SleeveImageContent imageDestination;
        public LineRenderer frameLine;
        public TMP_Text hint;
        public LabEnvironmentMode environment;
        [Range(30,90)] public float thumbOpenAngle = 45;
        [Range(5,45),Tooltip("Close the shutter when both thumb directions are within this angle of their index fingers.")]
        public float thumbClosedAngle = 30;
        [Range(.1f,1)] public float frameHold = .22f;
        [Range(256,2048)] public int captureWidth = 1024;
        public string Status {get;private set;} = "Camera: two L hands, then close both thumbs";
        public CameraFrameGesture Recognizer {get;} = new CameraFrameGesture();
        public CapturedImageCard LastCard {get;private set;}
        bool ownsHands, leftWasEnabled, rightWasEnabled, capturing;
        string lastHintStatus, lastEnvironmentStatus;
        readonly List<CapturedImageCard> cards = new List<CapturedImageCard>();

        bool Read(MonoBehaviour source, FingerFeatureStateProvider fingers, out bool open, out bool closed, out Vector3 index, out Vector3 thumb)
        {
            open=closed=false;index=thumb=default;
            if(!(source is IHand hand)||!hand.IsConnected||!hand.IsTrackedDataValid||!hand.IsHighConfidence||!fingers)return false;
            bool framePose=fingers.GetCurrentState(HandFinger.Index,FingerFeature.Curl,out var state)&&state=="0";
            for(int i=2;i<5;i++)framePose&=fingers.GetCurrentState((HandFinger)i,FingerFeature.Curl,out state)&&state!="0";
            if(!hand.GetJointPose(HandJointId.HandIndexTip,out var ip)||!hand.GetJointPose(HandJointId.HandThumbTip,out var tp)||
                !hand.GetJointPose(HandJointId.HandThumb1,out var tb)||!hand.GetJointPose(HandJointId.HandIndex1,out var ib))return false;
            index=ip.position;thumb=tp.position;
            float angle=Vector3.Angle(index-ib.position,thumb-tb.position);
            closed=angle<=thumbClosedAngle;
            open=framePose&&angle>=thumbOpenAngle&&!closed;
            return view && view.WorldToViewportPoint(index).z>.12f && view.WorldToViewportPoint(thumb).z>.12f;
        }
        void Update()
        {
            if(!view)return;
            bool lv=Read(leftHandSource,leftFingerStates,out bool lo,out bool lc,out var li,out var lt);
            bool rv=Read(rightHandSource,rightFingerStates,out bool ro,out bool rc,out var ri,out var rt);
            bool objectHeld=(leftGrab&&leftGrab.State==InteractorState.Select)||(rightGrab&&rightGrab.State==InteractorState.Select)||
                (!ownsHands&&manipulation&&manipulation.OwnsRightHand);
            var rect=ViewportBounds(view,li,lt,ri,rt);
            Recognizer.holdTime=frameHold;
            bool shutter=Recognizer.Step(lv&&rv&&!objectHeld&&!capturing,lo&&ro,lc&&rc,rect,Time.unscaledTime);
            bool framing=Recognizer.Current==CameraFrameGesture.Phase.Framing||Recognizer.Current==CameraFrameGesture.Phase.Closing;
            SetOwnership(framing||capturing);
            DrawFrame(framing);
            if(framing)Status="Frame ready: touch both thumbs to the sides of your hands";
            else if(!capturing&&Recognizer.Current!=CameraFrameGesture.Phase.Cooldown)Status="Camera: two L hands, then close both thumbs";
            if(shutter)StartCoroutine(Capture(Recognizer.Frame));
            if(hint)
            {
                string environmentStatus=environment?environment.Status:"";
                if(lastHintStatus!=Status||lastEnvironmentStatus!=environmentStatus)
                {
                    lastHintStatus=Status;lastEnvironmentStatus=environmentStatus;
                    hint.text=Status+"\nScene capture · passthrough is excluded"+(environment?"\n"+environmentStatus:"");
                }
                hint.transform.SetPositionAndRotation(view.ViewportToWorldPoint(new Vector3(.5f,.15f,.65f)),view.transform.rotation);
            }
        }
        public static Rect ViewportBounds(Camera camera, params Vector3[] points)
        {
            Vector2 min=Vector2.one,max=Vector2.zero;
            foreach(var p in points){var v=camera.WorldToViewportPoint(p);min=Vector2.Min(min,new Vector2(v.x,v.y));max=Vector2.Max(max,new Vector2(v.x,v.y));}
            min=Vector2.Max(Vector2.zero,min);max=Vector2.Min(Vector2.one,max);
            return Rect.MinMaxRect(min.x,min.y,Mathf.Max(min.x,max.x),Mathf.Max(min.y,max.y));
        }
        public static Rect ViewportBounds(Camera camera,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            Vector2 min=Vector2.one,max=Vector2.zero;
            void Include(Vector3 world){var p=camera.WorldToViewportPoint(world);var xy=new Vector2(p.x,p.y);min=Vector2.Min(min,xy);max=Vector2.Max(max,xy);}
            Include(a);Include(b);Include(c);Include(d);
            min=Vector2.Max(Vector2.zero,min);max=Vector2.Min(Vector2.one,max);
            return Rect.MinMaxRect(min.x,min.y,Mathf.Max(min.x,max.x),Mathf.Max(min.y,max.y));
        }
        void DrawFrame(bool visible)
        {
            if(!frameLine)return;
            frameLine.enabled=visible;
            if(!visible)return;
            var r=Recognizer.Frame;float z=.55f;
            frameLine.positionCount=4;frameLine.loop=true;frameLine.useWorldSpace=true;
            frameLine.SetPosition(0,view.ViewportToWorldPoint(new Vector3(r.xMin,r.yMin,z)));
            frameLine.SetPosition(1,view.ViewportToWorldPoint(new Vector3(r.xMax,r.yMin,z)));
            frameLine.SetPosition(2,view.ViewportToWorldPoint(new Vector3(r.xMax,r.yMax,z)));
            frameLine.SetPosition(3,view.ViewportToWorldPoint(new Vector3(r.xMin,r.yMax,z)));
        }
        void SetOwnership(bool own)
        {
            if(ownsHands==own)return;
            ownsHands=own;
            if(manipulation)manipulation.CameraOwnsHands=own;
            if(touchInput)touchInput.suppressForCamera=own;
            if(own){leftWasEnabled=leftGrab&&leftGrab.enabled;rightWasEnabled=rightGrab&&rightGrab.enabled;}
            if(leftGrab)leftGrab.enabled=own?false:leftWasEnabled;
            if(rightGrab)rightGrab.enabled=own?false:rightWasEnabled;
        }
        public IEnumerator Capture(Rect viewport)
        {
            if(capturing||!view||!cardPrefab)yield break;
            capturing=true;SetOwnership(true);Status="Capturing scene...";
            if(frameLine)frameLine.enabled=false;
            if(hint)hint.enabled=false;
            // Run outside the source camera's render callback. A mono camera renders the app scene,
            // never the headset compositor's protected passthrough image.
            yield return new WaitForEndOfFrame();
            Texture2D texture=null;
            try
            {
                texture=CaptureScene(view,viewport,captureWidth);
                var card=Instantiate(cardPrefab,view.ViewportToWorldPoint(new Vector3(viewport.center.x,viewport.center.y,.5f)),view.transform.rotation);
                card.Initialize(texture,imageDestination);texture=null;LastCard=card;cards.Add(card);
                cards.RemoveAll(c=>!c);
                if(cards.Count>3){Destroy(cards[0].gameObject);cards.RemoveAt(0);}
                Status="Captured: grab with both hands to scale; release over IMAGE to store";
            }
            catch(System.Exception error){Status="Capture failed: "+error.Message;Debug.LogException(error,this);}
            finally{if(texture)Destroy(texture);capturing=false;SetOwnership(false);if(hint)hint.enabled=true;}
        }
        public static Texture2D CaptureScene(Camera source, Rect viewport, int width)
        {
            int height=Mathf.Max(1,Mathf.RoundToInt(width/Mathf.Max(.1f,source.aspect)));
            var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;
            var go=new GameObject("Scene capture camera"){hideFlags=HideFlags.HideAndDontSave};
            Texture2D result=null;
            try
            {
                var camera=go.AddComponent<Camera>();camera.enabled=false;camera.CopyFrom(source);
                camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                camera.stereoTargetEye=StereoTargetEyeMask.None;camera.targetTexture=target;
                camera.aspect=source.aspect;camera.projectionMatrix=source.projectionMatrix;camera.worldToCameraMatrix=source.worldToCameraMatrix;
                var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=false;data.allowXRRendering=false;
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
                if(GraphicsSettings.currentRenderPipeline)RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();
                RenderTexture.active=target;
                int x=Mathf.Clamp(Mathf.FloorToInt(viewport.xMin*width),0,width-1),y=Mathf.Clamp(Mathf.FloorToInt(viewport.yMin*height),0,height-1);
                int w=Mathf.Clamp(Mathf.CeilToInt(viewport.width*width),1,width-x),h=Mathf.Clamp(Mathf.CeilToInt(viewport.height*height),1,height-y);
                result=new Texture2D(w,h,TextureFormat.RGB24,false){name="Gesture scene capture"};
                result.ReadPixels(new Rect(x,y,w,h),0,0);result.Apply(false,false);return result;
            }
            catch{if(result)Destroy(result);throw;}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Destroy(go);}
        }
        void OnDisable(){StopAllCoroutines();capturing=false;Recognizer.Cancel();SetOwnership(false);if(frameLine)frameLine.enabled=false;if(hint)hint.enabled=false;}
        void OnEnable(){if(hint)hint.enabled=true;}
        void OnDestroy(){foreach(var card in cards)if(card)Destroy(card.gameObject);}
    }
}
