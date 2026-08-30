using UnityEngine;

public class CameraHUDZoom : MonoBehaviour
{
    [Header("Camera Frame")]
    [SerializeField] private RectTransform cameraFrame;

    [Header("Corners")]
    [SerializeField] private RectTransform topLeft;
    [SerializeField] private RectTransform topRight;
    [SerializeField] private RectTransform bottomLeft;
    [SerializeField] private RectTransform bottomRight;

    [Header("Zoom")]
    [SerializeField] private float normalInset = 0f;
    [SerializeField] private float zoomInset = 120f;

    private readonly Vector3[] frameCorners = new Vector3[4];

    private void Start()
    {
        ApplyZoom(0f);
    }

    public void ApplyZoom(float zoom)
    {
        if (cameraFrame == null)
            return;

        zoom = Mathf.Clamp01(zoom);

        cameraFrame.GetWorldCorners(frameCorners);

        Vector3 bottomLeftPosition = frameCorners[0];
        Vector3 topLeftPosition = frameCorners[1];
        Vector3 topRightPosition = frameCorners[2];
        Vector3 bottomRightPosition = frameCorners[3];

        float inset = Mathf.Lerp(
            normalInset,
            zoomInset,
            zoom
        );

        Vector3 leftDirection =
            (topRightPosition - topLeftPosition).normalized;

        Vector3 rightDirection =
            (topRightPosition - topLeftPosition).normalized;

        Vector3 verticalDirection =
            (topLeftPosition - bottomLeftPosition).normalized;

        topLeftPosition +=
            leftDirection * inset -
            verticalDirection * inset;

        topRightPosition -=
            rightDirection * inset +
            verticalDirection * inset;

        bottomLeftPosition +=
            leftDirection * inset +
            verticalDirection * inset;

        bottomRightPosition -=
            rightDirection * inset -
            verticalDirection * inset;

        SetCornerPosition(
            topLeft,
            topLeftPosition
        );

        SetCornerPosition(
            topRight,
            topRightPosition
        );

        SetCornerPosition(
            bottomLeft,
            bottomLeftPosition
        );

        SetCornerPosition(
            bottomRight,
            bottomRightPosition
        );
    }

    private void SetCornerPosition(
        RectTransform corner,
        Vector3 worldPosition
    )
    {
        if (corner == null)
            return;

        corner.position = worldPosition;
    }

    public void ResetHUD()
    {
        ApplyZoom(0f);
    }
}