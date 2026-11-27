using UnityEngine;
using UnityEngine.SceneManagement;

// Liga as pecas da rodada final na fase Principal sem depender de mudancas na cena:
// - remove a mobilia gerada (grupo "Mobilia"), caso a cena ainda tenha;
// - adiciona folego (stamina), lanterna da camera e a camera do Cinemachine com inclinacao na corrida.
public static class FinalRoundBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Principal")
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform mobilia = root.name == "Mobilia" ? root.transform : root.transform.Find("Mobilia");
            if (mobilia != null)
                Object.Destroy(mobilia.gameObject);
        }

        // Sala de montagem na propria fase: a mesa antiga perto do inicio sai, a estacao da garra entra.
        GameObject oldPoint = GameObject.Find("PontoDeMontagem");
        if (oldPoint != null)
            Object.Destroy(oldPoint);
        if (Object.FindFirstObjectByType<AssemblyStation>() == null)
        {
            TemporalObjectPickup sample = Object.FindFirstObjectByType<TemporalObjectPickup>(FindObjectsInactive.Include);
            AssemblyStation station = new GameObject("SalaMontagem").AddComponent<AssemblyStation>();
            station.pieceModel = sample != null ? sample.gameObject : null;
        }

        // Lugares para fotografar: marcadores, lugares extras e a figura escondida atras das janelas.
        PhotoSpots.Setup();
        // Efeito de camera no HUD do visor (varredura, granulado, REC, zoom).
        if (Object.FindFirstObjectByType<CameraHudFx>() == null)
            new GameObject("CameraHudFx").AddComponent<CameraHudFx>();

        PlayerMovement movement = Object.FindFirstObjectByType<PlayerMovement>();
        if (movement == null)
            return;
        GameObject player = movement.gameObject;
        if (player.GetComponent<PlayerStamina>() == null) player.AddComponent<PlayerStamina>();
        if (player.GetComponent<CameraFlashlight>() == null) player.AddComponent<CameraFlashlight>();
        if (player.GetComponent<RunCameraRig>() == null) player.AddComponent<RunCameraRig>();
    }
}
