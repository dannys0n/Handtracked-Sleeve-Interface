using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace ForearmLab
{
    // Owns background presentation only. Sleeve placement and hand input stay independent.
    public class LabEnvironmentMode : MonoBehaviour
    {
        public enum Mode { VR, Passthrough }
        public enum StartupMode { SystemRecommendation, VR, Passthrough }

        public OVRManager manager;
        public OVRPassthroughLayer passthroughLayer;
        public Camera view;
        [Tooltip("Optional virtual scenery to hide in passthrough mode. Do not include the rig, hands, or sleeve.")]
        public GameObject vrEnvironment;
        public StartupMode startupMode = StartupMode.SystemRecommendation;
        public bool showDesktopControls = true;
        public bool enableShortcuts = true;
        [Min(1)] public float startupTimeout = 15;
        public Mode CurrentMode { get; private set; } = Mode.VR;
        public bool IsStartingPassthrough { get; private set; }
        public string Status { get; private set; } = "VR";

        CameraClearFlags vrClearFlags;
        Color vrBackground;
        bool vrEnvironmentActive;
        bool captured, userSelected;
        float requestTime;
        bool initializedByThis;
        Camera[] rigCameras;
        CameraClearFlags[] clearFlags;
        Color[] backgrounds;
        bool[] postProcessing;

        void Awake()
        {
            if (!view || !manager || !passthroughLayer)
            { Status = "Environment mode references missing"; enabled = false; return; }
            vrClearFlags = view.clearFlags;
            vrBackground = view.backgroundColor; vrBackground.a = 1;
            vrEnvironmentActive = vrEnvironment && vrEnvironment.activeSelf;
            var rig=view.GetComponentInParent<OVRCameraRig>();
            rigCameras=rig?rig.GetComponentsInChildren<Camera>(true):new[]{view};
            clearFlags=new CameraClearFlags[rigCameras.Length];backgrounds=new Color[rigCameras.Length];postProcessing=new bool[rigCameras.Length];
            for(int i=0;i<rigCameras.Length;i++)
            {
                clearFlags[i]=rigCameras[i].clearFlags;backgrounds[i]=rigCameras[i].backgroundColor;
                var data=rigCameras[i].GetComponent<UniversalAdditionalCameraData>();postProcessing[i]=data&&data.renderPostProcessing;
            }
            captured = true;
        }

        void OnEnable()
        {
            if (passthroughLayer) passthroughLayer.passthroughLayerResumed.AddListener(LayerReady);
            OVRManager.OnPassthroughInitializedStateChange += InitializationChanged;
        }

        IEnumerator Start()
        {
            ApplyVR("VR");
            float deadline = Time.unscaledTime + startupTimeout;
            while ((!RuntimeReady || !OVRManager.IsInsightPassthroughSupported()) && Time.unscaledTime < deadline && !userSelected) yield return null;
            if (userSelected) yield break;
            if (!RuntimeReady) { ApplyVR("VR - XR runtime is unavailable"); yield break; }
            if (startupMode == StartupMode.Passthrough ||
                (startupMode == StartupMode.SystemRecommendation && OVRManager.IsPassthroughRecommended()))
                BeginPassthrough(true);
        }

        bool RuntimeReady => OVRPlugin.initialized && OVRManager.instance != null;

        void Update()
        {
            if (enableShortcuts && ((Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame) ||
                (RuntimeReady && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch)))) Toggle();
            if (!IsStartingPassthrough && CurrentMode != Mode.Passthrough) return;
            if (!RuntimeReady) ApplyVR("VR - XR runtime is unavailable");
            else if (OVRManager.HasInsightPassthroughInitFailed()) ApplyVR("VR - passthrough initialization failed");
            else if (IsStartingPassthrough && Time.unscaledTime - requestTime > startupTimeout)
                ApplyVR("VR - passthrough timed out. Link: enable Passthrough over Link in Meta Quest Link Settings > Beta, then restart Play.");
        }

        [ContextMenu("Use VR (Play Mode)")]
        public void UseVR()
        {
            if (!Application.isPlaying) return;
            userSelected = true; ApplyVR("VR");
        }

        [ContextMenu("Use passthrough (Play Mode)")]
        public void UsePassthrough()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            userSelected = true; BeginPassthrough();
        }

        public void Toggle()
        {
            if (CurrentMode == Mode.Passthrough || IsStartingPassthrough) UseVR();
            else UsePassthrough();
        }

        void BeginPassthrough(bool atStartup=false)
        {
            if (!captured || !isActiveAndEnabled) return;
            if (!RuntimeReady) { ApplyVR("VR - XR runtime is not ready"); return; }
            if (!OVRManager.IsInsightPassthroughSupported()) { ApplyVR("VR - runtime reports no passthrough support. Check Meta Quest Link Settings > Beta > Passthrough over Link."); return; }
            if (IsStartingPassthrough || CurrentMode == Mode.Passthrough) return;
            IsStartingPassthrough = true; requestTime = Time.unscaledTime;
            Status = "Starting passthrough...";
            manager.isInsightPassthroughEnabled = true;
            initializedByThis=true;
            passthroughLayer.hidden = false;
            passthroughLayer.textureOpacity = 1;
            passthroughLayer.enabled = true;
            // Meta's contextual startup can already be presenting passthrough, without
            // a new resume event. At startup, configure transparency immediately;
            // in-session VR -> MR transitions still wait for the resumed callback.
            if(atStartup)LayerReady(passthroughLayer);
        }

        void LayerReady(OVRPassthroughLayer layer)
        {
            if (!isActiveAndEnabled || !IsStartingPassthrough || layer != passthroughLayer) return;
            // Keep the VR background until the compositor has presented the passthrough layer.
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Color.clear;
            for(int i=0;i<rigCameras.Length;i++)
            {
                rigCameras[i].clearFlags=CameraClearFlags.SolidColor;rigCameras[i].backgroundColor=Color.clear;
                // Post processing can force opaque alpha. Forearm UI does not need it.
                var data=rigCameras[i].GetComponent<UniversalAdditionalCameraData>();if(data)data.renderPostProcessing=false;
            }
            if (vrEnvironment) vrEnvironment.SetActive(false);
            CurrentMode = Mode.Passthrough; IsStartingPassthrough = false;
            Status = "Passthrough";
        }

        void InitializationChanged(bool initialized)
        {
            if (!initialized && OVRManager.HasInsightPassthroughInitFailed() && CurrentMode == Mode.Passthrough)
                ApplyVR("VR - passthrough initialization failed; check the active XR runtime");
        }

        void ApplyVR(string status)
        {
            CurrentMode = Mode.VR; IsStartingPassthrough = false; Status = status;
            if (!captured) return;
            if (view) { view.clearFlags = vrClearFlags; view.backgroundColor = vrBackground; }
            if(rigCameras!=null)for(int i=0;i<rigCameras.Length;i++)if(rigCameras[i])
            {
                rigCameras[i].clearFlags=clearFlags[i];var color=backgrounds[i];color.a=1;rigCameras[i].backgroundColor=color;
                var data=rigCameras[i].GetComponent<UniversalAdditionalCameraData>();if(data)data.renderPostProcessing=postProcessing[i];
            }
            if (vrEnvironment) vrEnvironment.SetActive(vrEnvironmentActive);
            if (passthroughLayer) { passthroughLayer.hidden = true; passthroughLayer.enabled = false; }
            // Keep the subsystem warm while switching modes; the layer owns visibility.
            // Repeated native shutdown/reinitialization can race a pending resume event.
        }

        void OnDisable()
        {
            if (passthroughLayer) passthroughLayer.passthroughLayerResumed.RemoveListener(LayerReady);
            OVRManager.OnPassthroughInitializedStateChange -= InitializationChanged;
            if (Application.isPlaying) ApplyVR("VR");
            if(initializedByThis&&manager)manager.isInsightPassthroughEnabled=false;
        }

        void OnGUI()
        {
            if (!showDesktopControls) return;
            GUILayout.BeginArea(new Rect(12, 12, 440, 128), GUI.skin.box);
            GUILayout.Label("FOREARM LAB  /  " + Status);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("VR")) UseVR();
            if (GUILayout.Button("Passthrough")) UsePassthrough();
            GUILayout.EndHorizontal();
            GUILayout.Label("P: switch mode  |  Right controller A: switch mode");
            GUILayout.EndArea();
        }
    }
}
