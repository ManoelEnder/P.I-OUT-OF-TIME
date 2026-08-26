using UnityEngine;

public class CameraFrameController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    [SerializeField] private RectTransform blackTop;
    [SerializeField] private RectTransform blackBottom;
    [SerializeField] private RectTransform blackLeft;
    [SerializeField] private RectTransform blackRight;

    [SerializeField] private RectTransform corners;

    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float maxZoomFOV = 25f;

    [SerializeField] private float maxVerticalBorder = 180f;
    [SerializeField] private float maxHorizontalBorder = 260f;

    [SerializeField] private float smoothness = 12f;

    private void Update()
    {
        if (targetCamera == null)
            return;

        float zoom =
            Mathf.InverseLerp(
                normalFOV,
                maxZoomFOV,
                targetCamera.fieldOfView
            );

        float vertical =
            Mathf.Lerp(
                0f,
                maxVerticalBorder,
                zoom
            );

        float horizontal =
            Mathf.Lerp(
                0f,
                maxHorizontalBorder,
                zoom
            );

        SetHeight(
            blackTop,
            vertical
        );

        SetHeight(
            blackBottom,
            vertical
        );

        SetWidth(
            blackLeft,
            horizontal
        );

        SetWidth(
            blackRight,
            horizontal
        );

        if (corners != null)
        {
            float scale =
                Mathf.Lerp(
                    1f,
                    0.65f,
                    zoom
                );

            corners.localScale =
                Vector3.Lerp(
                    corners.localScale,
                    Vector3.one * scale,
                    Time.deltaTime * smoothness
                );
        }
    }

    private void SetHeight(
        RectTransform target,
        float value
    )
    {
        if (target == null)
            return;

        Vector2 size =
            target.sizeDelta;

        size.y =
            Mathf.Lerp(
                size.y,
                value,
                Time.deltaTime * smoothness
            );

        target.sizeDelta = size;
    }

    private void SetWidth(
        RectTransform target,
        float value
    )
    {
        if (target == null)
            return;

        Vector2 size =
            target.sizeDelta;

        size.x =
            Mathf.Lerp(
                size.x,
                value,
                Time.deltaTime * smoothness
            );

        target.sizeDelta = size;
    }
}