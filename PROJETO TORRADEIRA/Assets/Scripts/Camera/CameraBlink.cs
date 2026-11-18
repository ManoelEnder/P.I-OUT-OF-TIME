using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Abre e fecha o visor como o diafragma de uma lente: laminas em espiral giram e se
// afastam do centro (abrir) ou se fecham ate o preto (fechar). O HUD entra e sai em
// fade suave, com um leve ajuste de escala, sem piscar.
public class CameraBlink : MonoBehaviour
{
    [Header("Bordas (legado, ficam escondidas)")]
    [SerializeField] private Image top;
    [SerializeField] private Image bottom;

    [Header("HUD")]
    [SerializeField] private CanvasGroup hudGroup;

    [Header("Fade da Tela")]
    [SerializeField] private Image screenFade;

    [Header("Duracao")]
    [SerializeField] private float openDuration = 0.45f;
    [SerializeField] private float closeDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.45f;

    [Header("Diafragma")]
    [SerializeField] private float irisOpenSeconds = 0.42f;
    [SerializeField] private float irisCloseSeconds = 0.3f;
    [SerializeField, Range(3, 12)] private int blades = 6;
    [Tooltip("Quanto as laminas giram (radianos) durante o movimento.")]
    [SerializeField] private float bladeTurn = 0.7f;

    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int RotationId = Shader.PropertyToID("_Rotation");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int BladesId = Shader.PropertyToID("_Blades");

