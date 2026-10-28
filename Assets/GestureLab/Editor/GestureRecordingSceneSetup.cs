using System;
using System.Linq;
using ForearmLab;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GestureRecordingSceneSetup
{
    public const string ScenePath = "Assets/GestureLab/Scenes/GestureRecording.unity";
    const string RowPath = "Assets/GestureLab/Prefabs/GestureRecordingRow.prefab";
    static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

    [MenuItem("Gesture Lab/Open recording scene")]
    public static void Open() => EditorSceneManager.OpenScene(ScenePath);

    [MenuItem("Gesture Lab/Create desktop recording scene")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before creating the recorder");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) throw new InvalidOperationException("Recording scene already exists");
        if (!Font) throw new InvalidOperationException("TMP essential resources missing");
        var source = EditorSceneManager.OpenScene("Assets/GestureLab/Scenes/GestureDesktop.unity");
        EditorSceneManager.SaveScene(source, ScenePath, true);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var input = UnityEngine.Object.FindAnyObjectByType<LabInput>();
        input.showOverlay = false;
        var anchor = UnityEngine.Object.FindAnyObjectByType<ForearmAnchor>();
        if (anchor) UnityEngine.Object.DestroyImmediate(anchor.gameObject);
        var canvas = new GameObject("Gesture recording UI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
        var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();

        var draw = Rect("Draw area", canvas.transform, new Vector2(0,0), new Vector2(.6f,1), new Vector2(24,24), new Vector2(-12,-24));
        var tint = draw.gameObject.AddComponent<UnityEngine.UI.Image>(); tint.color = new Color(.05f,.12f,.17f,.18f); tint.raycastTarget = false;
        input.desktopDrawArea = draw;
        Text("Title", draw, "GESTURE RECORDING", 36, new Vector2(0,1), new Vector2(1,1), new Vector2(20,-70), new Vector2(-20,-15));
        Text("Help", draw, "Hold left mouse to draw. Release between strokes.\nWheel: drawing depth  |  Enter: recognize  |  C: clear", 24, new Vector2(0,1), new Vector2(1,1), new Vector2(20,-145), new Vector2(-20,-78));
        var status = Text("Drawing status", draw, "Ready", 24, Vector2.zero, new Vector2(1,0), new Vector2(20,18), new Vector2(-20,110));

        var sidebar = Rect("Recordings", canvas.transform, new Vector2(.6f,0), Vector2.one, new Vector2(12,24), new Vector2(-24,-24));
        sidebar.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.035f,.055f,.08f,1);
        Text("Heading", sidebar, "Save a gesture", 32, new Vector2(0,1), Vector2.one, new Vector2(24,-65), new Vector2(-24,-18));
        var fieldRect = Rect("Gesture name", sidebar, new Vector2(0,1), Vector2.one, new Vector2(24,-125), new Vector2(-24,-75));
        fieldRect.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.12f,.17f,.22f,1);
        var field = fieldRect.gameObject.AddComponent<TMP_InputField>();
        var textArea = Rect("Text area", fieldRect, Vector2.zero, Vector2.one, new Vector2(14,5), new Vector2(-14,-5));
        textArea.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        var placeholder = Text("Placeholder", textArea, "Gesture name (e.g. Spiral)", 24, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        placeholder.color = new Color(.6f,.67f,.72f,1);
        var fieldText = Text("Name text", textArea, "", 24, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        field.textViewport = textArea; field.textComponent = fieldText; field.placeholder = placeholder;
        field.characterLimit = 64; field.richText = false; field.lineType = TMP_InputField.LineType.SingleLine;
        var save = Button("Save", sidebar, "Save sample", new Vector2(0,1), new Vector2(.5f,1), new Vector2(24,-190), new Vector2(-8,-140));
        var clear = Button("Clear", sidebar, "Clear drawing", new Vector2(.5f,1), Vector2.one, new Vector2(8,-190), new Vector2(-24,-140));
        var recognize = Button("Recognize", sidebar, "Test recognition", new Vector2(0,1), Vector2.one, new Vector2(24,-250), new Vector2(-24,-202));
        var message = Text("Message", sidebar, "All gesture XML samples appear below.\nSave and Delete edit the shared gesture library.", 20, new Vector2(0,1), Vector2.one, new Vector2(24,-333), new Vector2(-24,-263));
        var count = Text("List heading", sidebar, "Recorded samples (0)", 26, new Vector2(0,1), Vector2.one, new Vector2(24,-382), new Vector2(-24,-340));
        var scrollRect = Rect("Vertical recordings list", sidebar, Vector2.zero, Vector2.one, new Vector2(18,55), new Vector2(-18,-390));
        var scroll = scrollRect.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
        var viewport = Rect("Viewport", scrollRect, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-22,0));
        viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.025f,.04f,.06f,1);
        var content = Rect("Content", viewport, new Vector2(0,1), Vector2.one, Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(.5f,1);
        var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(10,10,10,10); layout.spacing = 10;
        layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        var bar = Rect("Vertical scrollbar", scrollRect, new Vector2(1,0), Vector2.one, new Vector2(-16,0), Vector2.zero);
        bar.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.08f,.12f,.16f,1);
        var scrollbar = bar.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
        scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        var slide = Rect("Sliding area",bar,Vector2.zero,Vector2.one,new Vector2(2,2),new Vector2(-2,-2));
        var handle = Rect("Handle",slide,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var handleImage = handle.gameObject.AddComponent<UnityEngine.UI.Image>(); handleImage.color = new Color(.25f,.65f,.7f,1);
        scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage;
        scroll.verticalScrollbar = scrollbar;
        Text("List help", sidebar, "Mouse wheel / drag to scroll. Delete removes that sample.", 18, Vector2.zero, new Vector2(1,0), new Vector2(24,8), new Vector2(-24,47));

        if (!AssetDatabase.IsValidFolder("Assets/GestureLab/Prefabs")) AssetDatabase.CreateFolder("Assets/GestureLab", "Prefabs");
        var row = Rect("GestureRecordingRow", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.sizeDelta = new Vector2(500,90);
        row.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.085f,.13f,.18f,1);
        row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 90;
        Text("Name and time", row, "Gesture name\nRecorded time", 23, Vector2.zero, new Vector2(.72f,1), new Vector2(14,8), new Vector2(-8,-8));
        var delete = Button("Delete", row, "Delete", new Vector2(.72f,0), Vector2.one, new Vector2(0,18), new Vector2(-12,-18));
        delete.GetComponent<UnityEngine.UI.Image>().color = new Color(.48f,.12f,.15f,1);
        var rowAsset = PrefabUtility.SaveAsPrefabAsset(row.gameObject, RowPath);
        UnityEngine.Object.DestroyImmediate(row.gameObject);
        var panel = sidebar.gameObject.AddComponent<GestureRecordingPanel>();
        panel.drawing = input.drawing; panel.nameField = field; panel.message = message;
        panel.drawingStatus = status; panel.count = count; panel.content = content; panel.rowPrefab = rowAsset;
        panel.saveButton = save; panel.clearButton = clear; panel.recognizeButton = recognize;
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,false)}).ToArray();
        AssetDatabase.SaveAssets();
    }

    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 bottom, Vector2 top)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        if (parent) rect.SetParent(parent,false);
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = bottom; rect.offsetMax = top;
        return rect;
    }

    static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Vector2 min, Vector2 max, Vector2 bottom, Vector2 top)
    {
        var text = Rect(name,parent,min,max,bottom,top).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = Font; text.fontSize = size; text.isOrthographic = true; text.text = value;
        text.color = new Color(.82f,.94f,1,1); text.raycastTarget = false; text.richText = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    static UnityEngine.UI.Button Button(string name, Transform parent, string value, Vector2 min, Vector2 max, Vector2 bottom, Vector2 top)
    {
        var rect = Rect(name,parent,min,max,bottom,top);
        rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.1f,.32f,.4f,1);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        var label = Text("Label",rect,value,22,Vector2.zero,Vector2.one,new Vector2(4,2),new Vector2(-4,-2));
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }
}
