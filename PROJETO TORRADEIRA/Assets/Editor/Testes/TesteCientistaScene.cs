using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Cria a cena Assets/Scenes/Teste/TesteCientista.unity (uma vez, ao abrir o Unity)
// e deixa o menu Ferramentas/Testes/Abrir teste do cientista.
[InitializeOnLoad]
public static class TesteCientistaScene
{
    private const string Path = "Assets/Scenes/Teste/TesteCientista.unity";

    static TesteCientistaScene()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Path) && !EditorApplication.isPlayingOrWillChangePlaymode)
                Create(false);
        };
    }

    [MenuItem("Ferramentas/Testes/Abrir teste do cientista")]
    public static void Open()
    {
        if (!File.Exists(Path)) Create(false);
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(Path);
    }

    private static void Create(bool open)
    {
        Directory.CreateDirectory("Assets/Scenes/Teste");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        Camera c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.12f, 0.13f, 0.12f);
        new GameObject("TesteCientista", typeof(TesteCientista));
        EditorSceneManager.MoveGameObjectToScene(cam, scene);
        EditorSceneManager.MoveGameObjectToScene(GameObject.Find("TesteCientista"), scene);
        EditorSceneManager.SaveScene(scene, Path);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh();
        Debug.Log("Cena de teste criada: " + Path);
    }
}
