using UnityEngine;

public class CameraHUDZoom : MonoBehaviour
{
    [SerializeField] private RectTransform topLeft;
    [SerializeField] private RectTransform topRight;
    [SerializeField] private RectTransform bottomLeft;
    [SerializeField] private RectTransform bottomRight;

    [SerializeField] private float maxZoomPositionMultiplier = 0.55f;

    private Vector2 topLeftOriginalPosition;
    private Vector2 topRightOriginalPosition;
    private Vector2 bottomLeftOriginalPosition;
    private Vector2 bottomRightOriginalPosition;

    private bool positionsSaved;

    private void Awake()
    {
        SaveOriginalPositions();
    }

    private void OnEnable()
    {
        if (!positionsSaved)
            SaveOriginalPositions();

        ResetHUD();
    }

    private void SaveOriginalPositions()
    {
        if (topLeft != null)
            topLeftOriginalPosition = topLeft.anchoredPosition;

        if (topRight != null)
            topRightOriginalPosition = topRight.anchoredPosition;

        if (bottomLeft != null)
            bottomLeftOriginalPosition = bottomLeft.anchoredPosition;

        if (bottomRight != null)
            bottomRightOriginalPosition = bottomRight.anchoredPosition;

        positionsSaved = true;
    }

    public void ApplyZoom(float zoom)
    {
        if (!positionsSaved)
            SaveOriginalPositions();

        zoom = Mathf.Clamp01(zoom);

        float multiplier = Mathf.Lerp(
            1f,
            maxZoomPositionMultiplier,
            zoom
        );

        if (topLeft != null)
        {
            topLeft.anchoredPosition =
                topLeftOriginalPosition * multiplier;
        }

        if (topRight != null)
        {
            topRight.anchoredPosition =
                topRightOriginalPosition * multiplier;
        }

        if (bottomLeft != null)
        {
            bottomLeft.anchoredPosition =
                bottomLeftOriginalPosition * multiplier;
        }

        if (bottomRight != null)
        {
            bottomRight.anchoredPosition =
                bottomRightOriginalPosition * multiplier;
        }
    }

    public void ResetHUD()
    {
        if (!positionsSaved)
            SaveOriginalPositions();

        if (topLeft != null)
            topLeft.anchoredPosition =
                topLeftOriginalPosition;

        if (topRight != null)
            topRight.anchoredPosition =
                topRightOriginalPosition;

        if (bottomLeft != null)
            bottomLeft.anchoredPosition =
                bottomLeftOriginalPosition;

        if (bottomRight != null)
            bottomRight.anchoredPosition =
                bottomRightOriginalPosition;
    }
}