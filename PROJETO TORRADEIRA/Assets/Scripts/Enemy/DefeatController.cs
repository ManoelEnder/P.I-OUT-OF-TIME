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
    private static AudioClip scream;

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

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.priority = 0;
        source.ignoreListenerPause = true;
        if (scream == null)
            scream = BuildScream();
        source.PlayOneShot(scream, screamVolume);

        Image flash = CreateOverlay();

        if (cam != null && enemy != null)
        {
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;
            enemy.enabled = false;

            Animator animator = enemy.GetComponentInChildren<Animator>();
            if (animator != null)
                animator.SetFloat("Speed", 4.1f);

            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(enemy.transform.position, Vector3.one * 2f);
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);
            float faceHeight = Mathf.Max(0.5f, b.max.y - enemy.transform.position.y - 0.28f);

            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = cam.transform.up;
            forward.Normalize();

            float startFov = cam.fieldOfView;
            Vector3 camPos = cam.transform.position;
            Quaternion camRot = cam.transform.rotation;

            for (float t = 0f; t < jumpscareSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / 0.22f);
                float lunge = 1f - (1f - k) * (1f - k) * (1f - k);
                float dist = Mathf.Lerp(1.7f, 0.55f, lunge);

                Vector3 face = camPos + forward * dist;
                enemy.transform.position = face - Vector3.up * faceHeight;
                enemy.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);

                // Tremor forte na camera, olhando para o rosto.
                float shake = Mathf.Lerp(0.6f, 0.2f, t / jumpscareSeconds);
                Vector3 jitter = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * shake * 3f;
                cam.transform.rotation = Quaternion.LookRotation(face - camPos) * Quaternion.Euler(jitter);
                cam.fieldOfView = Mathf.Lerp(startFov, startFov * 0.8f, lunge);

                // Estatica e flashes (so escurece com flashes desligados).
                float strobe = FlashSettings.Enabled
                    ? (Random.value < 0.35f ? Random.Range(0.25f, 0.7f) : 0f)
                    : 0.15f;
                flash.color = FlashSettings.Enabled && Random.value < 0.5f
                    ? new Color(0.8f, 0.05f, 0.05f, strobe)
                    : new Color(0.9f, 0.9f, 0.9f, strobe * 0.6f);

                yield return null;
            }

            cam.fieldOfView = startFov;
            cam.transform.rotation = camRot;
        }
        else
        {
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
                yield return null;
        }

        // Corte seco para o preto.
        flash.color = Color.black;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            yield return null;

        Destroy(flash.canvas.gameObject);
        ShowDefeatPanel();
    }

    private Image CreateOverlay()
    {
        GameObject canvasGo = new GameObject("JumpscareOverlay", typeof(Canvas));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        GameObject img = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        img.transform.SetParent(canvasGo.transform, false);
        RectTransform rect = img.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = img.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;
        return image;
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

    // Grito: serra grave descendo + chiado agudo + ruido, saturado (alto e aspero).
    private static AudioClip BuildScream()
    {
        const int rate = 44100;
        const float seconds = 1.6f;
        int n = (int)(rate * seconds);
        float[] data = new float[n];
        var rng = new System.Random(66);
        float phaseA = 0f, phaseB = 0f, phaseC = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Min(1f, t / 0.015f) * Mathf.Clamp01((seconds - t) / 0.5f);
            float wobble = Mathf.Sin(t * 2f * Mathf.PI * 17f) * 30f;
            phaseA += (Mathf.Lerp(420f, 140f, t / seconds) + wobble) / rate;
            phaseB += (Mathf.Lerp(1900f, 1300f, t / seconds) + wobble * 4f) / rate;
            phaseC += 61f / rate;
            float saw = (phaseA % 1f) * 2f - 1f;
            float screech = Mathf.Sin(phaseB * 2f * Mathf.PI);
            float rumble = Mathf.Sin(phaseC * 2f * Mathf.PI);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float mix = saw * 0.6f + screech * 0.45f + noise * 0.5f + rumble * 0.5f;
            data[i] = (float)System.Math.Tanh(mix * 2.2f) * env * 0.95f;
        }
        AudioClip clip = AudioClip.Create("JumpscareScream", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
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
