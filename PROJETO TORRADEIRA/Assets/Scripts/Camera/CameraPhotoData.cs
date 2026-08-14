using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CameraPhotoData : MonoBehaviour
{
    [Header("Photo Preview")]
    public RawImage photoPreview;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shutterSound;

    [Header("Photo Counter")]
    public TextMeshProUGUI photoCounter;

    [Header("Systems")]
    public MissionSystem missionSystem;
    public AlbumController albumController;

    [Header("Photo Settings")]
    public float cooldown = 2f;
    public float previewDuration = 1.3f;

    private int photoCount;

    public void Initialize()
    {
        photoCount = 0;

        if (photoPreview != null)
            photoPreview.gameObject.SetActive(false);

        UpdatePhotoCounter();
    }

    public void SetCameraVolume(bool active)
    {
        if (photoCameraVolume != null)
            photoCameraVolume.SetActive(active);
    }

    public void ProcessPhoto(Texture2D photo)
    {
        if (photo == null)
            return;

        photoCount++;

        UpdatePhotoCounter();

        PlayShutterSound();

        if (missionSystem != null)
            missionSystem.AddFoto();

        if (albumController != null)
            albumController.AddPhoto(photo);

        ShowPhotoPreview(photo);
    }

    void PlayShutterSound()
    {
        if (audioSource == null ||
            shutterSound == null)
        {
            return;
        }

        audioSource.PlayOneShot(shutterSound);
    }

    void ShowPhotoPreview(Texture2D photo)
    {
        if (photoPreview == null)
            return;

        photoPreview.texture = photo;
        photoPreview.gameObject.SetActive(true);
    }

    void UpdatePhotoCounter()
    {
        if (photoCounter != null)
        {
            photoCounter.text =
                "Fotos: " + photoCount;
        }
    }

    public int GetPhotoCount()
    {
        return photoCount;
    }
}