using System;
using System.IO;
using System.Linq;
using ForearmLab;
using PDollarGestureRecognizer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

public static class LabSetup
{
    const string XRScene = "Assets/ForearmLab/Scenes/ForearmXR.unity";
    const string DesktopScene = "Assets/GestureLab/Scenes/GestureDesktop.unity";
    const string GestureScene = "Assets/GestureLab/Scenes/GestureXR.unity";

    [MenuItem("Forearm Lab/Rebuild test scenes")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rebuilding the test scenes");
        Configure();
        CreateScene(false);
        CreateScene(false, true);
        CreateScene(true);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(XRScene, true), new EditorBuildSettingsScene(GestureScene, false), new EditorBuildSettingsScene(DesktopScene, false) };
        AssetDatabase.SaveAssets();
        VerifyRecognition();
        Debug.Log("FOREARM_LAB_SETUP_OK");
    }

    static void Configure()
    {
        var perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
        foreach (var group in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
        {
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var settings = perTarget.SettingsForBuildTarget(group);
            settings.InitManagerOnStart = group == BuildTargetGroup.Android;
            XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);
            settings.Manager.automaticLoading = true;
            settings.Manager.automaticRunning = true;
            EditorUtility.SetDirty(settings.Manager);
            EditorUtility.SetDirty(settings);
            var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            foreach (var feature in xr.GetFeatures())
            {
                string type = feature.GetType().Name;
                if (type == "MetaXRFeature" || type == "OculusTouchControllerProfile") feature.enabled = true;
            }
            EditorUtility.SetDirty(xr);
        }
        EditorUtility.SetDirty(perTarget);
        var config = OVRProjectConfig.CachedProjectConfig;
        config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
        OVRProjectConfig.CommitProjectConfig(config);
        PlayerSettings.companyName = "Danny Son";
        PlayerSettings.productName = "Forearm Gesture Lab";
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.dannyson.forearmlab");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
    }

    static GameObject PackagePrefab(string name)
    {
        string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(name + " t:Prefab")
            .First(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == name));
        return (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    }

    static void CreateScene(bool desktop, bool gesture = false)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(.45f, .5f, .6f);
        Camera camera;
        OVRSkeleton left = null, right = null;
        OVRHand rightHand = null;
        OVRHand leftHand = null;
        if (desktop)
        {
            camera = new GameObject("Desktop camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 1.4f, 0);
            camera.tag = "MainCamera";
        }
        else
        {
            var rigObject = PackagePrefab("OVRCameraRig");
            var rig = rigObject.GetComponent<OVRCameraRig>();
            camera = rig.centerEyeAnchor.GetComponent<Camera>();
            camera.tag = "MainCamera";
            var lh = PackagePrefab("OVRHandPrefab");
            lh.name = "Left hand"; lh.transform.SetParent(rig.leftHandAnchor, false);
            var rh = PackagePrefab("OVRHandPrefab");
            rh.name = "Right hand"; rh.transform.SetParent(rig.rightHandAnchor, false);
            foreach (var component in rh.GetComponents<Component>())
            {
                var so = new SerializedObject(component);
                foreach (string field in new[] { "HandType", "_skeletonType", "_meshType" })
                {
                    var p = so.FindProperty(field);
                    if (p != null) p.intValue = 1;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            left = lh.GetComponent<OVRSkeleton>(); right = rh.GetComponent<OVRSkeleton>(); rightHand = rh.GetComponent<OVRHand>(); leftHand = lh.GetComponent<OVRHand>();
            rigObject.AddComponent<LabXRStartup>();
        }
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.025f, .035f, .055f);
        var sun = new GameObject("Light", typeof(Light)).GetComponent<Light>();
        sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(50, -30, 0);
        var lab = new GameObject("Gesture Drawing");
        var drawing = lab.AddComponent<GestureDrawing>(); drawing.view = camera;
        var input = lab.AddComponent<LabInput>(); input.drawing = drawing; input.desktop = desktop;
        input.rightHand = rightHand; input.rightSkeleton = right;
        input.leftHand = leftHand;
        var anchor = new GameObject("Left forearm UI anchor (estimated)").AddComponent<ForearmAnchor>();
        anchor.desktop = desktop; anchor.leftSkeleton = left;
        var preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
        preview.name = "Forearm UI placeholder"; preview.transform.SetParent(anchor.transform, false);
        preview.transform.localScale = new Vector3(.085f, .003f, .16f);
        UnityEngine.Object.DestroyImmediate(preview.GetComponent<Collider>());
        anchor.preview = preview.transform;
        anchor.transform.position = new Vector3(-.2f, 1.15f, .5f);
        if (gesture) GestureSceneSetup.AddDisplay();
        else if (!desktop) UnityEngine.Object.DestroyImmediate(lab);
        foreach (var root in scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                    throw new Exception("Missing script in generated scene: " + child.name);
        EditorSceneManager.SaveScene(scene, desktop ? DesktopScene : gesture ? GestureScene : XRScene);
    }

    [MenuItem("Forearm Lab/Open desktop lab")]
    public static void OpenDesktop() => EditorSceneManager.OpenScene(DesktopScene);

    [MenuItem("Forearm Lab/Open Meta XR lab")]
    public static void OpenXR() => EditorSceneManager.OpenScene(XRScene);

    [MenuItem("Forearm Lab/Verify recognition")]
    public static void VerifyRecognition()
    {
        var templates = Resources.LoadAll<TextAsset>("CustomGestures").Select(x => GestureIO.ReadGestureFromXML(x.text)).ToArray();
        if (templates.Length != 4) throw new Exception("Expected four migrated templates");
        foreach (var template in templates)
        {
            var result = PointCloudRecognizer.Classify(template, templates);
            if (result.GestureClass != template.Name || result.Score < .99f) throw new Exception("Template recognition failed: " + template.Name);
        }
        Debug.Log("FOREARM_LAB_RECOGNITION_OK: X/T/O/M self recognition");
    }
}
