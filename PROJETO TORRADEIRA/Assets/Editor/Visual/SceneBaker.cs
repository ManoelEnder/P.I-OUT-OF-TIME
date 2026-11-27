using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Monta direto nas cenas (modo edicao) tudo o que antes era criado so ao dar Play:
// sala da garra, lugares de foto + easter egg, barra de folego, lanterna, camera do Cinemachine,
// aviso de bateria, efeito do visor, arte do radio, radar, lampadas com
// defeito e a tela de agradecimentos. Pode rodar de novo: apaga e monta outra vez.
public static class SceneBaker
{
    private const string MainScene = "Assets/Scenes/Principal.unity";
    private const string ThanksScene = "Assets/Scenes/Agradecimentos.unity";
    private const string Folder = "Assets/Art/Generated";

    public static void BakeBatch()
    {
        BakeAll();
    }

    [MenuItem("Tools/OUT OF TIME/Montar tudo na cena")]
    public static void BakeAll()
    {
        var log = new StringBuilder();
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Art", "Generated");

        // ---------- Principal ----------
        var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        Physics.SyncTransforms();

        // Componentes com script perdido (classes que estavam dentro de outro arquivo) saem.
        int missing = 0;
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            missing += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        log.AppendLine("scripts perdidos removidos: " + missing);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform mobilia = root.name == "Mobilia" ? root.transform : root.transform.Find("Mobilia");
            if (mobilia != null) { Object.DestroyImmediate(mobilia.gameObject); log.AppendLine("Mobilia removida"); }
        }
        GameObject oldPoint = GameObject.Find("PontoDeMontagem");
        if (oldPoint != null) { Object.DestroyImmediate(oldPoint); log.AppendLine("PontoDeMontagem removido"); }

        // Sala da garra.
        AssemblyStation station = Object.FindFirstObjectByType<AssemblyStation>(FindObjectsInactive.Include);
        if (station == null)
            station = new GameObject("SalaMontagem").AddComponent<AssemblyStation>();
        if (station.pieceModel == null)
        {
            TemporalObjectPickup sample = Object.FindFirstObjectByType<TemporalObjectPickup>(FindObjectsInactive.Include);
            station.pieceModel = sample != null ? sample.gameObject : null;
        }
        Physics.SyncTransforms();
        station.Build();
        log.AppendLine("sala da garra: " + station.Built + " (" + station.transform.childCount + " objetos)");

        // Lugares de foto, brilhos e easter egg.
        PhotoSpots.Rebuild();
        log.AppendLine("lugares de foto: " + Object.FindObjectsByType<PhotoLocation>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);

        // Jogador: folego, lanterna e camera do Cinemachine.
        PlayerMovement movement = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (movement != null)
        {
            GameObject player = movement.gameObject;
            Get<PlayerStamina>(player).BuildBar();
            Get<CameraFlashlight>(player).BuildAll();
            RunCameraRig rig = Get<RunCameraRig>(player);
            rig.noiseProfile = SaveAsset(RunCameraRig.BuildNoise(), Folder + "/RunHandheld.asset");
            rig.Build();
            log.AppendLine("jogador: folego, lanterna, Cinemachine ok");
        }
        else log.AppendLine("ERRO: sem PlayerMovement");

        // Aviso de bateria.
        GameObject hud = GameObject.Find("HUDCanvas");
        if (hud != null) { BatteryWarning.CreateIn(hud); log.AppendLine("aviso de bateria ok"); }
        else log.AppendLine("ERRO: sem HUDCanvas");

        // Efeito de camera do visor (varredura, granulado, REC, zoom).
        GameObject oldVolume = GameObject.Find("VolumeVisorCRT");
        if (oldVolume != null) Object.DestroyImmediate(oldVolume);
        CameraHudFx fx = Object.FindFirstObjectByType<CameraHudFx>(FindObjectsInactive.Include);
        if (fx == null) fx = new GameObject("CameraHudFx").AddComponent<CameraHudFx>();
        fx.Build();
        log.AppendLine("efeito do visor ok");

        // Arte do radio.
        RadioMessageUI radio = Object.FindFirstObjectByType<RadioMessageUI>(FindObjectsInactive.Include);
        if (radio != null) { radio.BuildRadio(); log.AppendLine("radio ok"); }

        // Radar portatil no HUD.
        ArmToggle arms = Object.FindFirstObjectByType<ArmToggle>(FindObjectsInactive.Include);
        if (arms != null && hud != null)
        {
            RadarDevice device = RadarDevice.Create(hud.transform);
            device.Rebuild();
            var so = new SerializedObject(arms);
            SerializedProperty radarProp = so.FindProperty("radar");
            GameObject oldRadar = radarProp.objectReferenceValue as GameObject;
            if (oldRadar != null && oldRadar != device.gameObject)
                oldRadar.SetActive(false);
            radarProp.objectReferenceValue = device.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("radar ok");
        }

        // Eventos aleatorios e lampadas com defeito.
        foreach (LampFlicker lf in Object.FindObjectsByType<LampFlicker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(lf);
        RandomEvents events = Object.FindFirstObjectByType<RandomEvents>(FindObjectsInactive.Include);
        if (events == null) events = new GameObject("RandomEvents").AddComponent<RandomEvents>();
        events.BakeLamps();
        log.AppendLine("lampadas com defeito: " + Object.FindObjectsByType<LampFlicker>(FindObjectsSortMode.None).Length);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ---------- Agradecimentos ----------
        var thanks = EditorSceneManager.OpenScene(ThanksScene, OpenSceneMode.Single);
        ThanksScreen screen = Object.FindFirstObjectByType<ThanksScreen>(FindObjectsInactive.Include);
        if (screen != null)
        {
            screen.Build();
            EditorSceneManager.MarkSceneDirty(thanks);
            EditorSceneManager.SaveScene(thanks);
            log.AppendLine("agradecimentos ok");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        Debug.Log("SceneBaker:\n" + log);
    }

    private static T Get<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static T SaveAsset<T>(T fresh, string path) where T : Object
    {
        T old = AssetDatabase.LoadAssetAtPath<T>(path);
        if (old != null)
        {
            EditorUtility.CopySerialized(fresh, old);
            Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(old);
            return old;
        }
        AssetDatabase.CreateAsset(fresh, path);
        return fresh;
    }
}
