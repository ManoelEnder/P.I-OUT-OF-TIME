using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Cena de agradecimento (depois da cutscene final): "obrigado por jogar", equipe e creditos dos
// assets, sons e ferramentas, rolando numa tela de CRT com estatica e linhas de varredura.
// ESC/ENTER pula para o fim; no fim volta ao menu.
public class ThanksScreen : MonoBehaviour
{
    public TMP_FontAsset font;
    public string menuScene = "Menu";
    public float scrollSpeed = 48f;

    [SerializeField, HideInInspector] private GameObject canvasRoot;
    [SerializeField, HideInInspector] private RectTransform content;
    [SerializeField, HideInInspector] private TextMeshProUGUI body;
    [SerializeField, HideInInspector] private RawImage noise;
    [SerializeField, HideInInspector] private Image fade;
    private Texture2D noiseTex;
    private bool leaving;

    private const string Accent = "#D23A32";
    private const string Dim = "#8C8C88";

    private static readonly string[] Sections =
    {
        "<size=70>OUT OF TIME</size>\n<color=" + Dim + ">LAB 1975  //  FITA FINAL</color>\n\n\n" +
        "<size=40>OBRIGADO POR JOGAR</size>\n\n" +
        "<color=" + Dim + ">A primeira câmera digital foi montada em 1975, num laboratório da Kodak.\nNesta fita, você trouxe ela de volta.</color>",

        "<color=" + Accent + ">EQUIPE</color>\n\n" +
        "MANOEL SCHEIDT ENDER\n<color=" + Dim + ">Programador e Game Designer</color>\n\n" +
        "JÚLLYA DIELLY MARTINS MONTEIRO\n<color=" + Dim + ">Programadora e Game Designer</color>\n\n" +
        "GABRIEL PAMPLONA\n<color=" + Dim + ">Sonoplasta e Programador</color>",

        "<color=" + Accent + ">PROJETO INTEGRADOR</color>\n\n" +
        "Jogo desenvolvido como Projeto Integrador\n<color=" + Dim + ">Senac Santa Catarina</color>\n\n" +
        "Inspirado na história real de Steven Sasson\ne do protótipo de câmera digital da Kodak (1975)",

        "<color=" + Accent + ">MODELOS 3D E CENÁRIOS</color>\n\n" +
        "Abandoned Office\nFacility-7 Genetics\nPSX Nostalgia (Computer Set)\nPSX Mega Pack (Sample)\n" +
        "Caution Tape\nBathroom Cleaning Props Set (Low Poly)\nDetective's Office in Noir Style\nIsometric Office\n" +
        "Women in Trades in the Classroom: School Lab\nLow Poly Household Items\nFurniture Pack\n" +
        "JustPlay: Furniture Used Set\nAbandoned World: Wood Box Free\n\n" +
        "<color=" + Dim + ">Todos os direitos aos seus respectivos autores.</color>",

        "<color=" + Accent + ">INTERFACE E FERRAMENTAS</color>\n\n" +
        "Kenney: Fantasy UI Borders\nVCR OSD Mono (fonte)\nQuick Outline\nEasy Collider Editor",

        "<color=" + Accent + ">MÚSICA E SONS</color>\n\n" +
        "Casual & Relaxing Game Music\nEfeito de detector de metais (Depositphotos)\n" +
        "Efeitos da cutscene e ambiente do laboratório\n\n" +
        "<color=" + Dim + ">Grito, estática, passos, bipes, servo da garra e\noutros efeitos sintetizados no próprio jogo</color>",

        "<color=" + Accent + ">FEITO COM</color>\n\n" +
        "Unity 6  ·  Universal Render Pipeline  ·  Cinemachine\nBlender\nRemotion (cutscenes)",

        "\n\n<size=40>FIM DA GRAVAÇÃO</size>\n\n<color=" + Dim + ">[ESC] voltar ao menu</color>",
    };

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        if (content == null)
            Build();
        noiseTex = new Texture2D(160, 90, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        noise.texture = noiseTex;
        content.anchoredPosition = new Vector2(0f, -760f);
        fade.color = Color.black;
        StartCoroutine(Run());
    }

