using System;
using System.IO;
using ForearmLab;
using UnityEditor;
using UnityEngine;

// File-triggered local validation, also usable from a batch editor.
[InitializeOnLoad]
public static class LabValidation
{
    static string Artifact(string name) => Path.Combine(Application.dataPath, "../Artifacts", name);
    static double started;
    static bool testing;
    static bool xrTesting;
    static LabValidation() { EditorApplication.update += Update; }

    [MenuItem("Forearm Lab/Run desktop Play Mode checks")]
    public static void RunDesktopTest()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Artifact("request-desktop-test")));
        File.WriteAllText(Artifact("request-desktop-test"), "");
    }

    static void Update()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists(Artifact("request-xr-test")) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Delete(Artifact("request-xr-test"));
            LabSetup.OpenXR();
            LabSimulator.ActivateLocal();
            xrTesting = true; started = 0;
            EditorApplication.isPlaying = true;
            return;
        }
        if (xrTesting && EditorApplication.isPlaying)
        {
            if (started == 0) started = EditorApplication.timeSinceStartup;
            var manager = UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager;
            var displays = new System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            bool running = displays.Exists(d => d.running);
            if (!running && EditorApplication.timeSinceStartup - started < 90) return;
            xrTesting = false;
            File.WriteAllText(Artifact("xr-test-result.txt"), $"Loader: {manager.activeLoader}; Display running: {running}");
            Debug.Log($"FOREARM_LAB_XR_CHECK: display running {running}");
            EditorApplication.isPlaying = false;
        }
        if (File.Exists(Artifact("request-setup")) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Delete(Artifact("request-setup"));
            try { LabSetup.Build(); File.WriteAllText(Artifact("setup-result.txt"), "PASS: scenes generated and four templates recognize correctly"); }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText(Artifact("setup-result.txt"), e.ToString()); }
            return;
        }
        if (File.Exists(Artifact("request-desktop-test")) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Delete(Artifact("request-desktop-test"));
            LabSetup.OpenDesktop();
            testing = true; started = EditorApplication.timeSinceStartup;
            EditorApplication.isPlaying = true;
        }
        if (!testing || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup - started < 5) return;
        testing = false;
        try
        {
            var drawing = UnityEngine.Object.FindAnyObjectByType<GestureDrawing>();
            var origin = drawing.view.transform.position + drawing.view.transform.forward * .7f;
            // Stationary / short inputs must not crash the recognizer.
            drawing.BeginStroke(origin); drawing.EndStroke(); drawing.Recognize();
            if (drawing.PointCount != 0) throw new Exception("Stationary point not discarded");
            drawing.Clear();
            for (int j = 0; j < 2; j++)
            {
                drawing.BeginStroke(origin + Vector3.left * .1f);
                for (int i = 1; i < 20; i++) drawing.AddPoint(origin + Vector3.left * .1f + Vector3.right * i * .01f + Vector3.up * (j == 0 ? i : 20-i) * .01f);
                drawing.EndStroke();
            }
            drawing.Recognize();
            if (drawing.PointCount < 20) throw new Exception("Multistroke capture failed");
            drawing.Clear();
            if (drawing.PointCount != 0) throw new Exception("Clear failed");
            File.WriteAllText(Artifact("desktop-test-result.txt"), "PASS: Play Mode capture, multi-stroke recognition, stationary-input guard, clear");
            Debug.Log("FOREARM_LAB_PLAYMODE_OK");
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText(Artifact("desktop-test-result.txt"), e.ToString()); }
        EditorApplication.isPlaying = false;
    }
}
