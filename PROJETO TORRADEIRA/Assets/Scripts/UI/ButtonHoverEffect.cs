using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float animationSpeed = 10f;

    [Header("Hover Sound")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 1f;

    [Header("Texto")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalTextColor = new Color(0.72f, 0.72f, 0.72f, 1f);
    [SerializeField] private Color hoverTextColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private string hoverPrefix = "> ";

    private Vector3 originalScale;
    private Vector3 targetScale;
    private string baseText;
    private bool hovered;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            baseText = label.text.Trim();
            ApplyLabel(false);
        }
    }

    private void OnDisable()
    {
        hovered = false;
        targetScale = originalScale;
        transform.localScale = originalScale;
        ApplyLabel(false);
    }

    private void Update()
    {
        // So anima enquanto a escala ainda nao chegou no alvo.
        if ((transform.localScale - targetScale).sqrMagnitude < 0.000001f)
            return;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHovered(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHovered(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHovered(false);
    }

    private void SetHovered(bool value)
    {
        if (hovered == value)
            return;

        hovered = value;
        targetScale = value ? originalScale * hoverScale : originalScale;
        ApplyLabel(value);

        if (value)
            PlayHoverSound();
    }

    private void ApplyLabel(bool hover)
    {
        if (label == null || baseText == null)
            return;

        label.text = hover ? hoverPrefix + baseText : baseText;
        label.color = hover ? hoverTextColor : normalTextColor;
    }

    private void PlayHoverSound()
    {
        if (hoverSound == null || audioSource == null)
            return;

        audioSource.PlayOneShot(hoverSound, hoverVolume);
    }
}
