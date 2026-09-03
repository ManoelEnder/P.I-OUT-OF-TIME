using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class MissionUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform missionPanel;
    [SerializeField] private TextMeshProUGUI tabHint;

    [Header("Animation")]
    [SerializeField] private float hideDistance = 800f;
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Timing")]
    [SerializeField] private float startVisibleTime = 5f;
    [SerializeField] private float autoHideTime = 30f;

    private Vector3 visiblePosition;
    private Vector3 hiddenPosition;

    private Coroutine autoHideCoroutine;
    private Coroutine animationCoroutine;

    private bool isVisible;

    private void Awake()
    {
        if (missionPanel == null)
            return;

        visiblePosition = missionPanel.localPosition;

        hiddenPosition =
            visiblePosition +
            Vector3.left * hideDistance;

        isVisible = true;
    }

    private void Start()
    {
        if (missionPanel == null)
            return;

        missionPanel.localPosition = visiblePosition;

        SetHintVisible(false);

        StartAutoHide(startVisibleTime);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (isVisible)
                HideMissions();
            else
                ShowMissions();
        }
    }

    public void ShowMissions()
    {
        if (isVisible)
            return;

        isVisible = true;

        SetHintVisible(false);

        AnimateTo(visiblePosition);

        StartAutoHide(autoHideTime);
    }

    public void HideMissions()
    {
        if (!isVisible)
            return;

        isVisible = false;

        StopAutoHide();

        AnimateTo(hiddenPosition);

        SetHintVisible(true);
    }

    private void AnimateTo(Vector3 targetPosition)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine =
            StartCoroutine(
                AnimatePanel(targetPosition)
            );
    }

    private IEnumerator AnimatePanel(Vector3 targetPosition)
    {
        Vector3 startPosition =
            missionPanel.localPosition;

        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time / animationDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            missionPanel.localPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smoothProgress
                );

            yield return null;
        }

        missionPanel.localPosition =
            targetPosition;

        animationCoroutine = null;
    }

    private void StartAutoHide(float delay)
    {
        StopAutoHide();

        autoHideCoroutine =
            StartCoroutine(
                AutoHide(delay)
            );
    }

    private void StopAutoHide()
    {
        if (autoHideCoroutine == null)
            return;

        StopCoroutine(autoHideCoroutine);

        autoHideCoroutine = null;
    }

    private IEnumerator AutoHide(float delay)
    {
        yield return new WaitForSeconds(delay);

        HideMissions();
    }

    private void SetHintVisible(bool visible)
    {
        if (tabHint == null)
            return;

        tabHint.gameObject.SetActive(visible);
    }
}