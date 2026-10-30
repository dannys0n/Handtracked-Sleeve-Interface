using UnityEngine;

namespace ForearmLab
{
    // Read-only presentation of Meta joints. Never feeds transforms back into tracking.
    [DefaultExecutionOrder(1250)]
    public class MetaBodyDebugView : MonoBehaviour
    {
        public MetaSleeveElbowBinding binding;
        public Material debugMaterial;
        public bool showDebugBody;
        [Min(.0005f)] public float lineWidth = .0025f;
        [Min(.001f)] public float jointRadius = .004f;
        [field: SerializeField] public string Status { get; private set; } = "Body debug off";
        public int VisibleMetaJoints { get; private set; }
        GameObject graphics;
        Transform[] markers;
        LineRenderer[] lines;
        LineRenderer fallback;
        Transform fallbackElbow;
        readonly Vector3[] points = new Vector3[27];
        readonly bool[] valid = new bool[27];

        static readonly OVRPlugin.BoneId[] joints = {
            OVRPlugin.BoneId.Body_Hips, OVRPlugin.BoneId.Body_SpineLower, OVRPlugin.BoneId.Body_SpineMiddle,
            OVRPlugin.BoneId.Body_SpineUpper, OVRPlugin.BoneId.Body_Chest, OVRPlugin.BoneId.Body_Neck, OVRPlugin.BoneId.Body_Head,
            OVRPlugin.BoneId.Body_LeftShoulder, OVRPlugin.BoneId.Body_LeftScapula, OVRPlugin.BoneId.Body_LeftArmUpper,
            OVRPlugin.BoneId.Body_LeftArmLower, OVRPlugin.BoneId.Body_LeftHandWristTwist, OVRPlugin.BoneId.Body_LeftHandWrist,
            OVRPlugin.BoneId.Body_RightShoulder, OVRPlugin.BoneId.Body_RightScapula, OVRPlugin.BoneId.Body_RightArmUpper,
            OVRPlugin.BoneId.Body_RightArmLower, OVRPlugin.BoneId.Body_RightHandWristTwist, OVRPlugin.BoneId.Body_RightHandWrist,
            OVRPlugin.BoneId.FullBody_LeftUpperLeg, OVRPlugin.BoneId.FullBody_LeftLowerLeg,
            OVRPlugin.BoneId.FullBody_LeftFootAnkle, OVRPlugin.BoneId.FullBody_LeftFootBall,
            OVRPlugin.BoneId.FullBody_RightUpperLeg, OVRPlugin.BoneId.FullBody_RightLowerLeg,
            OVRPlugin.BoneId.FullBody_RightFootAnkle, OVRPlugin.BoneId.FullBody_RightFootBall
        };
        static readonly int[,] edges = {
            {0,1},{1,2},{2,3},{3,4},{4,5},{5,6},
            {4,7},{7,8},{8,9},{9,10},{10,11},{11,12},
            {4,13},{13,14},{14,15},{15,16},{16,17},{17,18},
            {0,19},{19,20},{20,21},{21,22},{0,23},{23,24},{24,25},{25,26}
        };

        void OnEnable() => Application.onBeforeRender += Refresh;
        void LateUpdate() => Refresh();
        public void Toggle() => showDebugBody = !showDebugBody;

        [BeforeRenderOrder(125)]
        public void Refresh()
        {
            VisibleMetaJoints = 0;
            if (!showDebugBody || !binding || !debugMaterial)
            {
                if (graphics) graphics.SetActive(false);
                Status = !showDebugBody ? "Body debug off" : "Body debug references missing";
                return;
            }
            if (!graphics) Build();
            graphics.SetActive(true);
            for (int i = 0; i < joints.Length; i++)
            {
                valid[i] = binding.TryGetBodyJoint(joints[i], out points[i]);
                markers[i].gameObject.SetActive(valid[i]);
                if (!valid[i]) continue;
                VisibleMetaJoints++;
                markers[i].position = points[i];
                markers[i].localScale = Vector3.one * jointRadius * (i == 10 || i == 16 ? 4 : 2);
            }
            for (int i = 0; i < lines.Length; i++)
            {
                int a = edges[i,0], b = edges[i,1];
                lines[i].enabled = valid[a] && valid[b];
                if (!lines[i].enabled) continue;
                lines[i].widthMultiplier = lineWidth;
                lines[i].SetPosition(0, points[a]); lines[i].SetPosition(1, points[b]);
            }
            bool localEstimate = VisibleMetaJoints == 0 && binding.useElbow && binding.sleeve && binding.sleeve.wristAnchor &&
                binding.TryGetEstimatedShoulder(out _, out _);
            fallback.enabled = localEstimate; fallbackElbow.gameObject.SetActive(localEstimate);
            if (localEstimate)
            {
                binding.TryGetEstimatedShoulder(out var shoulder, out _);
                fallback.widthMultiplier = lineWidth;
                fallback.SetPosition(0, shoulder); fallback.SetPosition(1, binding.transform.position);
                fallback.SetPosition(2, binding.sleeve.wristAnchor.position);
                fallbackElbow.position = binding.transform.position;
                fallbackElbow.localScale = Vector3.one * jointRadius * 4;
            }
            Status = VisibleMetaJoints > 0 ? $"Meta body: {VisibleMetaJoints} joints; large dots = elbows" :
                localEstimate ? "Local arm estimate only (no Meta body data)" : "No valid body or wrist samples";
        }
        void Build()
        {
            graphics = new GameObject("Body debug graphics") { hideFlags = HideFlags.DontSave };
            graphics.transform.SetParent(transform, false);
            markers = new Transform[joints.Length];
            for (int i = 0; i < joints.Length; i++) markers[i] = Marker(joints[i].ToString());
            lines = new LineRenderer[edges.GetLength(0)];
            for (int i = 0; i < lines.Length; i++) lines[i] = Line("Body segment " + i, 2);
            fallback = Line("Local estimated arm (not Meta body)", 3);
            fallbackElbow = Marker("Local estimated elbow");
        }
        Transform Marker(string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name; go.layer = 2;
            go.transform.SetParent(graphics.transform, false);
            var collider = go.GetComponent<Collider>(); collider.enabled = false;
            if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = debugMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            return go.transform;
        }
        LineRenderer Line(string name, int count)
        {
            var go = new GameObject(name); go.layer = 2; go.transform.SetParent(graphics.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = debugMaterial;
            line.useWorldSpace = true; line.positionCount = count; line.widthMultiplier = lineWidth;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }
        void OnDisable() { Application.onBeforeRender -= Refresh; if (graphics) graphics.SetActive(false); }
        void OnDestroy()
        {
            if (!graphics) return;
            if (Application.isPlaying) Destroy(graphics); else DestroyImmediate(graphics);
        }
    }
}
