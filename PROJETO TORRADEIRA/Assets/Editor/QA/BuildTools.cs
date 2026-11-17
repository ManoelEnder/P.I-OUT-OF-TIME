using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds Windows do jogo: desenvolvimento (diagnostico/Profiler) e release (medicao final).
public static class BuildTools
{
    public const string DevPath = "Builds/Windows-Dev/OutOfTime.exe";
    public const string ReleasePath = "Builds/Windows/OutOfTime.exe";

    [MenuItem("Ferramentas/QA/Build de desenvolvimento")]
    public static void BuildDev()
    {
        Build(DevPath, BuildOptions.Development);
    }

    [MenuItem("Ferramentas/QA/Build de release")]
    public static void BuildRelease()
    {
        Build(ReleasePath, BuildOptions.None);
    }

    public static void BuildDevFromCommandLine()
    {
        EditorApplication.Exit(Build(DevPath, BuildOptions.Development) ? 0 : 1);
    }

    public static void BuildReleaseFromCommandLine()
    {
        EditorApplication.Exit(Build(ReleasePath, BuildOptions.None) ? 0 : 1);
    }

    private static bool Build(string path, BuildOptions options)
    {
        // Alvo: 1920x1080 em tela cheia (o padrao antigo era 1024x768).
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = options,
        });

        bool ok = report.summary.result == BuildResult.Succeeded;
        Debug.Log("BUILD " + (ok ? "OK" : "FALHOU") + " " + path + " " + (report.summary.totalSize / 1048576f).ToString("0") + " MB em " + report.summary.totalTime);
        return ok;
    }
}
