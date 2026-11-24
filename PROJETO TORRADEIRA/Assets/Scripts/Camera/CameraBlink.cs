using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Transicao do visor, em pixel art:
// ABRIR: a camera (vista por tras, nas maos) sobe da parte de baixo ate o rosto e o visor
//        preto cobre a tela; entao o diafragma abre e fica nos cantos como borda da lente.
// FECHAR: o diafragma fecha devagar ate as laminas se encontrarem no centro (preto);
//         depois a camera desce e o mundo volta (PlayReveal).
// O zoom fecha um pouco as laminas (no lugar das faixas pretas).
public class CameraBlink : MonoBehaviour
{
    [Header("Bordas (legado, ficam escondidas)")]
    [SerializeField] private Image top;
    [SerializeField] private Image bottom;

    [Header("HUD")]
    [SerializeField] private CanvasGroup hudGroup;

    [Header("Fade da Tela")]
    [SerializeField] private Image screenFade;

    [Header("Duracao (legado)")]
    [SerializeField] private float openDuration = 0.45f;
    [SerializeField] private float closeDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.45f;

    [Header("Camera ate o rosto")]
    [SerializeField] private float raiseTime = 0.5f;
    [SerializeField] private float lowerTime = 0.5f;

    [Header("Diafragma")]
    [SerializeField] private float irisOpenTime = 0.85f;
    [SerializeField] private float irisCloseTime = 1.0f;
    [Tooltip("Abertura com o visor aberto (1 = laminas fora da tela). As laminas ficam nos cantos como borda da lente.")]
    [SerializeField, Range(0.5f, 1f)] private float restAperture = 0.84f;
    [Tooltip("Abertura no zoom maximo (substitui as faixas pretas).")]
    [SerializeField, Range(0.3f, 1f)] private float zoomAperture = 0.62f;
    [SerializeField, Range(3, 12)] private int blades = 6;
    [Tooltip("Quanto as laminas giram (radianos) durante o movimento.")]
    [SerializeField] private float bladeTurn = 0.7f;

    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int RotationId = Shader.PropertyToID("_Rotation");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int BladesId = Shader.PropertyToID("_Blades");

    // Desenho da camera (pixels) e o retangulo do visor dentro dele.
    private const int CamW = 96, CamH = 64;
    private static readonly RectInt Eyepiece = new RectInt(40, 30, 16, 12);

    private Image iris;
    private Material irisMaterial;
    private Image held;
    private Coroutine animationCoroutine;

    private bool lensResting;

    // Fechamento por superaquecimento: laminas travando, vermelho, camera caindo das maos.
    private bool overheatClose;

    public void PrepareOverheat()
    {
        overheatClose = true;
    }
    private float zoom;
    private float restRadius;
    private float restRotation;

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

    // ---------- Abrir ----------

