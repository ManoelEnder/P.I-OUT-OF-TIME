using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class CreditsController : MonoBehaviour
{
    [Header("Credits")]
    [SerializeField] private RectTransform creditsContent;
    [SerializeField] private float scrollSpeed = 20f;

    [Header("End Settings")]
    [SerializeField] private float endDelay = 2f;

    [Header("Events")]
    [SerializeField] private UnityEvent onCreditsFinished;
    [SerializeField] private UnityEvent onEscapePressed;

    private bool hasFinished;
    private bool isWaitingForEnd;

    private void Update()
    {
        HandleEscape();

        if (!hasFinished)
        {
            MoveCredits();
            CheckCreditsEnd();
        }
    }

    private void HandleEscape()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            onEscapePressed?.Invoke();
        }
    }

    private void MoveCredits()
    {
        if (creditsContent == null)
            return;

        creditsContent.anchoredPosition +=
            Vector2.up * scrollSpeed * Time.unscaledDeltaTime;
    }

    private void CheckCreditsEnd()
    {
        if (creditsContent == null || isWaitingForEnd)
            return;

        float contentHeight = creditsContent.rect.height;

        if (creditsContent.anchoredPosition.y >= contentHeight)
        {
            isWaitingForEnd = true;
            StartCoroutine(FinishCreditsAfterDelay());
        }
    }

    private IEnumerator FinishCreditsAfterDelay()
    {
        yield return new WaitForSecondsRealtime(endDelay);

        FinishCredits();
    }

    private void FinishCredits()
    {
        if (hasFinished)
            return;

        hasFinished = true;
        onCreditsFinished?.Invoke();
    }

    public void FinishCreditsManually()
    {
        FinishCredits();
    }
}