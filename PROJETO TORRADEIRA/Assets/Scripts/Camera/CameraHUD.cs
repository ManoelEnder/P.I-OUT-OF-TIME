using System;
using UnityEngine;

[Serializable]
public class CameraHUD : MonoBehaviour
{
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject cameraHUD;
    [SerializeField] private CameraBlink cameraBlink;
    [SerializeField] private CameraBlackBorders blackBorders;
    [SerializeField] private CameraHUDZoom cameraHUDZoom;

    public void Initialize()
    {
        SetObjectActive(cameraHUD, false);
        SetObjectActive(crosshair, true);

        if (blackBorders != null)
        {
            blackBorders.ResetBorders();
            blackBorders.SetActive(false);
        }

        if (cameraHUDZoom != null)
            cameraHUDZoom.ResetHUD();
    }

    public void SetCrosshairVisible(bool visible)
    {
        SetObjectActive(crosshair, visible);
    }

    public void Show()
    {
        SetObjectActive(cameraHUD, true);

        if (blackBorders != null)
        {
            blackBorders.SetActive(true);
            blackBorders.ResetBorders();
        }

        if (cameraHUDZoom != null)
            cameraHUDZoom.ResetHUD();

        if (cameraBlink != null)
            cameraBlink.Play();
    }

    public void Hide()
    {
        SetObjectActive(cameraHUD, false);

        if (blackBorders != null)
        {
            blackBorders.ResetBorders();
            blackBorders.SetActive(false);
        }

        if (cameraHUDZoom != null)
            cameraHUDZoom.ResetHUD();
    }

    public void ApplyZoom(float zoomAmount)
    {
        if (cameraHUDZoom != null)
            cameraHUDZoom.ApplyZoom(zoomAmount);

        if (blackBorders != null)
            blackBorders.SetZoom(zoomAmount);
    }

    private void SetObjectActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
