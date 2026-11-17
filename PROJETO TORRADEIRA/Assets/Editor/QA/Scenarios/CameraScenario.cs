using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Visor da camera: olhar com o visor fechado e aberto, transicao, zoom, foto e volta a visao normal.
public class CameraScenario : QAScenario
{
    public override string Name => "Camera";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();

        PhotoCamera photo = Require<PhotoCamera>();
        CameraPhotoData data = Require<CameraPhotoData>();
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        if (photo == null || data == null)
            yield break;

        Camera view = photo.playerCam;
        photo.AddBattery(100);

        yield return CheckLook(player, view, "visor fechado");

        // Abre o visor.
        yield return QAInput.Tap(Key.C);
        yield return WaitSeconds(photo.enterTransitionTime + 0.4f);
        Check(photo.cameraHUD != null && photo.cameraHUD.activeSelf, "HUD do visor aparece");
        Check(photo.crosshair == null || !photo.crosshair.activeSelf, "mira some com o visor aberto");

        yield return CheckLook(player, view, "visor aberto");

        // Zoom.
        float fov = view.fieldOfView;
        for (int i = 0; i < 4; i++) yield return QAInput.Scroll(120f);
        yield return WaitSeconds(0.6f);
        Check(view.fieldOfView < fov - 1f, "zoom aproxima (" + fov.ToString("0.0") + " -> " + view.fieldOfView.ToString("0.0") + ")");
        Check(view.fieldOfView >= photo.minZoomFOV - 0.01f, "zoom respeita o minimo");
        for (int i = 0; i < 10; i++) yield return QAInput.Scroll(-120f);
        yield return WaitSeconds(0.6f);
        Check(view.fieldOfView <= photo.maxZoomFOV + 0.01f, "zoom respeita o maximo (" + view.fieldOfView.ToString("0.0") + ")");

        // Foto.
        int before = data.PhotoCount;
        yield return QAInput.Click();
        yield return WaitSeconds(0.6f);
        Check(data.PhotoCount == before + 1, "clique tira uma foto (" + before + " -> " + data.PhotoCount + ")");
        yield return WaitSeconds(data.PreviewDuration + data.Cooldown + 0.5f);

        // Fecha o visor.
        yield return QAInput.Tap(Key.C);
        yield return WaitSeconds(photo.enterTransitionTime + 0.6f);
        Check(photo.cameraHUD == null || !photo.cameraHUD.activeSelf, "HUD do visor some ao fechar");
        Check(photo.crosshair == null || photo.crosshair.activeSelf, "mira volta ao fechar");
        Check(Mathf.Abs(view.fieldOfView - photo.normalFOV) < 0.5f, "FOV volta ao normal (" + view.fieldOfView.ToString("0.0") + ")");

        // Controles restaurados.
        Vector3 start = player.position;
        QAInput.Hold(Key.W, true);
        yield return WaitSeconds(0.6f);
        QAInput.Hold(Key.W, false);
        yield return null;
        Check(Vector3.Distance(start, player.position) > 0.1f, "W anda depois de fechar o visor");
    }

    private IEnumerator CheckLook(Transform player, Camera view, string label)
    {
        float yaw = player.eulerAngles.y;
        yield return QAInput.Look(new Vector2(20f, 0f), 10);
        Check(Mathf.Abs(Mathf.DeltaAngle(yaw, player.eulerAngles.y)) > 1f, label + ": olhar para a direita gira");

        yaw = player.eulerAngles.y;
        yield return QAInput.Look(new Vector2(-20f, 0f), 10);
        Check(Mathf.Abs(Mathf.DeltaAngle(yaw, player.eulerAngles.y)) > 1f, label + ": olhar para a esquerda gira");

        float pitch = view.transform.eulerAngles.x;
        yield return QAInput.Look(new Vector2(0f, 15f), 10);
        Check(Mathf.Abs(Mathf.DeltaAngle(pitch, view.transform.eulerAngles.x)) > 1f, label + ": olhar para cima inclina");

        pitch = view.transform.eulerAngles.x;
        yield return QAInput.Look(new Vector2(0f, -15f), 10);
        Check(Mathf.Abs(Mathf.DeltaAngle(pitch, view.transform.eulerAngles.x)) > 1f, label + ": olhar para baixo inclina");
    }
}
