using UnityEngine;
using TMPro;
using System.Collections;

public class TimeTravelIntro : MonoBehaviour
{
    [Header("UI da Intro")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text yearText;
    [SerializeField] private TMP_Text glitchText;
    [SerializeField] private GameObject crosshair;

    [Header("Canvas Principal")]
    [SerializeField] private GameObject mainCanvas;
    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private float mainCanvasFadeDuration = 0.8f;

    [Header("Intro Volume")]
    [SerializeField] private UnityEngine.Rendering.Volume introVolume;

    [Header("Controles Bloqueados")]
    [SerializeField] private Behaviour[] playerControls;

    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip countdownSound;
    [SerializeField] private AudioClip glitchSound;
    [SerializeField] private AudioClip finalSound;

    [Header("Volume")]
    [SerializeField][Range(0f, 1f)] private float countdownVolume = 0.35f;
    [SerializeField][Range(0f, 1f)] private float glitchVolume = 0.7f;
    [SerializeField][Range(0f, 1f)] private float finalVolume = 0.8f;

    [Header("Anos")]
    [SerializeField] private int startYear = 2026;
    [SerializeField] private int targetYear = 1975;

    [Header("Contagem")]
    [SerializeField] private float countdownDuration = 2.2f;
    [SerializeField] private float countdownSoundInterval = 0.035f;

    [Header("Final")]
    [SerializeField] private float glitchDuration = 1.4f;
    [SerializeField] private float glitchInterval = 0.06f;
    [SerializeField] private float finalPause = 0.7f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Glitch Visual")]
    [SerializeField] private float glitchPosition = 4f;
    [SerializeField][Range(0f, 1f)] private float flickerAmount = 0.65f;
    [SerializeField][Range(0f, 1f)] private float duplicateChance = 0.45f;

    private float previousTimeScale;

    private Vector3 yearOriginalPosition;
    private Vector3 glitchOriginalPosition;

    private Color yearOriginalColor;
    private Color glitchOriginalColor;

    private bool running;
    private int lastPlayedYear = -1;

    private readonly string[] glitchValues =
    {
        "1974",
        "1975",
        "197_",
        "19?5",
        "19/5",
        "1_75",
        "197",
        "197?",
        "19 75"
    };

    private void Awake()
    {
        if (yearText != null)
        {
            yearOriginalPosition =
                yearText.rectTransform.localPosition;

            yearOriginalColor =
                yearText.color;
        }

        if (glitchText != null)
        {
            glitchOriginalPosition =
                glitchText.rectTransform.localPosition;

            glitchOriginalColor =
                glitchText.color;
        }

        if (mainCanvasGroup != null)
            mainCanvasGroup.alpha = 0f;
    }

    private void Start()
    {
        previousTimeScale =
            Time.timeScale;

        Time.timeScale = 0f;

        DisableGameplay();

        if (mainCanvas != null)
            mainCanvas.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (introVolume != null)
            introVolume.weight = 1f;

        ResetVisuals();

        SetYear(startYear);

        running = true;

        StartCoroutine(
            PlayIntro()
        );
    }

    private void Update()
    {
        if (!running)
            return;

        if (crosshair != null &&
            crosshair.activeSelf)
        {
            crosshair.SetActive(false);
        }
    }

    private IEnumerator PlayIntro()
    {
        yield return StartCoroutine(
            FastCountdown()
        );

        yield return StartCoroutine(
            TemporalGlitch()
        );

        ResetVisuals();

        SetYear(targetYear);

        PlaySound(
            finalSound,
            finalVolume
        );

        yield return new WaitForSecondsRealtime(
            finalPause
        );

        yield return StartCoroutine(
            FadeIntro()
        );

        yield return StartCoroutine(
            FadeInMainCanvas()
        );

        FinishIntro();
    }

    private IEnumerator FastCountdown()
    {
        float elapsed = 0f;
        float soundTimer = 0f;

        int lastYear = startYear;

        SetYear(startYear);

        while (elapsed < countdownDuration)
        {
            float delta =
                Time.unscaledDeltaTime;

            elapsed += delta;
            soundTimer += delta;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    countdownDuration
                );

            float eased =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            int year =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        startYear,
                        1976f,
                        eased
                    )
                );

            if (year != lastYear)
            {
                SetYear(year);
                lastYear = year;
            }

            if (
                countdownSound != null &&
                soundTimer >= countdownSoundInterval
            )
            {
                soundTimer = 0f;

                PlaySound(
                    countdownSound,
                    countdownVolume
                );
            }

