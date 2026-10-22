using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Dev helper: when Temp/claude_webgl_build.request appears, applies the itch.io WebGL settings, builds the enabled
// scenes to Builds/WebGL and switches back to the platform the editor was on. Writes Logs/ClaudeBuilds/webgl_done.txt
// with the result and the build errors, or "COMPILE FAILED" when scripts do not compile.
[InitializeOnLoad]
public static class ClaudeWebGLBuildTrigger
{
    private static double s_nextPoll;

    private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static string RequestPath => Path.Combine(ProjectRoot, "Temp", "claude_webgl_build.request");
    private static string OutDir => Path.Combine(ProjectRoot, "Logs", "ClaudeBuilds");

    static ClaudeWebGLBuildTrigger()
    {
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < s_nextPoll) return;
        s_nextPoll = EditorApplication.timeSinceStartup + 1.0;

        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        EditorApplication.delayCall += Build;
    }

    private static void Build()
    {
        Directory.CreateDirectory(OutDir);
        var donePath = Path.Combine(OutDir, "webgl_done.txt");
        if (File.Exists(donePath)) File.Delete(donePath);

        if (EditorUtility.scriptCompilationFailed)
        {
            File.WriteAllText(donePath, "COMPILE FAILED");
            return;
        }

        var startTarget = EditorUserBuildSettings.activeBuildTarget;
        var startGroup = BuildPipeline.GetBuildTargetGroup(startTarget);

        // itch.io serves the files without a Content-Encoding header, so the loader unpacks the gzip itself
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        // full-window canvas, so the game fills the itch.io viewport and any iframe it is embedded in
        PlayerSettings.WebGL.template = "PROJECT:Responsive";
        AssetDatabase.SaveAssets();

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(ProjectRoot, "Builds", "WebGL"),
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        var text = new StringBuilder();
        text.AppendLine($"RESULT {summary.result}");
        text.AppendLine($"output {summary.outputPath}");
        text.AppendLine($"size {summary.totalSize / (1024f * 1024f):0.0} MB, time {summary.totalTime.TotalSeconds:0} s");
        text.AppendLine($"errors {summary.totalErrors}, warnings {summary.totalWarnings}");
        foreach (var step in report.steps)
        foreach (var message in step.messages)
        {
            if (message.type == LogType.Error || message.type == LogType.Exception)
                text.AppendLine($"[{step.name}] {message.content}");
        }

        if (startTarget != BuildTarget.WebGL)
        {
            var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(startGroup, startTarget);
            text.AppendLine($"switched back to {startTarget}: {switched}");
        }

        File.WriteAllText(donePath, text.ToString());
    }
}
