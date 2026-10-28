using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Optional local extraction fallback. It changes only this editor process's runtime selection.
public static class LabSimulator
{
    const string Previous = "ForearmLab.PreviousRuntime";
    const string Enabled = "ForearmLab.LocalRuntimeEnabled";

    [MenuItem("Forearm Lab/Simulator/Use local extracted runtime")]
    public static void ActivateLocal()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/SimulatorPortable/PFiles/MetaXRSimulator/v207.1"));
        string manifest = Path.Combine(root, "meta_openxr_simulator.json");
        if (!File.Exists(manifest)) throw new FileNotFoundException("Install the official standalone simulator, or extract its 207.1 MSI into Artifacts/SimulatorPortable first", manifest);
        if (!SessionState.GetBool(Enabled, false)) SessionState.SetString(Previous, Environment.GetEnvironmentVariable("XR_RUNTIME_JSON") ?? "");
        Environment.SetEnvironmentVariable("XR_RUNTIME_JSON", manifest);
        Environment.SetEnvironmentVariable("XR_SELECTED_RUNTIME_JSON", manifest);
        SessionState.SetBool(Enabled, true);
        if (System.Diagnostics.Process.GetProcessesByName("MetaXRSimulator").Length == 0)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(root, "MetaXRSimulator.exe")) { WorkingDirectory = root, UseShellExecute = true });
        Debug.Log("Local Meta XR Simulator selected for this Unity process: " + manifest);
    }

    [MenuItem("Forearm Lab/Simulator/Restore previous runtime")]
    public static void Restore()
    {
        if (!SessionState.GetBool(Enabled, false)) return;
        Environment.SetEnvironmentVariable("XR_RUNTIME_JSON", SessionState.GetString(Previous, ""));
        Environment.SetEnvironmentVariable("XR_SELECTED_RUNTIME_JSON", null);
        SessionState.SetBool(Enabled, false);
        Debug.Log("Previous OpenXR runtime restored");
    }
}
