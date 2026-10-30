using System.Collections;
using UnityEngine;

namespace ForearmLab
{
    // Scene adapter; the sleeve itself continues to accept two ordinary transforms.
    [DefaultExecutionOrder(950)]
    public class MetaSleeveElbowBinding : MonoBehaviour
    {
        public WristSleeve sleeve;
        public Transform head, trackingSpace;
        public OVRBody body;
        public bool useElbow = true;
        [Tooltip("Automatically aim the sleeve at valid Meta body elbow data. Missing/invalid body data restores wrist-only attachment; the local estimate never drives the sleeve.")]
        public bool driveSleeve = true;
        public bool requestMetaBody = true;
        [Min(.1f)] public float upperArmLength = .29f, forearmLength = .24f;
        public Vector3 shoulderFromHead = new Vector3(-.18f, -.20f, -.04f);
        [Min(0)] public float smoothing = 15;
        [field: SerializeField, Tooltip("Runtime source actually being used. Meta body output is still inferred on Quest 2; head + wrist is the local fallback.")]
        public string Status { get; private set; } = "Wrist only";
        public bool HasMetaElbow { get; private set; }
        bool positioned;
        bool previousMetaSource;
        int lastPositionFrame = -1;
        OVRBody ownedBody;

        void OnEnable() => Application.onBeforeRender += UpdateElbow;

        IEnumerator Start()
        {
            float deadline = Time.unscaledTime + 15;
            while (!OVRPlugin.initialized && Time.unscaledTime < deadline) yield return null;
            if (!requestMetaBody || body || !OVRPlugin.initialized || !OVRPlugin.bodyTrackingSupported) yield break;
            OVRPermissionsRequester.Request(new[] { OVRPermissionsRequester.Permission.BodyTracking });
            // Permission may be answered later; fallback remains usable in the meantime.
            while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.BodyTracking)) yield return null;
            ownedBody = gameObject.AddComponent<OVRBody>();
            body = ownedBody;
        }

        void LateUpdate() => UpdateElbow();

        [BeforeRenderOrder(95)]
        void UpdateElbow()
        {
            if (!sleeve) return;
            HasMetaElbow = false;
            if (!useElbow)
            { Release(); Status = "Elbow estimation disabled; wrist only"; return; }
            if (!sleeve.wristAnchor)
            { Release(); Status = "Waiting for wrist tracking"; return; }
            Vector3 wrist = sleeve.wristAnchor.position;
            Vector3 target;
            if (TryGetBodyJoint(OVRPlugin.BoneId.Body_LeftArmLower, out target))
            {
                float distance = Vector3.Distance(wrist, target);
                HasMetaElbow = distance >= .1f && distance <= .5f;
            }
            if (!HasMetaElbow)
            {
                if (!head) { Release(); Status = "No valid elbow data; wrist only"; return; }
                target = Estimate(wrist);
            }
            else Status = driveSleeve ? "Meta body elbow (automatic)" : "Meta body elbow available; sleeve drive off";

            // Smooth once per frame. Before-render refreshes availability after the wrist
            // adapter, without applying a second smoothing step to the same body sample.
            if (lastPositionFrame != Time.frameCount || !positioned || previousMetaSource != HasMetaElbow)
            {
                transform.position = positioned && previousMetaSource == HasMetaElbow && smoothing > 0
                    ? Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-smoothing * Time.unscaledDeltaTime)) : target;
                lastPositionFrame = Time.frameCount;
                previousMetaSource = HasMetaElbow;
                positioned = true;
            }
            if (driveSleeve && HasMetaElbow) sleeve.elbowAnchor = transform;
            else DetachSleeve();
        }

        // Read-only adapter: Meta's right-handed tracking position -> Unity tracking
        // position -> world exactly once. Debug graphics use this same conversion.
        public bool TryGetBodyJoint(OVRPlugin.BoneId joint, out Vector3 world)
        {
            world = default;
            if (!body || !body.isActiveAndEnabled || !trackingSpace) return false;
            var state = body.BodyState;
            int index = (int)joint;
            if (!state.HasValue || !(state.Value.Confidence > .5f) || state.Value.JointLocations == null ||
                index < 0 || index >= state.Value.JointLocations.Length || !state.Value.JointLocations[index].PositionValid) return false;
            var position = state.Value.JointLocations[index].Pose.Position.FromFlippedZVector3f();
            if (!Finite(position)) return false;
            world = trackingSpace.TransformPoint(position);
            return Finite(world);
        }
        static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
            !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);

        Vector3 Estimate(Vector3 wrist)
        {
            Status = "Local elbow estimate (debug only); wrist-only sleeve";
            TryGetEstimatedShoulder(out var shoulder, out var yaw);
            var pole = yaw * new Vector3(-.65f, -1, -.25f);
            return EstimateElbow(shoulder, wrist, pole, upperArmLength, forearmLength);
        }

        public bool TryGetEstimatedShoulder(out Vector3 shoulder, out Quaternion yaw)
        {
            shoulder = default; yaw = Quaternion.identity;
            if (!head) return false;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            yaw = Quaternion.LookRotation(forward.normalized, Vector3.up);
            shoulder = head.position + yaw * shoulderFromHead;
            return true;
        }

        // The wrist is never moved. Outside reach, keep the configured forearm length.
        public static Vector3 EstimateElbow(Vector3 shoulder, Vector3 wrist, Vector3 pole, float upper, float lower)
        {
            upper = Mathf.Max(.1f, upper); lower = Mathf.Max(.1f, lower);
            var delta = shoulder - wrist;
            float distance = delta.magnitude;
            var axis = distance > .0001f ? delta / distance : Vector3.up;
            float reach = Mathf.Clamp(distance, Mathf.Abs(upper - lower) + .0001f, upper + lower - .0001f);
            float along = (lower * lower + reach * reach - upper * upper) / (2 * reach);
            float height = Mathf.Sqrt(Mathf.Max(0, lower * lower - along * along));
            var bend = Vector3.ProjectOnPlane(pole, axis);
            if (bend.sqrMagnitude < .0001f) bend = Vector3.Cross(axis, Vector3.right);
            if (bend.sqrMagnitude < .0001f) bend = Vector3.Cross(axis, Vector3.forward);
            return wrist + axis * along + bend.normalized * height;
        }
        void DetachSleeve() { if (sleeve && sleeve.elbowAnchor == transform) sleeve.elbowAnchor = null; }
        void Release() { positioned = false; HasMetaElbow = false; DetachSleeve(); }
        void OnDisable() { Application.onBeforeRender -= UpdateElbow; Release(); }
        void OnDestroy() { if (ownedBody) Destroy(ownedBody); }
    }
}
