using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Cria as cenas do final: SalaMontagem (garra mecanica, montada em codigo pelo AssemblyRoom) e
// Agradecimentos (creditos). Coloca as duas no Build Settings.
// Tambem: menu para remover de vez a mobilia gerada da cena Principal.
public static class FinalScenesBuilder
{
    private const string FontPath = "Assets/Fonts/VCR_OSD_MONO SDF.asset";
    private const string PiecePath = "Assets/Prefabs/Camera/todas.glb";

    [MenuItem("Tools/OUT OF TIME/Criar cenas do final (sala da garra + agradecimentos)")]
    public static void Build()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var piece = AssetDatabase.LoadAssetAtPath<GameObject>(PiecePath);

        var assembly = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var room = new GameObject("SalaMontagem").AddComponent<AssemblyRoom>();
        room.font = font;
        room.pieceModel = piece;
        room.nextScene = "OutroCutsceneVideo";
        EditorSceneManager.SaveScene(assembly, "Assets/Scenes/SalaMontagem.unity");

        var thanks = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        var screen = new GameObject("Agradecimentos").AddComponent<ThanksScreen>();
        screen.font = font;
        EditorSceneManager.SaveScene(thanks, "Assets/Scenes/Agradecimentos.unity");

        var scenes = EditorBuildSettings.scenes.ToList();
        foreach (string path in new[] { "Assets/Scenes/SalaMontagem.unity", "Assets/Scenes/Agradecimentos.unity" })
            if (!scenes.Any(s => s.path == path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("FinalScenesBuilder: cenas criadas");
    }

    // Remove o grupo "Mobilia" da cena Principal aberta, apaga as malhas/materiais gerados para ela
    // e refaz o occlusion culling.
    [MenuItem("Tools/OUT OF TIME/Remover mobília da cena Principal")]
    public static void RemoveFurniture()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Principal")
            scene = EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);

        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform t = root.name == "Mobilia" ? root.transform : root.transform.Find("Mobilia");
            if (t != null)
            {
                Undo.DestroyObjectImmediate(t.gameObject);
                removed++;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        foreach (string folder in new[] { "Assets/Art/Props/LOD", "Assets/Art/Materials/Mobilia" })
            if (AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);

        StaticOcclusionCulling.Compute();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Mobilia removida: " + removed + " grupo(s)");
    }

    public static void RemoveFurnitureBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);
        RemoveFurniture();
    }
}
