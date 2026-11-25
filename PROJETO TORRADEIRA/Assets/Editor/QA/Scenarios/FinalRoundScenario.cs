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

        // Sala da garra.
        MissionSystem.DeliveredPieces = 4;
        SceneManager.LoadScene(MissionSystem.AssemblyScene);
        yield return WaitForScene(MissionSystem.AssemblyScene, 20f);
        yield return WaitSeconds(1.6f);
        AssemblyRoom room = Object.FindFirstObjectByType<AssemblyRoom>();
        Check(room != null, "sala da garra carrega");
        if (room == null) yield break;
        yield return Shot("10_sala_entrada");
        room.PlaceAllForTest();
        float start = Time.realtimeSinceStartup;
        float[] marks = { 2f, 6f, 11f, 17f, 24f };
        int i = 0;
        while (!room.Finished && Time.realtimeSinceStartup - start < 90f)
        {
            if (i < marks.Length && Time.realtimeSinceStartup - start >= marks[i])
            {
                yield return Shot("1" + (i + 1) + "_garra_" + marks[i].ToString("0") + "s");
                i++;
            }
            yield return null;
        }
        Check(room.Finished, "garra monta todas as pecas (" + (Time.realtimeSinceStartup - start).ToString("0") + " s)");
        yield return Shot("19_prototipo_pronto");

        yield return WaitForScene("OutroCutsceneVideo", 15f);
        Check(SceneManager.GetActiveScene().name == "OutroCutsceneVideo", "segue para a cutscene final");
        var outro = Object.FindFirstObjectByType<IntroVideoCutscene>();
        yield return WaitSeconds(2f);
        if (outro != null) outro.Finish();

        yield return WaitForScene("Agradecimentos", 15f);
        Check(SceneManager.GetActiveScene().name == "Agradecimentos", "cutscene termina na cena de agradecimento");
        yield return WaitSeconds(2.5f);
        yield return Shot("20_agradecimentos");
        yield return WaitSeconds(14f);
        yield return Shot("21_agradecimentos_equipe");
        yield return WaitSeconds(16f);
        yield return Shot("22_agradecimentos_assets");
    }

    private IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(Folder, name + ".png"));
        yield return null;
    }
}
