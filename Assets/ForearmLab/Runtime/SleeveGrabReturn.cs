using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace ForearmLab
{
    // The element owns the mount. Meta owns the grabbed payload; this component owns release return.
    [DefaultExecutionOrder(1300)]
    public class SleeveGrabReturn : MonoBehaviour
    {
        public SleeveElement element;
        public Grabbable grabbable;
        public Transform payload;
        public HandGrabInteractable[] grabInteractables;
        public HandGrabPose[] gripPoses;
        public float[] gripHandScales;
        public Vector3[] gripWristOffsets;
        [Min(.05f)] public float returnDuration=.45f;
        public bool IsAway {get;private set;}
        public bool IsGrabbed=>grabbable&&grabbable.GrabPoints.Count>0;
        bool returning;
        float elapsed;
        Vector3 homePosition,homeScale,returnPosition;
        Quaternion homeRotation,returnRotation;
        bool initialSpin;
        MaterialPropertyBlock block;
        Renderer[] renderers;
        void Start()
        {
            if(!payload)payload=transform;
            homePosition=payload.localPosition;homeRotation=payload.localRotation;homeScale=payload.localScale;
            renderers=payload.GetComponentsInChildren<Renderer>(true);block=new MaterialPropertyBlock();
        }
        void LateUpdate()
        {
            if(!payload||!grabbable||!element)return;
            if(grabInteractables!=null)foreach(var grab in grabInteractables)if(grab)
                grab.enabled=(element.panel&&element.panel.enabled&&element.Sleeve&&element.Sleeve.closeAmount<.01f)||IsGrabbed;
            if(gripPoses!=null)for(int i=0;i<gripPoses.Length;i++)if(gripPoses[i])
            {
                float scale=Mathf.Max(.0001f,Mathf.Abs(payload.lossyScale.x));
                gripPoses[i].transform.localScale=Vector3.one*((gripHandScales!=null&&i<gripHandScales.Length?gripHandScales[i]:1)/scale);
                // Wrist offsets are authored in metres: resizing an element must not resize the user's hand.
                if(gripWristOffsets!=null&&i<gripWristOffsets.Length)
                {
                    var offset=gripWristOffsets[i];
                    gripPoses[i].transform.localPosition=(offset+offset.normalized*(scale*.5f-.02f))/scale;
                }
            }
            if(IsGrabbed)
            {
                if(!IsAway){IsAway=true;initialSpin=element.animateVolume;element.animateVolume=false;}
                returning=false;
            }
            else if(IsAway)
            {
                if(!returning){returning=true;elapsed=0;returnPosition=payload.localPosition;returnRotation=payload.localRotation;}
                elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/returnDuration);float ease=1-Mathf.Pow(1-t,3);
                payload.localPosition=Vector3.Lerp(returnPosition,homePosition,ease);payload.localRotation=Quaternion.Slerp(returnRotation,homeRotation,ease);payload.localScale=homeScale;
                if(t>=1){IsAway=false;returning=false;element.animateVolume=initialSpin;}
            }
            foreach(var renderer in renderers)
            {
                if(!renderer)continue;renderer.GetPropertyBlock(block);block.SetFloat("_SleeveDetached",IsAway?1:0);renderer.SetPropertyBlock(block);
            }
        }
        void OnDisable()
        {
            if(payload&&IsAway){payload.localPosition=homePosition;payload.localRotation=homeRotation;payload.localScale=homeScale;if(element)element.animateVolume=initialSpin;}
            IsAway=false;returning=false;
        }
    }
}
