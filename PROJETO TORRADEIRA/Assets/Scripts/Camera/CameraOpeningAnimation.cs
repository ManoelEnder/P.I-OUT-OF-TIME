using UnityEngine;
using System.Collections;

public class CameraOpeningAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform top;
    [SerializeField] private RectTransform bottom;

    [SerializeField] private float duration = 0.45f;
    [SerializeField] private float closedHeight = 540f;

    private Coroutine animationCoroutine;
    private bool isOpen;

    public void Play()
    {
        if (isOpen)
            return;

        gameObject.SetActive(true);

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(Opening());
    }

    private IEnumerator Opening()
    {
        isOpen = false;
        SetHeight(closedHeight);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float height = Mathf.Lerp(closedHeight, 0f, t);

            SetHeight(height);

            yield return null;
        }

        SetHeight(0f);

        isOpen = true;
        animationCoroutine = null;
    }

    public void ResetAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        SetHeight(0f);
        isOpen = false;
        gameObject.SetActive(false);
    }

    private void SetHeight(float height)
    {
        if (top != null)
        {
            Vector2 size = top.sizeDelta;
            size.y = height;
            top.sizeDelta = size;
        }

        if (bottom != null)
        {
            Vector2 size = bottom.sizeDelta;
            size.y = height;
            bottom.sizeDelta = size;
        }
    }
}