            yield return null;
        }

        SetYear(1976);
    }

    private IEnumerator TemporalGlitch()
    {
        float elapsed = 0f;
        float timer = 0f;
        float soundTimer = 0f;

        bool playedGlitchSound = false;

        SetYear(1974);

        while (elapsed < glitchDuration)
        {
            float delta =
                Time.unscaledDeltaTime;

            elapsed += delta;
            timer += delta;
            soundTimer += delta;

            if (timer >= glitchInterval)
            {
                timer = 0f;

                ApplyGlitch();
            }

            if (
                !playedGlitchSound &&
                glitchSound != null
            )
            {
                PlaySound(
                    glitchSound,
                    glitchVolume
                );

                playedGlitchSound = true;
            }

            if (soundTimer >= 0.2f)
            {
                soundTimer = 0f;

                if (glitchSound != null)
                {
                    PlaySound(
                        glitchSound,
                        glitchVolume * 0.35f
                    );
                }
            }

            yield return null;
        }

        ResetVisuals();

        SetYear(1975);
    }

    private void ApplyGlitch()
    {
        if (yearText == null)
            return;

        yearText.text =
            glitchValues[
                Random.Range(
                    0,
                    glitchValues.Length
                )
            ];

        Color mainColor =
            yearOriginalColor;

        mainColor.a =
            Random.Range(
                flickerAmount,
                1f
            );

        yearText.color =
            mainColor;

        Vector2 offset =
            Random.insideUnitCircle *
            glitchPosition;

        yearText.rectTransform.localPosition =
            yearOriginalPosition +
            new Vector3(
                offset.x,
                offset.y,
                0f
            );

        if (
            glitchText != null &&
            Random.value < duplicateChance
        )
        {
            glitchText.text =
                glitchValues[
                    Random.Range(
                        0,
                        glitchValues.Length
                    )
                ];

            Color duplicateColor =
                glitchOriginalColor;

            duplicateColor.a =
                Random.Range(
                    0.15f,
                    0.5f
                );

            glitchText.color =
                duplicateColor;

            glitchText.alpha =
                duplicateColor.a;

            Vector2 duplicateOffset =
                Random.insideUnitCircle *
                (glitchPosition * 1.5f);

            glitchText.rectTransform.localPosition =
                glitchOriginalPosition +
                new Vector3(
                    duplicateOffset.x,
                    duplicateOffset.y,
                    0f
                );
        }
        else if (glitchText != null)
        {
            glitchText.alpha = 0f;
        }
    }

    private void ResetVisuals()
    {
        if (yearText != null)
        {
            yearText.text =
                targetYear.ToString();

            yearText.color =
                yearOriginalColor;

            yearText.rectTransform.localPosition =
                yearOriginalPosition;
        }

        if (glitchText != null)
        {
            glitchText.alpha = 0f;

            glitchText.rectTransform.localPosition =
                glitchOriginalPosition;

            glitchText.color =
                glitchOriginalColor;
        }
    }

    private void SetYear(int year)
    {
        if (yearText != null)
            yearText.text =
                year.ToString();
    }

    private IEnumerator FadeIntro()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    1f - smooth;
            }

            if (introVolume != null)
            {
                introVolume.weight =
                    1f - smooth;
            }

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (introVolume != null)
            introVolume.weight = 0f;
    }

    private IEnumerator FadeInMainCanvas()
    {
        if (mainCanvas == null)
            yield break;

        mainCanvas.SetActive(true);

        if (mainCanvasGroup == null)
            yield break;

        mainCanvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (
            elapsed <
            mainCanvasFadeDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    mainCanvasFadeDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            mainCanvasGroup.alpha =
                smooth;

            yield return null;
        }

        mainCanvasGroup.alpha = 1f;
    }

    private void PlaySound(
        AudioClip clip,
        float volume
    )
    {
        if (
            audioSource == null ||
            clip == null
        )
        {
            return;
        }

        audioSource.PlayOneShot(
            clip,
            volume
        );
    }

    private void FinishIntro()
    {
        if (introVolume != null)
            introVolume.weight = 0f;

        if (crosshair != null)
            crosshair.SetActive(true);

        SetPlayerControls(true);

        Time.timeScale =
            previousTimeScale;

        running = false;

        enabled = false;
    }

    private void DisableGameplay()
    {
        if (crosshair != null)
            crosshair.SetActive(false);

        SetPlayerControls(false);
    }

    private void SetPlayerControls(
        bool enabledState
    )
    {
        if (playerControls == null)
            return;

        foreach (
            Behaviour control
            in playerControls
        )
        {
            if (control != null)
                control.enabled =
                    enabledState;
        }
    }

    private void OnDisable()
    {
        if (!running)
            return;

        Time.timeScale =
            previousTimeScale;

        SetPlayerControls(true);

        if (crosshair != null)
            crosshair.SetActive(true);
    }
}