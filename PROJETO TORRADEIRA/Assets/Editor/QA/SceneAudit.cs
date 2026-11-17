using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class AuditReport
{
    public int ScenesAudited;
    public readonly List<string> MissingScripts = new List<string>();
    public readonly List<string> NullReferences = new List<string>();
    public readonly List<string> MissingPrefabs = new List<string>();

    public int Total => MissingScripts.Count + NullReferences.Count + MissingPrefabs.Count;
}

// Abre cada cena habilitada no build e lista scripts faltando, prefabs sem asset e
// campos de referencia vazios ou quebrados nos componentes do jogo.
public static class SceneAudit
{
    public static string ReportPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/auditoria.md"));

    [MenuItem("Ferramentas/QA/Auditar cenas do build")]
    public static void RunFromMenu()
    {
        AuditReport report = Run();
        Debug.Log("AUDIT TOTAL " + report.Total + " (relatorio em " + ReportPath + ")");
    }

    public static void RunFromCommandLine()
    {
        AuditReport report = Run();
        Debug.Log("AUDIT TOTAL " + report.Total);
        EditorApplication.Exit(0);
    }

    public static AuditReport Run()
    {
        var report = new AuditReport();
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled)
                continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            report.ScenesAudited++;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    AuditObject(scene.name, t.gameObject, report);
            }
        }

        if (setup.Length > 0)
            EditorSceneManager.RestoreSceneManagerSetup(setup);

        Write(report);
        return report;
    }

    private static void AuditObject(string scene, GameObject go, AuditReport report)
    {
        string path = scene + "/" + PathOf(go.transform);

        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
            report.MissingScripts.Add(path);

        if (PrefabUtility.IsOutermostPrefabInstanceRoot(go) && PrefabUtility.IsPrefabAssetMissing(go))
            report.MissingPrefabs.Add(path);

        foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb.GetType().Assembly.GetName().Name != "Assembly-CSharp")
                continue;

            var so = new SerializedObject(mb);
            SerializedProperty it = so.GetIterator();

            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue != null)
                    continue;

                // instanceID != 0 com valor nulo = referencia para algo que nao existe mais.
                bool broken = it.objectReferenceInstanceIDValue != 0;
                report.NullReferences.Add((broken ? "[QUEBRADA] " : "") + path + " : " + mb.GetType().Name + "." + it.propertyPath);
            }
        }
    }

    private static string PathOf(Transform t)
    {
        return t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }

    private static void Write(AuditReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Auditoria das cenas do build");
        sb.AppendLine();
        sb.AppendLine("Cenas auditadas: " + report.ScenesAudited + " · Total de itens: " + report.Total);
        Section(sb, "Scripts faltando (Missing Script)", report.MissingScripts);
        Section(sb, "Prefabs sem asset", report.MissingPrefabs);
        Section(sb, "Referências vazias ou quebradas em componentes do jogo", report.NullReferences);

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, sb.ToString());
    }

    private static void Section(StringBuilder sb, string title, List<string> items)
    {
        sb.AppendLine();
        sb.AppendLine("## " + title + " (" + items.Count + ")");
        sb.AppendLine();

        foreach (string item in items)
            sb.AppendLine("- `" + item + "`");
    }
}
