using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using DG.Tweening;

public class StartScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup pressAnyButtonText;
    [SerializeField] private RectTransform pressAnyButtonRect;
    [SerializeField] private CanvasGroup startPanel;
    [SerializeField] private CanvasGroup nextPanel;
    [SerializeField] private CanvasGroup fadeScreen;
    [SerializeField] private UnityEvent nextPanelEvent;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float blinkSpeed = 1.5f;
    [SerializeField] private float growScale = 1.3f;
    [SerializeField] private float growDuration = 0.4f;

    private bool hasStarted = false;
    private Tween blinkTween;

    private void Awake()
    {
        pressAnyButtonText.alpha = 0;
        pressAnyButtonRect.localScale = Vector3.one;
        startPanel.alpha = 0;
        nextPanel.alpha = 0;
        fadeScreen.alpha = 1;
    }

    private void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;
    }

    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
    }

    private void Start()
    {
        startPanel.DOFade(1, fadeDuration);

        pressAnyButtonText.DOFade(1, fadeDuration).OnComplete(() =>
        {
            blinkTween = pressAnyButtonText
                .DOFade(0.3f, blinkSpeed)
                .SetLoops(-1, LoopType.Yoyo);
        });

        fadeScreen.DOFade(0, fadeDuration);
    }

    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (hasStarted)
            return;

        foreach (var control in device.allControls)
        {
            if (control is ButtonControl button && button.IsPressed())
            {
                TriggerStart();
                break;
            }
        }
    }

    private void TriggerStart()
    {
        hasStarted = true;

        if (blinkTween != null)
            blinkTween.Kill();

        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(pressAnyButtonRect
            .DOScale(growScale, growDuration)
            .SetEase(Ease.OutBack));
        seq.Join(pressAnyButtonText.DOFade(0, growDuration));
        seq.OnComplete(GoToNextPanel);
    }

    private void GoToNextPanel()
    {
        fadeScreen.DOFade(1, fadeDuration).OnComplete(() =>
        {
            startPanel.gameObject.SetActive(false);
            nextPanel.gameObject.SetActive(true);
            nextPanel.alpha = 0;

            nextPanelEvent.Invoke();

            nextPanel.DOFade(1, fadeDuration);
            fadeScreen.DOFade(0, fadeDuration);
        });
    }
}
