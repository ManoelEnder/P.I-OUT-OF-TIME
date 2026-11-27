using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

// Capturas de revisao: HUD do visor, easter egg com zoom, brilho de um lugar de foto, radar com
// ponto verde, album cheio (5a foto recusada) e album aberto. Saida: docs/revisao/qa/fotos_lugares/
public class PhotoSpotsLooksScenario : QAScenario
{
    public override string Name => "PhotoSpotsLooks";

    private string folder;

    public override IEnumerator Run()
    {
        folder = Path.Combine(QARunner.ResultsFolder, "fotos_lugares");
        Directory.CreateDirectory(folder);
        foreach (string f in Directory.GetFiles(folder, "*.png")) File.Delete(f);

        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();
        yield return WaitSeconds(0.5f);

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        PhotoCamera photo = Object.FindFirstObjectByType<PhotoCamera>();
        GameObject egg = GameObject.Find("EasterEgg");
        Check(egg != null, "easter egg criado");
        Check(PhotoLocation.All.Count >= 4, "lugares de foto (" + PhotoLocation.All.Count + ")");

        // 1) Brilho de um lugar de foto (o primeiro da lista), visto de perto.
        if (PhotoLocation.All.Count > 0)
        {
            Transform spot = PhotoLocation.All[0].transform;
            Transform glow = spot.childCount > 0 ? spot.GetChild(0) : spot;
            yield return Stand(player, glow.position, 3f);
            yield return Shot("01_brilho_lugar");
            QAInput.Hold(Key.Q, true);
            yield return WaitSeconds(1.2f);
            yield return Shot("02_radar_ponto_verde");
            QAInput.Hold(Key.Q, false);
            yield return WaitSeconds(0.4f);
        }

        // 2) Easter egg: diante da janela, abre o visor sem zoom e depois com zoom.
        if (egg != null && photo != null)
        {
            Vector3 window = egg.transform.position - egg.transform.forward * 0f;
            Vector3 look = egg.transform.position;
            Vector3 stand = new Vector3(-23.935f, 12.1f, -155.908f) - egg.transform.forward * 2.2f;
            Debug.Log("QA TRACE easter egg em " + egg.transform.position + " frente=" + egg.transform.forward + " jogador=" + stand);
            yield return StandAt(player, stand, look);
            yield return Shot("03_janela_sem_camera");
            yield return QAInput.Tap(Key.C);
            yield return WaitTransition(photo);
            yield return WaitSeconds(0.4f);
            yield return Shot("04_visor_sem_zoom");
            Check(!egg.GetComponent<Renderer>().enabled, "easter egg escondido sem zoom");
            for (int i = 0; i < 12; i++) yield return QAInput.Scroll(1f);
            yield return WaitSeconds(1.2f);
            Debug.Log("QA TRACE zoom=" + photo.ZoomAmount.ToString("0.00"));
            yield return Shot("05_visor_com_zoom_easter_egg");
            Check(egg.GetComponent<Renderer>().enabled, "easter egg aparece com zoom");
            for (int i = 0; i < 12; i++) yield return QAInput.Scroll(-1f);
            yield return WaitSeconds(0.6f);

            // 3) Album cheio: enche ate 4 e tenta a 5a foto.
            AlbumController album = Object.FindFirstObjectByType<AlbumController>();
            if (album != null)
            {
                while (!album.IsFull) album.AddPhoto(new Texture2D(16, 9));
                yield return QAInput.Click();
                yield return WaitSeconds(0.5f);
                yield return Shot("06_album_cheio_aviso");
                Check(album.Count == 4, "album nao passa de 4 (" + album.Count + ")");
            }
            yield return QAInput.Tap(Key.C);
            yield return WaitTransition(photo);
            yield return WaitSeconds(0.4f);
            yield return QAInput.Tap(Key.F);
            yield return WaitSeconds(0.8f);
            yield return Shot("07_album_aberto");
            yield return QAInput.Tap(Key.Escape);
        }
    }

    private IEnumerator Stand(Transform player, Vector3 target, float distance)
    {
        Vector3 flat = target - player.position;
        flat.y = 0f;
        Vector3 dir = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward;
        Vector3 pos = target - dir * distance;
        if (Physics.Raycast(new Vector3(pos.x, target.y + 1f, pos.z), Vector3.down, out RaycastHit h, 6f))
            pos.y = h.point.y + 0.1f;
        yield return StandAt(player, pos, target);
    }

    private IEnumerator StandAt(Transform player, Vector3 pos, Vector3 look)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.position = pos;
        Vector3 flat = look - pos;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.01f) player.rotation = Quaternion.LookRotation(flat);
        if (cc != null) cc.enabled = true;
        // Pitch da camera olhando para o alvo.
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        FieldInfo xr = typeof(PlayerMovement).GetField("xRotation", BindingFlags.NonPublic | BindingFlags.Instance);
        if (pm != null && xr != null)
        {
            Vector3 eye = pos + Vector3.up * 1.8f;
            Vector3 d = look - eye;
            float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            xr.SetValue(pm, Mathf.Clamp(pitch, -60f, 60f));
        }
        yield return WaitSeconds(0.8f);
    }

    private IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
        yield return null;
    }
}
