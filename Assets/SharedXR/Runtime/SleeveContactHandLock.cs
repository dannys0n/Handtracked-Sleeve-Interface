using Oculus.Interaction.Input;
using UnityEngine;

namespace ForearmLab
{
    // Presentation only. Acquisition, motion and release always read the raw hand.
    [DefaultExecutionOrder(1200)]
    public class SleeveContactHandLock : MonoBehaviour
    {
        public WristSleeve sleeve;
        public SleeveManipulation manipulation;
        public SyntheticHand syntheticHand;
        public MonoBehaviour rawHandSource;
        public Material debugMaterial;
        public bool showContactGuides = true;
        [Min(.005f)] public float releaseDistance = .04f;
        public bool IsLocked { get; private set; }
        public Vector3 ContactPoint { get; private set; }
        public Pose LockedWristPose { get; private set; }
        public string Status => IsLocked ? (rolling ? "Roll contact locked" : "Scroll contact locked") : "Contact unlocked";
        IHand Raw => rawHandSource as IHand;
        bool rolling;
        float startAngle, currentAngle, cuffDepth, capturedRadius;
        Vector3 contactInWrist, wristInContact;
        Quaternion wristRotationInContact;
        LineRenderer edge, innerEdge, outerEdge, tether, volumeStart, volumeEnd, leftTargetLine;
        LineRenderer[] volumeRibs, leftRange;
        Transform marker;
        GameObject graphics;
        Vector3 candidateContact;
        bool hasCandidate;
        public float ContactMinZ => manipulation ? manipulation.CuffContactMinZ : sleeve.CuffEdgeZ-.09f;
        public float ContactMaxZ => manipulation ? manipulation.CuffContactMaxZ : Mathf.Min(sleeve.EffectiveLength,sleeve.CuffEdgeZ+.09f);
        public void SetCandidate(Vector3 contact) { candidateContact = contact; hasCandidate = true; }

        void OnEnable() { Application.onBeforeRender += RefreshVisual; }

        public bool Begin(Vector3 rawContact, bool roll)
        {
            if (!sleeve || !syntheticHand || !syntheticHand.Started || Raw == null || !Raw.IsConnected ||
                !Raw.IsTrackedDataValid || !Raw.IsHighConfidence || !Raw.GetRootPose(out var wrist)) return false;
            // Query before freezing so SDK captures the current source pose, not an authored grip.
            syntheticHand.GetRootPose(out _);
            for (int i = 0; i < 5; i++) syntheticHand.LockFingerAtCurrent((HandFinger)i);
            var local = sleeve.transform.InverseTransformPoint(rawContact);
            startAngle = currentAngle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
            cuffDepth = Mathf.Clamp(local.z - sleeve.CuffEdgeZ, -(manipulation ? manipulation.cuffOutsideWidth : .09f),
                manipulation ? manipulation.cuffInwardWidth : .09f);
            capturedRadius = new Vector2(local.x,local.y).magnitude;
            contactInWrist = Quaternion.Inverse(wrist.rotation) * (rawContact - wrist.position);
            var frame = sleeve.transform.rotation * Quaternion.AngleAxis(startAngle, Vector3.forward);
            var projectedContact = sleeve.transform.TransformPoint(new Vector3(Mathf.Cos(startAngle*Mathf.Deg2Rad)*sleeve.radius,
                Mathf.Sin(startAngle*Mathf.Deg2Rad)*sleeve.radius,Mathf.Clamp(local.z,ContactMinZ,ContactMaxZ)));
            wristInContact = Quaternion.Inverse(frame) * (wrist.position - projectedContact);
            wristRotationInContact = Quaternion.Inverse(frame) * wrist.rotation;
            rolling = roll; IsLocked = true;
            UpdateTarget(); RefreshVisual();
            return true;
        }

