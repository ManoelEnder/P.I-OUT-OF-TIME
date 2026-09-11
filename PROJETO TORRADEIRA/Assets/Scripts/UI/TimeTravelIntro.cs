using UnityEngine;
using TMPro;
using System.Collections;

public class TimeTravelIntro : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text yearText;

    [Header("Blur")]
    [SerializeField] private UnityEngine.Rendering.Volume blurVolume;

    [Header("Player Controls")]
    [SerializeField] private Behaviour[] playerControls;

    [Header("Timeline")]
    [SerializeField] private int startYear = 2026;
    [SerializeField] private int targetYear = 1975;

    [Header("Fast Countdown")]
    [SerializeField] private float fastDuration = 1.8f;

    [Header("Final Countdown")]
    [SerializeField] private float slowDuration = 1.5f;

    [Header("1975 Pause")]
    [SerializeField] private float finalPause = 0.6f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("Glitch")]
    [SerializeField] private float glitchChance = 0.35f;
    [SerializeField] private float glitchDuration = 0.04f;

    private float previousTimeScale;

    private void Start()
    {
        previousTimeScale = Time.timeScale;

        Time.timeScale = 0f;

        SetPlayerControls(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (blurVolume != null)
            blurVolume.weight = 1f;

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        yield return StartCoroutine(
            FastCountdown()
        );

        yield return StartCoroutine(
            SlowCountdown()
        );

        yield return new WaitForSecondsRealtime(
            finalPause
        );

        yield return StartCoroutine(
            FadeOut()
        );

        if (blurVolume != null)
            blurVolume.weight = 0f;

        SetPlayerControls(true);

        Time.timeScale = previousTimeScale;

        gameObject.SetActive(false);
    }

    private IEnumerator FastCountdown()
    {
        float elapsed = 0f;

        while (elapsed < fastDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / fastDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            int year =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        startYear,
                        1976,
                        smooth
                    )
                );

            SetYear(year);

            yield return null;
        }

        SetYear(1976);
    }

    private IEnumerator SlowCountdown()
    {
        float elapsed = 0f;

        SetYear(1974);

        while (elapsed < slowDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (Random.value < glitchChance)
            {
                yield return StartCoroutine(
                    PlayGlitch()
                );
            }

            float t =
                Mathf.Clamp01(
                    elapsed / slowDuration
                );

            if (t < 0.7f)
            {
                SetYear(1974);
            }
            else
            {
                SetYear(targetYear);
            }

            yield return null;
        }

        SetYear(targetYear);
    }

    private IEnumerator PlayGlitch()
    {
        if (yearText == null)
            yield break;

        string original =
            yearText.text;

        string[] glitchTexts =
        {
            "197",
            "19?5",
            "197_",
            "19/5",
            "1974"
        };

        yearText.text =
            glitchTexts[
                Random.Range(
                    0,
                    glitchTexts.Length
                )
            ];

        yield return new WaitForSecondsRealtime(
            glitchDuration
        );

        yearText.text = original;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );

            if (canvasGroup != null)
                canvasGroup.alpha =
                    1f - t;

            if (blurVolume != null)
                blurVolume.weight =
                    1f - t;

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (blurVolume != null)
            blurVolume.weight = 0f;
    }

    private void SetYear(int year)
    {
        if (yearText != null)
            yearText.text = year.ToString();
    }

    private void SetPlayerControls(
        bool enabledState
    )
    {
        if (playerControls == null)
            return;

        foreach (Behaviour control in playerControls)
        {
            if (control != null)
                control.enabled = enabledState;
        }
    }
}