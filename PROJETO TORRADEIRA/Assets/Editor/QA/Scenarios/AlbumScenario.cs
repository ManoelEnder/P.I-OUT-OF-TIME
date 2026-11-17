using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Album: varias fotos, album lotado (sem vazar textura), abrir, apagar pelo teclado e pela API, fechar.
public class AlbumScenario : QAScenario
{
    public override string Name => "Album";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        yield return WaitGameplay();

        PhotoCamera photo = Require<PhotoCamera>();
        CameraPhotoData data = Require<CameraPhotoData>();
        AlbumController album = Require<AlbumController>();
        if (photo == null || data == null || album == null)
            yield break;

        photo.AddBattery(100);
        yield return QAInput.Tap(Key.C);
        yield return WaitSeconds(photo.enterTransitionTime + 0.4f);

        // Algumas fotos de verdade.
        for (int i = 0; i < 3; i++)
            yield return Shoot(photo, data);
        Check(album.Photos.Count == 3, "album guarda as fotos tiradas (" + album.Photos.Count + ")");

        // Enche o resto com texturas do tamanho da tela.
        while (!album.IsFull)
            album.AddPhoto(new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false));

        int textures = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
        for (int i = 0; i < 3; i++)
            yield return Shoot(photo, data);
        yield return null;
        int growth = Resources.FindObjectsOfTypeAll<Texture2D>().Length - textures;
        Check(growth <= 1, "album lotado: fotos extras nao acumulam textura (+" + growth + ")");

        yield return QAInput.Tap(Key.C);
        yield return WaitSeconds(photo.enterTransitionTime + 0.6f);

        // Abre, apaga pelo teclado (Delete + Y) e pela API.
        yield return QAInput.Tap(Key.F);
        yield return WaitSeconds(0.5f);
        Check(album.IsAlbumOpen, "F abre o album");

        int count = album.Photos.Count;
        yield return QAInput.Tap(Key.Delete);
        yield return WaitSeconds(0.3f);
        yield return QAInput.Tap(Key.Y);
        yield return WaitSeconds(0.3f);
        Check(album.Photos.Count == count - 1, "Delete + Y apaga a foto (" + count + " -> " + album.Photos.Count + ")");

        Texture2D removed = album.Photos[0].Texture;
        count = album.Photos.Count;
        album.RemovePhoto(0);
        yield return null;
        Check(album.Photos.Count == count - 1, "RemovePhoto tira do album");
        Check(removed == null, "textura da foto apagada e destruida");

        yield return QAInput.Tap(Key.F);
        yield return WaitSeconds(0.5f);
        Check(!album.IsAlbumOpen, "F fecha o album");
        Check(Time.timeScale == 1f, "tempo volta ao normal depois do album (" + Time.timeScale + ")");
        Check(CursorCaptured, "cursor volta a ser do jogo depois do album");
    }

    private IEnumerator Shoot(PhotoCamera photo, CameraPhotoData data)
    {
        photo.AddBattery(100);
        yield return QAInput.Click();
        yield return WaitSeconds(data.PreviewDuration + data.Cooldown + 0.5f);
    }
}
