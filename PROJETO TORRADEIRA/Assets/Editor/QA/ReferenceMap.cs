using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Descobre o que o jogo usa de fato: cenas do build, pastas Resources, assets citados por
// caminho nas ferramentas de editor e tudo que as configuracoes do projeto referenciam.
// O resto vira candidato a remocao em docs/revisao/nao-usados.md.
public static class ReferenceMap
{
    private static readonly Regex PathLiteral = new Regex("\"(Assets/[^\"]+)\"");
    private static readonly Regex GuidRef = new Regex(@"guid: ([0-9a-f]{32})");
    private static readonly Regex DynamicLoad = new Regex(@"(Resources\.Load\w*|Shader\.Find|SceneManager\.LoadScene\w*)\s*(<[^>]*>)?\s*\(");

    public static string ReportPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/nao-usados.md"));

    public static HashSet<string> UsedAssets()
    {
        var roots = new HashSet<string>();
        string[] all = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/")).ToArray();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled)
                roots.Add(scene.path);

        foreach (string path in all)
        {
            if (path.Contains("/Resources/") || path.Contains("/Editor/") || path.EndsWith(".cs") || path.EndsWith(".asmdef"))
                roots.Add(path);
        }

        // Ferramentas de montagem carregam por caminho; um literal pode ser uma pasta (prefixo).
        // As ferramentas de QA citam caminhos so para checar; nao contam como uso.
        foreach (string script in all.Where(p => p.EndsWith(".cs") && p.Contains("/Editor/") && !p.StartsWith("Assets/Editor/QA/")))
        {
            foreach (Match m in PathLiteral.Matches(File.ReadAllText(script)))
            {
                string literal = m.Groups[1].Value;

                // "Assets/" sozinho (ou pasta de um nivel) e so base de concatenacao, nao uma pasta usada.
                bool folderPrefix = literal.EndsWith("/") && literal.Count(c => c == '/') >= 3;

                foreach (string path in all)
                    if (folderPrefix ? path.StartsWith(literal) : path == literal)
                        roots.Add(path);
            }
        }

        // Pipeline, input, volumes e tudo mais que as configuracoes do projeto apontam.
        foreach (string settings in Directory.GetFiles(Path.Combine(Application.dataPath, "../ProjectSettings"), "*.asset"))
        {
            // A lista de cenas (inclusive as desativadas) ja entra acima so pelas habilitadas.
            if (settings.EndsWith("EditorBuildSettings.asset"))
                continue;

            foreach (Match m in GuidRef.Matches(File.ReadAllText(settings)))
            {
                string path = AssetDatabase.GUIDToAssetPath(m.Groups[1].Value);
                if (!string.IsNullOrEmpty(path))
                    roots.Add(path);
            }
        }

        // Pastas citadas nas configuracoes (ex.: pasta padrao de algum pacote) nao contam:
        // GetDependencies de uma pasta devolve tudo que esta dentro dela.
        roots.RemoveWhere(AssetDatabase.IsValidFolder);

        var used = new HashSet<string>(AssetDatabase.GetDependencies(roots.ToArray(), true));
        used.UnionWith(roots);
        return used;
    }

    [MenuItem("Ferramentas/QA/Mapa de referencias")]
    public static void RunFromMenu()
    {
        Write(UsedAssets());
        Debug.Log("REFMAP relatorio em " + ReportPath);
    }

    public static void RunFromCommandLine()
    {
        Write(UsedAssets());
        EditorApplication.Exit(0);
    }

    private static void Write(HashSet<string> used)
    {
        List<string> unused = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p) && !used.Contains(p))
            .OrderBy(p => p)
            .ToList();

        var groups = unused
            .GroupBy(Group)
            .Select(g => new { Name = g.Key, Count = g.Count(), Bytes = g.Sum(Size) })
            .OrderByDescending(g => g.Bytes)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("# Assets sem uso (candidatos à remoção)");
        sb.AppendLine();
        sb.AppendLine("Usados: " + used.Count + " · Sem uso: " + unused.Count + " (" + Mb(groups.Sum(g => g.Bytes)) + ")");
        sb.AppendLine();
        sb.AppendLine("Raízes: cenas habilitadas no build, pastas `Resources` e `Editor`, scripts, caminhos citados nas ferramentas de editor e referências das configurações do projeto.");
        sb.AppendLine();
        sb.AppendLine("| Pasta | Arquivos | Tamanho |");
        sb.AppendLine("|---|---:|---:|");
        foreach (var g in groups)
            sb.AppendLine("| `" + g.Name + "` | " + g.Count + " | " + Mb(g.Bytes) + " |");

        sb.AppendLine();
        sb.AppendLine("## Carregado por nome — conferir antes de remover");
        sb.AppendLine();
        foreach (string script in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.EndsWith(".cs") && !p.Contains("/Editor/")))
        {
            string[] lines = File.ReadAllLines(script);
            for (int i = 0; i < lines.Length; i++)
                if (DynamicLoad.IsMatch(lines[i]))
                    sb.AppendLine("- `" + script + ":" + (i + 1) + "` " + lines[i].Trim());
        }

        sb.AppendLine();
        sb.AppendLine("## Lista completa");
        sb.AppendLine();
        foreach (string path in unused)
            sb.AppendLine("- `" + path + "`");

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, sb.ToString());
    }

    private static string Group(string path)
    {
        string[] parts = path.Split('/');
        return parts.Length > 3 ? parts[0] + "/" + parts[1] + "/" + parts[2] : string.Join("/", parts.Take(parts.Length - 1));
    }

    private static long Size(string path)
    {
        var info = new FileInfo(path);
        return info.Exists ? info.Length : 0;
    }

    private static string Mb(long bytes)
    {
        return (bytes / 1048576f).ToString("0.0") + " MB";
    }
}