        public bool TryUpdateContact(out Vector3 rawContact)
        {
            rawContact = default;
            if (!IsLocked || !sleeve || Raw == null || !Raw.IsConnected || !Raw.IsTrackedDataValid || !Raw.IsHighConfidence ||
                !Raw.GetRootPose(out var wrist)) return false;
            rawContact = wrist.position + wrist.rotation * contactInWrist;
            var local = sleeve.transform.InverseTransformPoint(rawContact);
            if (!rolling) currentAngle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
            UpdateTarget();
            // Preserve the acquired radial grip offset: release measures movement away
            // from the captured contact path, not the palm's initial distance from skin.
            float a=(rolling?startAngle:currentAngle)*Mathf.Deg2Rad;
            var capturedContact=ContactPoint+sleeve.transform.TransformDirection(new Vector3(Mathf.Cos(a),Mathf.Sin(a),0))*(capturedRadius-sleeve.radius);
            return Vector3.Distance(rawContact, capturedContact) <= releaseDistance;
        }

        void UpdateTarget()
        {
            float angle = (rolling ? startAngle : currentAngle) * Mathf.Deg2Rad;
            var local = new Vector3(Mathf.Cos(angle) * sleeve.radius, Mathf.Sin(angle) * sleeve.radius,
                Mathf.Clamp(sleeve.CuffEdgeZ + cuffDepth, ContactMinZ, ContactMaxZ));
            ContactPoint = sleeve.transform.TransformPoint(local);
            var rotation = sleeve.transform.rotation * Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);
            LockedWristPose = new Pose(ContactPoint + rotation * wristInContact, rotation * wristRotationInContact);
        }

        public void Release()
        {
            if (!IsLocked) return;
            IsLocked = false;
            if (syntheticHand)
            {
                syntheticHand.FreeAllJoints(); syntheticHand.FreeWrist();
                syntheticHand.MarkInputDataRequiresUpdate();
            }
        }

        void LateUpdate() => RefreshVisual();
        [BeforeRenderOrder(130)]
        void RefreshVisual()
        {
            if (IsLocked && sleeve && syntheticHand)
            {
                UpdateTarget();
                for (int i = 0; i < 5; i++) syntheticHand.SetFingerFreedom((HandFinger)i, JointFreedom.Locked, true);
                syntheticHand.LockWristPose(LockedWristPose, 1, SyntheticHand.WristLockMode.Full, true, true);
                syntheticHand.MarkInputDataRequiresUpdate();
            }
            DrawGuides();
        }

