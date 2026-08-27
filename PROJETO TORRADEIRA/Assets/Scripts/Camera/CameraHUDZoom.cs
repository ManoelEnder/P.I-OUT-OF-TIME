using UnityEngine;

public class CameraHUDZoom : MonoBehaviour
{
    [SerializeField] private RectTransform topLeft;
    [SerializeField] private RectTransform topRight;
    [SerializeField] private RectTransform bottomLeft;
    [SerializeField] private RectTransform bottomRight;

    [SerializeField] private float normalInset = 70f;
    [SerializeField] private float zoomInset = 180f;

    private void Awake()
    {
        ApplyZoom(0f);
    }

    public void ApplyZoom(float zoom)
    {
        zoom = Mathf.Clamp01(zoom);

        float inset =
            Mathf.Lerp(
                normalInset,
                zoomInset,
                zoom
            );

        if (topLeft != null)
            topLeft.anchoredPosition =
                new Vector2(inset, -inset);

        if (topRight != null)
            topRight.anchoredPosition =
                new Vector2(-inset, -inset);

        if (bottomLeft != null)
            bottomLeft.anchoredPosition =
                new Vector2(inset, inset);

        if (bottomRight != null)
            bottomRight.anchoredPosition =
                new Vector2(-inset, inset);
    }

    public void ResetHUD()
    {
        ApplyZoom(0f);
    }
}