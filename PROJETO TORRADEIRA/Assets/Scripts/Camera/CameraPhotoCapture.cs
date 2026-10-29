using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraPhotoCapture : MonoBehaviour
{
    private Camera photoCam;
    private Camera playerCam;
    private CameraPhotoData photoData;

    private Image flashImage;

    private float flashDuration;
    private float flashFadeDuration;

    public void Initialize(
        Camera photoCamera,
        Camera playerCamera,
        CameraPhotoData data,
        Image flash,
        float duration,
        float fadeDuration
    )
    {
        photoCam = photoCamera;
        playerCam = playerCamera;
        photoData = data;

        flashImage = flash;

        flashDuration = Mathf.Max(0f, duration);
        flashFadeDuration = Mathf.Max(0f, fadeDuration);

        if (flashImage != null)
        {
            Color color = flashImage.color;
            color.a = 0f;

            flashImage.color = color;
            flashImage.gameObject.SetActive(false);
        }

        if (photoData != null)
            photoData.Initialize();
    }

    public void PrepareCamera()
    {
        if (photoCam != null && playerCam != null)
            photoCam.fieldOfView = playerCam.fieldOfView;

        if (photoData != null)
            photoData.HidePreviewBeforeCapture();
    }

    public Texture2D Capture()
    {
        return ScreenCapture.CaptureScreenshotAsTexture();
    }

    public void ProcessPhoto(Texture2D photo)
    {
        if (photo == null)
        {
            Debug.LogWarning(
                "Nao foi possivel capturar a fotografia."
            );
            return;
        }

        if (photoData != null)
            photoData.ProcessPhoto(photo);
        else
            Object.Destroy(photo);
    }

    public IEnumerator PlayFlash()
    {
        if (flashImage == null)
            yield break;

        flashImage.gameObject.SetActive(true);

        Color color = flashImage.color;
        color.a = 1f;

        flashImage.color = color;

        if (flashDuration > 0f)
            yield return new WaitForSeconds(flashDuration);

        float timer = 0f;

        while (timer < flashFadeDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                timer / flashFadeDuration
            );

            color.a = 1f - progress;
            flashImage.color = color;

            yield return null;
        }

        color.a = 0f;
        flashImage.color = color;

        flashImage.gameObject.SetActive(false);
    }

    public void Release()
    {
    }
}