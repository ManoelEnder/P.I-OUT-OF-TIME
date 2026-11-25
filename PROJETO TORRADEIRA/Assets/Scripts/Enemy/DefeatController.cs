using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Derrota: jumpscare (o Apagador avanca no rosto com grito alto e estatica) e depois a tela
// "GRAVACAO INTERROMPIDA". Tentar de novo recarrega a fase inteira (estado limpo).
public class DefeatController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject defeatPanel;

    [Header("Player")]
    [SerializeField] private GameObject player;

    [Header("Respawn")]
    [SerializeField] private Transform currentSpawnPoint;

    [Header("Scene")]
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Events")]
    [SerializeField] private UnityEvent onRetry;

    [Header("Jumpscare")]
    [SerializeField] private float jumpscareSeconds = 1.35f;
    [SerializeField, Range(0f, 1f)] private float screamVolume = 1f;

    private bool isDefeated;
    private DefeatScreenView view;

    // Outros scripts (movimento, camera) param enquanto a derrota acontece.
    public static bool IsDefeated { get; private set; }

    private void Start()
    {
        IsDefeated = false;

        if (defeatPanel != null)
        {
            view = defeatPanel.GetComponent<DefeatScreenView>();
            if (view == null)
                view = defeatPanel.AddComponent<DefeatScreenView>();
            view.Init(this);
            defeatPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (isDefeated)
        {
            IsDefeated = false;
            Time.timeScale = 1f;
        }
    }

    public void TriggerDefeat()
    {
        if (isDefeated || PauseMenu.IsPaused)
            return;

        isDefeated = true;
        IsDefeated = true;
        StartCoroutine(Jumpscare());
    }

    private IEnumerator Jumpscare()
    {
        Camera cam = Camera.main;
        TemporalEraserController enemy = FindFirstObjectByType<TemporalEraserController>();

        // Fecha o visor e esconde o HUD na hora.
        PhotoCamera photo = FindFirstObjectByType<PhotoCamera>();
        if (photo != null)
            photo.TryExitCameraMode();

        yield return JumpscareSequence.Play(this, cam, enemy, screamVolume);

        ShowDefeatPanel();
        // O preto do circulo some por cima da tela de derrota (que tambem e escura): sem piscar o jogo.
        yield return JumpscareSequence.FadeOutOverlay();
    }

    private void ShowDefeatPanel()
    {
        if (defeatPanel != null)
            defeatPanel.SetActive(true);
        if (view != null)
            view.Show();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Recarrega a fase: inimigo, pecas, bateria e missoes voltam ao comeco sem estado preso.
    public void Retry()
    {
        Time.timeScale = 1f;
        isDefeated = false;
        IsDefeated = false;
        onRetry?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        isDefeated = false;
        IsDefeated = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(menuSceneName);
    }

    public void SetSpawnPoint(Transform spawnPoint)
    {
        currentSpawnPoint = spawnPoint;
    }
}

// Tela de derrota montada em cima do DefeatPanel da cena (reaproveita os botoes RetryButton e MenuButton).
public class DefeatScreenView : MonoBehaviour
{
    private static readonly Color Ink = Color.white;
    private static readonly Color Dim = new Color(0.55f, 0.55f, 0.53f, 1f);
    private static readonly Color Accent = new Color(0.85f, 0.12f, 0.12f, 1f);

    private DefeatController owner;
    private CanvasGroup group;
    private RectTransform content;
    private TextMeshProUGUI title;
    private TextMeshProUGUI stats;
    private RawImage noise;
    private Texture2D noiseTex;
    private float shownAt;

    public void Init(DefeatController controller)
    {
        owner = controller;
        RectTransform root = (RectTransform)transform;

        TMP_FontAsset font = null;
        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>(true))
        {
            font = t.font;
            break;
        }

        Button retry = FindButton("RetryButton");
        Button menu = FindButton("MenuButton");

        // Esconde o layout antigo (menos os botoes, que mudam de lugar).
        foreach (Transform child in root)
            child.gameObject.SetActive(false);

        Image bg = GetComponent<Image>();
        if (bg == null)
            bg = gameObject.AddComponent<Image>();
        bg.sprite = null;
        bg.color = new Color(0.02f, 0.02f, 0.02f, 0.97f);

        group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        // Estatica de fita ao fundo.
        noiseTex = new Texture2D(160, 90, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        noise = NewRaw(root, "Static");
        noise.texture = noiseTex;
        noise.color = new Color(1f, 1f, 1f, 0.07f);
        Stretch(noise.rectTransform);

        content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(root, false);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(900f, 520f);

        // REC parado e titulo.
        TextMeshProUGUI rec = NewText(content, "Rec", font, "REC  00:00:00  //  FITA 01", 20f, Accent, new Vector2(12f, 200f));
        rec.alignment = TextAlignmentOptions.Center;
        Image dot = NewImage(content, "RecDot", Accent);
        dot.rectTransform.sizeDelta = new Vector2(12f, 12f);
        dot.rectTransform.anchoredPosition = new Vector2(-150f, 200f);
        title = NewText(content, "Title", font, "GRAVAÇÃO INTERROMPIDA", 64f, Ink, new Vector2(0f, 120f));
        title.alignment = TextAlignmentOptions.Center;

        Image line = NewImage(content, "Line", Accent);
        line.rectTransform.sizeDelta = new Vector2(520f, 3f);
        line.rectTransform.anchoredPosition = new Vector2(0f, 72f);

        TextMeshProUGUI sub = NewText(content, "Subtitle", font, "O APAGADOR TE ALCANÇOU. O LABORATÓRIO FICOU NO ESCURO.", 22f, Dim, new Vector2(0f, 40f));
        sub.alignment = TextAlignmentOptions.Center;

        stats = NewText(content, "Stats", font, "", 22f, Dim, new Vector2(0f, -10f));
        stats.alignment = TextAlignmentOptions.Center;

        PlaceButton(retry, owner.Retry, "REBOBINAR E TENTAR DE NOVO", font, new Vector2(0f, -100f));
        PlaceButton(menu, owner.GoToMenu, "VOLTAR AO MENU", font, new Vector2(0f, -160f));
    }

    private Button FindButton(string name)
    {
        Canvas canvas = GetComponentInParent<Canvas>(true);
        Component scope = canvas != null ? (Component)canvas : this;
        foreach (Button b in scope.GetComponentsInChildren<Button>(true))
            if (b.name == name)
                return b;
        return null;
    }

    private void PlaceButton(Button button, UnityAction action, string label, TMP_FontAsset font, Vector2 position)
    {
        if (button == null)
        {
            button = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        }

        RectTransform rect = (RectTransform)button.transform;
        rect.SetParent(content, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 48f);
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        button.gameObject.SetActive(true);

        // Fundo invisivel (so area de clique), texto proprio e barra vermelha no hover.
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = null;
            image.color = new Color(1f, 1f, 1f, 0.001f);
        }
        foreach (Transform child in rect)
            child.gameObject.SetActive(false);

        TextMeshProUGUI text = NewText(rect, "Label", font, label, 30f, Dim, Vector2.zero);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = rect.sizeDelta;

        Image bar = NewImage(rect, "Marker", Accent);
        bar.rectTransform.sizeDelta = new Vector2(6f, 30f);
        bar.rectTransform.anchoredPosition = new Vector2(-250f, 0f);

        PauseMenuItem item = button.GetComponent<PauseMenuItem>();
        if (item == null)
            item = button.gameObject.AddComponent<PauseMenuItem>();
        item.Init(text, bar.rectTransform);

        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        // O botao da cena ja chama Retry/GoToMenu pelo evento salvo; so os criados aqui precisam do listener.
        if (button.onClick.GetPersistentEventCount() == 0)
            button.onClick.AddListener(action);
    }

    public void Show()
    {
        shownAt = Time.unscaledTime;
        MissionSystem missions = FindFirstObjectByType<MissionSystem>();
        if (stats != null && missions != null)
            stats.text = "PEÇAS " + missions.GetPieceCount() + "/" + missions.PiecesRequired +
                         "     FOTOS " + missions.GetPhotoCount() + "/" + missions.PhotosRequired;
        Update();
    }

    private void Update()
    {
        if (group == null)
            return;

        float t = Time.unscaledTime - shownAt;
        group.alpha = Mathf.Clamp01(t / 0.5f);
        if (content != null)
            content.anchoredPosition = new Vector2(0f, Mathf.Lerp(-24f, 0f, 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.6f), 3f)));

        // Titulo treme de leve, como fita gasta.
        if (title != null)
            title.rectTransform.anchoredPosition = new Vector2(Random.value < 0.06f ? Random.Range(-6f, 6f) : 0f, 120f);

        if (noiseTex != null && Time.frameCount % 3 == 0)
        {
            Color32[] px = noiseTex.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                byte v = (byte)Random.Range(0, 255);
                px[i] = new Color32(v, v, v, 255);
            }
            noiseTex.SetPixels32(px);
            noiseTex.Apply(false);
        }
    }

    private void OnDestroy()
    {
        if (noiseTex != null)
            Destroy(noiseTex);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static RawImage NewRaw(RectTransform parent, string name)
    {
        RawImage raw = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        raw.transform.SetParent(parent, false);
        raw.raycastTarget = false;
        return raw;
    }

    private static Image NewImage(RectTransform parent, string name, Color color)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI NewText(RectTransform parent, string name, TMP_FontAsset font, string value, float size, Color color, Vector2 position)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        if (font != null)
            text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.rectTransform.sizeDelta = new Vector2(900f, size * 1.4f);
        text.rectTransform.anchoredPosition = position;
        return text;
    }
}
