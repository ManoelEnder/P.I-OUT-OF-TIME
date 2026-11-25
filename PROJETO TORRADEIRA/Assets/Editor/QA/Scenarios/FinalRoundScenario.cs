using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Rodada final: sem mobilia, folego (stamina), lanterna, inclinacao da corrida (Cinemachine),
// sala da garra montando o prototipo e a cena de agradecimento. Capturas em docs/revisao/qa/final/.
public class FinalRoundScenario : QAScenario
{
    public override string Name => "FinalRound";

    private static string Folder => Path.Combine(QARunner.ResultsFolder, "final");

    public override IEnumerator Run()
    {
        Directory.CreateDirectory(Folder);
        foreach (string f in Directory.GetFiles(Folder, "*.png")) File.Delete(f);

        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();
        yield return WaitSeconds(0.5f);

        Check(GameObject.Find("Mobilia") == null, "sem mobilia na fase");
        PlayerStamina stamina = Object.FindFirstObjectByType<PlayerStamina>();
        CameraFlashlight flashlight = Object.FindFirstObjectByType<CameraFlashlight>();
        RunCameraRig rig = Object.FindFirstObjectByType<RunCameraRig>();
        Check(stamina != null && flashlight != null && rig != null, "stamina, lanterna e camera Cinemachine ativos");
        Check(Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineBrain>() != null, "CinemachineBrain na camera");

        // Vira para a direcao mais livre (corredor) antes de correr.
        Transform runner = GameObject.FindGameObjectWithTag("Player").transform;
        float bestDist = 0f; float bestYaw = 0f;
        for (int k = 0; k < 16; k++)
        {
            Vector3 dir = Quaternion.Euler(0f, k * 22.5f, 0f) * Vector3.forward;
            float d = Physics.Raycast(runner.position + Vector3.up, dir, out RaycastHit h, 40f, ~0, QueryTriggerInteraction.Ignore) ? h.distance : 40f;
            if (d > bestDist) { bestDist = d; bestYaw = k * 22.5f; }
        }
        runner.rotation = Quaternion.Euler(0f, bestYaw, 0f);
        yield return null;

        // Corrida: Shift + W por 2,5 s, inclinando; captura no meio.
        yield return Shot("00_parado");
        QAInput.Hold(Key.W, true);
        QAInput.Hold(Key.LeftShift, true);
        yield return WaitSeconds(1.2f);
        QAInput.Hold(Key.D, true);
        yield return WaitSeconds(0.5f);
        yield return Shot("01_correndo_curva");
        QAInput.Hold(Key.D, false);
        yield return WaitSeconds(0.8f);
        yield return Shot("02_correndo");
        float afterRun = stamina != null ? stamina.Normalized : 1f;
        Check(afterRun < 0.85f, "correr gasta folego (" + afterRun.ToString("0.00") + ")");

        // Esgota o folego.
        yield return WaitSeconds(5f);
        Debug.Log("QA TRACE folego depois de correr 7 s: " + (stamina != null ? stamina.Normalized.ToString("0.00") : "-") + " esgotado=" + (stamina != null && stamina.Exhausted));
        yield return Shot("03_folego_baixo");
        QAInput.Hold(Key.LeftShift, false);
        QAInput.Hold(Key.W, false);
        float low = stamina != null ? stamina.Normalized : 0f;
        yield return WaitSeconds(3f);
        Check(stamina == null || low > 0.98f || stamina.Normalized > low, "folego recupera parado (" + low.ToString("0.00") + " -> " + (stamina != null ? stamina.Normalized.ToString("0.00") : "-") + ")");

        // Lanterna.
        CameraBattery battery = Object.FindFirstObjectByType<CameraBattery>();
        int before = battery != null ? battery.Current : 0;
        for (int t = 0; t < 4 && flashlight != null && !flashlight.IsOn; t++)
        {
            QAInput.Hold(Key.L, true);
            yield return null;
            yield return null;
            QAInput.Hold(Key.L, false);
            yield return WaitSeconds(0.4f);
        }
        Debug.Log("QA TRACE lanterna on=" + (flashlight != null && flashlight.IsOn) + " bateria=" + before + " perdida=" + (Object.FindFirstObjectByType<CameraOverheat>()?.LostCamera != null));
        Check(flashlight != null && flashlight.IsOn, "L liga a lanterna");
        yield return Shot("04_lanterna");
        yield return WaitSeconds(6.5f);
        Check(battery == null || battery.Current < before, "lanterna gasta bateria (" + before + " -> " + (battery != null ? battery.Current : 0) + ")");
        yield return QAInput.Tap(Key.L);
        yield return WaitSeconds(0.3f);
        Check(flashlight != null && !flashlight.IsOn, "L desliga a lanterna");

        // Bateria zerada: a camera nao abre e aparece o aviso.
        if (battery != null)
        {
            battery.Use(battery.Current);
            yield return QAInput.Tap(Key.C);
            yield return WaitSeconds(0.6f);
            PhotoCamera photo = Object.FindFirstObjectByType<PhotoCamera>();
            Check(photo == null || !photo.IsCameraMode, "camera nao abre sem bateria");
            Check(GameObject.Find("BatteryWarning") != null, "aviso de sem bateria aparece");
            yield return Shot("05_sem_bateria");
            battery.Add(battery.Max);
        }

        // Sala de montagem dentro da fase: anda ate a porta e entra.
        AssemblyStation station = AssemblyStation.Instance;
        Check(station != null, "estacao da garra na fase");
        if (station == null) yield break;
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        CharacterController cc = player.GetComponent<CharacterController>();
        Vector3 door = station.floorCenter + new Vector3(0f, 0.1f, station.doorSide * (station.size.y * 0.5f + 1.2f));
        if (cc != null) cc.enabled = false;
        player.position = door;
        player.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, -station.doorSide));
        if (cc != null) cc.enabled = true;
        yield return WaitSeconds(0.6f);
        // Abre a porta mais proxima (se estiver fechada).
        Door nearest = null;
        foreach (Door d in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            if (nearest == null || Vector3.Distance(d.transform.position, door) < Vector3.Distance(nearest.transform.position, door))
                nearest = d;
        Debug.Log("QA TRACE porta mais proxima " + (nearest != null ? nearest.transform.position.ToString("F1") + " dist=" + Vector3.Distance(nearest.transform.position, door).ToString("F1") : "-") + " estacao=" + station.floorCenter);
        if (nearest != null && Vector3.Distance(nearest.transform.position, door) < 4f && nearest.CanInteract())
            nearest.Interact();
        yield return WaitSeconds(1.2f);
        yield return Shot("10_porta_sala");
        if (cc != null) cc.enabled = false;
        player.position = station.floorCenter + new Vector3(0f, 0.1f, station.doorSide * station.size.y * 0.38f);
        player.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, -station.doorSide));
        if (cc != null) cc.enabled = true;
        if (flashlight != null) flashlight.Set(true, false);
        yield return WaitSeconds(0.6f);
        yield return Shot("11_dentro_sala");

        MissionSystem missions = Object.FindFirstObjectByType<MissionSystem>();
        station.Begin(4, missions);
        float start = Time.realtimeSinceStartup;
        float[] marks = { 2f, 6f, 11f, 17f, 24f };
        int i = 0;
        while (!station.Finished && Time.realtimeSinceStartup - start < 90f)
        {
            if (i < marks.Length && Time.realtimeSinceStartup - start >= marks[i])
            {
                yield return Shot("1" + (i + 2) + "_garra_" + marks[i].ToString("0") + "s");
                i++;
            }
            yield return null;
        }
        Check(station.Finished, "garra monta todas as pecas (" + (Time.realtimeSinceStartup - start).ToString("0") + " s)");
        yield return Shot("19_prototipo_pronto");

        yield return WaitForScene("OutroCutsceneVideo", 20f);
        Check(SceneManager.GetActiveScene().name == "OutroCutsceneVideo", "segue para a cutscene final");
        var outro = Object.FindFirstObjectByType<IntroVideoCutscene>();
        yield return WaitSeconds(2f);
        if (outro != null) outro.Finish();

        yield return WaitForScene("Agradecimentos", 15f);
        Check(SceneManager.GetActiveScene().name == "Agradecimentos", "cutscene termina na cena de agradecimento");
        yield return WaitSeconds(2.5f);
        yield return Shot("20_agradecimentos");
        yield return WaitSeconds(8f);
        yield return Shot("21_agradecimentos_equipe");
    }

    private IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(Folder, name + ".png"));
        yield return null;
    }
}
