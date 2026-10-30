
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
        SetScreenFade(0f);
        SetBordersActive(true);

        float fullSize = GetClosedSize();

        SetBorderSize(fullSize);
        SetHUDAlpha(0f);

        float duration = Mathf.Max(
            0.001f,
            openDuration
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

            SetBorderSize(
                Mathf.Lerp(
                    fullSize,
                    0f,
                    smooth
                )
            );

            SetHUDAlpha(smooth);

            yield return null;
        }

        SetBorderSize(0f);
        SetBordersActive(false);
        SetHUDAlpha(1f);

        animationCoroutine = null;
    }

    private IEnumerator AnimateClose()
    {
        SetScreenFade(0f);
        SetBordersActive(true);

        float fullSize = GetClosedSize();

        SetBorderSize(0f);
        SetHUDAlpha(1f);

        float duration = Mathf.Max(
            0.001f,
            closeDuration
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

            SetBorderSize(
                Mathf.Lerp(
                    0f,
                    fullSize,
                    smooth
                )
            );

            float hudProgress = Mathf.Clamp01(
                progress / Mathf.Max(
                    0.01f,
                    hudFadeOutPortion
                )
            );

            float hudSmooth = Mathf.SmoothStep(
                0f,
                1f,
                hudProgress
            );

            SetHUDAlpha(1f - hudSmooth);

            yield return null;
        }

        SetBorderSize(fullSize);
        SetHUDAlpha(0f);

        SetScreenFade(1f);
        SetBordersActive(false);

        animationCoroutine = null;
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
        SetScreenFade(0f);
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
