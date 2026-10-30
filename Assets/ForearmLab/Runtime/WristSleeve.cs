using UnityEngine;
using System.Collections.Generic;

namespace ForearmLab
{
    // Source-independent sleeve. Input wrist axes: +Z toward fingers, +Y toward back of hand.
    // Sleeve local +Z runs from wrist to elbow; +Y is the palm-facing side.
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public class WristSleeve : MonoBehaviour
    {
        [Tooltip("External wrist pose. Uses OpenXR wrist axes: +Z toward fingers, +Y toward back of hand.")]
        public Transform wristAnchor;
        [Tooltip("Optional external elbow pose. Leave empty for wrist-only simulation.")]
        public Transform elbowAnchor;
        [Min(.005f)] public float radius = .03f;
        [Min(.02f), Tooltip("Sleeve length in metres when no elbow is supplied, or automatic fitting is off.")]
        public float length = .22f;
        public bool fitLengthToElbow = true;
        [Tooltip("Draw the cylinder as a debug guide. Does not affect tracking, layout, or projected content.")]
        public bool showDebugCylinder;
        [UnityEngine.Serialization.FormerlySerializedAs("rollAmount")]
        [Range(0, 1), Tooltip("Presentation-only closing animation. Does not resize the workspace or reflow its grid.")]
        public float closeAmount;
        [UnityEngine.Serialization.FormerlySerializedAs("collapseThreshold")]
        [Range(.01f, 1), Tooltip("Cross this while closing to finish rolling up.")]
        public float rollUpThreshold = .75f;
        [Range(0, .99f), Tooltip("Cross this while opening to finish rolling down.")]
        public float rollDownThreshold = .25f;
        [Min(.005f)] public float foldedCuffLength = .025f;
        [Min(.01f)] public float rollAssistDuration = .25f;
        public bool IsRollAssisting { get; private set; }
        float rollAssistTarget;
        public float CuffEdgeZ => (EffectiveLength - Mathf.Min(foldedCuffLength, EffectiveLength)) *
            Mathf.SmoothStep(0, 1, Mathf.Clamp01(closeAmount));

        // Thresholds assist the interaction; they never remap or scale its fold input.
        public void SetRollAmount(float amount)
        {
            IsRollAssisting = false;
            float previous = closeAmount;
            closeAmount = Mathf.Clamp01(amount);
            if (closeAmount > previous && previous < rollUpThreshold && closeAmount >= rollUpThreshold)
            { rollAssistTarget = 1; IsRollAssisting = true; }
            else if (closeAmount < previous && previous > rollDownThreshold && closeAmount <= rollDownThreshold)
            { rollAssistTarget = 0; IsRollAssisting = true; }
        }
        [Tooltip("Aligns the sleeve to the wrist input convention. Default points the tube back along the arm.")]
        public Vector3 wristRotationOffset = new Vector3(180, 0, 0);
        [SerializeField, HideInInspector] Transform surface;

        public float EffectiveLength { get; private set; }
        public Transform Surface => surface;
        public float VisibleLength => EffectiveLength;
        readonly Dictionary<Material,Material> projectionMaterials=new Dictionary<Material,Material>();
        MaterialPropertyBlock surfaceProperties;
        Renderer surfaceRenderer;
        Vector3 lastShape = new Vector3(-1,-1,-1);
        MaterialPropertyBlock projectionBlock;

        public void ConfigureProjectionRenderer(Renderer renderer)
        {
            if(!renderer)return;
            if(projectionBlock==null)projectionBlock=new MaterialPropertyBlock();
            renderer.GetPropertyBlock(projectionBlock);
            projectionBlock.SetMatrix("_ProjectorWorldToLocal",transform.worldToLocalMatrix);
            projectionBlock.SetMatrix("_ProjectorLocalToWorld",transform.localToWorldMatrix);
            projectionBlock.SetFloat("_ProjectorLength",EffectiveLength);
            projectionBlock.SetFloat("_CloseAmount",Mathf.Clamp01(closeAmount));
            projectionBlock.SetFloat("_FoldedCuffLength",foldedCuffLength);
            renderer.SetPropertyBlock(projectionBlock);
        }

        public Material ProjectionMaterial(Material source)
        {
            if(!source)return null;
            foreach(var value in projectionMaterials.Values)if(value==source)return source;
            if(projectionMaterials.TryGetValue(source,out var instance)&&instance)return instance;
            instance=new Material(source){name=source.name+" (sleeve instance)",hideFlags=HideFlags.HideAndDontSave};
            projectionMaterials[source]=instance;UpdateMaterial(instance);return instance;
        }
        void UpdateMaterial(Material material)
        {
            material.SetMatrix("_ProjectorWorldToLocal",transform.worldToLocalMatrix);
            material.SetMatrix("_ProjectorLocalToWorld",transform.localToWorldMatrix);
            material.SetFloat("_ProjectorLength",EffectiveLength);
            material.SetFloat("_CloseAmount",Mathf.Clamp01(closeAmount));
            material.SetFloat("_FoldedCuffLength",foldedCuffLength);
        }
        void OnDestroy()
        {
            foreach(var material in projectionMaterials.Values)if(material)
            {if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        }

        // u: turns around the arm (0 = +X, .25 = palm side); v: wrist end -> elbow end.
        public Pose SampleSurface(float u, float v, float offset = 0)
        {
            float t = Mathf.Clamp01(v);
            float angle = u * Mathf.PI * 2;
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            return new Pose(radial * (Mathf.Max(.005f, radius) + offset) +
                Vector3.forward * (t * EffectiveLength), Quaternion.LookRotation(-radial, Vector3.back));
        }

        void OnEnable()
        {
            Application.onBeforeRender += UpdateSleeve;
            UpdateSleeve();
        }

        void OnDisable() => Application.onBeforeRender -= UpdateSleeve;
        void LateUpdate()
        {
            if (Application.isPlaying && IsRollAssisting)
            {
                closeAmount = Mathf.MoveTowards(closeAmount, rollAssistTarget,
                    Time.unscaledDeltaTime / Mathf.Max(.01f, rollAssistDuration));
                if (closeAmount == rollAssistTarget) IsRollAssisting = false;
            }
            UpdateSleeve();
        }
        void OnValidate()
        {
            radius = Mathf.Max(.005f, radius); length = Mathf.Max(.02f, length);
            rollDownThreshold = Mathf.Min(rollDownThreshold, rollUpThreshold - .01f);
        }

        [BeforeRenderOrder(100)]
        public void UpdateSleeve()
        {
            EffectiveLength = Mathf.Max(.02f, length);
            if (wristAnchor)
            {
                var rotation = wristAnchor.rotation * Quaternion.Euler(wristRotationOffset);
                if (elbowAnchor)
                {
                    var segment = elbowAnchor.position - wristAnchor.position;
                    if (segment.sqrMagnitude > .000001f)
                    {
                        var forward = segment.normalized;
                        var up = Vector3.ProjectOnPlane(rotation * Vector3.up, forward);
                        if (up.sqrMagnitude < .000001f)
                            up = Vector3.Cross(forward, rotation * Vector3.right);
                        rotation = Quaternion.LookRotation(forward, up.normalized);
                        if (fitLengthToElbow) EffectiveLength = Mathf.Max(.02f, segment.magnitude);
                    }
                }
                if (transform.position != wristAnchor.position || transform.rotation != rotation)
                    transform.SetPositionAndRotation(wristAnchor.position, rotation);
            }
            if (!surface) return;
            var scale = new Vector3(Mathf.Max(.005f, radius), Mathf.Max(.005f, radius), EffectiveLength);
            if (surface.localScale != scale) surface.localScale = scale;
            var shape = new Vector3(radius, EffectiveLength, Mathf.Clamp01(closeAmount));
            if (!surfaceRenderer) surfaceRenderer = surface.GetComponent<Renderer>();
            if (surfaceRenderer && shape != lastShape)
            {
                if (surfaceProperties == null) surfaceProperties = new MaterialPropertyBlock();
                surfaceRenderer.GetPropertyBlock(surfaceProperties);
                surfaceProperties.SetFloat("_SleeveRadius", Mathf.Max(.005f, radius));
                surfaceProperties.SetFloat("_SleeveLength", EffectiveLength);
                surfaceProperties.SetFloat("_CloseAmount", shape.z);
                surfaceRenderer.SetPropertyBlock(surfaceProperties);
                lastShape = shape;
            }
            if(surfaceRenderer)
            {
                surfaceRenderer.enabled=showDebugCylinder;
                if(surfaceProperties==null)surfaceProperties=new MaterialPropertyBlock();
                surfaceRenderer.GetPropertyBlock(surfaceProperties);
                surfaceProperties.SetMatrix("_ProjectorWorldToLocal",transform.worldToLocalMatrix);
                surfaceProperties.SetMatrix("_ProjectorLocalToWorld",transform.localToWorldMatrix);
                surfaceProperties.SetFloat("_ProjectorLength",EffectiveLength);
                surfaceProperties.SetFloat("_FoldedCuffLength",foldedCuffLength);
                surfaceRenderer.SetPropertyBlock(surfaceProperties);
            }
            foreach(var material in projectionMaterials.Values)if(material)UpdateMaterial(material);
            // Unassigned prefab instances remain visible for editing; runtime needs a valid anchor.
            bool visible = !Application.isPlaying || wristAnchor;
            if (surface.gameObject.activeSelf != visible) surface.gameObject.SetActive(visible);
        }
    }
}
