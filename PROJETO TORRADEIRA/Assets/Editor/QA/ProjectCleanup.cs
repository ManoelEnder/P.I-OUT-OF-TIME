using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Limpeza autorizada pelo autor (design da revisao, 2026-11-17). Idempotente.
// So apaga o que o mapa de referencias confirma sem uso; o resto vai para "Preservados".
public static class ProjectCleanup
{
    // Ordem final do build (fluxo oficial).
    private static readonly string[] BuildScenes =
    {
        "Assets/Scenes/TelaInicio.unity",
        "Assets/Scenes/Menu.unity",
        "Assets/Scenes/IntroCutsceneVideo.unity",
        "Assets/Scenes/Principal.unity",
        "Assets/Scenes/Victory TEMP.unity",
        "Assets/Scenes/Credits.unity",
    };

    // Cenas de rascunho/pessoais que o autor autorizou remover, e restos de recuperacao.
    private static readonly string[] Authorized =
    {
        "Assets/_Recovery",
        "Assets/Jullya",
        "Assets/Scenes/CenaLabFut.unity",
        "Assets/Scenes/CenaLabFut",
        "Assets/Scenes/Tutorial.unity",
        "Assets/Scenes/TestScene.unity",
        "Assets/Scenes/Radio.unity",
        "Assets/Scenes/Radio",
        "Assets/ThirdParty/CRT-Free/Scenes",
    };

    // Pacotes que ficam mesmo sem uso hoje.
    private static readonly Dictionary<string, string> KeepPackages = new Dictionary<string, string>
    {
        { "Assets/ThirdParty/CautionTape", "fitas de isolamento que o autor vai colocar" },
    };

    public static string ReportPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/limpeza.md"));

    [MenuItem("Ferramentas/QA/Limpar projeto")]
    public static void Run()
    {
        var report = new StringBuilder();
        report.AppendLine("# Limpeza do projeto");
        report.AppendLine();

        SetBuildScenes(report);

        HashSet<string> used = ReferenceMap.UsedAssets();
        var removed = new List<(string path, long bytes)>();
        var preserved = new List<(string path, string why)>();

        foreach (string path in Authorized)
            TryDelete(path, used, removed, preserved);

        foreach (string package in AssetDatabase.GetSubFolders("Assets/ThirdParty"))
        {
            if (KeepPackages.TryGetValue(package, out string why))
            {
                preserved.Add((package, why));
                continue;
            }

            string[] files = FilesUnder(package);
            if (files.Length > 0 && files.All(f => !used.Contains(f)))
                TryDelete(package, used, removed, preserved);
        }

        AssetDatabase.Refresh();

        report.AppendLine("## Removidos");
        report.AppendLine();
        report.AppendLine("| Caminho | Tamanho |");
        report.AppendLine("|---|---:|");
        foreach (var r in removed)
            report.AppendLine("| `" + r.path + "` | " + (r.bytes / 1048576f).ToString("0.0") + " MB |");
        report.AppendLine();
        report.AppendLine("Total liberado: " + (removed.Sum(r => r.bytes) / 1048576f).ToString("0.0") + " MB");
        report.AppendLine();
        report.AppendLine("## Preservados (dúvida ou uso parcial)");
        report.AppendLine();
        foreach (var p in preserved)
            report.AppendLine("- `" + p.path + "`: " + p.why);

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, report.ToString());
        Debug.Log("CLEANUP removidos " + removed.Count + ", preservados " + preserved.Count + " (" + ReportPath + ")");
    }

    public static void RunFromCommandLine()
    {
        Run();
        EditorApplication.Exit(0);
    }

    private static void SetBuildScenes(StringBuilder report)
    {
        var scenes = BuildScenes
            .Where(p => File.Exists(p))
            .Select(p => new EditorBuildSettingsScene(p, true))
            .ToList();

        // A cutscene desenhada em codigo fica no projeto, fora do build (a oficial e a de video).
        if (File.Exists("Assets/Scenes/IntroCutscene.unity"))
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/IntroCutscene.unity", false));

        EditorBuildSettings.scenes = scenes.ToArray();

        report.AppendLine("## Cenas do build");
        report.AppendLine();
        foreach (var s in scenes)
            report.AppendLine("- " + (s.enabled ? "" : "(fora do build) ") + "`" + s.path + "`");
        report.AppendLine();
    }

    private static void TryDelete(string path, HashSet<string> used, List<(string, long)> removed, List<(string, string)> preserved)
    {
        bool folder = AssetDatabase.IsValidFolder(path);
        if (!folder && !File.Exists(path))
            return;

        string[] files = folder ? FilesUnder(path) : new[] { path };
        string inUse = files.FirstOrDefault(used.Contains);

        if (inUse != null)
        {
            preserved.Add((path, "em uso: `" + inUse + "`"));
            return;
        }

        long bytes = files.Sum(f => new FileInfo(f).Exists ? new FileInfo(f).Length : 0);
        if (AssetDatabase.DeleteAsset(path))
            removed.Add((path, bytes));
        else
            preserved.Add((path, "o Unity recusou apagar"));
    }

    private static string[] FilesUnder(string folder)
    {
        return AssetDatabase.FindAssets("", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !AssetDatabase.IsValidFolder(p))
            .Distinct()
            .ToArray();
    }
}
