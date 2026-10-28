using System;
using System.Linq;
using ForearmLab;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GestureSceneSetup
{
    public const string ScenePath = "Assets/Scenes/GestureXR.unity";

    [MenuItem("Forearm Lab/Open Gesture XR lab")]
    public static void Open() => EditorSceneManager.OpenScene(ScenePath);

    // One-time migration preserves the existing scene's rig and settings.
    public static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) throw new InvalidOperationException("GestureXR already exists; open it instead of migrating again");
        var source = EditorSceneManager.OpenScene("Assets/Scenes/ForearmXR.unity");
        EditorSceneManager.SaveScene(source, ScenePath, true);
        var gesture = EditorSceneManager.OpenScene(ScenePath);
        AddDisplay();
        EditorSceneManager.SaveScene(gesture);
        var forearm = EditorSceneManager.OpenScene("Assets/Scenes/ForearmXR.unity");
        var drawing = UnityEngine.Object.FindAnyObjectByType<GestureDrawing>();
        if (drawing) UnityEngine.Object.DestroyImmediate(drawing.gameObject);
        EditorSceneManager.SaveScene(forearm);
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, false));
        EditorBuildSettings.scenes = scenes.ToArray();
        Open();
        Debug.Log("GESTURE_XR_MIGRATED");
    }

    public static void AddDisplay()
    {
        if (!Shader.Find("TextMeshPro/Distance Field"))
            throw new InvalidOperationException("Import TMP essentials with TMP_PackageResourceImporter.ImportResources(true, false, false), then retry");
        var input = UnityEngine.Object.FindAnyObjectByType<LabInput>();
        if (!input || !input.leftHand || !input.drawing) throw new InvalidOperationException("Gesture XR needs the drawing logic and Meta hands");
        if (UnityEngine.Object.FindAnyObjectByType<InnerForearmDetectionDisplay>()) return;
        var oldAnchor = UnityEngine.Object.FindAnyObjectByType<ForearmAnchor>();
        if (oldAnchor) UnityEngine.Object.DestroyImmediate(oldAnchor.gameObject);

        var owner = new GameObject("Left inner forearm detection display").AddComponent<InnerForearmDetectionDisplay>();
        owner.leftHand = input.leftHand;
        owner.leftSkeleton = input.leftHand.GetComponent<OVRSkeleton>();
        owner.drawing = input.drawing; owner.input = input;
        owner.transform.SetPositionAndRotation(new Vector3(-.18f, 1.25f, .5f), Quaternion.identity);
        var panel = new GameObject("Detection Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(owner.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.sizeDelta = new Vector2(380, 640);
        rect.localScale = Vector3.one * .00028f;
        var canvas = panel.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = input.drawing.view;
        panel.GetComponent<UnityEngine.UI.CanvasScaler>().dynamicPixelsPerUnit = 1;
        var background = panel.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(.015f,.035f,.055f,.96f); background.raycastTarget = false;
        var text = new GameObject("Detection status", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(panel.transform, false);
        var tr = text.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(22,22); tr.offsetMax = new Vector2(-22,-22);
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Packages/com.meta.xr.sdk.interaction/Runtime/Sample/Fonts/Roboto-Regular SDF.asset");
        if (!text.font) throw new InvalidOperationException("Meta Roboto font missing");
        text.fontSize = 29; text.color = new Color(.65f,1f,.9f); text.raycastTarget = false;
        text.isOrthographic = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.richText = false; text.overflowMode = TextOverflowModes.Truncate;
        owner.display = canvas; owner.statusText = text; owner.RefreshText();
    }
}
