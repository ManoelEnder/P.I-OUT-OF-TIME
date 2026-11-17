using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pausa: ESC no meio da transicao do visor, pausar/retomar, ESC com a derrota aberta
// e saida para o menu pela derrota, sem estado preso (tempo, cursor, objetos persistentes).
public class PauseScenario : QAScenario
{
    public override string Name => "Pause";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        yield return WaitGameplay();

        string[] persistentBefore = PersistentRoots();

        // C e ESC no mesmo quadro.
        QAInput.Hold(Key.C, true);
        QAInput.Hold(Key.Escape, true);
        yield return null;
        yield return null;
        QAInput.Hold(Key.C, false);
        QAInput.Hold(Key.Escape, false);
        yield return WaitSeconds(1.5f);

        bool paused = PauseMenu.IsPaused;
        Check(paused == (Time.timeScale == 0f), "pausa e timeScale coerentes apos C+ESC");
        Check(paused == Cursor.visible, "cursor coerente com a pausa apos C+ESC");

        if (paused)
        {
            yield return QAInput.Tap(Key.Escape);
            yield return WaitSeconds(0.5f);
        }

        PhotoCamera photo = Require<PhotoCamera>();
        if (photo != null && photo.TryExitCameraMode())
            yield return WaitSeconds(1f);

        // Pausar e retomar.
        yield return QAInput.Tap(Key.Escape);
        yield return WaitSeconds(0.5f);
        Check(PauseMenu.IsPaused && Time.timeScale == 0f && Cursor.visible, "ESC pausa (pausado=" + PauseMenu.IsPaused + " timeScale=" + Time.timeScale + " visivel=" + Cursor.visible + ")");
        yield return QAInput.Tap(Key.Escape);
        yield return WaitSeconds(0.5f);
        Check(!PauseMenu.IsPaused && Time.timeScale == 1f && CursorCaptured, "ESC retoma (pausado=" + PauseMenu.IsPaused + " timeScale=" + Time.timeScale + " cursor=" + Cursor.lockState + " visivel=" + Cursor.visible + ")");

        // Derrota e ESC.
        TemporalEraserController enemy = Require<TemporalEraserController>();
        if (enemy == null)
            yield break;

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        PerfBenchmark.Teleport(player, enemy.transform.position + enemy.transform.forward * 6f, enemy.transform.eulerAngles.y + 180f);

        float end = Time.realtimeSinceStartup + 15f;
        while (!DefeatController.IsDefeated && Time.realtimeSinceStartup < end)
            yield return null;
        Check(DefeatController.IsDefeated, "inimigo derrota o jogador");

        yield return QAInput.Tap(Key.Escape);
        yield return WaitSeconds(0.5f);
        Check(!PauseMenu.IsPaused, "ESC nao abre a pausa por cima da derrota");

        Button menu = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(b => b.name == "MenuButton" && b.GetComponentInParent<Canvas>(true).name == "DefeatCanvas");
        Check(menu != null, "derrota tem botao MENU");
        if (menu == null)
            yield break;

        menu.onClick.Invoke();
        yield return WaitForScene("Menu", 15f);
        yield return WaitSeconds(1f);

        Check(Time.timeScale == 1f, "timeScale 1 no menu depois da derrota");
        Check(Cursor.visible, "cursor visivel no menu depois da derrota");

        string[] persistentAfter = PersistentRoots();
        Check(persistentAfter.Length == persistentAfter.Distinct().Count(),
            "sem objetos persistentes duplicados (" + string.Join(", ", persistentAfter) + ")");
        Check(persistentAfter.Length <= persistentBefore.Length + 1,
            "nenhum objeto persistente novo acumulado (" + persistentBefore.Length + " -> " + persistentAfter.Length + ")");
    }

    private static string[] PersistentRoots()
    {
        return QACoroutineHost.Get().gameObject.scene.GetRootGameObjects().Select(g => g.name).OrderBy(n => n).ToArray();
    }
}
