using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraBlink : MonoBehaviour
{
    [SerializeField] private Image top;
    [SerializeField] private Image bottom;

    [SerializeField] private float duration = 0.45f;
    [SerializeField] private float closedSize = 600f;

    private Coroutine blinkCoroutine;

    private void Awake()
    {
        ForceClose();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        ForceClose();
    }

    public void Play()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(Blink());
    }

    private IEnumerator Blink()
    {
        if (top == null || bottom == null)
            yield break;

        top.gameObject.SetActive(true);
        bottom.gameObject.SetActive(true);

        SetSize(closedSize);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(time / duration);

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            float size =
                Mathf.Lerp(
                    closedSize,
                    0f,
                    smooth
                );

            SetSize(size);

            yield return null;
        }

        SetSize(0f);

        top.gameObject.SetActive(false);
        bottom.gameObject.SetActive(false);

        blinkCoroutine = null;
    }

    private void ForceClose()
    {
        if (top != null)
        {
            top.gameObject.SetActive(false);
            SetupTop();
        }

        if (bottom != null)
        {
            bottom.gameObject.SetActive(false);
            SetupBottom();
        }
    }

    private void SetupTop()
    {
        RectTransform rect = top.rectTransform;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        top.color = Color.black;
        top.raycastTarget = false;
    }

    private void SetupBottom()
    {
        RectTransform rect = bottom.rectTransform;

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);

        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        bottom.color = Color.black;
        bottom.raycastTarget = false;
    }

    private void SetSize(float size)
    {
        if (top != null)
        {
            RectTransform rect = top.rectTransform;

            rect.offsetMin =
                new Vector2(0f, -size);

            rect.offsetMax =
                Vector2.zero;
        }

        if (bottom != null)
        {
            RectTransform rect = bottom.rectTransform;

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                new Vector2(0f, size);
        }
    }
}