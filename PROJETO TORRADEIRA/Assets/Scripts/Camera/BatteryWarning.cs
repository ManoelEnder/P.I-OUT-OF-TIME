using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Aviso de sem bateria: so o icone de pilha vermelha riscada, perto do centro da tela. Aparece quando
// a bateria zera e toda vez que o jogador tenta abrir a camera (ou ligar a lanterna) sem carga,
// com um bipe de recusa. Com flashes desligados, nao pisca.
public class BatteryWarning : MonoBehaviour
{
    private static BatteryWarning instance;
    private static AudioClip denied;

    private CanvasGroup group;
    private RectTransform icon;
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
        // So o icone: pilha vermelha com um risco diagonal atravessando (sem texto, sem fundo).
        RectTransform root = (RectTransform)transform;
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, -110f);
        root.sizeDelta = new Vector2(120f, 70f);
        root.localScale = Vector3.one * 2f;
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        Color red = new Color(0.86f, 0.18f, 0.16f, 1f);
        Color dark = new Color(0.05f, 0.02f, 0.02f, 0.9f);
        icon = root;
        // Sombra (contorno escuro) e pilha vermelha por cima.
        Frame(root, new Vector2(-2f, -2f), new Vector2(92f, 52f), 9f, dark);
        Frame(root, new Vector2(-4f, 0f), new Vector2(88f, 48f), 6f, red);
        Image pole = NewImage("Polo", root, red);
        pole.rectTransform.anchoredPosition = new Vector2(44f, 0f);
        pole.rectTransform.sizeDelta = new Vector2(9f, 20f);
        // Risco diagonal (pixelado: degraus pequenos).
        for (int k = 0; k < 13; k++)
        {
            Vector2 p = new Vector2(-50f + k * 8.4f, 28f - k * 4.7f);
            Image sh = NewImage("RiscoSombra", root, dark);
            sh.rectTransform.anchoredPosition = p + new Vector2(2f, -2f);
            sh.rectTransform.sizeDelta = new Vector2(10f, 9f);
        }
        for (int k = 0; k < 13; k++)
        {
            Vector2 p = new Vector2(-50f + k * 8.4f, 28f - k * 4.7f);
            Image st = NewImage("Risco", root, red);
            st.rectTransform.anchoredPosition = p;
            st.rectTransform.sizeDelta = new Vector2(9f, 7f);
        }

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        if (denied == null) denied = BuildDenied();
    }

    private void Trigger(bool attempted)
    {
        shownAt = Time.unscaledTime;
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
        // Pequeno tranco ao aparecer e pisca (sem piscar com flashes desligados).
        float shake = t < 0.25f ? Mathf.Sin(t * 80f) * 6f * (1f - t / 0.25f) : 0f;
        ((RectTransform)transform).anchoredPosition = new Vector2(shake, -110f);
        bool blinkOn = !FlashSettings.Enabled || Mathf.Repeat(t * 3f, 1f) < 0.65f;
        group.alpha = Mathf.MoveTowards(group.alpha, blinkOn ? 1f : 0.35f, Time.unscaledDeltaTime * 14f);
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
