using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CameraPhotoData : MonoBehaviour
{
    public RawImage photoPreview;
    public AudioSource audioSource;
    public AudioClip shutterSound;
    public TextMeshProUGUI photoCounter;

    public MissionSystem missionSystem;
    public AlbumController albumController;

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

    public void ProcessPhoto(Texture2D photo)
    {
        if (photo == null)
            return;

        photoCount++;

        UpdatePhotoCounter();

        if (audioSource != null &&
            shutterSound != null)
        {
            audioSource.PlayOneShot(shutterSound);
        }

        if (missionSystem != null)
            missionSystem.AddFoto();

        if (albumController != null)
            albumController.AddPhoto(photo);

        if (photoPreview != null)
        {
            photoPreview.texture = photo;
            photoPreview.gameObject.SetActive(true);
        }
    }

    private void UpdatePhotoCounter()
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