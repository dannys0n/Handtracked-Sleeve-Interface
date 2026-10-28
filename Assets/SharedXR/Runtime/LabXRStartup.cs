using UnityEngine;
using UnityEngine.XR.Management;

namespace ForearmLab
{
    // Desktop editor XR starts only in this scene. The desktop lab never starts a runtime.
    [DefaultExecutionOrder(-10000)]
    public class LabXRStartup : MonoBehaviour
    {
        bool ownsRuntime;
        void Awake()
        {
            var manager = XRGeneralSettings.Instance.Manager;
            if (manager.activeLoader) return;
            manager.InitializeLoaderSync();
            if (!manager.activeLoader) { Debug.LogError("No OpenXR runtime. Activate Meta XR Simulator, or open GestureDesktop."); return; }
            manager.StartSubsystems();
            ownsRuntime = true;
        }

        void OnDestroy()
        {
            if (!ownsRuntime || !XRGeneralSettings.Instance) return;
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
    }
}
