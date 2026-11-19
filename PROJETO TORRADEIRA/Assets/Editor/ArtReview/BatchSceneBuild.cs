using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Roda as ferramentas de montagem por linha de comando (Unity fechado):
//   Unity.exe -batchmode -projectPath <projeto> -executeMethod BatchSceneBuild.Credits -quit
public static class BatchSceneBuild
{
    public static void Credits()
    {
        Run("Assets/Scenes/Credits.unity", MonitorMenuBuilder.BuildCredits);
    }

    public static void Enemy()
    {
        Run("Assets/Scenes/Principal.unity", TemporalEraserBuilder.Build);
    }

    // So desenha o mapa do NavMesh atual (antes de refazer).
    public static void EnemyNavMapBefore()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);
        EnemyNavigationBuilder.DrawMap("navmesh_antes.png");
    }

    public static void EnemyNav()
    {
        Run("Assets/Scenes/Principal.unity", EnemyNavigationBuilder.Build);
    }

    private static void Run(string scenePath, System.Action build)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        build();
        bool saved = EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("BatchSceneBuild: " + scenePath + (saved ? " salva." : " NAO salva."));
    }
}
