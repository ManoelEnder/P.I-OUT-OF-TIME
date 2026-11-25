using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Cria a cena de Agradecimentos (creditos) e coloca no Build Settings.
// (A sala da garra fica dentro da fase Principal: AssemblyStation, criada pelo FinalRoundBootstrap.)
public static class FinalScenesBuilder
{
    private const string FontPath = "Assets/Fonts/VCR_OSD_MONO SDF.asset";
    private const string PiecePath = "Assets/Prefabs/Camera/todas.glb";

    [MenuItem("Tools/OUT OF TIME/Criar cenas do final (sala da garra + agradecimentos)")]
    public static void Build()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var thanks = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        var screen = new GameObject("Agradecimentos").AddComponent<ThanksScreen>();
        screen.font = font;
        EditorSceneManager.SaveScene(thanks, "Assets/Scenes/Agradecimentos.unity");

        var scenes = EditorBuildSettings.scenes.ToList();
        foreach (string path in new[] { "Assets/Scenes/Agradecimentos.unity" })
            if (!scenes.Any(s => s.path == path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("FinalScenesBuilder: cenas criadas");
    }
}
