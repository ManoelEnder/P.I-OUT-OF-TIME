using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Captura da mensagem de radio (arte balancando, texto inclinado) e confere que some com o visor.
public class RadioLooksScenario : QAScenario
{
    public override string Name => "RadioLooks";

    public override IEnumerator Run()
    {
        string folder = Path.Combine(QARunner.ResultsFolder, "radio");
        Directory.CreateDirectory(folder);
        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();

        RadioMessageUI radio = Require<RadioMessageUI>();
        PhotoCamera photo = Require<PhotoCamera>();
        if (radio == null || photo == null) yield break;

        radio.ShowMessage("Aqui é o laboratório. Se estiver ouvindo,\nprocure as peças antes que ele te encontre.\nNão confie no que aparece no visor.", null);
        yield return WaitSeconds(1.4f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, "01_radio.png"));
        yield return WaitSeconds(0.15f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, "02_radio_balanco.png"));

        yield return QAInput.Tap(Key.C);
        yield return WaitTransition(photo);
        yield return WaitSeconds(0.3f);
        Check(radio.messageText.color.a < 0.05f, "radio some com o visor aberto");
        radio.ShowMessage("teste com visor", null);
        yield return WaitSeconds(0.8f);
        Check(radio.messageText.color.a < 0.05f, "radio nao toca com o visor aberto");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, "03_visor_sem_radio.png"));

        // Zoom do visor: rolar a roda tem que fechar o FOV.
        float before = photo.playerCam.fieldOfView;
        for (int i = 0; i < 6; i++)
            yield return QAInput.Scroll(120f);
        yield return WaitSeconds(0.8f);
        yield return new WaitForEndOfFrame();
        float after = photo.playerCam.fieldOfView;   // FOV realmente usado no quadro (depois do Cinemachine)
        var vcam = Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        var brain = photo.playerCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
        Debug.Log("QA FOV render=" + after + " lente=" + (vcam != null ? vcam.Lens.FieldOfView.ToString() : "sem vcam") + " brain=" + (brain != null && brain.enabled) + " ativa=" + (brain != null ? brain.ActiveVirtualCamera?.Name : "-") + " bridge=" + (photo.playerCam.GetComponent<FovBridge>() != null));
        Check(after < before - 5f, "zoom funciona (FOV " + before.ToString("F1") + " -> " + after.ToString("F1") + ")");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, "04_visor_zoom.png"));
    }
}
