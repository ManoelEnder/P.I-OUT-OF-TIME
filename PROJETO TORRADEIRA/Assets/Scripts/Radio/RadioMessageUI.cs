using UnityEngine;
using TMPro;
using System.Collections;

public class RadioMessageUI : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public AudioSource audioSource;

    public float enterDuration = 0.5f;
    public float visibleDuration = 5f;
    public float exitDuration = 0.4f;

    public float startOffset = 70f;

    public float movementAmount = 2f;
    public float movementSpeed = 2f;

    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private Coroutine animationCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        targetPosition =
            rectTransform.anchoredPosition;

        HideImmediate();
    }

    public void ShowMessage(
        string message,
        AudioClip audioClip
    )
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine =
            StartCoroutine(
                ShowRoutine(
                    message,
                    audioClip
                )
            );
    }

    IEnumerator ShowRoutine(
        string message,
        AudioClip audioClip
    )
    {
        messageText.text = message;

        if (audioSource != null &&
            audioClip != null)
        {
            audioSource.PlayOneShot(audioClip);
        }

        Vector2 startPosition =
            targetPosition -
            new Vector2(
                0f,
                startOffset
            );

        rectTransform.anchoredPosition =
            startPosition;

        SetAlpha(0f);

        float time = 0f;

        while (time < enterDuration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time / enterDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    smooth
                );

            SetAlpha(smooth);

            yield return null;
        }

        time = 0f;

        while (time < visibleDuration)
        {
            time += Time.deltaTime;

            float movement =
                Mathf.Sin(
                    Time.time *
                    movementSpeed
                ) *
                movementAmount;

            rectTransform.anchoredPosition =
                targetPosition +
                new Vector2(
                    0f,
                    movement
                );

            yield return null;
        }

        Vector2 exitPosition =
            targetPosition +
            new Vector2(
                0f,
                startOffset
            );

        time = 0f;

        while (time < exitDuration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time / exitDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    targetPosition,
                    exitPosition,
                    smooth
                );

            SetAlpha(
                1f - smooth
            );

            yield return null;
        }

        HideImmediate();
    }

    void SetAlpha(float alpha)
    {
        Color color =
            messageText.color;

        color.a = alpha;

        messageText.color = color;
    }

    void HideImmediate()
    {
        SetAlpha(0f);

        rectTransform.anchoredPosition =
            targetPosition -
            new Vector2(
                0f,
                startOffset
            );
    }
}