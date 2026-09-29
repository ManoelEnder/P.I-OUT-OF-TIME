using System;
using UnityEngine;

[Serializable]
public class PhotoCapture : MonoBehaviour
{
    [SerializeField] private Camera photoCam;
    [SerializeField] private int resolution = 512;
    [SerializeField] private int depthBits = 24;

    private RenderTexture renderTexture;

    public void Initialize()
    {
        renderTexture = new RenderTexture(resolution, resolution, depthBits);
        renderTexture.Create();

        if (photoCam != null)
        {
            photoCam.targetTexture = renderTexture;
            photoCam.enabled = false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (photoCam != null)
            photoCam.enabled = enabled;
    }

    public void SyncWith(Camera playerCam)
    {
        if (photoCam == null || playerCam == null)
            return;

        photoCam.transform.SetPositionAndRotation(
            playerCam.transform.position,
            playerCam.transform.rotation
        );

        photoCam.fieldOfView = playerCam.fieldOfView;
    }

    public Texture2D Capture()
    {
        if (photoCam != null)
            photoCam.Render();

        RenderTexture.active = renderTexture;

        Texture2D photo = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGB24,
            false
        );

        photo.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );

        photo.Apply();

        RenderTexture.active = null;

        return photo;
    }
}
