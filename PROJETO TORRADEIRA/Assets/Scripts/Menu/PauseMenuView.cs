using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Visual e animacao do menu de pausa (o PauseMenu continua cuidando da logica):
// - coluna a esquerda com espacamento fixo (nada sobreposto), titulo grande com linha vermelha;
// - fundo em degrade escuro e o HUD do jogo some durante a pausa;
// - itens entram em cascata deslizando; troca de tela animada;
// - hover: a opcao desliza e acende uma barrinha vermelha.
// Tudo em tempo real (a pausa usa timeScale 0).
public class PauseMenuView : MonoBehaviour
{
    private static readonly Color Ink = Color.white;
    private static readonly Color Idle = new Color(0.66f, 0.66f, 0.64f, 1f);
    private static readonly Color Dim = new Color(0.5f, 0.5f, 0.48f, 1f);
    private static readonly Color Accent = new Color(0.85f, 0.12f, 0.12f, 1f);

    private const float Left = 120f;

    private GameObject pausePanel;
    private GameObject settingsPanel;
    private GameObject controlsPanel;

    private Canvas canvas;
    private CanvasGroup backdrop;
    private CanvasGroup hud;
    private readonly Dictionary<GameObject, bool> wasActive = new Dictionary<GameObject, bool>();
    private Coroutine backdropFade;
    private bool laidOut;

    public void Init(GameObject pause, GameObject settings, GameObject controls)
    {
        pausePanel = pause;
        settingsPanel = settings;
        controlsPanel = controls;
    }

    private void Start()
    {
        if (pausePanel == null)
            return;

        canvas = pausePanel.GetComponentInParent<Canvas>(true).rootCanvas;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject hudGo = GameObject.Find("HUDCanvas");
        if (hudGo != null)
        {
            hud = hudGo.GetComponent<CanvasGroup>();
            if (hud == null)
                hud = hudGo.AddComponent<CanvasGroup>();
        }

        BuildBackdrop();
        LayOut();

        foreach (GameObject panel in Panels())
            wasActive[panel] = panel.activeSelf;
    }

    private IEnumerable<GameObject> Panels()
    {
        if (pausePanel != null) yield return pausePanel;
        if (settingsPanel != null) yield return settingsPanel;
        if (controlsPanel != null) yield return controlsPanel;
    }

    // Percebe quando o PauseMenu liga/desliga cada tela e anima.
    private void LateUpdate()
    {
        if (!laidOut)
            return;

        bool anyOpen = false;

        foreach (GameObject panel in Panels())
        {
            bool active = panel.activeSelf;
            anyOpen |= active;

            if (active && !wasActive[panel])
            {
                StartCoroutine(Enter(panel));
                if (panel == pausePanel && tipText != null)
                    tipText.text = "DICA:  " + GameTips.Random();
            }

            wasActive[panel] = active;
        }

        SetBackdrop(anyOpen);
    }

    // ---------- Fundo e HUD ----------

    private void BuildBackdrop()
    {
        GameObject go = new GameObject("PauseBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(CanvasGroup));
        go.layer = canvas.gameObject.layer;
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling();

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        // Degrade: bem escuro a esquerda (onde fica o texto), mais leve a direita.
        var texture = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < 64; x++)
        {
            float a = Mathf.Lerp(0.88f, 0.45f, Mathf.SmoothStep(0f, 1f, x / 63f));
            texture.SetPixel(x, 0, new Color(0.02f, 0.02f, 0.025f, a));
        }
        texture.Apply();

        RawImage raw = go.GetComponent<RawImage>();
        raw.texture = texture;
        raw.raycastTarget = true;

        backdrop = go.GetComponent<CanvasGroup>();
        backdrop.alpha = 0f;
        backdrop.blocksRaycasts = false;
        go.SetActive(false);
    }

    private bool backdropShown;

    private void SetBackdrop(bool show)
    {
        if (show == backdropShown)
            return;

        backdropShown = show;
        if (backdropFade != null)
            StopCoroutine(backdropFade);
        backdropFade = StartCoroutine(FadeBackdrop(show));
    }

