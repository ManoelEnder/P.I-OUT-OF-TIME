using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

// As ferramentas de montagem (menu, pausa, inimigo, cutscene...) carregam assets por caminho.
// Todo caminho citado nelas precisa existir; pasta (terminando em /) precisa ser pasta valida.
// Ferramentas de QA ficam de fora: citam caminhos apagados de proposito (limpeza, checagens).
public class ToolPathsCheck : QAScenario
{
    public override string Name => "ToolPathsCheck";
    public override bool NeedsPlayMode => false;

    private static readonly Regex Literal = new Regex("\"(Assets/[^\"]+)\"");

    public override IEnumerator Run()
    {
        string[] scripts = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.EndsWith(".cs") && (p.StartsWith("Assets/Editor/") || p.StartsWith("Assets/Cutscene/Editor/")) && !p.StartsWith("Assets/Editor/QA/"))
            .ToArray();

        foreach (string script in scripts)
        {
            foreach (Match m in Literal.Matches(File.ReadAllText(script)))
            {
                string path = m.Groups[1].Value;
                bool exists = path.EndsWith("/")
                    ? AssetDatabase.IsValidFolder(path.TrimEnd('/'))
                    : File.Exists(path) || AssetDatabase.IsValidFolder(path);

                Check(exists, Path.GetFileName(script) + " cita " + path);
            }
        }

        yield break;
    }
}
