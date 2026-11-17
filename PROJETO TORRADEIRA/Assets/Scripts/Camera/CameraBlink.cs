
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraBlink : MonoBehaviour
{
    [Header("Bordas")]
    [SerializeField] private Image top;
    [SerializeField] private Image bottom;

    [Header("HUD")]
    [SerializeField] private CanvasGroup hudGroup;

    [Header("Fade da Tela")]
    [SerializeField] private Image screenFade;

    [Header("Duracao")]
    [SerializeField] private float openDuration = 0.45f;
    [SerializeField] private float closeDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.45f;

    [Header("Bordas")]
    [SerializeField] private float closedSize = 600f;

    [Header("Fade do HUD")]
    [SerializeField, Range(0.1f, 1f)]
    private float hudFadeOutPortion = 0.4f;

    [Header("Efeito CRT")]
    [SerializeField] private float slitHalfHeight = 3f;
    [SerializeField] private float pixelStep = 4f;
    [SerializeField] private Color scanLineColor = new Color(0.95f, 0.95f, 0.95f, 1f);

    private Image scanLine;

    private Coroutine animationCoroutine;

    private void Awake()
    {
        Setup();
        ResetBlink();
    }

    public Coroutine Play()
    {
        StopAnimation();

        animationCoroutine = StartCoroutine(
            AnimateOpen()
        );

        return animationCoroutine;
    }

    public Coroutine PlayClose()
    {
        StopAnimation();

        animationCoroutine = StartCoroutine(
            AnimateClose()
        );

        return animationCoroutine;
    }

    public Coroutine PlayReveal()
    {
        StopAnimation();

        animationCoroutine = StartCoroutine(
            AnimateReveal()
        );

        return animationCoroutine;
    }

    private IEnumerator AnimateOpen()
    {
        // Liga como um CRT: a linha de fosforo acende no centro e a imagem se abre a partir dela.
        SetScreenFade(0f);
        SetBordersActive(true);

        float fullSize = GetClosedSize();
        float slitSize = Mathf.Max(0f, fullSize - slitHalfHeight);
        float duration = Mathf.Max(0.001f, openDuration);
        float lineEnd = duration * 0.28f;
        float width = GetScreenWidth();

        SetBorderSize(fullSize);
        SetHUDAlpha(0f);
        SetHUDScale(1f);
        SetScanLine(0f, 1f, true);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            if (time < lineEnd)
            {
                float k = EaseOutCubic(time / lineEnd);
                SetBorderSize(Snap(Mathf.Lerp(fullSize, slitSize, k)));
                SetScanLine(width * k, 1f, true);
            }
            else
            {
                float k = EaseOutCubic(Mathf.Clamp01((time - lineEnd) / (duration - lineEnd)));
                SetBorderSize(Snap(Mathf.Lerp(slitSize, 0f, k)));
                SetScanLine(width, 1f - k, true);

                // HUD entra piscando em degraus e com um leve "pop".
                SetHUDAlpha(Stepped(k, 4) * (Flicker(time) ? 1f : 0.55f));
                SetHUDScale(Mathf.Lerp(1.05f, 1f, k));
            }

            yield return null;
        }

        SetBorderSize(0f);
        SetBordersActive(false);
        SetScanLine(0f, 0f, false);
        SetHUDAlpha(1f);
        SetHUDScale(1f);

        animationCoroutine = null;
    }

    private IEnumerator AnimateClose()
    {
        // Desliga como um CRT: a imagem se espreme numa linha, que encolhe ate um ponto.
        SetScreenFade(0f);
        SetBordersActive(true);

        float fullSize = GetClosedSize();
        float slitSize = Mathf.Max(0f, fullSize - slitHalfHeight);
        float duration = Mathf.Max(0.001f, closeDuration);
        float squeezeEnd = duration * 0.6f;
        float width = GetScreenWidth();

        SetBorderSize(0f);
        SetHUDAlpha(1f);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float hudK = Mathf.Clamp01(time / (duration * Mathf.Max(0.05f, hudFadeOutPortion)));
            SetHUDAlpha(1f - Stepped(hudK, 3));
            SetHUDScale(Mathf.Lerp(1f, 0.97f, hudK));

            if (time < squeezeEnd)
            {
                float k = EaseInCubic(time / squeezeEnd);
                SetBorderSize(Snap(Mathf.Lerp(0f, slitSize, k)));
                SetScanLine(width, k, true);
            }
            else
            {
                float k = EaseInCubic(Mathf.Clamp01((time - squeezeEnd) / (duration - squeezeEnd)));
                SetBorderSize(Snap(Mathf.Lerp(slitSize, fullSize, k)));
                SetScanLine(Mathf.Lerp(width, slitHalfHeight * 2f, k), 1f, true);
            }

            yield return null;
        }

        SetBorderSize(fullSize);
        SetHUDAlpha(0f);
        SetHUDScale(1f);
        SetScanLine(0f, 0f, false);

        SetScreenFade(1f);
        SetBordersActive(false);

        animationCoroutine = null;
    }

    private float Snap(float size)
    {
        return pixelStep > 0f ? Mathf.Round(size / pixelStep) * pixelStep : size;
    }

    private static float Stepped(float value, int steps)
    {
        return Mathf.Floor(Mathf.Clamp01(value) * steps + 0.0001f) / steps;
    }

    private static bool Flicker(float time)
    {
        return Mathf.Repeat(time * 30f, 1f) > 0.25f;
    }

    private static float EaseOutCubic(float t)
    {
        t = 1f - Mathf.Clamp01(t);
        return 1f - t * t * t;
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private float GetScreenWidth()
    {
        RectTransform parent = top != null ? top.rectTransform.parent as RectTransform : null;
        return parent != null ? parent.rect.width : 1920f;
    }

    private void SetHUDScale(float scale)
    {
        if (hudGroup != null)
            hudGroup.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void SetScanLine(float width, float alpha, bool active)
    {
        if (scanLine == null)
            CreateScanLine();

        if (scanLine == null)
            return;

        if (scanLine.gameObject.activeSelf != active)
            scanLine.gameObject.SetActive(active);

        if (!active)
            return;

        scanLine.rectTransform.sizeDelta = new Vector2(Snap(width), slitHalfHeight * 2f);
        Color color = scanLineColor;
        color.a *= Mathf.Clamp01(alpha);
        scanLine.color = color;
        scanLine.rectTransform.SetAsLastSibling();
    }

    private void CreateScanLine()
    {
        RectTransform parent = top != null ? top.rectTransform.parent as RectTransform : null;

        if (parent == null)
            return;

        GameObject line = new GameObject("ScanLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        line.layer = parent.gameObject.layer;
        line.transform.SetParent(parent, false);

        scanLine = line.GetComponent<Image>();
        scanLine.raycastTarget = false;

        RectTransform rect = scanLine.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        line.SetActive(false);
    }

    private IEnumerator AnimateReveal()
    {
        SetBordersActive(false);
        SetHUDAlpha(0f);
        SetScreenFade(1f);

        float duration = Mathf.Max(
            0.001f,
            fadeInDuration
        );

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress = Mathf.Clamp01(
                time / duration
            );

            float smooth = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            SetScreenFade(1f - smooth);

            yield return null;
        }

        SetScreenFade(0f);

        animationCoroutine = null;
    }

    public void ResetBlink()
    {
        StopAnimation();

        SetBorderSize(0f);
        SetBordersActive(false);
        SetHUDAlpha(0f);
        SetHUDScale(1f);
        SetScreenFade(0f);

        if (scanLine != null)
            scanLine.gameObject.SetActive(false);
    }

    private void Setup()
    {
        if (top != null)
        {
            RectTransform rect = top.rectTransform;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            top.color = Color.black;
            top.raycastTarget = false;
        }

        if (bottom != null)
        {
            RectTransform rect = bottom.rectTransform;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            bottom.color = Color.black;
            bottom.raycastTarget = false;
        }

        if (screenFade != null)
        {
            RectTransform rect =
                screenFade.rectTransform;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            screenFade.raycastTarget = false;
        }
    }

    private float GetClosedSize()
    {
        float size = closedSize;

        if (top != null)
        {
            RectTransform parent =
                top.rectTransform.parent as RectTransform;

            if (parent != null)
            {
                size = Mathf.Max(
                    size,
                    parent.rect.height * 0.5f + 5f
                );
            }
        }

        return size;
    }

    private void SetBorderSize(float size)
    {
        if (top != null)
        {
            top.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                size
            );
        }

        if (bottom != null)
        {
            bottom.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                size
            );
        }
    }

    private void SetHUDAlpha(float alpha)
    {
        if (hudGroup == null)
            return;

        hudGroup.alpha = Mathf.Clamp01(alpha);
    }

    private void SetScreenFade(float alpha)
    {
        if (screenFade == null)
            return;

        alpha = Mathf.Clamp01(alpha);

        if (alpha > 0f)
            screenFade.gameObject.SetActive(true);

        screenFade.color = new Color(
            0f,
            0f,
            0f,
            alpha
        );

        if (alpha <= 0f)
            screenFade.gameObject.SetActive(false);
    }

    private void SetBordersActive(bool active)
    {
        if (top != null)
            top.gameObject.SetActive(active);

        if (bottom != null)
            bottom.gameObject.SetActive(active);
    }

    private void StopAnimation()
    {
        if (animationCoroutine == null)
            return;

        StopCoroutine(animationCoroutine);
        animationCoroutine = null;
    }

    private void OnDisable()
    {
        ResetBlink();
    }
}
