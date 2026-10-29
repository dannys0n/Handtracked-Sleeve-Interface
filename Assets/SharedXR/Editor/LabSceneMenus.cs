using UnityEditor;
using UnityEditor.SceneManagement;

// User-facing navigation for the saved lab scenes. No generation or test automation.
public static class LabSceneMenus
{
    [MenuItem("Forearm Lab/Open desktop lab")]
    public static void OpenDesktop()=>Open("Assets/GestureLab/Scenes/GestureDesktop.unity");
    [MenuItem("Forearm Lab/Open Meta XR lab")]
    public static void OpenXR()=>Open("Assets/ForearmLab/Scenes/ForearmXR.unity");
    [MenuItem("Forearm Lab/Open Gesture XR lab")]
    public static void OpenGestureXR()=>Open("Assets/GestureLab/Scenes/GestureXR.unity");
    [MenuItem("Gesture Lab/Open recording scene")]
    public static void OpenRecording()=>Open("Assets/GestureLab/Scenes/GestureRecording.unity");
    static void Open(string path)
    {
        if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(path);
    }
}