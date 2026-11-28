using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela de carregamento no visual do jogo: fita VHS tocando num deck (arte da cutscene),
// fonte VCR, contador de fita, barra em blocos e uma dica.
// Tudo e desenhado em 480x270 por uma camera propria (pixelado de verdade), com um
// Global Volume so dela (bloom, aberracao, grao, vinheta) e o shader CrtScreen por cima.
// Uso: LoadingScreen.Load("Principal");
public class LoadingScreen : MonoBehaviour
{
    private const int W = 480, H = 270;
    private const int Layer = 31;            // layer reservada para a tela de carregamento
    private const float FadeTime = 0.45f;
    private const float MinShowTime = 2.4f;
    private const int Blocks = 24;

    private static readonly Color Paper = new Color(0.93f, 0.89f, 0.78f);
    private static readonly Color Dim = new Color(0.55f, 0.52f, 0.45f);
    private static readonly Color Accent = new Color(0.85f, 0.12f, 0.12f);

    private static LoadingScreen active;
    public static bool IsLoading => active != null;

    private CanvasGroup screenGroup;
    private RenderTexture rt;
    private Camera cam;
    private TMP_FontAsset font;
    private TextMeshProUGUI status, counter;
    private RectTransform reelA, reelB;
    private Image[] blocks;
    private float progress;

    public static void Load(string sceneName, bool fadeIn = true)
    {
        if (active != null) return;
        GameObject go = new GameObject("LoadingScreen");
        DontDestroyOnLoad(go);
        active = go.AddComponent<LoadingScreen>();
        active.Build();
        active.StartCoroutine(active.Run(sceneName, fadeIn));
    }

    // ---------- Montagem ----------
    private void Build()
    {
        LoadingScreenArt art = Resources.Load<LoadingScreenArt>("LoadingScreenArt");
        font = art != null ? art.font : null;

        rt = new RenderTexture(W * 3, H * 3, 16) { filterMode = FilterMode.Point, name = "LoadingRT" };

        // Camera que so enxerga a layer da tela de carregamento.
        GameObject camGo = new GameObject("LoadingCamera");
        camGo.transform.SetParent(transform, false);
        camGo.layer = Layer;
        cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.035f, 0.032f, 0.028f);
        cam.cullingMask = 1 << Layer;
        cam.orthographic = true;
        cam.targetTexture = rt;
        cam.depth = 100;
        var camData = cam.GetUniversalAdditionalCameraData();
        camData.renderPostProcessing = true;
        camData.volumeLayerMask = 1 << Layer;
        camData.antialiasing = AntialiasingMode.None;

        BuildVolume();

        // Conteudo (Canvas na camera, 480x270).
        GameObject cGo = new GameObject("Conteudo", typeof(RectTransform));
        cGo.transform.SetParent(transform, false);
        cGo.layer = Layer;
        Canvas c = cGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.worldCamera = cam;
        c.planeDistance = 1f;
        CanvasScaler cs = cGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(W, H);
        RectTransform root = (RectTransform)cGo.transform;

        // Cabecalho de fita.
        TextMeshProUGUI play = Text(root, "PLAY ►", 16f, Paper, TextAlignmentOptions.TopLeft);
        Place(play.rectTransform, new Vector2(0, 1), new Vector2(30, -18), new Vector2(200, 20));
        counter = Text(root, "SP 0:00:00", 16f, Paper, TextAlignmentOptions.TopRight);
        Place(counter.rectTransform, new Vector2(1, 1), new Vector2(-30, -18), new Vector2(200, 20));
        TextMeshProUGUI lab = Text(root, "LAB 1975  //  FITA 01", 11f, Dim, TextAlignmentOptions.TopLeft);
        Place(lab.rectTransform, new Vector2(0, 1), new Vector2(30, -38), new Vector2(260, 14));

        // Deck com as bobinas girando.
        if (art != null && art.tapeDeck != null)
        {
            float dw = 150f, dh = dw * art.tapeDeck.height / art.tapeDeck.width;
            RawImage deck = Raw(root, art.tapeDeck);
            Place(deck.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(dw, dh));
            if (art.tapeReel != null)
            {
                reelA = Reel(deck.rectTransform, art.tapeReel, new Vector2(0.35f, 0.55f), dw * 0.17f);
                reelB = Reel(deck.rectTransform, art.tapeReel, new Vector2(0.65f, 0.55f), dw * 0.17f);
            }
        }

