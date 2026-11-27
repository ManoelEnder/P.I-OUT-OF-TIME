using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Efeito de camera de 1975 no HUD do visor (so aparece com o visor aberto, pois fica dentro dele):
// - linhas de varredura, granulado de fita, faixa clara rolando e vinheta;
// - "[CAM] MODO TEMPORAL" vira um cabecalho de gravacao: ponto vermelho piscando, REC e o tempo
//   desde que o visor abriu; embaixo "MODO TEMPORAL // LAB 1975" e a leitura de zoom;
// - o contador "Fotos: N" sai do visor;
// - mensagens curtas no centro (ex.: album cheio).
public class CameraHudFx : MonoBehaviour
{
    private static CameraHudFx instance;

    private PhotoCamera photo;
    private RectTransform hud;
    private RawImage scan, noise, band;
    private Image recDot;
    private TextMeshProUGUI recText, modeText, zoomText, message;
    private Texture2D noiseTex;
    private float openedAt;
    private bool wasOpen;
    private float messageUntil;

    public static void Message(string text)
    {
        if (instance == null || instance.message == null) return;
        instance.message.text = text;
        instance.messageUntil = Time.unscaledTime + 2.6f;
    }

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (noiseTex != null) Destroy(noiseTex);
    }

    private void Start()
    {
        photo = FindFirstObjectByType<PhotoCamera>();
        if (photo == null || photo.cameraHUD == null)
        {
            enabled = false;
            return;
        }
        RectTransform hudRoot = photo.cameraHUD.GetComponent<RectTransform>();
        Canvas canvas = photo.cameraHUD.GetComponentInParent<Canvas>();
        if (hudRoot == null || canvas == null)
        {
            enabled = false;
            return;
        }
        // Camada do tamanho da tela inteira, dentro do HUD do visor (o objeto do HUD tem tamanho zero).
        RectTransform canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        hud = new GameObject("EfeitoCamera", typeof(RectTransform)).GetComponent<RectTransform>();
        hud.gameObject.layer = hudRoot.gameObject.layer;
        hud.SetParent(hudRoot, false);
        hud.anchorMin = hud.anchorMax = new Vector2(0.5f, 0.5f);
        hud.pivot = new Vector2(0.5f, 0.5f);
        hud.position = canvasRect.position;
        hud.sizeDelta = canvasRect.rect.size;
        hud.SetAsFirstSibling();

        TMP_FontAsset font = null;
        foreach (TMP_Text t in photo.cameraHUD.GetComponentsInChildren<TMP_Text>(true))
        {
            if (font == null) font = t.font;
            string s = t.text == null ? string.Empty : t.text.Trim();
            if (s.StartsWith("Fotos"))
                t.gameObject.SetActive(false);                 // tira o contador de fotos do visor
            else if (s.Contains("MODO TEMPORAL"))
                t.gameObject.SetActive(false);                 // substituido pelo cabecalho novo
        }

        // ---- Efeitos de tela (atras dos elementos do HUD) ----
        Texture2D lines = new Texture2D(1, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        lines.SetPixels(new[] { new Color(0f, 0f, 0f, 0.12f), Color.clear, Color.clear });
        lines.Apply();
        scan = Raw("Varredura", lines);
        scan.uvRect = new Rect(0f, 0f, 1f, 240f);

        noiseTex = new Texture2D(192, 108, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        noise = Raw("Granulado", noiseTex);
        noise.color = new Color(1f, 1f, 1f, 0.5f);

        Texture2D bandTex = new Texture2D(1, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++)
            bandTex.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Sin(y / 31f * Mathf.PI) * 0.025f));
        bandTex.Apply();
        band = Raw("FaixaRolando", bandTex);
        band.rectTransform.anchorMin = new Vector2(0f, 0f);
        band.rectTransform.anchorMax = new Vector2(1f, 0f);
        band.rectTransform.sizeDelta = new Vector2(0f, 40f);

        Texture2D vig = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                vig.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp01((d - 0.55f) * 1.1f) * 0.55f));
            }
        vig.Apply();
        Raw("Vinheta", vig);

        // ---- Cabecalho de gravacao ----
        RectTransform header = new GameObject("Gravacao", typeof(RectTransform)).GetComponent<RectTransform>();
        header.SetParent(hud, false);
        header.anchorMin = header.anchorMax = new Vector2(0f, 1f);
        header.pivot = new Vector2(0f, 1f);
        header.anchoredPosition = new Vector2(40f, -34f);
        header.sizeDelta = new Vector2(400f, 40f);

        recDot = new GameObject("PontoRec", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        recDot.transform.SetParent(header, false);
        recDot.rectTransform.anchorMin = recDot.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        recDot.rectTransform.anchoredPosition = new Vector2(10f, 0f);
        recDot.rectTransform.sizeDelta = new Vector2(14f, 14f);
        recDot.color = new Color(0.86f, 0.16f, 0.14f);
        recDot.raycastTarget = false;
        recText = Text(header, font, 24f, new Vector2(28f, 0f), TextAlignmentOptions.Left, new Color(0.93f, 0.93f, 0.9f));

        RectTransform footer = new GameObject("Modo", typeof(RectTransform)).GetComponent<RectTransform>();
        footer.SetParent(hud, false);
        footer.anchorMin = footer.anchorMax = new Vector2(0f, 0f);
        footer.pivot = new Vector2(0f, 0f);
        footer.anchoredPosition = new Vector2(40f, 58f);
        footer.sizeDelta = new Vector2(520f, 30f);
        modeText = Text(footer, font, 20f, Vector2.zero, TextAlignmentOptions.Left, new Color(0.93f, 0.93f, 0.9f, 0.85f));
        modeText.characterSpacing = 6f;
        modeText.text = "MODO TEMPORAL  <color=#D23A32>//</color>  LAB 1975";

        RectTransform zoomRect = new GameObject("Zoom", typeof(RectTransform)).GetComponent<RectTransform>();
        zoomRect.SetParent(hud, false);
        zoomRect.anchorMin = zoomRect.anchorMax = new Vector2(1f, 0f);
        zoomRect.pivot = new Vector2(1f, 0f);
        zoomRect.anchoredPosition = new Vector2(-40f, 58f);
        zoomRect.sizeDelta = new Vector2(260f, 30f);
        zoomText = Text(zoomRect, font, 20f, Vector2.zero, TextAlignmentOptions.Right, new Color(0.93f, 0.93f, 0.9f, 0.85f));

        message = Text(hud, font, 22f, new Vector2(0f, -150f), TextAlignmentOptions.Center, new Color(0.95f, 0.95f, 0.92f));
        message.rectTransform.anchorMin = message.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        message.rectTransform.sizeDelta = new Vector2(1100f, 40f);
        message.alpha = 0f;
    }

    private void Update()
    {
        if (photo == null || hud == null || recDot == null)
            return;

        bool open = photo.IsCameraMode;
        if (open && !wasOpen) openedAt = Time.time;
        wasOpen = open;
        if (!hud.gameObject.activeInHierarchy)
            return;

        float t = Time.time - openedAt;
        int secs = Mathf.FloorToInt(t);
        bool blink = !FlashSettings.Enabled || Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f;
        recDot.enabled = blink;
        recText.text = "REC  " + (secs / 3600).ToString("00") + ":" + (secs / 60 % 60).ToString("00") + ":" + (secs % 60).ToString("00");
        zoomText.text = "ZOOM " + Mathf.Lerp(1f, 3f, photo.ZoomAmount).ToString("0.0") + "x";

        // Faixa clara rolando de baixo para cima e granulado novo a cada poucos quadros.
        RectTransform parent = (RectTransform)band.rectTransform.parent;
        float h = parent.rect.height;
        band.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Repeat(Time.time * 70f, h + 90f) - 45f);
        if (Time.frameCount % 3 == 0)
        {
            Color32[] px = noiseTex.GetPixels32();
            for (int i = 0; i < px.Length; i++)
                px[i] = Random.value < 0.0025f ? new Color32(220, 220, 220, 150) : new Color32(0, 0, 0, 0);
            noiseTex.SetPixels32(px);
            noiseTex.Apply(false);
        }

        if (message != null)
            message.alpha = Mathf.MoveTowards(message.alpha, Time.unscaledTime < messageUntil ? 1f : 0f, Time.unscaledDeltaTime * 4f);
    }

    private RawImage Raw(string name, Texture tex)
    {
        RawImage img = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        img.gameObject.layer = hud.gameObject.layer;
        img.transform.SetParent(hud, false);
        img.transform.SetAsFirstSibling();
        RectTransform r = img.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        img.texture = tex;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI Text(RectTransform parent, TMP_FontAsset font, float size, Vector2 pos, TextAlignmentOptions align, Color color)
    {
        TextMeshProUGUI t = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.gameObject.layer = parent.gameObject.layer;
        t.transform.SetParent(parent, false);
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        RectTransform r = t.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.anchoredPosition = pos;
        return t;
    }
}
