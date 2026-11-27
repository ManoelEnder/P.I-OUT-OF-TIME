using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Mensagem do radio:
// - um radio portatil em pixel art (preto e branco com LED vermelho) aparece ao lado do texto e
//   balanca para cima e para baixo enquanto a transmissao toca (LED piscando, antena tremendo);
// - nao funciona no modo camera: se o visor abrir, a mensagem some na hora.
public class RadioMessageUI : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public AudioSource audioSource;

    public float enterDuration = 0.5f;
    public float visibleDuration = 5f;
    public float exitDuration = 0.4f;

    public float startOffset = 70f;

    public float movementAmount = 2f;
    public float movementSpeed = 2f;

    private const float RadioX = 6f, RadioY = -6f, Px = 4.5f, LiftForRadio = 48f;
    [SerializeField, HideInInspector] private RectTransform radio;
    [SerializeField, HideInInspector] private Image radioImage, led;
    [SerializeField, HideInInspector] private RectTransform antenna;
    private PhotoCamera photo;

    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private Coroutine animationCoroutine;
    private bool showing;
    private float alpha;

    public static bool CameraOpen
    {
        get
        {
            PhotoCamera p = FindFirstObjectByType<PhotoCamera>();
            return p != null && p.IsCameraMode;
        }
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetPosition = rectTransform.anchoredPosition;
        // Sobe um pouco o texto para o radio caber embaixo dele.
        if (messageText != null)
            messageText.rectTransform.anchoredPosition += new Vector2(0f, LiftForRadio);
        if (radio == null)
            BuildRadio();
        HideImmediate();
    }

    void Start()
    {
        photo = FindFirstObjectByType<PhotoCamera>();
    }

    public void ShowMessage(string message, AudioClip audioClip)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        // Com o visor aberto o radio nao toca: espera ele fechar.
        animationCoroutine = StartCoroutine(WaitCameraThenShow(message, audioClip));
    }

    IEnumerator WaitCameraThenShow(string message, AudioClip audioClip)
    {
        while (CameraOpen)
            yield return null;
        yield return ShowRoutine(message, audioClip);
    }

    IEnumerator ShowRoutine(string message, AudioClip audioClip)
    {
        showing = true;
        messageText.text = message;

        if (audioSource != null && audioClip != null)
            audioSource.PlayOneShot(audioClip);

        Vector2 startPosition = targetPosition - new Vector2(0f, startOffset);
        rectTransform.anchoredPosition = startPosition;
        SetAlpha(0f);

        float time = 0f;
        while (time < enterDuration)
        {
            if (Interrupted()) yield break;
            time += Time.deltaTime;
            float smooth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / enterDuration));
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, smooth);
            SetAlpha(smooth);
            AnimateRadio(true);
            yield return null;
        }

        time = 0f;
        float talk = visibleDuration;
        if (audioClip != null) talk = Mathf.Max(visibleDuration, audioClip.length);
        while (time < talk)
        {
            if (Interrupted()) yield break;
            time += Time.deltaTime;
            float movement = Mathf.Sin(Time.time * movementSpeed) * movementAmount;
            rectTransform.anchoredPosition = targetPosition + new Vector2(0f, movement);
            AnimateRadio(true);
            yield return null;
        }

        Vector2 exitPosition = targetPosition + new Vector2(0f, startOffset);
        time = 0f;
        while (time < exitDuration)
        {
            if (Interrupted()) yield break;
            time += Time.deltaTime;
            float smooth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / exitDuration));
            rectTransform.anchoredPosition = Vector2.Lerp(targetPosition, exitPosition, smooth);
            SetAlpha(1f - smooth);
            AnimateRadio(false);
            yield return null;
        }

        HideImmediate();
    }

    // Visor aberto no meio da transmissao: corta o audio e esconde tudo.
    private bool Interrupted()
    {
        if (photo == null || !photo.IsCameraMode)
            return false;
        if (audioSource != null)
            audioSource.Stop();
        HideImmediate();
        animationCoroutine = null;
        return true;
    }

    void SetAlpha(float a)
    {
        alpha = a;
        Color color = messageText.color;
        color.a = a;
        messageText.color = color;
        if (radioImage != null) radioImage.color = new Color(1f, 1f, 1f, a);
        if (led != null) { Color c = led.color; c.a = a; led.color = c; }
    }

    void HideImmediate()
    {
        showing = false;
        SetAlpha(0f);
        rectTransform.anchoredPosition = targetPosition - new Vector2(0f, startOffset);
    }

    // ---------- Radio em pixel art ----------

    public void BuildRadio()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (radio != null) SceneBake.Kill(radio.gameObject);
        Texture2D tex = RadioTexture();
        GameObject go = new GameObject("RadioArte", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        radio = go.GetComponent<RectTransform>();
        // Embaixo do texto, no canto esquerdo (o texto fica junto da borda da tela).
        radio.SetParent(messageText != null ? messageText.rectTransform : rectTransform, false);
        radio.anchorMin = radio.anchorMax = new Vector2(0f, 0f);
        radio.pivot = new Vector2(0f, 1f);
        radio.sizeDelta = new Vector2(tex.width * Px, tex.height * Px);
        radio.anchoredPosition = new Vector2(RadioX, RadioY);
        radioImage = go.GetComponent<Image>();
        radioImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        radioImage.raycastTarget = false;
        radioImage.preserveAspect = true;

        // LED vermelho e antena separados (piscam/tremem).
        GameObject ledGo = new GameObject("Led", typeof(RectTransform), typeof(Image));
        ledGo.layer = gameObject.layer;
        ledGo.transform.SetParent(radio, false);
        led = ledGo.GetComponent<Image>();
        led.rectTransform.anchorMin = led.rectTransform.anchorMax = new Vector2(0f, 0f);
        led.rectTransform.pivot = new Vector2(0f, 0f);
        led.rectTransform.anchoredPosition = new Vector2(13f * Px, 32f * Px);
        led.rectTransform.sizeDelta = new Vector2(3f * Px, 2f * Px);
        led.color = new Color(0.86f, 0.16f, 0.14f, 0f);
        led.raycastTarget = false;

        GameObject antGo = new GameObject("Antena", typeof(RectTransform), typeof(Image));
        antGo.layer = gameObject.layer;
        antGo.transform.SetParent(radio, false);
        antenna = antGo.GetComponent<RectTransform>();
        antenna.anchorMin = antenna.anchorMax = new Vector2(0f, 0f);
        antenna.pivot = new Vector2(0.5f, 0f);
        antenna.anchoredPosition = new Vector2(7.5f * Px, 37f * Px);
        antenna.sizeDelta = new Vector2(Px, 14f * Px);
        Image antImg = antGo.GetComponent<Image>();
        antImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        antImg.raycastTarget = false;
    }

    // Balanca para cima e para baixo (ritmo de fala), inclina um pouco, LED pisca e antena treme.
    private void AnimateRadio(bool talking)
    {
        if (radio == null) return;
        float t = Time.time;
        float bob = talking ? (Mathf.Sin(t * 9f) * 5f + Mathf.Sin(t * 23f) * 2f) : 0f;
        radio.anchoredPosition = new Vector2(RadioX, RadioY + bob);
        radio.localRotation = Quaternion.Euler(0f, 0f, talking ? Mathf.Sin(t * 6f) * 4f : 0f);
        if (led != null)
        {
            bool on = !talking || !FlashSettings.Enabled || Mathf.Repeat(t * 5f, 1f) < 0.55f;
            led.color = on ? new Color(0.9f, 0.18f, 0.15f, alpha) : new Color(0.3f, 0.05f, 0.04f, alpha);
        }
        if (antenna != null)
        {
            antenna.localRotation = Quaternion.Euler(0f, 0f, talking ? Mathf.Sin(t * 17f) * 6f : 0f);
            antenna.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, alpha);
        }
    }

    // Radio 22x38: corpo cinza com contorno, visor escuro, grade de alto-falante e botoes.
    private static Texture2D RadioTexture()
    {
        const int w = 22, h = 38;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "RadioArte" };
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0.07f, 0.07f, 0.07f, 1f);
        Color body = new Color(0.55f, 0.55f, 0.53f, 1f);
        Color light = new Color(0.72f, 0.72f, 0.7f, 1f);
        Color dark = new Color(0.36f, 0.36f, 0.35f, 1f);
        Color screen = new Color(0.12f, 0.14f, 0.13f, 1f);
        Color screenLine = new Color(0.55f, 0.62f, 0.58f, 1f);
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = clear;
        void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c; }
        void Rect(int x0, int y0, int x1, int y1, Color c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Set(x, y, c); }

        // Corpo (cantos arredondados) com luz a esquerda e sombra a direita.
        Rect(2, 0, 19, 35, outline);
        Rect(3, 1, 18, 34, body);
        Rect(3, 1, 4, 34, light);
        Rect(17, 1, 18, 34, dark);
        Set(2, 0, clear); Set(19, 0, clear); Set(2, 35, clear); Set(19, 35, clear);
        // Base da antena.
        Rect(5, 35, 9, 37, outline);
        Rect(6, 35, 8, 36, dark);
        // Visor.
        Rect(5, 22, 16, 30, outline);
        Rect(6, 23, 15, 29, screen);
        for (int x = 7; x <= 14; x += 2) Set(x, 26, screenLine);
        Set(8, 27, screenLine); Set(12, 25, screenLine);
        // Grade do alto-falante.
        for (int y = 6; y <= 18; y += 2) Rect(6, y, 15, y, dark);
        // Botoes laterais.
        Rect(0, 20, 1, 26, outline);
        Rect(20, 14, 21, 18, outline);
        // Botao de falar.
        Rect(8, 2, 13, 4, outline);
        Rect(9, 3, 12, 3, light);

        tex.SetPixels(px);
        tex.Apply(false);
        return tex;
    }
}
