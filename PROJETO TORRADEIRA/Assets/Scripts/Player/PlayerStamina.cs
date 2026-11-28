using UnityEngine;
using UnityEngine.UI;

// Folego do jogador: correr gasta, andar/parar recupera (parado recupera mais rapido).
// Esgotou: so volta a correr depois de recuperar parte do folego, e ofega.
// Barra discreta no HUD (pixel, preto e branco), escondida quando cheia.
public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private float max = 100f;
    [SerializeField] private float drainPerSecond = 16f;      // ~6 s de corrida
    [SerializeField] private float regenWalking = 9f;
    [SerializeField] private float regenStill = 18f;
    [SerializeField] private float regenDelay = 0.8f;          // espera depois de parar de correr
    [SerializeField, Range(0f, 1f)] private float recoverToRun = 0.3f;

    public static PlayerStamina Instance { get; private set; }
    public float Normalized => current / max;
    public bool Exhausted { get; private set; }

    // Lido pelo PlayerMovement: pode correr agora?
    public static bool CanSprint => Instance == null || (!Instance.Exhausted && Instance.current > 0f);

    private float current;
    private float lastSprint = -99f;
    private PlayerMovement movement;
    private AudioSource breath;
    private static AudioClip pant;

    [SerializeField, HideInInspector] private RectTransform barRoot;
    [SerializeField, HideInInspector] private Image fill;
    [SerializeField, HideInInspector] private CanvasGroup group;

    private void Awake()
    {
        Instance = this;
        current = max;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        movement = GetComponent<PlayerMovement>();
        breath = gameObject.AddComponent<AudioSource>();
        breath.playOnAwake = false;
        breath.spatialBlend = 0f;
        breath.volume = 0.55f;
        if (pant == null) pant = BuildPant();
        if (fill == null)
            BuildBar();
    }

    private void Update()
    {
        if (PauseMenu.IsPaused || DefeatController.IsDefeated)
            return;

        float dt = Time.deltaTime;
        bool sprinting = movement != null && movement.IsSprinting && movement.MoveBlend > 0.2f;

        if (sprinting)
        {
            current -= drainPerSecond * dt;
            lastSprint = Time.time;
            if (current <= 0f)
            {
                current = 0f;
                Exhausted = true;
                breath.pitch = Random.Range(0.95f, 1.05f);
                breath.PlayOneShot(pant, 1f);
            }
        }
        else if (Time.time - lastSprint > regenDelay)
        {
            bool still = movement == null || movement.MoveBlend < 0.1f;
            current = Mathf.Min(max, current + (still ? regenStill : regenWalking) * dt);
        }

        if (Exhausted && current >= max * recoverToRun)
            Exhausted = false;

        UpdateBar(dt);
    }

    // ---------- Barra ----------

    public void BuildBar()
    {
        if (barRoot != null)
            SceneBake.Kill(barRoot.gameObject);
        GameObject hud = GameObject.Find("HUDCanvas");
        if (hud == null)
            return;

        GameObject root = new GameObject("StaminaBar", typeof(RectTransform), typeof(CanvasGroup));
        root.layer = hud.layer;
        barRoot = root.GetComponent<RectTransform>();
        barRoot.SetParent(hud.transform, false);
        barRoot.anchorMin = barRoot.anchorMax = new Vector2(0.5f, 0f);
        barRoot.pivot = new Vector2(0.5f, 0f);
        barRoot.sizeDelta = new Vector2(244f, 14f);
        barRoot.anchoredPosition = new Vector2(0f, 26f);
        group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        Image frame = NewImage("Frame", barRoot, new Color(0.06f, 0.06f, 0.06f, 0.85f));
        Stretch(frame.rectTransform, 0f);
        Image border = NewImage("Border", barRoot, new Color(0.75f, 0.75f, 0.73f, 0.9f));
        Stretch(border.rectTransform, 0f);
        Image inner = NewImage("Inner", barRoot, new Color(0.06f, 0.06f, 0.06f, 1f));
        Stretch(inner.rectTransform, 2f);
        fill = NewImage("Fill", inner.rectTransform, new Color(0.88f, 0.88f, 0.86f, 1f));
        RectTransform fr = fill.rectTransform;
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(1f, 1f);
        fr.pivot = new Vector2(0f, 0.5f);
        fr.offsetMin = new Vector2(2f, 2f);
        fr.offsetMax = new Vector2(-2f, -2f);

        // Marcas a cada 10% (aparencia de aparelho).
        for (int i = 1; i < 10; i++)
        {
            Image tick = NewImage("Tick", inner.rectTransform, new Color(0.06f, 0.06f, 0.06f, 0.9f));
            RectTransform tr = tick.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(i / 10f, 0.5f);
            tr.sizeDelta = new Vector2(2f, 10f);
        }
    }

    private void UpdateBar(float dt)
    {
        if (fill == null)
            return;

        float n = Normalized;
        fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, n), 1f);

        // Some quando cheia; esgotada pisca.
        float targetAlpha = n >= 0.999f && !Exhausted ? 0f : 1f;
        // Nao fica por cima do album, do menu de pausa nem do visor da camera.
        if (OverlayOpen())
        {
            targetAlpha = 0f;
            group.alpha = 0f;
        }
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, dt * (targetAlpha > 0f ? 6f : 1.5f));

        Color c = new Color(0.88f, 0.88f, 0.86f, 1f);
        if (Exhausted)
            c = Mathf.Repeat(Time.time * 3f, 1f) < 0.5f ? new Color(0.55f, 0.55f, 0.53f, 1f) : new Color(0.3f, 0.3f, 0.29f, 1f);
        fill.color = c;
    }

    private AlbumController album;
    private bool OverlayOpen()
    {
        if (PauseMenu.IsPaused || RadioMessageUI.CameraOpen) return true;
        if (album == null) album = FindFirstObjectByType<AlbumController>();
        return album != null && album.IsAlbumOpen;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void Stretch(RectTransform r, float inset)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }

    // Ofegar: tres respiracoes de ruido filtrado, cada vez mais fracas.
    private static AudioClip BuildPant()
    {
        const int rate = 44100;
        int n = (int)(rate * 1.8f);
        float[] data = new float[n];
        var rng = new System.Random(5);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float cycle = Mathf.Repeat(t, 0.6f) / 0.6f;
            float env = Mathf.Sin(Mathf.Clamp01(cycle / 0.7f) * Mathf.PI) * (1f - t / 2.2f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * (cycle < 0.35f ? 0.25f : 0.12f);
            data[i] = lp * env * 0.6f;
        }
        AudioClip clip = AudioClip.Create("Pant", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
