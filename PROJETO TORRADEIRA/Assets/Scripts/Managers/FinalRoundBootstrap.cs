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

        PlayerMovement movement = Object.FindFirstObjectByType<PlayerMovement>();
        if (movement == null)
            return;
        GameObject player = movement.gameObject;
        if (player.GetComponent<PlayerStamina>() == null) player.AddComponent<PlayerStamina>();
        if (player.GetComponent<CameraFlashlight>() == null) player.AddComponent<CameraFlashlight>();
        if (player.GetComponent<RunCameraRig>() == null) player.AddComponent<RunCameraRig>();
    }
}
