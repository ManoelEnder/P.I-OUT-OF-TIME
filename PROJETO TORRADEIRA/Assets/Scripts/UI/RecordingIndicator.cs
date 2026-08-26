using UnityEngine;
using UnityEngine.UI;

public class RecordingIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image recordingDot;

    [Header("Blink Settings")]
    [SerializeField] private float minimumInterval = 0.8f;
    [SerializeField] private float maximumInterval = 2.5f;
    [SerializeField] private float blinkDuration = 0.12f;

    private float nextBlinkTime;
    private float blinkTimer;
    private bool isBlinking;

    private void Awake()
    {
        if (recordingDot == null)
        {
            recordingDot = GetComponent<Image>();
        }

        SetVisible(true);
        ScheduleNextBlink();
    }

    private void Update()
    {
        if (!isBlinking)
        {
            if (Time.unscaledTime >= nextBlinkTime)
            {
                StartBlink();
            }

            return;
        }

        blinkTimer += Time.unscaledDeltaTime;

        if (blinkTimer >= blinkDuration)
        {
            SetVisible(true);
            isBlinking = false;

            ScheduleNextBlink();
        }
    }

    private void StartBlink()
    {
        isBlinking = true;
        blinkTimer = 0f;

        SetVisible(false);
    }

    private void ScheduleNextBlink()
    {
        nextBlinkTime = Time.unscaledTime +
                        Random.Range(
                            minimumInterval,
                            maximumInterval
                        );
    }

    private void SetVisible(bool visible)
    {
        recordingDot.enabled = visible;
    }

    public void SetRecording(bool recording)
    {
        recordingDot.gameObject.SetActive(recording);

        if (recording)
        {
            SetVisible(true);
            ScheduleNextBlink();
        }
    }
}