        void DrawGuides()
        {
            if (!showContactGuides || !sleeve || !sleeve.wristAnchor || !debugMaterial)
            { if (graphics) graphics.SetActive(false); return; }
            if (!graphics) BuildGuides();
            graphics.SetActive(true);
            Ring(edge, sleeve.CuffEdgeZ);
            Ring(innerEdge, ContactMaxZ);
            Ring(outerEdge, ContactMinZ);
            float reachRadius=sleeve.radius+(manipulation?manipulation.gripTolerance:.055f);
            Ring(volumeStart, ContactMinZ, reachRadius);
            Ring(volumeEnd, ContactMaxZ, reachRadius);
            for(int i=0;i<volumeRibs.Length;i++)
            {
                float angle=i*Mathf.PI*2/volumeRibs.Length;
                var radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*reachRadius;
                volumeRibs[i].SetPosition(0,sleeve.transform.TransformPoint(radial+Vector3.forward*ContactMinZ));
                volumeRibs[i].SetPosition(1,sleeve.transform.TransformPoint(radial+Vector3.forward*ContactMaxZ));
            }
            DrawLeftContact();
            bool hasContact = IsLocked;
            Vector3 rawPoint = default;
            if (IsLocked && Raw != null && Raw.GetRootPose(out var wrist))
                rawPoint = wrist.position + wrist.rotation * contactInWrist;
            else if (Raw != null && Raw.IsTrackedDataValid &&
                Raw.GetJointPose(HandJointId.HandIndexTip, out var index) && Raw.GetJointPose(HandJointId.HandMiddleTip, out var middle))
            {
                rawPoint = hasCandidate ? candidateContact : (index.position + middle.position) * .5f;
                var p = sleeve.transform.InverseTransformPoint(rawPoint);
                float a = Mathf.Atan2(p.y, p.x);
                var projected = new Vector3(Mathf.Cos(a) * sleeve.radius, Mathf.Sin(a) * sleeve.radius,
                    Mathf.Clamp(p.z, ContactMinZ, ContactMaxZ));
                ContactPoint = sleeve.transform.TransformPoint(projected);
                hasContact = Vector3.Distance(rawPoint, ContactPoint) <= (manipulation?manipulation.gripTolerance:.055f);
            }
            marker.gameObject.SetActive(hasContact); tether.enabled = hasContact;
            if (hasContact)
            {
                marker.position = ContactPoint;
                marker.localScale = Vector3.one * (IsLocked ? .009f : .005f);
                tether.SetPosition(0, rawPoint); tether.SetPosition(1, ContactPoint);
            }
        }
        void DrawLeftContact()
        {
            bool valid=false, hasTarget=false;Vector3 point=default, surfacePoint=default;
            if(manipulation)valid=manipulation.TryGetLeftContact(out point,out surfacePoint,out hasTarget);
            foreach(var line in leftRange)line.enabled=valid;
            leftTargetLine.enabled=valid&&hasTarget;
            if(!valid)return;
            float radius=manipulation.pinchTargetDistance;
            for(int axis=0;axis<3;axis++)for(int i=0;i<leftRange[axis].positionCount;i++)
            {
                float a=i*Mathf.PI*2/leftRange[axis].positionCount;
                var offset=axis==0?new Vector3(0,Mathf.Cos(a),Mathf.Sin(a)):
                    axis==1?new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)):new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                leftRange[axis].SetPosition(i,point+offset*radius);
            }
            // The small cross is the exact thumb/index midpoint, even without a target.
            leftRange[3].SetPosition(0,point-Vector3.right*.004f);leftRange[3].SetPosition(1,point+Vector3.right*.004f);
            leftRange[4].SetPosition(0,point-Vector3.up*.004f);leftRange[4].SetPosition(1,point+Vector3.up*.004f);
            if(hasTarget){leftTargetLine.SetPosition(0,point);leftTargetLine.SetPosition(1,surfacePoint);}
        }
        void Ring(LineRenderer line, float z, float radius=-1)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i * Mathf.PI * 2 / line.positionCount;
                line.SetPosition(i, sleeve.transform.TransformPoint(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) *
                    (radius<0?sleeve.radius+.002f:radius) + Vector3.forward * z));
            }
        }
        void BuildGuides()
        {
            graphics = new GameObject("Sleeve contact guides") { hideFlags = HideFlags.DontSave };
            graphics.transform.SetParent(transform, false);
            edge = Line("Cuff contact edge", 64, true, .0015f);
            innerEdge = Line("Contact band inner edge", 64, true, .0006f);
            outerEdge = Line("Contact band hand-side edge", 64, true, .0006f);
            volumeStart=Line("Roll grasp volume hand side",64,true,.0006f);
            volumeEnd=Line("Roll grasp volume elbow side",64,true,.0006f);
            volumeRibs=new LineRenderer[8];
            for(int i=0;i<volumeRibs.Length;i++)volumeRibs[i]=Line("Roll grasp area rib "+i,2,false,.0006f);
            leftRange=new LineRenderer[5];
            for(int i=0;i<3;i++)leftRange[i]=Line("Left pinch reach "+i,48,true,.0007f);
            for(int i=3;i<5;i++)leftRange[i]=Line("Left pinch midpoint "+i,2,false,.0015f);
            leftTargetLine=Line("Left pinch to element contact",2,false,.0015f);
            tether = Line("Tracked contact to locked contact", 2, false, .001f);
            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere); dot.name = "Contact point"; dot.layer = 2;
            dot.transform.SetParent(graphics.transform, false); marker = dot.transform;
            var collider = dot.GetComponent<Collider>(); collider.enabled = false;
            if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            var renderer = dot.GetComponent<Renderer>(); renderer.sharedMaterial = debugMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        LineRenderer Line(string name, int count, bool loop, float width)
        {
            var go = new GameObject(name); go.layer = 2; go.transform.SetParent(graphics.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = debugMaterial;
            line.useWorldSpace = true; line.positionCount = count; line.loop = loop; line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }
        void OnDisable() { Application.onBeforeRender -= RefreshVisual; Release(); if (graphics) graphics.SetActive(false); }
        void OnDestroy()
        {
            if (!graphics) return;
            if (Application.isPlaying) Destroy(graphics); else DestroyImmediate(graphics);
        }
    }
}
