using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fluxo completo duas vezes: tela inicial -> menu -> cutscene (pulada) -> fase -> final -> menu,
// e de novo menu -> fase, conferindo que nada da primeira partida vazou para a segunda.
public class FlowScenario : QAScenario
{
    public override string Name => "Flow";

    public override IEnumerator Run()
    {
        yield return LoadScene("TelaInicio");
        yield return WaitSeconds(1.5f);
        yield return QAInput.Tap(Key.Enter);
        yield return WaitSeconds(1f);
        Check(!PhotosensitivityWarning.IsShowing, "aviso de fotossensibilidade fecha com ENTER");
        yield return QAInput.Tap(Key.Space);
        yield return WaitForScene("Menu", 15f);

        // Primeira partida: completa a missao e chega ao final.
        yield return PlayFromMenu();
        CheckGameplayState("1a partida");

        MissionSystem missions = Require<MissionSystem>();
        if (missions != null)
            yield return CompleteMissions(missions);

        // Missao final: entregar as pecas no ponto de montagem -> cutscene final -> creditos.
        Check(missions != null && missions.ReadyToDeliver, "missoes completas liberam a entrega");
        DeliveryPoint delivery = Object.FindFirstObjectByType<DeliveryPoint>();
        Check(delivery != null && delivery.CanInteract(), "ponto de montagem na fase");
        if (delivery != null)
            delivery.Interact();

        // A garra da sala de montagem (dentro da fase) monta e segue para a cutscene final.
        Check(AssemblyStation.Instance != null && AssemblyStation.Instance.Running, "garra comeca a montar");
        yield return WaitForScene("OutroCutsceneVideo", 120f);
        yield return WaitSeconds(2f);
        IntroVideoCutscene outro = Object.FindFirstObjectByType<IntroVideoCutscene>();
        Check(outro != null && outro.player != null && outro.player.isPlaying, "cutscene final tocando");
        if (outro != null)
            outro.Finish();

        yield return WaitForScene("Agradecimentos", 20f);
        yield return WaitSeconds(1f);
        Check(SceneManager.GetActiveScene().name == "Agradecimentos", "agradecimentos depois da cutscene final");
        SceneManager.LoadScene("Menu");
        yield return WaitForScene("Menu", 15f);
        Check(Cursor.visible && Cursor.lockState != CursorLockMode.Locked, "cursor livre no menu");

        // Segunda partida: estado limpo.
        yield return PlayFromMenu();
        CheckGameplayState("2a partida");
        Check(!PauseMenu.IsPaused, "2a partida nao comeca pausada");
        Check(!DefeatController.IsDefeated, "2a partida nao comeca derrotada");

        MissionSystem second = Require<MissionSystem>();
        if (second != null)
            Check(second.GetPhotoCount() == 0 && second.GetPieceCount() == 0 && second.GetLocationCount() == 0,
                "missoes zeradas na 2a partida");

        // Volta ao menu pela pausa e confere audio fora de hora.
        PauseMenu pause = Require<PauseMenu>();
        if (pause != null)
            pause.GoToMainMenu();
        yield return WaitForScene("Menu", 15f);
        yield return WaitSeconds(1f);

        GameObject host = QACoroutineHost.Get().gameObject;
        string[] stray = host.scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<AudioSource>(true))
            .Where(a => a.isPlaying && a.GetComponentInParent<MusicManager>() == null)
            .Select(a => a.name)
            .ToArray();
        Check(stray.Length == 0, "nenhum audio persistente da fase tocando no menu (" + string.Join(", ", stray) + ")");
    }

    private IEnumerator PlayFromMenu()
    {
        Button play = FindButtonNamed("PlayButton") ?? FindButtonNamed("Play") ?? FindButtonWithLabel("PLAY");
        Check(play != null, "menu tem PlayButton");
        if (play == null)
            yield break;

        play.onClick.Invoke();
        yield return WaitForScene("IntroCutsceneVideo", 15f);
        yield return WaitSeconds(1f);
        yield return QAInput.Tap(Key.Escape);
        yield return WaitSeconds(0.3f);
        yield return QAInput.Tap(Key.Escape);
        yield return WaitForScene("Principal", 20f);
        yield return WaitGameplay();
    }

    private void CheckGameplayState(string label)
    {
        int listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.isActiveAndEnabled);
        int mainCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled && c.CompareTag("MainCamera"));

        Check(listeners == 1, label + ": um AudioListener ativo (" + listeners + ")");
        Check(mainCameras == 1, label + ": uma camera principal (" + mainCameras + ")");
        Check(Mathf.Approximately(Time.timeScale, 1f), label + ": timeScale 1 (" + Time.timeScale + ")");
        Check(CursorCaptured, label + ": cursor capturado no jogo (" + Cursor.lockState + ", visivel=" + Cursor.visible + ")");
    }

    private IEnumerator CompleteMissions(MissionSystem missions)
    {
        for (int i = 0; i < 50; i++) missions.AddFoto();
        for (int i = 0; i < 50; i++) missions.AddPeca();

        // Fotografa cada local com uma camera temporaria apontada para ele.
        GameObject go = new GameObject("QAPhotoCam");
        Camera cam = go.AddComponent<Camera>();
        cam.enabled = false;

        foreach (PhotoLocation location in PhotoLocation.All.ToArray())
        {
            Vector3 target = location.TargetPoint;
            foreach (Vector3 dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                go.transform.position = target - dir * 2.5f + Vector3.up * 0.3f;
                go.transform.LookAt(target);
                if (location.IsInFrame(cam))
                {
                    missions.RegisterPhoto(cam);
                    break;
                }
            }
            yield return null;
        }

        Object.Destroy(go);
        Check(missions.GetLocationCount() >= 1, "fotografou locais da missao (" + missions.GetLocationCount() + ")");
    }

    private static Button FindButtonNamed(string name)
    {
        return Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(b => b.name == name);
    }

    private static Button FindButtonWithLabel(string label)
    {
        return Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(b =>
            {
                var tmp = b.GetComponentInChildren<TMPro.TMP_Text>(true);
                var legacy = b.GetComponentInChildren<Text>(true);
                string text = tmp != null ? tmp.text : legacy != null ? legacy.text : "";
                return text.Trim().ToLowerInvariant().Contains(label.ToLowerInvariant());
            });
    }
}
