using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Batch-mode entry points, e.g.
    /// Unity.exe -batchmode -projectPath NU.D.36_Unity -executeMethod NU36.Editor.BuildCommands.BuildWindows
    /// </summary>
    public static class BuildCommands
    {
        // Relative to the Unity project folder; keeps builds outside Assets/.
        const string WindowsOutputPath = "../Builds/Windows/NU.D.36.exe";

        // Lab 3 player: same scenes, but Rota03 first so the executable starts in Lab 3.
        // Build Settings order (Rota02 first) is not changed; separate folder keeps the Lab 2 build.
        const string WindowsRota03OutputPath = "../Builds/Windows_Rota03/NU.D.36.exe";

        // Lab 5 player: Rota05 first, in its own folder (earlier builds are kept).
        const string WindowsRota05OutputPath = "../Builds/Windows_Rota05/NU.D.36.exe";

        public static void BuildWindows() => Build(WindowsOutputPath, null);

        public static void BuildWindowsRota05() => Build(WindowsRota05OutputPath, Lab5SceneBuilder.ScenePath);

        // Lab 6 grey box player: Bolum3_GriKutu first.
        public static void BuildWindowsGriKutu() => Build("../Builds/Windows_Bolum3_GriKutu/NU.D.36.exe", Lab6SceneBuilder.ScenePath);

        public static void BuildWindowsRota03() => Build(WindowsRota03OutputPath, Lab3SceneBuilder.ScenePath);

        static void Build(string outputPath, string firstScene)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .OrderBy(p => p == firstScene ? 0 : 1) // stable: other scenes keep their order
                .ToArray();

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[NU36 Build] result={summary.result} errors={summary.totalErrors} " +
                      $"warnings={summary.totalWarnings} size={summary.totalSize} bytes " +
                      $"output={summary.outputPath}");

            if (Application.isBatchMode)
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
