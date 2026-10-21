using System.IO;
using UnityEditor;
using UnityEngine;

// Dev helper: when Temp/claude_capture.request appears, refreshes assets, waits for the compile and starts play mode
// with DevPourCapture armed. Writes Logs/ClaudeCaptures/done.txt with "COMPILE FAILED" when scripts do not compile.
[InitializeOnLoad]
public static class ClaudeCaptureTrigger
{
    private const string PendingKey = "ClaudeCapture.Pending";
    private static double s_nextPoll;

    private static string RequestPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "claude_capture.request"));

    static ClaudeCaptureTrigger()
    {
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < s_nextPoll) return;
        s_nextPoll = EditorApplication.timeSinceStartup + 1.0;

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (File.Exists(RequestPath))
        {
            File.Delete(RequestPath);
            SessionState.SetBool(PendingKey, true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return;
        }

        if (!SessionState.GetBool(PendingKey, false) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        SessionState.SetBool(PendingKey, false);

        if (EditorUtility.scriptCompilationFailed)
        {
            Directory.CreateDirectory(DevPourCaptureOutDir);
            File.WriteAllText(Path.Combine(DevPourCaptureOutDir, "done.txt"), "COMPILE FAILED");
            return;
        }

        SessionState.SetBool("ClaudeCapture.Active", true);
        EditorApplication.isPlaying = true;
    }

    private static string DevPourCaptureOutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "ClaudeCaptures"));
}
