using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CameraPhotoData : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private RawImage photoPreview;

    [Header("Audio")]
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

    [Header("Animacao: entrada")]
    [SerializeField] private float popDuration = 0.18f;
    [SerializeField, Range(0.5f, 1f)]
    private float popStartScale = 0.85f;

    [Header("Animacao: movimento para o canto")]
    [SerializeField] private float moveDuration = 0.75f;
    [SerializeField] private float endScale = 0.3f;
    [SerializeField] private float endRotationZ = -12f;
    [SerializeField]
    private Vector2 cornerMargin =
        new Vector2(40f, 40f);

    [Header("Animacao: saida")]
    [SerializeField] private float cornerHoldDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private int photoCount;
    private Coroutine previewCoroutine;
    private Texture2D ownedPreviewTexture;

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

        EnsureCard();
        if (card != null)
            card.gameObject.SetActive(false);
    }

    public void RegisterPhotographedLocations(Camera camera)
    {
        if (missionSystem != null)
            missionSystem.RegisterPhoto(camera);
    }

    public void HidePreviewBeforeCapture()
    {
        StopPreview();

        if (photoPreview == null)
            return;

        if (card != null)
            card.gameObject.SetActive(false);
        if (albumHint != null)
            albumHint.gameObject.SetActive(false);
    }

    public void ProcessPhoto(Texture2D photo)
    {
        if (photo == null)
            return;

        StopPreview();
        ReleaseOwnedPreview();

        photoCount++;

        UpdatePhotoCounter();
        PlayShutterSound();
        RegisterPhoto(photo);
        PlayPreview(photo);
    }

    public void SetPhotoCount(int value)
    {
        photoCount = Mathf.Max(0, value);
        if (photoCounter != null)
            photoCounter.text = "Fotos: " + photoCount;
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

    private bool lastStoredInAlbum = true;

    private void RegisterPhoto(Texture2D photo)
    {
        if (missionSystem != null)
            missionSystem.AddFoto();

        bool storedInAlbum =
            albumController != null &&
            albumController.AddPhoto(photo);
        lastStoredInAlbum = storedInAlbum;

        if (!storedInAlbum)
            ownedPreviewTexture = photo;
    }

    private void ReleaseOwnedPreview()
    {
        if (ownedPreviewTexture == null)
            return;

        if (photoPreview != null && photoPreview.texture == ownedPreviewTexture)
            photoPreview.texture = null;

        Destroy(ownedPreviewTexture);
        ownedPreviewTexture = null;
    }

    private void OnDestroy()
    {
        ReleaseOwnedPreview();
    }

    private void PlayPreview(Texture2D photo)
    {
        if (photoPreview == null)
        {
            ReleaseOwnedPreview();
            return;
        }

        StopPreview();

        previewCoroutine = StartCoroutine(
            PreviewRoutine(photo)
        );
    }

    private void StopPreview()
    {
        if (previewCoroutine == null)
            return;

        StopCoroutine(previewCoroutine);
        previewCoroutine = null;
    }

    // ---------- Foto instantanea ----------
    // A foto sai de baixo num cartao branco (na proporcao certa), "revela" do branco ate a imagem
    // e depois voa encolhendo para o canto do album.

    private RectTransform card;
    private Image developLayer;
    private TextMeshProUGUI cardLabel;
    private TextMeshProUGUI albumHint;

    private const float CardWidth = 300f;
    private const float Border = 14f;
    private const float BottomBorder = 46f;

    private void EnsureCard()
    {
        if (card != null || photoPreview == null)
            return;

        RectTransform parent = photoPreview.rectTransform.parent as RectTransform;

        GameObject go = new GameObject("PhotoCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        go.layer = photoPreview.gameObject.layer;
        card = (RectTransform)go.transform;
        card.SetParent(parent, false);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);

        Image frame = go.GetComponent<Image>();
        frame.color = new Color(0.92f, 0.91f, 0.87f, 1f);
        frame.raycastTarget = false;

        // Sombra simples atras do cartao (filho desenhado antes).
        Image shadow = new GameObject("Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        shadow.gameObject.layer = go.layer;
        shadow.transform.SetParent(parent, false);
        shadow.color = new Color(0f, 0f, 0f, 0f);
        shadow.raycastTarget = false;
        shadow.gameObject.SetActive(false);

        // A foto vai para dentro do cartao.
        RectTransform photo = photoPreview.rectTransform;
        photo.SetParent(card, false);
        photo.anchorMin = Vector2.zero;
        photo.anchorMax = Vector2.one;
        photo.pivot = new Vector2(0.5f, 0.5f);
        photo.offsetMin = new Vector2(Border, BottomBorder);
        photo.offsetMax = new Vector2(-Border, -Border);
        photo.anchoredPosition = Vector2.zero;
        photo.localScale = Vector3.one;
        photo.localRotation = Quaternion.identity;
        photoPreview.gameObject.SetActive(true);

        developLayer = new GameObject("Develop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        developLayer.gameObject.layer = go.layer;
        developLayer.transform.SetParent(card, false);
        developLayer.raycastTarget = false;
        RectTransform dev = developLayer.rectTransform;
        dev.anchorMin = Vector2.zero;
        dev.anchorMax = Vector2.one;
        dev.offsetMin = new Vector2(Border, BottomBorder);
        dev.offsetMax = new Vector2(-Border, -Border);

        TMP_FontAsset font = photoCounter != null ? photoCounter.font : null;
        cardLabel = CardText(card, "Label", font, 22f, new Color(0.18f, 0.17f, 0.16f), TextAlignmentOptions.Bottom);
        StretchRect(cardLabel.rectTransform, new Vector2(Border, 10f), new Vector2(-Border, 0f));

        albumHint = CardText(parent, "AlbumHint", font, 22f, Color.white, TextAlignmentOptions.BottomRight);
        RectTransform hint = albumHint.rectTransform;
        hint.anchorMin = hint.anchorMax = new Vector2(0.5f, 0.5f);
        hint.pivot = new Vector2(1f, 0f);
        hint.sizeDelta = new Vector2(400f, 40f);
        albumHint.gameObject.SetActive(false);

        go.SetActive(false);
    }

    private static TextMeshProUGUI CardText(Transform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.gameObject.layer = parent.gameObject.layer;
        text.transform.SetParent(parent, false);
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void StretchRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    private IEnumerator PreviewRoutine(Texture2D photo)
    {
        EnsureCard();

        if (card == null)
            yield break;

        RectTransform parent = card.parent as RectTransform;
        Vector2 half = parent != null && parent.rect.width > 200f ? parent.rect.size * 0.5f : new Vector2(960f, 540f);

        float aspect = photo.height > 0 ? (float)photo.width / photo.height : 16f / 9f;
        float photoWidth = CardWidth - Border * 2f;
        card.sizeDelta = new Vector2(CardWidth, photoWidth / aspect + Border + BottomBorder);

        photoPreview.texture = photo;
        photoPreview.color = Color.white;
        cardLabel.text = "FOTO " + photoCount.ToString("00");
        card.localScale = Vector3.one;
        card.gameObject.SetActive(true);
        card.SetAsLastSibling();

        CanvasGroup group = card.GetComponent<CanvasGroup>();
        group.alpha = 1f;

        Vector2 below = new Vector2(half.x * 0.42f, -half.y - card.sizeDelta.y);
        Vector2 rest = new Vector2(half.x * 0.42f, -half.y + card.sizeDelta.y * 0.5f + 70f);

        // 1) Sai de baixo, girando ate assentar.
        SetDevelop(1f);
        yield return Tween(0.38f, t =>
        {
            float e = EaseOutBack(t);
            card.anchoredPosition = Vector2.LerpUnclamped(below, rest, e);
            card.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, -4f, EaseOutCubic(t)));
        });

        // 2) Revela: branco -> imagem.
        yield return Tween(1.1f, t => SetDevelop(1f - Mathf.SmoothStep(0f, 1f, t)));
        SetDevelop(0f);

        yield return new WaitForSeconds(Mathf.Max(0f, previewDuration - 1.1f));

        // 3) Voa para o canto do album, encolhendo.
        Vector2 corner = new Vector2(half.x - cornerMargin.x - 50f, -half.y + cornerMargin.y + 40f);
        ShowAlbumHint(corner + new Vector2(30f, 70f));

        yield return Tween(0.55f, t =>
        {
            float e = EaseInOutCubic(t);
            card.anchoredPosition = Vector2.Lerp(rest, corner, e);
            card.localScale = Vector3.one * Mathf.Lerp(1f, 0.22f, e);
            card.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-4f, 10f, e));
            group.alpha = 1f - Mathf.InverseLerp(0.7f, 1f, t);
        });

        card.gameObject.SetActive(false);
        photoPreview.texture = null;

        yield return new WaitForSeconds(cornerHoldDuration * 0.6f);

        if (albumHint != null)
        {
            yield return Tween(fadeOutDuration, t => albumHint.alpha = 1f - t);
            albumHint.gameObject.SetActive(false);
        }

        ReleaseOwnedPreview();
        previewCoroutine = null;
    }

    private void SetDevelop(float amount)
    {
        if (developLayer != null)
            developLayer.color = new Color(0.95f, 0.94f, 0.9f, Mathf.Clamp01(amount));
    }

    private void ShowAlbumHint(Vector2 position)
    {
        if (albumHint == null)
            return;

        albumHint.text = lastStoredInAlbum
            ? "+1 NO ÁLBUM  [F]"
            : "ÁLBUM CHEIO (4/4)  [F] APAGAR";
        albumHint.rectTransform.anchoredPosition = position;
        albumHint.alpha = 1f;
        albumHint.gameObject.SetActive(true);
        albumHint.transform.SetAsLastSibling();
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.4f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private IEnumerator Tween(
        float duration,
        Action<float> onProgress
    )
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

            onProgress(
                Mathf.Clamp01(elapsed / duration)
            );

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

        rect.localRotation = Quaternion.Euler(
            0f,
            0f,
            restRotationZ
        );

        SetPreviewAlpha(1f);
    }

    private Vector2 GetCornerPosition(
        RectTransform rect,
        Vector3 scale
    )
    {
        RectTransform parent =
            rect.parent as RectTransform;

        if (parent == null)
            return rect.anchoredPosition;

        Vector2 size = Vector2.Scale(
            rect.rect.size,
            scale
        );

        Vector2 halfParent = parent.rect.size * 0.5f;

        return new Vector2(
            halfParent.x -
            cornerMargin.x -
            size.x * 0.5f,

            -halfParent.y +
            cornerMargin.y +
            size.y * 0.5f
        );
    }

    private float EaseOutCubic(float t)
    {
        float inverse = 1f - t;

        return 1f -
               inverse * inverse * inverse;
    }

    private void SetPreviewAlpha(float alpha)
    {
        if (photoPreview == null)
            return;

        Color color = photoPreview.color;
        color.a = alpha;

        photoPreview.color = color;
    }
}