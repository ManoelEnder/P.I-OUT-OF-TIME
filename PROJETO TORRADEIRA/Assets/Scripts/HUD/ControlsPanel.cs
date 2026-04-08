using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ControlsPanel : MonoBehaviour
{
    public RectTransform panel;

    public float animationTime = 0.25f;
    public float hiddenY = -800f;
    public float visibleY = 0f;

    bool isVisible = false;
    Coroutine currentAnimation;

    void Start()
    {
        SetPosition(hiddenY);
    }

    void Update()
    {
        if (Keyboard.current.tabKey.isPressed && !isVisible)
        {
            Show();
        }
        else if (!Keyboard.current.tabKey.isPressed && isVisible)
        {
            Hide();
        }
    }

    void Show()
    {
        isVisible = true;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(Animate(hiddenY, visibleY));
    }

    void Hide()
    {
        isVisible = false;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(Animate(visibleY, hiddenY));
    }

    IEnumerator Animate(float from, float to)
    {
        float t = 0f;

        while (t < animationTime)
        {
            t += Time.deltaTime;

            float y = Mathf.Lerp(from, to, t / animationTime);
            SetPosition(y);

            yield return null;
        }

        SetPosition(to);
    }

    void SetPosition(float y)
    {
        Vector2 pos = panel.anchoredPosition;
        pos.y = y;
        panel.anchoredPosition = pos;
    }
}