    private IEnumerator AnimateOpen()
    {
        SetScreenFade(0f);
        lensResting = false;
        SetHUDAlpha(0f);
        SetHUDScale(1.04f);

        // 1) Camera sobe ate o rosto: o visor preto cobre a tela.
        yield return RaiseCamera(true, raiseTime);

        // 2) Diafragma abre a partir do preto.
        SetIrisActive(true);
        SetHeldVisible(false);

        float duration = Mathf.Max(0.05f, irisOpenTime);
        float target = TargetRadius();
        SetIris(ClosedRadius(), bladeTurn);

        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(time / duration);
            float k = EaseOutQuart(t);
            SetIris(Mathf.Lerp(ClosedRadius(), target, k), bladeTurn * (1f - k));

            float hud = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, t));
            SetHUDAlpha(hud);
            SetHUDScale(Mathf.Lerp(1.04f, 1f, hud));
            yield return null;
        }

        restRadius = target;
        restRotation = 0f;
        lensResting = true;
        SetIris(restRadius, restRotation);
        SetHUDAlpha(1f);
        SetHUDScale(1f);
        animationCoroutine = null;
    }

    // ---------- Fechar ----------

    private IEnumerator AnimateClose()
    {
        SetScreenFade(0f);
        SetIrisActive(true);

        float duration = Mathf.Max(0.05f, irisCloseTime);
        float from = lensResting ? restRadius : TargetRadius();
        float fromRotation = lensResting ? restRotation : 0f;
        lensResting = false;

        bool overheat = overheatClose;
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(time / duration);
            // Comeca devagar, acelera e desacelera quando as laminas se encontram.
            float k = EaseInOutCubic(t);
            float rotation = Mathf.Lerp(fromRotation, -bladeTurn, k);

            if (overheat)
            {
                // Mecanismo travando: fecha aos trancos (6 batidas) tremendo, piscando vermelho.
                k = Mathf.Floor(t * 6f + 0.5f) / 6f;
                rotation += Mathf.Sin(time * 70f) * 0.06f;
                if (iris != null)
                    iris.color = Mathf.Repeat(time * 9f, 1f) < 0.5f ? new Color(1f, 0.35f, 0.32f) : Color.white;
            }

            SetIris(Mathf.Lerp(from, ClosedRadius(), k), rotation);

            float hud = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.45f, t));
            SetHUDAlpha(hud);
            SetHUDScale(Mathf.Lerp(0.98f, 1f, hud));
            yield return null;
        }

        // Laminas encostadas: segura o preto um instante.
        SetIris(ClosedRadius(), -bladeTurn);
        SetHUDAlpha(0f);
        SetHUDScale(1f);
        yield return WaitUnscaled(0.08f);

        if (iris != null)
            iris.color = Color.white;

        // Fica preto: quem chamou troca a camera e depois usa PlayReveal (camera desce).
        SetScreenFade(1f);
        SetIrisActive(false);
        animationCoroutine = null;
    }

    // Depois de fechar: a camera desce do rosto e o mundo aparece.
    private IEnumerator AnimateReveal()
    {
        SetIrisActive(false);
        SetHUDAlpha(0f);

        if (EnsureHeld())
        {
            SetScreenFade(0f);
            if (overheatClose)
                yield return DropCamera();
            else
                yield return RaiseCamera(false, lowerTime);
        }
        else
        {
            // Sem a arte da camera: fade simples.
            float duration = Mathf.Max(0.001f, fadeInDuration);
            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                SetScreenFade(1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration)));
                yield return null;
            }
        }

        overheatClose = false;

        SetScreenFade(0f);
        SetHeldVisible(false);
        animationCoroutine = null;
    }

    public void ResetBlink()
    {
        StopAnimation();
        lensResting = false;
        zoom = 0f;
        SetIrisActive(false);
        SetHeldVisible(false);
        SetHUDAlpha(0f);
        SetHUDScale(1f);
        SetScreenFade(0f);
    }

    // ---------- Zoom ----------

    public void SetZoom(float value)
    {
        zoom = Mathf.Clamp01(value);
    }

    private void Update()
    {
        if (!lensResting || animationCoroutine != null)
            return;

        float k = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
        restRadius = Mathf.Lerp(restRadius, TargetRadius(), k);
        restRotation = Mathf.Lerp(restRotation, zoom * bladeTurn * 0.35f, k);
        SetIris(restRadius, restRotation);
    }

    private float TargetRadius()
    {
        return FullyOpenRadius() * Mathf.Lerp(restAperture, zoomAperture, zoom);
    }

    // Um pouco abaixo de zero: as laminas se sobrepoem e nao sobra buraco no centro.
    private static float ClosedRadius()
    {
        return -0.03f;
    }

    // ---------- Camera nas maos (pixel art) ----------

    // up = true: de baixo ate cobrir a tela com o visor. up = false: o contrario.
    private IEnumerator RaiseCamera(bool up, float duration)
    {
        if (!EnsureHeld())
            yield break;

        RectTransform host = HostRect();
        Vector2 size = host != null ? host.rect.size : new Vector2(1920f, 1080f);
        RectTransform rect = held.rectTransform;

        // Tamanho "na mao": 60% da largura da tela.
        float baseWidth = size.x * 0.6f;
        rect.sizeDelta = new Vector2(baseWidth, baseWidth * CamH / CamW);
        float unit = baseWidth / CamW; // unidades por pixel do desenho

        // Visor (em unidades, relativo ao centro do desenho).
        Vector2 eyeCenter = new Vector2((Eyepiece.x + Eyepiece.width * 0.5f - CamW * 0.5f) * unit, (Eyepiece.y + Eyepiece.height * 0.5f - CamH * 0.5f) * unit);
        Vector2 eyeSize = new Vector2(Eyepiece.width, Eyepiece.height) * unit;
        float coverScale = Mathf.Max(size.x / eyeSize.x, size.y / eyeSize.y) * 1.08f;

        Vector2 below = new Vector2(0f, -size.y * 0.5f - rect.sizeDelta.y * 0.6f);
        Vector2 inHands = new Vector2(0f, -size.y * 0.18f);

        SetHeldVisible(true);
        duration = Mathf.Max(0.05f, duration);

        for (float time = 0f; time <= duration; time += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(time / duration);
            float s = up ? t : 1f - t;

            // 0..0.55: sobe ate as maos; 0.55..1: encosta no rosto (cresce ate o visor cobrir tudo).
            float lift = EaseOutCubic(Mathf.Clamp01(s / 0.55f));
            float close = EaseInCubic(Mathf.Clamp01((s - 0.55f) / 0.45f));
            float scale = Mathf.Lerp(1f, coverScale, close);

            Vector2 pos = Vector2.Lerp(below, inHands, lift);
            // Ao crescer, o visor vai para o centro da tela.
            pos = Vector2.Lerp(pos, -eyeCenter * scale, close);

            // Balanco leve enquanto sobe.
            float wobble = Mathf.Sin(s * Mathf.PI) * 3f;
            rect.anchoredPosition = pos;
            rect.localScale = Vector3.one * scale;
            rect.localRotation = Quaternion.Euler(0f, 0f, wobble * (1f - close));

            yield return null;
            if (t >= 1f)
                break;
        }

        if (!up)
            SetHeldVisible(false);
    }

    // Superaquecimento: a camera sai do rosto e escorrega das maos, girando para fora da tela.
    private IEnumerator DropCamera()
    {
        yield return RaiseCamera(false, lowerTime * 0.45f);

        RectTransform rect = held.rectTransform;
        RectTransform host = HostRect();
        float height = host != null ? host.rect.height : 1080f;
        SetHeldVisible(true);

        Vector2 start = new Vector2(0f, -height * 0.18f);
        for (float time = 0f; time < 0.55f; time += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(time / 0.55f);
            float fall = t * t;
            rect.anchoredPosition = start + new Vector2(t * 160f, -fall * height * 0.9f);
            rect.localRotation = Quaternion.Euler(0f, 0f, -fall * 55f);
            rect.localScale = Vector3.one;
            yield return null;
        }

        SetHeldVisible(false);
    }

    private bool EnsureHeld()
    {
        if (held != null)
            return true;

        RectTransform parent = HostRect();
        if (parent == null)
            return false;

        GameObject go = new GameObject("CameraInHands", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        held = go.GetComponent<Image>();
        held.sprite = BuildCameraSprite();
        held.raycastTarget = false;
        held.preserveAspect = true;

        RectTransform rect = held.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        go.SetActive(false);
        return true;
    }

    private void SetHeldVisible(bool visible)
    {
        if (held == null)
            return;

        if (held.gameObject.activeSelf != visible)
            held.gameObject.SetActive(visible);

        if (visible)
            held.rectTransform.SetAsLastSibling();
    }

    // Camera de 1975 vista por tras (lado do fotografo), segurada pelas duas maos.
    private static Sprite BuildCameraSprite()
    {
        var tex = new Texture2D(CamW, CamH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "CameraInHands" };
        var px = new Color32[CamW * CamH];

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 outline = new Color32(8, 8, 9, 255);
        Color32 leather = new Color32(28, 28, 30, 255);
        Color32 leatherDot = new Color32(40, 40, 42, 255);
        Color32 chrome = new Color32(150, 152, 154, 255);
        Color32 chromeLight = new Color32(205, 207, 208, 255);
        Color32 chromeDark = new Color32(96, 98, 100, 255);
        Color32 glass = new Color32(5, 5, 6, 255);
        Color32 rubber = new Color32(16, 16, 17, 255);
        Color32 skin = new Color32(168, 150, 136, 255);
        Color32 skinDark = new Color32(122, 106, 96, 255);
        Color32 sleeve = new Color32(54, 52, 46, 255);

        for (int i = 0; i < px.Length; i++)
            px[i] = clear;

        // Corpo com couro texturizado.
        Fill(px, 6, 8, 89, 46, outline);
        for (int y = 9; y < 46; y++)
            for (int x = 7; x < 89; x++)
                Put(px, x, y, ((x * 3 + y * 5) % 7 == 0) ? leatherDot : leather);

        // Tampa superior cromada com brilho.
        Fill(px, 10, 46, 85, 55, outline);
        Fill(px, 11, 47, 84, 54, chrome);
        Fill(px, 11, 53, 84, 53, chromeLight);
        Fill(px, 11, 47, 84, 47, chromeDark);

        // Botao de rebobinar (esq.), disparador e alavanca (dir.).
        Fill(px, 16, 55, 26, 59, outline);
        Fill(px, 17, 56, 25, 58, chromeDark);
        Fill(px, 18, 58, 24, 58, chromeLight);
        Fill(px, 68, 55, 74, 58, outline);
        Fill(px, 69, 56, 73, 57, chromeLight);
        Fill(px, 77, 55, 86, 56, chromeDark);

        // Visor: ocular de borracha com vidro preto.
        Fill(px, Eyepiece.x - 3, Eyepiece.y - 3, Eyepiece.x + Eyepiece.width + 2, Eyepiece.y + Eyepiece.height + 2, outline);
        Fill(px, Eyepiece.x - 2, Eyepiece.y - 2, Eyepiece.x + Eyepiece.width + 1, Eyepiece.y + Eyepiece.height + 1, rubber);
        Fill(px, Eyepiece.x, Eyepiece.y, Eyepiece.x + Eyepiece.width - 1, Eyepiece.y + Eyepiece.height - 1, glass);

        // Contador de fotos e tampa traseira.
        Fill(px, 70, 36, 78, 41, outline);
        Fill(px, 71, 37, 77, 40, chromeDark);
        Fill(px, 72, 38, 73, 39, chromeLight);
        Fill(px, 12, 12, 83, 12, chromeDark);

        // Mao esquerda segurando a lateral.
        Fill(px, 0, 4, 12, 40, outline);
        Fill(px, 1, 5, 11, 39, skin);
        for (int y = 14; y <= 34; y += 6)
            Fill(px, 7, y, 11, y, skinDark);
        Fill(px, 0, 0, 10, 6, sleeve);

        // Mao direita com o indicador no disparador.
        Fill(px, 84, 4, 95, 40, outline);
        Fill(px, 85, 5, 94, 39, skin);
        for (int y = 14; y <= 34; y += 6)
            Fill(px, 85, y, 88, y, skinDark);
        Fill(px, 70, 58, 80, 62, outline);
        Fill(px, 71, 59, 80, 61, skin);
        Fill(px, 80, 40, 86, 61, outline);
        Fill(px, 81, 41, 85, 60, skin);
        Fill(px, 86, 0, 95, 6, sleeve);

        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, CamW, CamH), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void Put(Color32[] px, int x, int y, Color32 c)
    {
        if (x >= 0 && y >= 0 && x < CamW && y < CamH)
            px[y * CamW + x] = c;
    }

    private static void Fill(Color32[] px, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                Put(px, x, y, c);
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

    private static IEnumerator WaitUnscaled(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            yield return null;
    }

    private static float EaseOutQuart(float t)
    {
        t = 1f - Mathf.Clamp01(t);
        return 1f - t * t * t * t;
    }

    private static float EaseOutCubic(float t)
    {
        t = 1f - Mathf.Clamp01(t);
        return 1f - t * t * t;
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private static float EaseInOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private void Setup()
    {
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