        // Status e barra em blocos.
        status = Text(root, "CARREGANDO FITA", 14f, Paper, TextAlignmentOptions.Left);
        status.alignment = TextAlignmentOptions.Center;
        Place(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -36), new Vector2(300, 18));
        blocks = new Image[Blocks];
        float bw = 9f, gap = 3f, total = Blocks * (bw + gap) - gap;
        for (int i = 0; i < Blocks; i++)
        {
            Image b = Img(root, Dim);
            Place(b.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-total / 2f + bw / 2f + i * (bw + gap), -54), new Vector2(bw, 7));
            blocks[i] = b;
        }

        // Dica.
        TextMeshProUGUI tipLabel = Text(root, "DICA", 12f, Accent, TextAlignmentOptions.TopLeft);
        Place(tipLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(400, 14));
        TextMeshProUGUI tip = Text(root, GameTips.Random(), 12f, Paper, TextAlignmentOptions.TopLeft);
        tip.textWrappingMode = TextWrappingModes.Normal;
        Place(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(400, 32));

        // Saida final: a imagem 480x270 ampliada sem filtro, com o shader de tubo CRT.
        GameObject sGo = new GameObject("Tela", typeof(RectTransform));
        sGo.transform.SetParent(transform, false);
        Canvas sc = sGo.AddComponent<Canvas>();
        sc.renderMode = RenderMode.ScreenSpaceOverlay;
        sc.sortingOrder = 32000;
        screenGroup = sGo.AddComponent<CanvasGroup>();
        screenGroup.alpha = 0f;
        screenGroup.blocksRaycasts = true;
        Image back = Img((RectTransform)sGo.transform, Color.black);
        Stretch(back.rectTransform);
        RawImage screen = Raw((RectTransform)sGo.transform, rt);
        Stretch(screen.rectTransform);
        Shader crt = Resources.Load<Shader>("CrtScreen");
        if (crt != null) screen.material = new Material(crt);
    }

    private void BuildVolume()
    {
        GameObject vGo = new GameObject("LoadingVolume");
        vGo.transform.SetParent(transform, false);
        vGo.layer = Layer;
        Volume v = vGo.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 100f;
        VolumeProfile p = ScriptableObject.CreateInstance<VolumeProfile>();
        v.profile = p;
        Bloom bloom = p.Add<Bloom>(true);
        bloom.intensity.Override(0.35f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.65f);
        p.Add<ChromaticAberration>(true).intensity.Override(0.15f);
        FilmGrain g = p.Add<FilmGrain>(true);
        g.type.Override(FilmGrainLookup.Medium4);
        g.intensity.Override(0.25f);
        ColorAdjustments ca = p.Add<ColorAdjustments>(true);
        ca.contrast.Override(18f);
        ca.saturation.Override(-15f);
    }

    // As cameras do jogo nao podem pegar o volume da tela de carregamento.
    private void IsolateGameCameras()
    {
        foreach (Camera c in Camera.allCameras)
        {
            if (c == cam) continue;
            var d = c.GetUniversalAdditionalCameraData();
            if (d != null) d.volumeLayerMask &= ~(1 << Layer);
        }
    }

    // ---------- Fluxo ----------
    private IEnumerator Run(string sceneName, bool fadeIn)
    {
        IsolateGameCameras();
        AudioListener.pause = false;
        Time.timeScale = 1f;
        if (fadeIn) yield return Fade(0f, 1f);
        screenGroup.alpha = 1f;

        float start = Time.unscaledTime;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f || Time.unscaledTime - start < MinShowTime)
        {
            float target = Mathf.Min(op.progress / 0.9f, (Time.unscaledTime - start) / MinShowTime);
            progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime * 1.2f);
            yield return null;
        }
        progress = 1f;
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
        IsolateGameCameras();
        yield return null;
        yield return null;
        IsolateGameCameras();
        yield return Fade(1f, 0f);
        active = null;
        Destroy(gameObject);
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        if (reelA != null) reelA.localRotation = Quaternion.Euler(0, 0, -t * 140f);
        if (reelB != null) reelB.localRotation = Quaternion.Euler(0, 0, -t * 110f);
        if (status != null)
        {
            int dots = (int)(t * 2.5f) % 4;
            status.text = (progress >= 1f ? "PRONTO" : "CARREGANDO FITA") + new string('.', dots);
        }
        if (counter != null)
        {
            int s = (int)(t * 7f);
            counter.text = string.Format("SP {0}:{1:00}:{2:00}", s / 3600, (s / 60) % 60, s % 60);
        }
        if (blocks != null)
        {
            int lit = Mathf.RoundToInt(progress * Blocks);
            bool blink = Mathf.Repeat(t * 3f, 1f) < 0.5f;
            for (int i = 0; i < blocks.Length; i++)
                blocks[i].color = i < lit ? Paper : (i == lit && blink ? Dim : new Color(0.2f, 0.19f, 0.17f));
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
        {
            screenGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / FadeTime));
            yield return null;
        }
        screenGroup.alpha = to;
    }

    private void OnDestroy()
    {
        if (cam != null) cam.targetTexture = null;
        if (rt != null) { rt.Release(); Destroy(rt); }
    }

    // ---------- Helpers ----------
    private TextMeshProUGUI Text(RectTransform parent, string value, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = value;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    private static Image Img(RectTransform parent, Color color)
    {
        GameObject go = new GameObject("Img", typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image i = go.GetComponent<Image>();
        i.color = color;
        i.raycastTarget = false;
        return i;
    }

    private static RawImage Raw(RectTransform parent, Texture tex)
    {
        GameObject go = new GameObject("Raw", typeof(RectTransform), typeof(RawImage));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        RawImage r = go.GetComponent<RawImage>();
        r.texture = tex;
        r.raycastTarget = false;
        return r;
    }

    private static RectTransform Reel(RectTransform deck, Texture tex, Vector2 anchor, float size)
    {
        RawImage r = Raw(deck, tex);
        RectTransform rt = r.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = new Vector2(anchor.x == 0.5f ? 0.5f : anchor.x, anchor.y == 0.5f ? 0.5f : anchor.y);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