    // Monta a tela (editor; fica salva na cena).
    public void Build()
    {
        if (canvasRoot != null) DestroyNow(canvasRoot);
        GameObject c = new GameObject("TelaAgradecimentos", typeof(Canvas), typeof(CanvasScaler));
        c.transform.SetParent(transform, false);
        canvasRoot = c;
        Canvas canvas = c.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = c.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 1f;

        // Tela de CRT: fundo, texto rolando, estatica, linhas, vinheta.
        Image bg = Full(c.transform, "Fundo", new Color(0.035f, 0.04f, 0.04f, 1f));
        RectTransform mask = new GameObject("Tela", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        mask.SetParent(c.transform, false);
        mask.anchorMin = new Vector2(0.5f, 0f);
        mask.anchorMax = new Vector2(0.5f, 1f);
        mask.sizeDelta = new Vector2(900f, -80f);

        content = new GameObject("Conteudo", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(mask, false);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(900f, 100f);

        body = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        body.transform.SetParent(content, false);
        if (font != null) body.font = font;
        body.fontSize = 28f;
        body.lineSpacing = 6f;
        body.alignment = TextAlignmentOptions.Top;
        body.color = new Color(0.9f, 0.9f, 0.86f);
        body.text = string.Join("\n\n\n\n\n", Sections);
        RectTransform br = body.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
        br.pivot = new Vector2(0.5f, 1f);
        br.sizeDelta = new Vector2(880f, 100f);
        body.ForceMeshUpdate();
        br.sizeDelta = new Vector2(880f, body.preferredHeight + 40f);
        content.anchoredPosition = new Vector2(0f, -120f);

        noise = new GameObject("Estatica", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        noise.transform.SetParent(c.transform, false);
        Stretch(noise.rectTransform);
        noise.color = new Color(1f, 1f, 1f, 0.035f);
        noise.raycastTarget = false;

        Texture2D lines = new Texture2D(1, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        lines.SetPixels(new[] { new Color(0, 0, 0, 0.35f), new Color(0, 0, 0, 0f), new Color(0, 0, 0, 0f), new Color(0, 0, 0, 0f) });
        lines.Apply();
        RawImage scan = new GameObject("Linhas", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        scan.transform.SetParent(c.transform, false);
        Stretch(scan.rectTransform);
        scan.texture = lines;
        scan.uvRect = new Rect(0f, 0f, 1f, 720f / 4f);
        scan.raycastTarget = false;

        TextMeshProUGUI rec = new GameObject("Rec", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        rec.transform.SetParent(c.transform, false);
        if (font != null) rec.font = font;
        rec.fontSize = 20f;
        rec.color = new Color(0.82f, 0.23f, 0.2f);
        rec.text = "● PLAY  //  FITA 01";
        rec.rectTransform.anchorMin = rec.rectTransform.anchorMax = new Vector2(0f, 1f);
        rec.rectTransform.pivot = new Vector2(0f, 1f);
        rec.rectTransform.anchoredPosition = new Vector2(28f, -22f);
        rec.rectTransform.sizeDelta = new Vector2(400f, 30f);

        fade = Full(c.transform, "Fade", Color.clear);
    }

    private static void DestroyNow(Object o)
    {
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    private IEnumerator Run()
    {
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            fade.color = new Color(0f, 0f, 0f, 1f - t / 1.2f);
            yield return null;
        }
        fade.color = Color.clear;

        float end = body.rectTransform.sizeDelta.y - 260f;
        while (!leaving && content.anchoredPosition.y < end)
        {
            content.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
            yield return null;
        }
        // Para no "fim da gravacao" e espera o jogador.
        while (!leaving)
            yield return null;
    }

    private void Update()
    {
        if (Time.frameCount % 3 == 0 && noiseTex != null)
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

        var kb = Keyboard.current;
        if (leaving || kb == null)
            return;
        if (kb.spaceKey.isPressed || kb.downArrowKey.isPressed)
            content.anchoredPosition += Vector2.up * scrollSpeed * 3f * Time.deltaTime;
        if (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            StartCoroutine(Leave());
    }

    private IEnumerator Leave()
    {
        leaving = true;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            fade.color = new Color(0f, 0f, 0f, t / 0.8f);
            yield return null;
        }
        SceneManager.LoadScene(menuScene);
    }

    private void OnDestroy()
    {
        if (noiseTex != null) Destroy(noiseTex);
    }

    private static Image Full(Transform parent, string name, Color color)
    {
        Image img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        Stretch(img.rectTransform);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