    private Image iris;
    private Material irisMaterial;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        Setup();
        ResetBlink();
    }

    public Coroutine Play()
    {
        StopAnimation();
        animationCoroutine = StartCoroutine(AnimateOpen());
        return animationCoroutine;
    }

    public Coroutine PlayClose()
    {
        StopAnimation();
        animationCoroutine = StartCoroutine(AnimateClose());
        return animationCoroutine;
    }

    public Coroutine PlayReveal()
    {
        StopAnimation();
        animationCoroutine = StartCoroutine(AnimateReveal());
        return animationCoroutine;
    }

    private IEnumerator AnimateOpen()
    {
        SetScreenFade(0f);
        SetIrisActive(true);

        float duration = Mathf.Max(0.05f, irisOpenSeconds > 0f ? irisOpenSeconds : openDuration);
        float full = FullyOpenRadius();

        SetIris(0f, bladeTurn);
        SetHUDAlpha(0f);
        SetHUDScale(1.04f);

        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float k = EaseOutQuart(t);

            SetIris(full * k, bladeTurn * (1f - k));

            // HUD aparece na segunda metade, assentando no lugar.
            float hud = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, t));
            SetHUDAlpha(hud);
            SetHUDScale(Mathf.Lerp(1.04f, 1f, hud));

            yield return null;
        }

        SetIrisActive(false);
        SetHUDAlpha(1f);
        SetHUDScale(1f);
        animationCoroutine = null;
    }

    private IEnumerator AnimateClose()
    {
        SetScreenFade(0f);
        SetIrisActive(true);

        float duration = Mathf.Max(0.05f, irisCloseSeconds > 0f ? irisCloseSeconds : closeDuration);
        float full = FullyOpenRadius();

        SetIris(full, 0f);
        SetHUDAlpha(1f);

        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float k = EaseInCubic(t);

            SetIris(full * (1f - k), -bladeTurn * k);

            float hud = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.5f, t));
            SetHUDAlpha(hud);
            SetHUDScale(Mathf.Lerp(0.98f, 1f, hud));

            yield return null;
        }

        SetIris(0f, -bladeTurn);
        SetHUDAlpha(0f);
        SetHUDScale(1f);

        // Fica preto: quem chamou troca a camera e depois usa PlayReveal.
        SetScreenFade(1f);
        SetIrisActive(false);
        animationCoroutine = null;
    }

    private IEnumerator AnimateReveal()
    {
        SetIrisActive(false);
        SetHUDAlpha(0f);
        SetScreenFade(1f);

        float duration = Mathf.Max(0.001f, fadeInDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            SetScreenFade(1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration)));
            yield return null;
        }

        SetScreenFade(0f);
        animationCoroutine = null;
    }

    public void ResetBlink()
    {
        StopAnimation();
        SetIrisActive(false);
        SetHUDAlpha(0f);
        SetHUDScale(1f);
        SetScreenFade(0f);
    }

    // ---------- Diafragma ----------

    private void CreateIris()
    {
        RectTransform parent = HostRect();

        if (parent == null)
            return;

        Shader shader = Resources.Load<Shader>("ApertureIris");

        if (shader == null)
        {
            Debug.LogWarning("CameraBlink: shader ApertureIris nao encontrado em Resources.");
            return;
        }

        GameObject go = new GameObject("ApertureIris", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        irisMaterial = new Material(shader) { name = "ApertureIris (instancia)" };
        irisMaterial.SetFloat(BladesId, blades);

        iris = go.GetComponent<Image>();
        iris.material = irisMaterial;
        iris.color = Color.white;
        iris.raycastTarget = false;

        go.SetActive(false);
    }

    private RectTransform HostRect()
    {
        if (top != null)
            return top.rectTransform.parent as RectTransform;

        if (screenFade != null)
            return screenFade.rectTransform.parent as RectTransform;

        return hudGroup != null ? hudGroup.transform.parent as RectTransform : null;
    }

    private float Aspect()
    {
        RectTransform host = HostRect();

        if (host != null && host.rect.height > 1f)
            return host.rect.width / host.rect.height;

        return Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
    }

    // Raio em que o hexagono cobre os cantos da tela (com folga).
    private float FullyOpenRadius()
    {
        float aspect = Aspect();
        return Mathf.Sqrt(aspect * aspect + 1f) * 1.08f;
    }

    private void SetIris(float radius, float rotation)
    {
        if (irisMaterial == null)
            return;

        irisMaterial.SetFloat(RadiusId, radius);
        irisMaterial.SetFloat(RotationId, rotation);
        irisMaterial.SetFloat(AspectId, Aspect());
    }

    private void SetIrisActive(bool active)
    {
        if (iris == null && active)
            CreateIris();

        if (iris == null)
            return;

        if (iris.gameObject.activeSelf != active)
            iris.gameObject.SetActive(active);

        if (active)
            iris.rectTransform.SetAsLastSibling();
    }

    // ---------- Utilitarios ----------

    private static float EaseOutQuart(float t)
    {
        t = 1f - Mathf.Clamp01(t);
        return 1f - t * t * t * t;
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private void Setup()
    {
        // As bordas pretas antigas nao sao mais usadas.
        if (top != null)
        {
            top.raycastTarget = false;
            top.gameObject.SetActive(false);
        }

        if (bottom != null)
        {
            bottom.raycastTarget = false;
            bottom.gameObject.SetActive(false);
        }

        if (screenFade != null)
        {
            RectTransform rect = screenFade.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            screenFade.raycastTarget = false;
        }
    }

    private void SetHUDScale(float scale)
    {
        if (hudGroup != null)
            hudGroup.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void SetHUDAlpha(float alpha)
    {
        if (hudGroup != null)
            hudGroup.alpha = Mathf.Clamp01(alpha);
    }

    private void SetScreenFade(float alpha)
    {
        if (screenFade == null)
            return;

        alpha = Mathf.Clamp01(alpha);

        if (alpha > 0f)
            screenFade.gameObject.SetActive(true);

        screenFade.color = new Color(0f, 0f, 0f, alpha);

        if (alpha <= 0f)
            screenFade.gameObject.SetActive(false);
    }

    private void StopAnimation()
    {
        if (animationCoroutine == null)
            return;

        StopCoroutine(animationCoroutine);
        animationCoroutine = null;
    }

    private void OnDisable()
    {
        ResetBlink();
    }

    private void OnDestroy()
    {
        if (irisMaterial != null)
            Destroy(irisMaterial);
    }
}
