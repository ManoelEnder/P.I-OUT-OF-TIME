using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class PhotoCounter
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private string prefix = "Fotos: ";

    public int Count { get; private set; }

    public void Reset()
    {
        Count = 0;
        Refresh();
    }

    public void Increment()
    {
        Count++;
        Refresh();
    }

    private void Refresh()
    {
        if (label != null)
            label.text = prefix + Count;
    }
}

[Serializable]
public class PhotoPreview
{
    [SerializeField] private RawImage image;

    public void Hide()
    {
        if (image != null)
            image.gameObject.SetActive(false);
    }

    public void Show(Texture2D photo)
    {
        if (image == null)
            return;

        image.texture = photo;
        image.gameObject.SetActive(true);
    }
}

[Serializable]
public class ShutterSound
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clip;

    public void Play()
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}

public class CameraPhotoData : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private PhotoPreview preview;
    [SerializeField] private ShutterSound shutter;
    [SerializeField] private PhotoCounter counter;

    [Header("Sistemas")]
    [SerializeField] private MissionSystem missionSystem;
    [SerializeField] private AlbumController albumController;

    [Header("Tempos")]
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private float previewDuration = 1.3f;

    public float Cooldown => cooldown;
    public float PreviewDuration => previewDuration;
    public int PhotoCount => counter.Count;

    public void Initialize()
    {
        preview.Hide();
        counter.Reset();
    }

    public void ProcessPhoto(Texture2D photo)
    {
        if (photo == null)
            return;

        counter.Increment();
        shutter.Play();
        RegisterPhoto(photo);
        preview.Show(photo);
    }

    public int GetPhotoCount()
    {
        return PhotoCount;
    }

    private void RegisterPhoto(Texture2D photo)
    {
        if (missionSystem != null)
            missionSystem.AddFoto();

        if (albumController != null)
            albumController.AddPhoto(photo);
    }
}