    private IEnumerator FadeBackdrop(bool show)
    {
        backdrop.gameObject.SetActive(true);
        backdrop.blocksRaycasts = show;
        float from = backdrop.alpha, to = show ? 1f : 0f;
        float hudFrom = hud != null ? hud.alpha : 1f, hudTo = show ? 0f : 1f;

        for (float t = 0f; t < 0.22f; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 0.22f);
            backdrop.alpha = Mathf.Lerp(from, to, k);
            if (hud != null)
                hud.alpha = Mathf.Lerp(hudFrom, hudTo, k);
            yield return null;
        }

        backdrop.alpha = to;
        if (hud != null)
            hud.alpha = hudTo;
        if (!show)
            backdrop.gameObject.SetActive(false);
        backdropFade = null;
    }

    private void OnDisable()
    {
        if (hud != null)
            hud.alpha = 1f;
    }

    // ---------- Layout ----------

    private void LayOut()
    {
        LayOutPause();
        LayOutSettings();
        LayOutControls();
        laidOut = true;
    }

    private void LayOutPause()
    {
        Transform terminal = pausePanel.transform.Find("Terminal");
        RectTransform root = terminal != null ? (RectTransform)terminal : (RectTransform)pausePanel.transform;

        Stretch(root);
        Image frame = root.GetComponent<Image>();
        if (frame != null)
            frame.color = new Color(0f, 0f, 0f, 0f);

        TextMeshProUGUI header = FindText(root, "Header");
        Title(root, header, "PAUSADO", "LAB 1975  //  FITA 01", 170f);

        Button[] buttons = pausePanel.GetComponentsInChildren<Button>(true);
        float y = 40f;
        foreach (Button button in buttons)
        {
            RectTransform rect = (RectTransform)button.transform;
            LeftAnchor(rect, new Vector2(Left, y), new Vector2(460f, 52f));
            y -= 60f;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 34f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }

            MakeItem(button, label);
        }

        // Dica (antes vinha pelo radio). Troca a cada vez que o jogo e pausado.
        tipText = NewText(root, "Dica", header != null ? header.font : null, "", 24f, Dim);
        tipText.textWrappingMode = TextWrappingModes.Normal;
        tipText.alignment = TextAlignmentOptions.TopLeft;
        LeftAnchor(tipText.rectTransform, new Vector2(Left, y - 30f), new Vector2(820f, 90f));
        tipText.rectTransform.pivot = new Vector2(0f, 1f);
    }

    private TextMeshProUGUI tipText;

    private void LayOutSettings()
    {
        if (settingsPanel == null)
            return;

        RectTransform root = (RectTransform)settingsPanel.transform;
        Stretch(root);

        Title(root, FindText(root, "Header"), "CONFIGURAÇÕES", null, 200f);

        float y = 70f;
        Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>(true);
        System.Array.Sort(sliders, (a, b) => ((RectTransform)b.transform).anchoredPosition.y.CompareTo(((RectTransform)a.transform).anchoredPosition.y));

        foreach (Slider slider in sliders)
        {
            RectTransform rect = (RectTransform)slider.transform;
            LeftAnchor(rect, new Vector2(Left + 330f, y), new Vector2(320f, 18f));

            foreach (TMP_Text text in slider.GetComponentsInChildren<TMP_Text>(true))
            {
                RectTransform t = text.rectTransform;
                if (text.name == "Name")
                {
                    // Nome a esquerda, alinhado com a coluna.
                    t.anchorMin = t.anchorMax = new Vector2(0f, 0.5f);
                    t.pivot = new Vector2(0f, 0.5f);
                    t.sizeDelta = new Vector2(300f, 40f);
                    t.anchoredPosition = new Vector2(-330f, 0f);
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.fontSize = 26f;
                }
                else
                {
                    // Porcentagem logo depois do slider.
                    t.anchorMin = t.anchorMax = new Vector2(1f, 0.5f);
                    t.pivot = new Vector2(0f, 0.5f);
                    t.sizeDelta = new Vector2(130f, 40f);
                    t.anchoredPosition = new Vector2(24f, 0f);
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.fontSize = 26f;
                }
            }

            y -= 70f;
        }

        Transform flashes = FindDeep(settingsPanel.transform, "Flashes");
        if (flashes != null)
        {
            RectTransform rect = (RectTransform)flashes;
            LeftAnchor(rect, new Vector2(Left + 330f, y), new Vector2(320f, 40f));

            TextMeshProUGUI name = FindText(rect, "Name");
            if (name != null)
            {
                RectTransform t = name.rectTransform;
                t.anchorMin = t.anchorMax = new Vector2(0f, 0.5f);
                t.pivot = new Vector2(0f, 0.5f);
                t.sizeDelta = new Vector2(300f, 40f);
                t.anchoredPosition = new Vector2(-330f, 0f);
                name.alignment = TextAlignmentOptions.MidlineLeft;
                name.fontSize = 26f;
            }

            TextMeshProUGUI value = FindText(rect, "Value");
            if (value != null)
                value.fontSize = 26f;

            y -= 70f;
        }

        // Linha de idioma (criada pelo SettingsMenu copiando a de Flashes).
        SettingsMenu settingsMenu = settingsPanel.GetComponentInChildren<SettingsMenu>(true);
        if (settingsMenu == null) settingsMenu = settingsPanel.GetComponentInParent<SettingsMenu>(true);
        if (settingsMenu == null) settingsMenu = FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include);
        if (settingsMenu != null) settingsMenu.EnsureLanguageRow();
        Transform language = FindDeep(settingsPanel.transform, "Idioma");
        if (language != null)
        {
            RectTransform rect = (RectTransform)language;
            LeftAnchor(rect, new Vector2(Left + 330f, y), new Vector2(320f, 40f));
            TextMeshProUGUI name = FindText(rect, "Name");
            if (name != null)
            {
                RectTransform t = name.rectTransform;
                t.anchorMin = t.anchorMax = new Vector2(0f, 0.5f);
                t.pivot = new Vector2(0f, 0.5f);
                t.sizeDelta = new Vector2(300f, 40f);
                t.anchoredPosition = new Vector2(-330f, 0f);
                name.alignment = TextAlignmentOptions.MidlineLeft;
                name.fontSize = 26f;
            }
            TextMeshProUGUI value = FindText(rect, "Value");
            if (value != null)
            {
                value.fontSize = 26f;
                value.textWrappingMode = TextWrappingModes.NoWrap;
            }
            y -= 70f;
        }

        BackHint(root, y - 30f);
    }

    private void LayOutControls()
    {
        if (controlsPanel == null)
            return;

        RectTransform root = (RectTransform)controlsPanel.transform;
        Stretch(root);

        Title(root, FindText(root, "Header"), "CONTROLES", null, 230f);

        TextMeshProUGUI list = FindText(root, "ControlsText");
        if (list != null)
        {
            LeftAnchor(list.rectTransform, new Vector2(Left, -10f), new Vector2(620f, 380f));
            list.alignment = TextAlignmentOptions.Left;
            list.fontSize = 22f;
            list.lineSpacing = 12f;
            list.text = list.text.Replace("<pos=130>", "<pos=170>");
            // Interagir agora e com o clique esquerdo (o mesmo clique tira foto com o visor aberto).
            if (!list.text.Contains("(visor)"))
                list.text = list.text.Replace("<color=#FFFFFF>E</color>", "<color=#FFFFFF>CLIQUE</color>")
                                     .Replace("Tirar foto", "Tirar foto (visor)");
            if (!list.text.Contains("Levantar radar"))
                list.text = list.text.Replace("<color=#FFFFFF>TAB</color>", "<color=#FFFFFF>Q</color><pos=170>Levantar radar\n<color=#FFFFFF>L</color><pos=170>Lanterna (com o visor)\n<color=#FFFFFF>TAB</color>");
        }

        BackHint(root, -240f);
    }

    private void Title(RectTransform root, TextMeshProUGUI header, string text, string sub, float y)
    {
        if (header == null)
            return;

        header.text = text;
        header.fontSize = 56f;
        header.color = Ink;
        header.alignment = TextAlignmentOptions.BottomLeft;
        LeftAnchor(header.rectTransform, new Vector2(Left, y), new Vector2(800f, 70f));

        Image line = NewImage(root, "TitleLine", Accent);
        LeftAnchor(line.rectTransform, new Vector2(Left, y - 42f), new Vector2(90f, 4f));

        if (!string.IsNullOrEmpty(sub))
        {
            TextMeshProUGUI subtitle = NewText(root, "Subtitle", header.font, sub, 20f, Dim);
            LeftAnchor(subtitle.rectTransform, new Vector2(Left, y - 72f), new Vector2(600f, 30f));
        }
    }

    private void BackHint(RectTransform root, float y)
    {
        TextMeshProUGUI hint = FindText(root, "BackHint");
        if (hint == null)
            return;

        hint.text = "[ESC]  VOLTAR";
        hint.fontSize = 22f;
        hint.color = Dim;
        hint.alignment = TextAlignmentOptions.MidlineLeft;
        LeftAnchor(hint.rectTransform, new Vector2(Left, y), new Vector2(400f, 34f));
    }

    // ---------- Animacao de entrada ----------

    private IEnumerator Enter(GameObject panel)
    {
        // Elementos de primeiro nivel da tela, de cima para baixo.
        var items = new List<RectTransform>();
        Transform root = panel.transform.Find("Terminal");
        if (root == null)
            root = panel.transform;
        foreach (Transform child in root)
        {
            if (child.gameObject.activeSelf && child is RectTransform rect)
                items.Add(rect);
        }
        items.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));

        var homes = new Vector2[items.Count];
        var groups = new CanvasGroup[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            homes[i] = items[i].anchoredPosition;
            groups[i] = items[i].GetComponent<CanvasGroup>();
            if (groups[i] == null)
                groups[i] = items[i].gameObject.AddComponent<CanvasGroup>();
            groups[i].alpha = 0f;
            items[i].anchoredPosition = homes[i] + new Vector2(-36f, 0f);
        }

        const float each = 0.22f, stagger = 0.035f;
        float total = each + stagger * items.Count;

        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;
                float k = Mathf.Clamp01((t - i * stagger) / each);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                items[i].anchoredPosition = homes[i] + new Vector2(-36f * (1f - e), 0f);
                groups[i].alpha = e;
            }
            yield return null;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;
            items[i].anchoredPosition = homes[i];
            groups[i].alpha = 1f;
        }
    }

    // ---------- Itens com hover ----------

    private void MakeItem(Button button, TMP_Text label)
    {
        // O "> " do ButtonHoverEffect da lugar a barrinha deslizante.
        ButtonHoverEffect old = button.GetComponent<ButtonHoverEffect>();
        if (old != null)
        {
            var prefix = typeof(ButtonHoverEffect).GetField("hoverPrefix", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prefix?.SetValue(old, "");
        }

        PauseMenuItem item = button.GetComponent<PauseMenuItem>();
        if (item == null)
            item = button.gameObject.AddComponent<PauseMenuItem>();
        Image marker = NewImage((RectTransform)button.transform, "Marker", Accent);
        RectTransform m = marker.rectTransform;
        m.anchorMin = m.anchorMax = new Vector2(0f, 0.5f);
        m.pivot = new Vector2(0f, 0.5f);
        m.sizeDelta = new Vector2(6f, 30f);
        m.anchoredPosition = new Vector2(-22f, 0f);
        item.Init(label, m);
    }

    // ---------- Utilitarios ----------

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void LeftAnchor(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static TextMeshProUGUI FindText(Transform root, string name)
    {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;
        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static Image NewImage(RectTransform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI NewText(RectTransform parent, string name, TMP_FontAsset font, string value, float size, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }
}

// Opcao do menu de pausa: no hover desliza para a direita e acende a barrinha vermelha.
public class PauseMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private TMP_Text label;
    private RectTransform marker;
    private Vector2 labelHome;
    private bool hovered;
    private float amount;

    public void Init(TMP_Text text, RectTransform bar)
    {
        label = text;
        marker = bar;
        if (label != null)
            labelHome = label.rectTransform.anchoredPosition;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; }
    public void OnSelect(BaseEventData eventData) { hovered = true; }
    public void OnDeselect(BaseEventData eventData) { hovered = false; }

    private void OnDisable()
    {
        hovered = false;
        amount = 0f;
        Apply();
    }

    private void Update()
    {
        float target = hovered ? 1f : 0f;
        if (Mathf.Approximately(amount, target))
            return;

        amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime * 7f);
        Apply();
    }

    private void Apply()
    {
        float e = 1f - Mathf.Pow(1f - amount, 3f);

        if (label != null)
            label.rectTransform.anchoredPosition = labelHome + new Vector2(16f * e, 0f);

        if (marker != null)
            marker.localScale = new Vector3(1f, e, 1f);
    }
}
