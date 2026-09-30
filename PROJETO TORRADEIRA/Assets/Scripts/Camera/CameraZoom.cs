using System;
using UnityEngine;

[Serializable]
public class CameraZoom : MonoBehaviour
{
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomFOV = 40f;
    [SerializeField] private float minZoomFOV = 25f;
    [SerializeField] private float maxZoomFOV = 60f;
    [SerializeField] private float zoomSpeed = 2.5f;

    private const float SmoothFactor = 10f;

    public float NormalFOV => normalFOV;
    public float ZoomFOV => zoomFOV;
    public float TargetFOV { get; set; }

    public void Initialize(Camera playerCam)
    {
        TargetFOV = normalFOV;

        if (playerCam != null)
            playerCam.fieldOfView = normalFOV;
    }

    public void UpdateZoom(Camera playerCam, float scroll, float deltaTime)
    {
        if (scroll != 0f)
        {
            TargetFOV = Mathf.Clamp(
                TargetFOV - scroll * zoomSpeed,
                minZoomFOV,
                maxZoomFOV
            );
        }

        if (playerCam != null)
        {
            playerCam.fieldOfView = Mathf.Lerp(
                playerCam.fieldOfView,
                TargetFOV,
                deltaTime * SmoothFactor
            );
        }
    }

    public float GetZoomAmount(Camera playerCam)
    {
        float currentFOV = playerCam != null
            ? playerCam.fieldOfView
            : normalFOV;

        return Mathf.Clamp01(
            Mathf.InverseLerp(normalFOV, minZoomFOV, currentFOV)
        );
    }
}