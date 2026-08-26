using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float animationSpeed = 10f;

    [Header("Hover Sound")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 1f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;

        PlayHoverSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    private void PlayHoverSound()
    {
        if (hoverSound == null || audioSource == null)
            return;

        audioSource.PlayOneShot(hoverSound, hoverVolume);
    }
}