using System;
using ForearmLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnvironmentModeSetup
{
    [MenuItem("Forearm Lab/Set up VR and passthrough")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/ForearmLab/Scenes/ForearmXR.unity") throw new InvalidOperationException("Open ForearmXR first");
        AddToScene(); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
    }

    public static void AddToScene()
    {
        var config = OVRProjectConfig.CachedProjectConfig;
        config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Supported;
        OVRProjectConfig.CommitProjectConfig(config);
        if (UnityEngine.Object.FindAnyObjectByType<LabEnvironmentMode>()) return;
        var rig = UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>();
        if (!rig) throw new InvalidOperationException("OVRCameraRig missing");
        var root = new GameObject("Environment mode");
        var layer = root.AddComponent<OVRPassthroughLayer>();
        layer.hidden = true; layer.enabled = false;
        var controller = root.AddComponent<LabEnvironmentMode>();
        controller.manager = rig.GetComponent<OVRManager>(); controller.passthroughLayer = layer;
        controller.view = rig.centerEyeAnchor.GetComponent<Camera>();
        controller.startupMode = LabEnvironmentMode.StartupMode.SystemRecommendation;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
