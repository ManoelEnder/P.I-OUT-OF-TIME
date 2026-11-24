using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Capturas da interface (revisao visual): radar abaixado/levantado, album e missoes (TAB),
// pausa/configuracoes/controles, lente do visor com e sem zoom, foto instantanea revelando.
// Saida: docs/revisao/qa/ui/*.png
public class UiLooksScenario : QAScenario
{
    public override string Name => "UiLooks";

    private static string Folder => Path.Combine(QARunner.ResultsFolder, "ui");

    public override IEnumerator Run()
    {
        Directory.CreateDirectory(Folder);

        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();
        yield return WaitSeconds(1f);

        // Radar.
        RadarDevice device = Object.FindFirstObjectByType<RadarDevice>(FindObjectsInactive.Include);
        Check(device != null, "aparelho de radar existe");
        if (device != null)
        {
            RectTransform r = (RectTransform)device.transform;
            Canvas c = device.GetComponentInParent<Canvas>(true);
            var corners = new Vector3[4];
            r.GetWorldCorners(corners);
            Debug.Log("QA TRACE radar ativo=" + device.gameObject.activeInHierarchy + " pai=" + r.parent.name +
                      " canvas=" + (c != null ? c.name + " " + c.renderMode + " cam=" + (c.worldCamera != null ? c.worldCamera.name : "-") + " escala=" + c.scaleFactor : "nenhum") +
                      " ancora=" + r.anchoredPosition + " tam=" + r.rect.size + " cantos=" + corners[0] + " ate " + corners[2] +
                      " camada=" + LayerMask.LayerToName(device.gameObject.layer));
        }
        yield return Shot("radar_abaixado");
        QAInput.Hold(Key.Q, true);
        yield return WaitSeconds(0.6f);
        yield return Shot("radar_levantado");
        QAInput.Hold(Key.W, true);
        yield return WaitSeconds(1.2f);
        yield return Shot("radar_andando");
        QAInput.Hold(Key.W, false);
        QAInput.Hold(Key.Q, false);
        yield return WaitSeconds(0.5f);

        // Missoes pelo TAB (abre o album na pagina de missoes) e troca para o album.
        yield return QAInput.Tap(Key.Tab);
        yield return WaitSeconds(0.5f);
        yield return Shot("diario_missoes");
        yield return QAInput.Tap(Key.Tab);
        yield return WaitSecondsRealtime(0.12f);
        yield return Shot("diario_trocando");
        yield return WaitSecondsRealtime(0.5f);
        yield return Shot("diario_album");
        yield return QAInput.Tap(Key.Escape);
        yield return WaitSeconds(0.5f);

        // Pausa.
        yield return QAInput.Tap(Key.Escape);
        yield return WaitSecondsRealtime(0.12f);
        yield return Shot("pausa_entrando");
        yield return WaitSecondsRealtime(0.6f);
        yield return Shot("pausa");

        PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>();
        if (pause != null)
        {
            pause.OpenSettings();
            yield return WaitSecondsRealtime(0.6f);
            yield return Shot("pausa_config");
            pause.CloseSettings();
            pause.OpenControls();
            yield return WaitSecondsRealtime(0.6f);
            yield return Shot("pausa_controles");
            pause.CloseControls();
            pause.ResumeGame();
        }
        yield return WaitSeconds(0.6f);

        // Visor: lente abrindo, parada, com zoom, foto.
        PhotoCamera photo = Object.FindFirstObjectByType<PhotoCamera>();
        if (photo != null)
        {
            photo.AddBattery(100);
            yield return QAInput.Tap(Key.C);
            yield return WaitSecondsRealtime(0.18f);
            yield return Shot("camera_subindo_1");
            yield return WaitSecondsRealtime(0.2f);
            yield return Shot("camera_subindo_2");
            yield return WaitSecondsRealtime(0.45f);
            yield return Shot("lente_abrindo");
            yield return WaitTransition(photo);
            yield return WaitSeconds(0.3f);
            yield return Shot("lente_aberta");
            for (int i = 0; i < 6; i++) yield return QAInput.Scroll(120f);
            yield return WaitSeconds(0.8f);
            yield return Shot("lente_zoom");
            for (int i = 0; i < 8; i++) yield return QAInput.Scroll(-120f);
            yield return WaitSeconds(0.6f);

            yield return QAInput.Click();
            yield return WaitSecondsRealtime(0.25f);
            yield return Shot("foto_saindo");
            yield return WaitSecondsRealtime(0.6f);
            yield return Shot("foto_revelando");
            yield return WaitSecondsRealtime(1.2f);
            yield return Shot("foto_revelada");
            yield return WaitSecondsRealtime(0.9f);
            yield return Shot("foto_indo_album");
            yield return WaitSeconds(2.5f);

            yield return QAInput.Tap(Key.C);
            yield return WaitSecondsRealtime(0.5f);
            yield return Shot("lente_fechando");
            yield return WaitSecondsRealtime(0.55f);
            yield return Shot("lente_fechada");
            yield return WaitSecondsRealtime(0.35f);
            yield return Shot("camera_descendo");
            yield return WaitSeconds(1.5f);
        }
    }

    private IEnumerator WaitSecondsRealtime(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return null;
    }

    private IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(Folder, name + ".png"));
        yield return null;
    }
}
