using UnityEngine;
using UnityEngine.UI;

public class CameraBlackBorders : MonoBehaviour
{
    [SerializeField] private Image top;
    [SerializeField] private Image bottom;
    [SerializeField] private Image left;
    [SerializeField] private Image right;

    [SerializeField] private float maxBorder = 180f;

    private RectTransform canvasRect;

    private void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        Configure();
        ResetBorders();
        SetActive(false);
    }

    private void Configure()
    {
        ConfigureTop();
        ConfigureBottom();
        ConfigureLeft();
        ConfigureRight();
    }

    public void SetZoom(float zoom)
    {
        zoom = Mathf.Clamp01(zoom);

        float size =
            maxBorder * zoom;

        ApplyBorders(size);
    }

    public void ResetBorders()
    {
        ApplyBorders(0f);
    }

    public void SetActive(bool active)
    {
        if (top != null)
            top.gameObject.SetActive(active);

        if (bottom != null)
            bottom.gameObject.SetActive(active);

        if (left != null)
            left.gameObject.SetActive(active);

        if (right != null)
            right.gameObject.SetActive(active);
    }

    private void ApplyBorders(float size)
    {
        if (top != null)
            top.rectTransform.sizeDelta =
                new Vector2(
                    0f,
                    size
                );

        if (bottom != null)
            bottom.rectTransform.sizeDelta =
                new Vector2(
                    0f,
                    size
                );

        if (left != null)
            left.rectTransform.sizeDelta =
                new Vector2(
                    size,
                    0f
                );

        if (right != null)
            right.rectTransform.sizeDelta =
                new Vector2(
                    size,
                    0f
                );
    }

    private void ConfigureTop()
    {
        if (top == null)
            return;

        RectTransform r = top.rectTransform;

        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);

        r.anchoredPosition = Vector2.zero;
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;

        top.color = Color.black;
        top.raycastTarget = false;
    }

    private void ConfigureBottom()
    {
        if (bottom == null)
            return;

        RectTransform r = bottom.rectTransform;

        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(1f, 0f);
        r.pivot = new Vector2(0.5f, 0f);

        r.anchoredPosition = Vector2.zero;
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;

        bottom.color = Color.black;
        bottom.raycastTarget = false;
    }

    private void ConfigureLeft()
    {
        if (left == null)
            return;

        RectTransform r = left.rectTransform;

        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 0.5f);

        r.anchoredPosition = Vector2.zero;
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;

        left.color = Color.black;
        left.raycastTarget = false;
    }

    private void ConfigureRight()
    {
        if (right == null)
            return;

        RectTransform r = right.rectTransform;

        r.anchorMin = new Vector2(1f, 0f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(1f, 0.5f);

        r.anchoredPosition = Vector2.zero;
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;

        right.color = Color.black;
        right.raycastTarget = false;
    }
}