using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CameraPhotoData : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private RawImage photoPreview;

    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shutterSound;

    [Header("Contador")]
    [SerializeField] private TextMeshProUGUI photoCounter;

    [Header("Sistemas")]
    [SerializeField] private MissionSystem missionSystem;
    [SerializeField] private AlbumController albumController;

    [Header("Tempos")]
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private float previewDuration = 1.3f;

    [Header("Animação: entrada")]
    [SerializeField] private float popDuration = 0.18f;
    [SerializeField, Range(0.5f, 1f)] private float popStartScale = 0.85f;

    [Header("Animação: movimento para o canto")]
    [SerializeField] private float moveDuration = 0.75f;
    [SerializeField] private float endScale = 0.3f;
    [SerializeField] private float endRotationZ = -12f;
    [SerializeField] private Vector2 cornerMargin = new Vector2(40f, 40f);

    [Header("Animação: saída")]
    [SerializeField] private float cornerHoldDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private int photoCount;
    private Coroutine previewCoroutine;

    private Vector2 restPosition;
    private Vector3 restScale;
    private float restRotationZ;
    private bool restCaptured;

    public float Cooldown => cooldown;
    public float PreviewDuration => previewDuration;
    public int PhotoCount => photoCount;

    public void Initialize()
    {
        photoCount = 0;
        UpdatePhotoCounter();

        StopPreview();

        if (photoPreview == null)
            return;

        CaptureRestPose();
        ResetPreviewPose();
        photoPreview.gameObject.SetActive(false);
    }

    public void ProcessPhoto(Texture2D photo)
    {
        if (photo == null)
            return;

        photoCount++;
        UpdatePhotoCounter();

        PlayShutterSound();
        RegisterPhoto(photo);
        PlayPreview(photo);
    }

    public int GetPhotoCount()
    {
        return photoCount;
    }

    private void UpdatePhotoCounter()
    {
        if (photoCounter != null)
            photoCounter.text = "Fotos: " + photoCount;
    }

    private void PlayShutterSound()
    {
        if (audioSource != null && shutterSound != null)
            audioSource.PlayOneShot(shutterSound);
    }

    private void RegisterPhoto(Texture2D photo)
    {
        if (missionSystem != null)
            missionSystem.AddFoto();

        if (albumController != null)
            albumController.AddPhoto(photo);
    }

    private void PlayPreview(Texture2D photo)
    {
        if (photoPreview == null)
            return;

        StopPreview();
        previewCoroutine = StartCoroutine(PreviewRoutine(photo));
    }

    private void StopPreview()
    {
        if (previewCoroutine == null)
            return;

        StopCoroutine(previewCoroutine);
        previewCoroutine = null;
    }

    private IEnumerator PreviewRoutine(Texture2D photo)
    {
        CaptureRestPose();
        ResetPreviewPose();

        photoPreview.texture = photo;
        SetPreviewAlpha(0f);
        photoPreview.gameObject.SetActive(true);

        RectTransform rect = photoPreview.rectTransform;

        yield return Tween(popDuration, t =>
        {
            float eased = EaseOutCubic(t);
            rect.localScale = Vector3.Lerp(restScale * popStartScale, restScale, eased);
            SetPreviewAlpha(eased);
        });

        rect.localScale = restScale;
        SetPreviewAlpha(1f);

        yield return new WaitForSeconds(previewDuration);

        Vector3 targetScale = restScale * endScale;
        Vector2 targetPosition = GetCornerPosition(rect, targetScale);

        yield return Tween(moveDuration, t =>
        {
            float eased = Mathf.SmoothStep(0f, 1f, t);
            rect.anchoredPosition = Vector2.Lerp(restPosition, targetPosition, eased);
            rect.localScale = Vector3.Lerp(restScale, targetScale, eased);
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(restRotationZ, endRotationZ, eased)
            );
        });

        rect.anchoredPosition = targetPosition;
        rect.localScale = targetScale;
        rect.localRotation = Quaternion.Euler(0f, 0f, endRotationZ);

        yield return new WaitForSeconds(cornerHoldDuration);

        yield return Tween(fadeOutDuration, t => SetPreviewAlpha(1f - t));

        ResetPreviewPose();
        photoPreview.gameObject.SetActive(false);
        previewCoroutine = null;
    }

    private IEnumerator Tween(float duration, Action<float> onProgress)
    {
        if (duration <= 0f)
        {
            onProgress(1f);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            onProgress(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        onProgress(1f);
    }

    private void CaptureRestPose()
    {
        if (restCaptured || photoPreview == null)
            return;

        RectTransform rect = photoPreview.rectTransform;

        restPosition = rect.anchoredPosition;
        restScale = rect.localScale;
        restRotationZ = rect.localEulerAngles.z;
        restCaptured = true;
    }

    private void ResetPreviewPose()
    {
        if (!restCaptured || photoPreview == null)
            return;

        RectTransform rect = photoPreview.rectTransform;

        rect.anchoredPosition = restPosition;
        rect.localScale = restScale;
        rect.localRotation = Quaternion.Euler(0f, 0f, restRotationZ);
        SetPreviewAlpha(1f);
    }

    private Vector2 GetCornerPosition(RectTransform rect, Vector3 scale)
    {
        RectTransform parent = rect.parent as RectTransform;

        if (parent == null)
            return rect.anchoredPosition;

        Vector2 size = Vector2.Scale(rect.rect.size, scale);
        Vector2 halfParent = parent.rect.size * 0.5f;

        return new Vector2(
            halfParent.x - cornerMargin.x - size.x * 0.5f,
            -halfParent.y + cornerMargin.y + size.y * 0.5f
        );
    }

    private float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private void SetPreviewAlpha(float alpha)
    {
        Color color = photoPreview.color;
        color.a = alpha;
        photoPreview.color = color;
    }
}