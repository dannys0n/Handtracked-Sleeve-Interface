using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

        void Awake()
        {
            if (!view || !manager || !passthroughLayer)
            { Status = "Environment mode references missing"; enabled = false; return; }
            vrClearFlags = view.clearFlags;
            vrBackground = view.backgroundColor; vrBackground.a = 1;
            vrEnvironmentActive = vrEnvironment && vrEnvironment.activeSelf;
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
            while (!RuntimeReady && Time.unscaledTime < deadline && !userSelected) yield return null;
            if (userSelected) yield break;
            if (!RuntimeReady) { ApplyVR("VR - XR runtime is unavailable"); yield break; }
            if (startupMode == StartupMode.Passthrough ||
                (startupMode == StartupMode.SystemRecommendation && OVRManager.IsPassthroughRecommended()))
                BeginPassthrough();
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
                ApplyVR("VR - passthrough did not become ready; check the runtime or simulator environment");
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

        void BeginPassthrough()
        {
            if (!captured || !isActiveAndEnabled) return;
            if (!RuntimeReady) { ApplyVR("VR - XR runtime is not ready"); return; }
            if (!OVRManager.IsInsightPassthroughSupported()) { ApplyVR("VR - this runtime does not support passthrough"); return; }
            if (IsStartingPassthrough || CurrentMode == Mode.Passthrough) return;
            IsStartingPassthrough = true; requestTime = Time.unscaledTime;
            Status = "Starting passthrough...";
            manager.isInsightPassthroughEnabled = true;
            passthroughLayer.hidden = false;
            passthroughLayer.textureOpacity = 1;
            passthroughLayer.enabled = true;
        }

        void LayerReady(OVRPassthroughLayer layer)
        {
            if (!isActiveAndEnabled || !IsStartingPassthrough || layer != passthroughLayer) return;
            // Keep the VR background until the compositor has presented the passthrough layer.
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Color.clear;
            if (vrEnvironment) vrEnvironment.SetActive(false);
            CurrentMode = Mode.Passthrough; IsStartingPassthrough = false;
            Status = "Passthrough";
        }

        void InitializationChanged(bool initialized)
        {
            if (!initialized && CurrentMode == Mode.Passthrough) ApplyVR("VR - passthrough became unavailable");
        }

        void ApplyVR(string status)
        {
            CurrentMode = Mode.VR; IsStartingPassthrough = false; Status = status;
            if (!captured) return;
            if (view) { view.clearFlags = vrClearFlags; view.backgroundColor = vrBackground; }
            if (vrEnvironment) vrEnvironment.SetActive(vrEnvironmentActive);
            if (passthroughLayer) { passthroughLayer.hidden = true; passthroughLayer.enabled = false; }
            if (manager) manager.isInsightPassthroughEnabled = false;
        }

        void OnDisable()
        {
            if (passthroughLayer) passthroughLayer.passthroughLayerResumed.RemoveListener(LayerReady);
            OVRManager.OnPassthroughInitializedStateChange -= InitializationChanged;
            if (Application.isPlaying) ApplyVR("VR");
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
