using System.IO;
using UnityEditor;
using UnityEngine;

// Reorganiza as pastas de Assets (revisao 2026-11-17). Move com AssetDatabase.MoveAsset,
// que preserva GUID e .meta. Idempotente: o que ja foi movido e pulado.
public static class AssetReorganizer
{
    private static readonly string[,] Moves =
    {
        { "Assets/Sounds", "Assets/Audio" },
        { "Assets/Font", "Assets/Fonts" },
        { "Assets/Materials", "Assets/Art/Materials" },
        { "Assets/Textures", "Assets/Art/Textures" },
        { "Assets/UI", "Assets/Art/UI" },
        { "Assets/Shaders", "Assets/Art/Shaders" },
        { "Assets/Scripts/Settings", "Assets/Settings/Pipeline" },
        { "Assets/New Universal Render Pipeline Asset_Renderer.asset", "Assets/Settings/Pipeline/New Universal Render Pipeline Asset_Renderer.asset" },
        { "Assets/Rendering/UI_Renderer.asset", "Assets/Settings/Rendering/UI_Renderer.asset" },
        { "Assets/DefaultVolumeProfile.asset", "Assets/Settings/DefaultVolumeProfile.asset" },
        { "Assets/InputSystem_Actions.inputactions", "Assets/Settings/Input/InputSystem_Actions.inputactions" },
        { "Assets/Input Action Reference.inputactions", "Assets/Settings/Input/Input Action Reference.inputactions" },
        { "Assets/UIInputActions.inputactions", "Assets/Settings/Input/UIInputActions.inputactions" },
    };

    [MenuItem("Ferramentas/QA/Reorganizar pastas")]
    public static void Run()
    {
        for (int i = 0; i < Moves.GetLength(0); i++)
        {
            string from = Moves[i, 0];
            string to = Moves[i, 1];

            if (!AssetDatabase.IsValidFolder(from) && !File.Exists(from))
                continue;

            EnsureFolder(Path.GetDirectoryName(to).Replace('\\', '/'));
            string error = AssetDatabase.MoveAsset(from, to);
            Debug.Log(string.IsNullOrEmpty(error) ? "MOVE " + from + " -> " + to : "MOVE ERRO " + from + ": " + error);
        }

        // Pasta que ficou vazia depois de mover o arquivo dela.
        if (AssetDatabase.IsValidFolder("Assets/Rendering") && AssetDatabase.FindAssets("", new[] { "Assets/Rendering" }).Length == 0)
            AssetDatabase.DeleteAsset("Assets/Rendering");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void RunFromCommandLine()
    {
        Run();
        EditorApplication.Exit(0);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
