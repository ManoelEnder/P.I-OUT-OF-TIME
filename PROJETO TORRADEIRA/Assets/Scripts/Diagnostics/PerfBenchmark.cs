using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Percorre sozinho os trechos medidos do jogo quando a build roda com -benchmark.
// Os resultados saem pelo PerfProbe (perf/<data>_summary.csv). Nenhuma cena e alterada.
public class PerfBenchmark : MonoBehaviour
{
    private const float SegmentSeconds = 20f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!PerfProbe.HasArg("-benchmark"))
            return;

        GameObject go = new GameObject("PerfBenchmark");
        DontDestroyOnLoad(go);
        go.AddComponent<PerfBenchmark>();
    }

    private IEnumerator Start()
    {
        QAInput.Ensure();
        yield return Wait(2f);

        // Tela inicial: fecha o aviso e passa para o menu.
        yield return QAInput.Tap(Key.Enter);
        yield return Wait(1f);
        yield return QAInput.Tap(Key.Space);
        yield return WaitScene("Menu");

        yield return Measure("menu", SegmentSeconds, null);

        SceneManager.LoadScene("IntroCutsceneVideo");
        yield return WaitScene("IntroCutsceneVideo");
        yield return Measure("cutscene", 15f, null);

        SceneManager.LoadScene("Principal");
        yield return WaitScene("Principal");
        yield return WaitGameplay();

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        GameObject enemy = FindEnemy();
        if (enemy != null)
            enemy.SetActive(false);

        yield return Measure("principal_normal", SegmentSeconds, QAInput.Look(new Vector2(4f, 0f), int.MaxValue));

        foreach (PerfPoints.Point point in PerfPoints.Heavy)
        {
            Teleport(player, point.Position, point.Yaw);
            yield return Wait(1f);
            yield return Measure("principal_pesado_" + point.Name, 10f, QAInput.Look(new Vector2(3f, 0f), int.MaxValue));
        }

        PhotoCamera photo = FindAnyObjectByType<PhotoCamera>();
        yield return QAInput.Tap(Key.C);
        yield return Wait(1f);
        yield return Measure("visor_aberto", SegmentSeconds, ZoomLoop());

        CameraPhotoData data = FindAnyObjectByType<CameraPhotoData>();
        float cooldown = data != null ? data.Cooldown + 0.15f : 1f;
        yield return Measure("fotos_sequencia", cooldown * 10f + 1f, Photos(10, cooldown, photo));

        // 10 fotos seguidas superaquecem a camera (mecanica do jogo); recupera para medir o resto com camera.
        CameraOverheat heat = FindAnyObjectByType<CameraOverheat>();
        if (heat != null && heat.IsCameraLost)
            heat.RecoverCamera();
        yield return Wait(1f);

        yield return QAInput.Tap(Key.C);
        yield return Wait(1f);
        yield return QAInput.Tap(Key.F);
        yield return Wait(0.5f);
        yield return Measure("album", 10f, AlbumBrowse());
        yield return QAInput.Tap(Key.F);
        yield return Wait(1f);

        yield return QAInput.Tap(Key.Escape);
        yield return ShotLater("pausa", 0.5f);
        yield return QAInput.Tap(Key.Escape);
        yield return Wait(0.5f);

        yield return QAInput.Tap(Key.C);
        yield return Measure("superaquecimento", 60f, Overheat(heat, photo));

        if (enemy != null)
        {
            enemy.SetActive(true);
            yield return null;
            PlaceInFrontOf(player, enemy.transform);
            yield return Measure("inimigo_perseguindo", 10f, null, () => DefeatController.IsDefeated);
        }

        PerfProbe.End();
        Debug.Log("BENCHMARK FIM " + PerfProbe.Folder);
        yield return Wait(0.5f);
        Application.Quit();
    }

    // Mede um trecho enquanto a acao roda; termina no tempo ou quando "done" ficar verdadeiro.
    private IEnumerator Measure(string name, float seconds, IEnumerator action, System.Func<bool> done = null)
    {
        // A captura (PNG) trava um quadro: tirada antes de comecar a medir.
        Coroutine running = action != null ? StartCoroutine(action) : null;
        yield return ShotLater(name, 1f);
        yield return Wait(0.3f);
        PerfProbe.Begin(name);

        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end && (done == null || !done()))
            yield return null;

        PerfProbe.End();

        if (running != null)
            StopCoroutine(running);
    }

    // Captura comparavel (mesmo roteiro antes e depois), com HUD e pos-processamento.
    private static IEnumerator ShotLater(string name, float delay)
    {
        yield return Wait(delay);

        if (PerfProbe.Folder != null)
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(PerfProbe.Folder, "shot_" + name.Replace(':', '_') + ".png"));
    }

    private IEnumerator ZoomLoop()
    {
        while (true)
        {
            for (int i = 0; i < 6; i++) { yield return QAInput.Scroll(120f); yield return Wait(0.2f); }
            for (int i = 0; i < 6; i++) { yield return QAInput.Scroll(-120f); yield return Wait(0.2f); }
            yield return QAInput.Look(new Vector2(3f, 1f), 60);
        }
    }

    private IEnumerator Photos(int count, float cooldown, PhotoCamera photo)
    {
        for (int i = 0; i < count; i++)
        {
            if (photo != null)
                photo.AddBattery(100);

            yield return QAInput.Click();
            yield return Wait(cooldown);
        }
    }

    private IEnumerator AlbumBrowse()
    {
        while (true)
        {
            yield return QAInput.Tap(Key.RightArrow);
            yield return Wait(0.4f);
        }
    }

    private IEnumerator Overheat(CameraOverheat heat, PhotoCamera photo)
    {
        while (heat != null && !heat.IsCameraLost)
        {
            if (photo != null)
                photo.AddBattery(100);

            heat.RegisterPhotoHeat();
            yield return Wait(0.5f);
        }
    }

    private static IEnumerator Wait(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return null;
    }

    private static IEnumerator WaitScene(string name)
    {
        float end = Time.realtimeSinceStartup + 30f;
        while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < end)
            yield return null;
        yield return null;
    }

    // A intro de viagem no tempo congela o jogo; espera ela liberar.
    private static IEnumerator WaitGameplay()
    {
        float end = Time.realtimeSinceStartup + 60f;
        float free = 0f;

        while (free < 2f && Time.realtimeSinceStartup < end)
        {
            free = Time.timeScale > 0f ? free + Time.unscaledDeltaTime : 0f;
            yield return null;
        }
    }

    private static GameObject FindEnemy()
    {
        TemporalEraserController enemy = FindAnyObjectByType<TemporalEraserController>();
        return enemy != null ? enemy.gameObject : null;
    }

    // Jogador a 8-12 m do inimigo, num ponto do NavMesh com caminho livre ate ele, olhando para ele:
    // a perseguicao dura alguns segundos antes da derrota (antes caia atras de parede ou em cima dele).
    public static bool PlaceInFrontOf(Transform player, Transform enemy)
    {
        foreach (float distance in new[] { 12f, 10f, 8f, 6f })
        {
            foreach (float angle in new[] { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f, 180f })
            {
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * enemy.forward;
                if (!NavMesh.SamplePosition(enemy.position + dir * distance, out NavMeshHit spot, 1.5f, NavMesh.AllAreas))
                    continue;

                if (NavMesh.Raycast(enemy.position, spot.position, out _, NavMesh.AllAreas))
                    continue;

                Vector3 back = enemy.position - spot.position;
                back.y = 0f;
                Teleport(player, spot.position, Quaternion.LookRotation(back).eulerAngles.y);
                return true;
            }
        }

        return false;
    }

    public static void Teleport(Transform player, Vector3 position, float yaw)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        player.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        if (controller != null)
            controller.enabled = true;
    }
}
