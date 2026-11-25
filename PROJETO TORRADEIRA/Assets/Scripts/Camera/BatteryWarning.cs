using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Aviso de "SEM BATERIA" no centro da tela: aparece quando a bateria zera e toda vez que o jogador
// tenta abrir a camera (ou ligar a lanterna) sem carga. Icone de pilha vazia em pixel art + bipe
// de recusa. Com flashes desligados, nao pisca (so aparece e some).
public class BatteryWarning : MonoBehaviour
{
    private static BatteryWarning instance;
    private static AudioClip denied;

    private CanvasGroup group;
    private TextMeshProUGUI title, sub;
    private Image[] cells;
    private AudioSource source;
    private float shownAt = -99f;
    private const float Duration = 2.4f;

    public static void Show(bool attempted)
    {
        if (instance == null)
        {
            GameObject hud = GameObject.Find("HUDCanvas");
            if (hud == null) return;
            GameObject go = new GameObject("BatteryWarning", typeof(RectTransform), typeof(CanvasGroup));
            go.layer = hud.layer;
            go.transform.SetParent(hud.transform, false);
            instance = go.AddComponent<BatteryWarning>();
            instance.Build(hud);
        }
        instance.Trigger(attempted);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Build(GameObject hud)
    {
        RectTransform root = (RectTransform)transform;
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, -120f);
        root.sizeDelta = new Vector2(520f, 150f);
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        Image backdrop = NewImage("Fundo", root, new Color(0f, 0f, 0f, 0.6f));
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;

        // Pilha vazia: contorno, polo e 4 celulas (todas apagadas, a primeira pisca em vermelho).
        Color line = new Color(0.9f, 0.9f, 0.88f, 1f);
        RectTransform icon = new GameObject("Pilha", typeof(RectTransform)).GetComponent<RectTransform>();
        icon.SetParent(root, false);
        icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
        icon.anchoredPosition = new Vector2(70f, 0f);
        icon.sizeDelta = new Vector2(96f, 48f);
        Frame(icon, new Vector2(-4f, 0f), new Vector2(88f, 48f), 5f, line);
        Image pole = NewImage("Polo", icon, line);
        pole.rectTransform.anchoredPosition = new Vector2(46f, 0f);
        pole.rectTransform.sizeDelta = new Vector2(8f, 20f);
        cells = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            cells[i] = NewImage("Celula", icon, new Color(0.18f, 0.18f, 0.18f, 1f));
            cells[i].rectTransform.anchoredPosition = new Vector2(-34f + i * 19f, 0f);
            cells[i].rectTransform.sizeDelta = new Vector2(14f, 30f);
        }

        TMP_Text any = hud.GetComponentInChildren<TMP_Text>(true);
        title = NewText(root, "SEM BATERIA", 34f, new Color(0.86f, 0.2f, 0.18f), new Vector2(60f, 22f), any);
        sub = NewText(root, "Procure pilhas pelo laboratório", 18f, new Color(0.85f, 0.85f, 0.82f, 0.85f), new Vector2(60f, -24f), any);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        if (denied == null) denied = BuildDenied();
    }

    private void Trigger(bool attempted)
    {
        shownAt = Time.unscaledTime;
        sub.text = attempted ? "A câmera não liga  //  procure pilhas" : "Procure pilhas pelo laboratório";
        source.PlayOneShot(denied, 0.7f);
        transform.SetAsLastSibling();
    }

    private void Update()
    {
        float t = Time.unscaledTime - shownAt;
        if (t > Duration)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime * 4f);
            return;
        }
        group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime * 10f);
        // Pequeno tranco ao aparecer.
        float shake = t < 0.25f ? Mathf.Sin(t * 80f) * 6f * (1f - t / 0.25f) : 0f;
        ((RectTransform)transform).anchoredPosition = new Vector2(shake, -120f);

        bool blinkOn = !FlashSettings.Enabled || Mathf.Repeat(t * 3f, 1f) < 0.6f;
        cells[0].color = blinkOn ? new Color(0.86f, 0.2f, 0.18f, 1f) : new Color(0.25f, 0.06f, 0.05f, 1f);
        title.alpha = blinkOn ? 1f : 0.45f;
    }

    private static void Frame(RectTransform parent, Vector2 center, Vector2 size, float thick, Color c)
    {
        Image top = NewImage("Borda", parent, c); top.rectTransform.anchoredPosition = center + new Vector2(0f, size.y / 2f - thick / 2f); top.rectTransform.sizeDelta = new Vector2(size.x, thick);
        Image bot = NewImage("Borda", parent, c); bot.rectTransform.anchoredPosition = center - new Vector2(0f, size.y / 2f - thick / 2f); bot.rectTransform.sizeDelta = new Vector2(size.x, thick);
        Image l = NewImage("Borda", parent, c); l.rectTransform.anchoredPosition = center - new Vector2(size.x / 2f - thick / 2f, 0f); l.rectTransform.sizeDelta = new Vector2(thick, size.y);
        Image r = NewImage("Borda", parent, c); r.rectTransform.anchoredPosition = center + new Vector2(size.x / 2f - thick / 2f, 0f); r.rectTransform.sizeDelta = new Vector2(thick, size.y);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        Image img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.gameObject.layer = parent.gameObject.layer;
        img.transform.SetParent(parent, false);
        img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI NewText(RectTransform parent, string text, float size, Color color, Vector2 pos, TMP_Text fontFrom)
    {
        TextMeshProUGUI t = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.gameObject.layer = parent.gameObject.layer;
        t.transform.SetParent(parent, false);
        if (fontFrom != null) t.font = fontFrom.font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Left;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        t.rectTransform.sizeDelta = new Vector2(360f, 40f);
        t.rectTransform.anchoredPosition = pos + new Vector2(40f, 0f);
        return t;
    }

    private static AudioClip BuildDenied()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.42f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float local = t < 0.18f ? t : t - 0.22f;
            bool on = t < 0.18f || (t > 0.22f && t < 0.4f);
            float f = t < 0.2f ? 330f : 220f;
            d[i] = on ? (Mathf.Sin(local * 2f * Mathf.PI * f) >= 0f ? 1f : -1f) * 0.3f * Mathf.Min(1f, local / 0.005f) * (1f - Mathf.Clamp01(local / 0.18f) * 0.6f) : 0f;
        }
        AudioClip c = AudioClip.Create("BatteryDenied", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
