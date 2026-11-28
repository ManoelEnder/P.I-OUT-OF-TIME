using UnityEngine;
using UnityEngine.InputSystem;

// Cena de teste: simula mortes e mostra o cientista falando no radio.
// [ESPACO] ou botao = morrer  |  [R] = zerar mortes
public class TesteCientista : MonoBehaviour
{
    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (k.spaceKey.wasPressedThisFrame) Morrer();
        if (k.rKey.wasPressedThisFrame) PlayerRespawn.ResetDeaths();
    }

    private void Morrer()
    {
        foreach (ScientistRadio r in FindObjectsByType<ScientistRadio>(FindObjectsSortMode.None))
            Destroy(r.gameObject);
        ScientistRadio.Say(PlayerRespawn.SimulateDeath(), 0.2f);
    }

    private void OnGUI()
    {
        GUI.skin.label.fontSize = 22;
        GUI.skin.button.fontSize = 22;
        int d = PlayerRespawn.Deaths;
        string humor = d <= 1 ? "calmo" : d <= 3 ? "preocupado" : d <= 5 ? "irritado" : "FURIOSO";
        GUI.Label(new Rect(30, 30, 700, 40), "Mortes: " + d + "   (humor: " + humor + ")");
        if (GUI.Button(new Rect(30, 80, 320, 50), "Morrer  [ESPACO]")) Morrer();
        if (GUI.Button(new Rect(30, 140, 320, 50), "Zerar mortes  [R]")) PlayerRespawn.ResetDeaths();
    }
}
