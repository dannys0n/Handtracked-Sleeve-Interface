using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace ForearmLab
{
    [DefaultExecutionOrder(1250)]
    public class SleeveImageInsertion : MonoBehaviour
    {
        public SleeveImageContent content;
        public SleeveElement element;
        public Material slotMaterial;
        public Material recessMaterial;
        public HandGrabInteractor leftGrab, rightGrab;
        public SyntheticHand leftVisual, rightVisual;
        public HandVisual leftHandVisual, rightHandVisual;
        [Min(.02f)] public float approachDistance=.16f;
        [Min(.001f)] public float contactDistance=.012f;
        [Min(.01f)] public float releaseDistance=.06f;
        [Min(.005f)] public float slotWidth=.012f;
        [Min(.02f)] public float finishDuration=.25f;
        public bool IsExpanded => candidate;
        public CapturedImageCard LockedCard { get; private set; }
        public float Progress { get; private set; }
        public Pose SlotPose { get; private set; }
        public string Status { get; private set; }="Bring a held image near the sleeve";
        SleeveGridLayout grid;
        WristSleeve sleeve;
        CapturedImageCard candidate, rebaseCard;
        SleeveElement previousFocus;
        GameObject slot;
        Transform recess, railA, railB;
        bool finishing, imageWasEnabled, leftLocked, rightLocked;
        bool leftHidden, rightHidden, leftWasHidden, rightWasHidden;
        float angle, depth, halfDepth, startDepth;
        Vector3 grabStart, lockedScale;
        Quaternion leftOffset, rightOffset, contactCardRotation;

        void Start()
        {
            if(!content)content=GetComponent<SleeveImageContent>();
            if(!element)element=GetComponent<SleeveElement>();
            grid=GetComponentInParent<SleeveGridLayout>();sleeve=grid?grid.sleeve:null;
        }
        void OnEnable()=>Application.onBeforeRender+=RefreshPresentation;
        void LateUpdate()
        {
            if(!grid||!sleeve||!content)return;
            if(!sleeve.wristAnchor||sleeve.closeAmount>=.01f){EndPreview();return;}
            if(LockedCard)
            {
                if(finishing)
                {
                    depth=Mathf.MoveTowards(depth,-halfDepth,Time.unscaledDeltaTime*(2*halfDepth)/Mathf.Max(.02f,finishDuration));
                    ApplyConstraint();
                    if(Progress>=.98f)TryStore(LockedCard);
                }
                RefreshPresentation();return;
            }
            CapturedImageCard nearest=null;float best=approachDistance;
            foreach(var card in CapturedImageCard.ActiveCards)
            {
                if(!card||!card.IsHeld||!card.HasImage||card.destination!=content||!card.grabCollider)continue;
                var local=sleeve.transform.InverseTransformPoint(card.transform.position);
                float a=Mathf.Atan2(local.y,local.x);
                var point=sleeve.transform.TransformPoint(new Vector3(Mathf.Cos(a)*sleeve.radius,Mathf.Sin(a)*sleeve.radius,
                    Mathf.Clamp(local.z,0,sleeve.EffectiveLength)));
                float distance=Vector3.Distance(card.ClosestPoint(point),point);
                if(distance<best){best=distance;nearest=card;}
            }
            if(!nearest){EndPreview();return;}
            if(candidate!=nearest){EndPreview();BeginPreview(nearest);}
            var p=sleeve.transform.InverseTransformPoint(nearest.transform.position);
            angle=Mathf.Atan2(p.y,p.x)*Mathf.Rad2Deg;
            grid.AimFocusedElement(angle);RefreshPresentation();
        }
        void BeginPreview(CapturedImageCard card)
        {
            candidate=card;previousFocus=grid.FocusedElement;
            grid.Focus(element);grid.ResizeFocusedElement(1,grid.rows);
            imageWasEnabled=content.image&&content.image.enabled;if(content.image)content.image.enabled=false;
            if(!slot)BuildSlot();slot.SetActive(true);
            Status="Align the image edge with the insert";
        }
        void UpdateSlot()
        {
            float a=angle*Mathf.Deg2Rad;
            var radial=sleeve.transform.TransformDirection(new Vector3(Mathf.Cos(a),Mathf.Sin(a),0));
            var position=sleeve.transform.TransformPoint(new Vector3(Mathf.Cos(a)*(sleeve.radius+element.Lift+.002f),
                Mathf.Sin(a)*(sleeve.radius+element.Lift+.002f),sleeve.EffectiveLength*.5f));
            SlotPose=new Pose(position,Quaternion.LookRotation(radial,sleeve.transform.forward));
            if(!slot)return;
            slot.transform.SetPositionAndRotation(position,SlotPose.rotation);
            float length=sleeve.EffectiveLength*.88f;
            recess.localScale=new Vector3(slotWidth,length,.003f);
            railA.localScale=railB.localScale=new Vector3(.002f,length,.004f);
            railA.localPosition=new Vector3(-slotWidth*.5f,0,.002f);
            railB.localPosition=new Vector3(slotWidth*.5f,0,.002f);
        }
        Quaternion CardRotation(CapturedImageCard card)
        {
            var radial=SlotPose.rotation*Vector3.forward;var along=sleeve.transform.forward;
            bool landscape=card.canvas.rect.width>=card.canvas.rect.height;
            return landscape?Quaternion.LookRotation(Vector3.Cross(along,radial),radial):
                Quaternion.LookRotation(Vector3.Cross(radial,along),along);
        }
        float CardHalfDepth(CapturedImageCard card)
        {
            var size=card.grabCollider.size;var scale=card.transform.lossyScale;
            return Mathf.Max(.005f,Mathf.Min(Mathf.Abs(size.x*scale.x),Mathf.Abs(size.y*scale.y))*.5f);
        }
        Vector3 GrabLocal(IGrabbable grabbable)
        {
            Vector3 point=Vector3.zero;foreach(var p in grabbable.GrabPoints)point+=p.position;
            return sleeve.transform.InverseTransformPoint(point/Mathf.Max(1,grabbable.GrabPoints.Count));
        }
        public bool Constrain(CapturedImageCard card,IGrabbable grabbable)
        {
            if(card!=candidate||!sleeve||sleeve.closeAmount>=.01f)return false;
            UpdateSlot();var radial=SlotPose.rotation*Vector3.forward;
            if(!LockedCard)
            {
                if(Vector3.Distance(card.ClosestPoint(SlotPose.position),SlotPose.position)>contactDistance)return false;
                LockedCard=card;finishing=false;halfDepth=CardHalfDepth(card);
                contactCardRotation=card.transform.rotation;
                depth=Mathf.Max(halfDepth,Vector3.Dot(card.transform.position-SlotPose.position,radial));
                startDepth=depth;grabStart=GrabLocal(grabbable);lockedScale=card.transform.localScale;
                Status="Insert image along the slot";
            }
            var delta=sleeve.transform.TransformVector(GrabLocal(grabbable)-grabStart);
            var lateral=Vector3.ProjectOnPlane(delta,radial);
            depth=startDepth+Vector3.Dot(delta,radial);
            if(lateral.magnitude>releaseDistance||depth>halfDepth+releaseDistance)
            {CancelInsertion();rebaseCard=card;return false;}
            ApplyConstraint();
            if(Progress>=.98f)TryStore(card);
            return true;
        }
        public void RebaseGrab(CapturedImageCard card,IGrabbable grabbable)
        {
            if(LockedCard!=card)return;
            grabStart=GrabLocal(grabbable);startDepth=depth;
        }
        public bool ConsumeRebase(CapturedImageCard card)
        {if(rebaseCard!=card)return false;rebaseCard=null;return true;}
        void ApplyConstraint()
        {
            if(!LockedCard)return;
            UpdateSlot();LockedCard.transform.SetPositionAndRotation(SlotPose.position+(SlotPose.rotation*Vector3.forward)*depth,CardRotation(LockedCard));
            LockedCard.transform.localScale=lockedScale;
            Progress=Mathf.InverseLerp(halfDepth,-halfDepth,depth);
        }
        public void Released(CapturedImageCard card)
        {
            if(LockedCard!=card)return;
            FreeHands();
            if(Progress>=.5f){finishing=true;Status="Storing image";}
            else{CancelInsertion();EndPreview();}
        }
        public bool TryStore(CapturedImageCard card)
        {
            if(card!=LockedCard||Progress<.98f)return false;
            bool stored=card.CompleteStorage();EndPreview();return stored;
        }
        void CancelInsertion(){LockedCard=null;finishing=false;Progress=0;FreeHands();}
        void EndPreview()
        {
            CancelInsertion();
            if(slot)slot.SetActive(false);
            if(!candidate)return;
            candidate=null;
            if(content&&content.image)content.image.enabled=imageWasEnabled;
            if(grid&&grid.FocusedElement==element)
            {if(previousFocus&&previousFocus!=element)grid.Focus(previousFocus);else grid.ClearFocus();}
            previousFocus=null;Status="Bring a held image near the sleeve";
        }
        [BeforeRenderOrder(140)]
        void RefreshPresentation()
        {
            if(!candidate||!sleeve)return;
            UpdateSlot();
            if(LockedCard)
            {
                ApplyConstraint();
                LockHand(leftGrab,leftVisual,leftHandVisual,ref leftLocked,ref leftOffset,ref leftHidden,ref leftWasHidden);
                LockHand(rightGrab,rightVisual,rightHandVisual,ref rightLocked,ref rightOffset,ref rightHidden,ref rightWasHidden);
            }
        }
        void LockHand(HandGrabInteractor grab,SyntheticHand visual,HandVisual handVisual,ref bool locked,ref Quaternion offset,ref bool hidden,ref bool wasHidden)
        {
            var target=grab?.TargetInteractable as Component;
            bool ours=grab&&visual&&grab.IsGrabbing&&target&&target.GetComponentInParent<CapturedImageCard>()==LockedCard;
            SetHandHidden(handVisual,ours,ref hidden,ref wasHidden);
            if(!ours){if(locked&&visual){visual.FreeWrist(SyntheticHand.WristLockMode.Rotation);visual.MarkInputDataRequiresUpdate();}locked=false;return;}
            if(!grab.Hand.GetRootPose(out var wrist))return;
            if(!locked){offset=Quaternion.Inverse(contactCardRotation)*wrist.rotation;locked=true;}
            visual.LockWristPose(new Pose(wrist.position,LockedCard.transform.rotation*offset),1,SyntheticHand.WristLockMode.Rotation,true,true);
            visual.MarkInputDataRequiresUpdate();
        }
        void FreeHands()
        {
            SetHandHidden(leftHandVisual,false,ref leftHidden,ref leftWasHidden);
            SetHandHidden(rightHandVisual,false,ref rightHidden,ref rightWasHidden);
            if(leftLocked&&leftVisual){leftVisual.FreeWrist(SyntheticHand.WristLockMode.Rotation);leftVisual.MarkInputDataRequiresUpdate();}
            if(rightLocked&&rightVisual){rightVisual.FreeWrist(SyntheticHand.WristLockMode.Rotation);rightVisual.MarkInputDataRequiresUpdate();}
            leftLocked=rightLocked=false;
        }
        static void SetHandHidden(HandVisual visual,bool hide,ref bool hidden,ref bool wasHidden)
        {
            if(hide==hidden)return;
            if(visual)
            {
                if(hide){wasHidden=visual.ForceOffVisibility;visual.ForceOffVisibility=true;}
                else visual.ForceOffVisibility=wasHidden;
            }
            hidden=hide&&visual;
        }
        void BuildSlot()
        {
            slot=new GameObject("Image insertion slot"){hideFlags=HideFlags.DontSave};slot.transform.SetParent(sleeve.transform,false);
            recess=Part("Insert opening",recessMaterial);railA=Part("Insert rail",slotMaterial);railB=Part("Insert rail",slotMaterial);
        }
        Transform Part(string name,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(slot.transform,false);
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        void OnDisable(){Application.onBeforeRender-=RefreshPresentation;EndPreview();}
        void OnDestroy(){if(slot)Destroy(slot);}
    }
}
