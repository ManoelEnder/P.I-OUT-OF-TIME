using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using E = CutoutLayer.Entrance;

// Monta a cena IntroCutscene. Menu: Ferramentas > Cutscene de Abertura.
// Também atende pedidos por arquivo (Temp/IntroCutscene.request: build | preview | frames t1,t2,...),
// usados para automatizar a geração e a prévia com o Editor aberto.
[InitializeOnLoad]
public static class IntroCutsceneBuilder
{
    const string Root = "Assets/Cutscene";
    const string ArtDir = Root + "/Art";
    const string SfxDir = Root + "/Audio/Efeitos";
    const string VoiceDir = Root + "/Audio/Voz";
    const string FontPath = "Assets/Fonts/VCR_OSD_MONO SDF.asset";
    const string RTPath = Root + "/TelaPixelada_640x360.renderTexture";
    public const string ScenePath = "Assets/Scenes/IntroCutscene.unity";
    const string RequestPath = "Temp/IntroCutscene.request";
    const string LogPath = "Temp/IntroCutscene.log";
    const string PreviewDir = "CutscenePreview";

    static double nextPoll;

    static IntroCutsceneBuilder() { EditorApplication.update += Poll; }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1.0;
        if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        string cmd = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        if (SceneManager.sceneCount > 0 && Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            Log("ABORTADO: há cenas com alterações não salvas no Editor. Salve e peça de novo.");
            return;
        }
        Run(cmd);
    }

    [MenuItem("Ferramentas/Cutscene de Abertura/Gerar arte, áudio e cena")]
    static void MenuBuild() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Run("build"); }

    [MenuItem("Ferramentas/Cutscene de Abertura/Exportar prévia (quadros + áudio)")]
    static void MenuPreview() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Run("preview"); }

    static void Log(string msg)
    {
        Debug.Log("[IntroCutscene] " + msg);
        File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss ") + msg + "\n");
    }

    static void Run(string cmd)
    {
        string[] previous = Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path).Where(p => !string.IsNullOrEmpty(p)).ToArray();
        try
        {
            Log("início: " + cmd);
            if (cmd.StartsWith("build")) Build();
            else if (cmd.StartsWith("videoscene")) IntroVideoSceneBuilder.Build();
            else if (cmd.StartsWith("preview")) Export(null);
            else if (cmd.StartsWith("frames"))
                Export(cmd.Substring(6).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray());
            Log("fim: " + cmd);
        }
        catch (Exception e)
        {
            Log("ERRO: " + e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (previous.Length > 0)
            {
                EditorSceneManager.OpenScene(previous[0], OpenSceneMode.Single);
                foreach (string p in previous.Skip(1)) EditorSceneManager.OpenScene(p, OpenSceneMode.Additive);
            }
        }
    }

    // ------------------------------------------------------------------ build

    static void Build()
    {
        EditorUtility.DisplayProgressBar("Cutscene", "Desenhando camadas", 0.1f);
        IntroCutsceneArt.Generate(ArtDir);
        IntroCutsceneAudio.Generate(SfxDir);
        AssetDatabase.Refresh();
        ImportArt();


        EditorUtility.DisplayProgressBar("Cutscene", "Montando a cena", 0.7f);
        BuildScene();
        AddToBuildSettings();
    }

    static void ImportArt()
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var kv in IntroCutsceneArt.Specs)
                ConfigureSprite($"{ArtDir}/{kv.Key}.png", kv.Value.ppu, kv.Value.pivot, false);
        }
        finally { AssetDatabase.StopAssetEditing(); }
    }

    static void ConfigureSprite(string path, float ppu, Vector2 pivot, bool point)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(s);
        ti.mipmapEnabled = true; // reduz cintilação ao reduzir para a tela de 640x360
        ti.alphaIsTransparency = true;
        ti.maxTextureSize = 4096;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
        ti.SaveAndReimport();
    }

    static void AddToBuildSettings()
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (list.Any(s => s.path == ScenePath)) return;
        int menu = list.FindIndex(s => s.path.EndsWith("/Menu.unity"));
        list.Insert(menu >= 0 ? menu + 1 : list.Count, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        Log("IntroCutscene adicionada ao Build Settings.");
    }

    // ------------------------------------------------------------------ cena

    static Sprite Sp(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");
        if (s == null) Log("sprite ausente: " + name);
        return s;
    }

    static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

    static AudioClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    static AudioClip Sfx(string name) => Clip($"{SfxDir}/{name}.wav");

    static Color Hex(string hex, float a = 1f) { ColorUtility.TryParseHtmlString(hex, out Color c); c.a = a; return c; }

    static Transform Node(Transform parent, string name, float x, float y)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        return go.transform;
    }

    static CutoutLayer L(Transform parent, string sprite, float x, float y, int order, string anchor, float enter,
                         E e = E.Drop, float scale = 1f, float rot = 0f, string name = null)
    {
        Transform t = Node(parent, name ?? sprite ?? "rig", x, y);
        t.localEulerAngles = new Vector3(0f, 0f, rot);
        t.localScale = Vector3.one * scale;
        if (sprite != null)
        {
            var sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Sp(sprite);
            sr.sortingOrder = order;
        }
        var l = t.gameObject.AddComponent<CutoutLayer>();
        l.anchor = anchor;
        l.enter = enter;
        l.entrance = e;
        return l;
    }

    static CutoutLayer Txt(Transform parent, string text, float x, float y, float size, Color color, int order,
                           string anchor, float enter, E e = E.Pop, float width = 18f, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        Transform t = Node(parent, "txt " + text, x, y);
        var tmp = t.gameObject.AddComponent<TextMeshPro>();
        tmp.font = Font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.rectTransform.sizeDelta = new Vector2(width, 2f);
        tmp.sortingOrder = order;
        var l = tmp.gameObject.AddComponent<CutoutLayer>();
        l.anchor = anchor; l.enter = enter; l.entrance = e;
        return l;
    }

    static CutoutLayer Tint(this CutoutLayer l, Color c) { l.GetComponent<SpriteRenderer>().color = c; return l; }
    static CutoutLayer Do(this CutoutLayer l, Action<CutoutLayer> a) { a(l); return l; }
    static CutoutLayer Only(this CutoutLayer l, params string[] shots) { l.onlyIn = shots; return l; }
    static CutoutLayer Exit(this CutoutLayer l, float exit, float dur = 0.2f) { l.exit = exit; l.exitDuration = dur; return l; }
    static CutoutLayer Move(this CutoutLayer l, float a, float b, float dx = 0, float dy = 0, float rot = 0, float scaleTo = 1f, float alphaTo = 1f)
    {
        l.moveStart = a; l.moveEnd = b; l.moveBy = new Vector3(dx, dy, 0); l.rotateBy = rot; l.scaleTo = scaleTo; l.alphaTo = alphaTo; return l;
    }
    static CutoutLayer Sz(this CutoutLayer l, float sx, float sy) { l.transform.localScale = new Vector3(sx, sy, 1f); return l; }
    static Sprite[] Frames(string prefix) => Enumerable.Range(0, 4).Select(i => Sp(prefix + i)).ToArray();

    static readonly Color Phos = new Color(0.49f, 1f, 0.63f);
    static readonly Color AlertC = new Color(0.66f, 0.28f, 0.24f);
    static readonly Color Paper = new Color(0.93f, 0.88f, 0.76f);

    static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // câmera + sobreposição sempre ativa (granulação, papel, vinheta, legendas, fade)
        var camT = Node(null, "Camera da Cutscene", 0f, 0f);
        camT.position = new Vector3(0f, 0f, -10f);
        camT.tag = "MainCamera";
        var cam = camT.gameObject.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        camT.gameObject.AddComponent<AudioListener>();

        // tudo é desenhado em 640x360 (mesma resolução do PixelRenderTexture do jogo) e ampliado sem filtro
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RTPath);
        if (rt == null)
        {
            rt = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, name = "TelaPixelada_640x360" };
            AssetDatabase.CreateAsset(rt, RTPath);
        }
        cam.targetTexture = rt;
        var screenCam = Node(null, "Camera da Tela (fundo preto)", 0f, 0f).gameObject.AddComponent<Camera>();
        screenCam.cullingMask = 0; screenCam.clearFlags = CameraClearFlags.SolidColor; screenCam.backgroundColor = Color.black; screenCam.depth = -10;
        var canvas = new GameObject("Tela Pixelada", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var raw = new GameObject("Imagem", typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.UI.AspectRatioFitter));
        raw.transform.SetParent(canvas.transform, false);
        raw.GetComponent<UnityEngine.UI.RawImage>().texture = rt;
        raw.GetComponent<UnityEngine.UI.RawImage>().raycastTarget = false;
        var fit = raw.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f / 9f;

        Transform overlay = Node(camT, "Sobreposicao", 0f, 0f);
        overlay.localPosition = new Vector3(0f, 0f, 5f);
        L(overlay, "grain_0", 0, 0, 900, null, 0, E.Cut).Do(l => { l.frames = Frames("grain_"); l.fps = 12; });
        L(overlay, "paper_overlay", 0, 0, 899, null, 0, E.Cut);
        L(overlay, "vignette", 0, 0, 898, null, 0, E.Cut);
        var subBack = L(overlay, "white", 0, -4.45f, 940, null, 0, E.Cut, name: "fundo legenda").Sz(19.2f, 1.05f).Tint(new Color(0, 0, 0, 0.5f));
        UnityEngine.Object.DestroyImmediate(subBack);   // a faixa é controlada pelo diretor, não pela linha do tempo
        var subBackSr = overlay.Find("fundo legenda").GetComponent<SpriteRenderer>();
        var subtitle = Node(overlay, "Legenda", 0f, -4.45f).gameObject.AddComponent<TextMeshPro>();
        subtitle.font = Font;
        subtitle.fontSize = 5f; subtitle.alignment = TextAlignmentOptions.Center; subtitle.color = Paper;
        subtitle.rectTransform.sizeDelta = new Vector2(17f, 1.2f); subtitle.sortingOrder = 950;
        subtitle.textWrappingMode = TextWrappingModes.Normal;
        var skip = Node(overlay, "Aviso pular", 6.6f, 4.9f).gameObject.AddComponent<TextMeshPro>();
        skip.font = Font;
        skip.text = "Pressione novamente para pular"; skip.fontSize = 3.4f; skip.alignment = TextAlignmentOptions.Right;
        skip.color = Paper; skip.rectTransform.sizeDelta = new Vector2(9f, 0.8f); skip.sortingOrder = 960;
        skip.transform.localPosition = new Vector3(5.0f, 4.75f, 0f);
        var fadeT = Node(overlay, "Fade", 0f, 0f);
        fadeT.localScale = new Vector3(22f, 13f, 1f);
        var fade = fadeT.gameObject.AddComponent<SpriteRenderer>();
        fade.sprite = Sp("white"); fade.color = Color.black; fade.sortingOrder = 1000;

        Transform lab = BuildLab(Node(null, "G1 Laboratorio 1975", 0f, 0f));
        Transform floor = BuildFloor(Node(null, "G2 Chao (impacto)", 40f, 0f));
        Transform comic = BuildComic(Node(null, "G3 Linha do tempo (motion comic)", 80f, 0f));
        Transform rec = BuildRecording(Node(null, "G4 Gravacao do pesquisador", 120f, 0f));
        Transform play = BuildGameplay(Node(null, "G5 Demonstracao de jogo", 160f, 0f));

        foreach (var l in UnityEngine.Object.FindObjectsByType<CutoutLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            l.Capture();

        // ---------- diretor: tempos centralizados ----------
        var dirGo = new GameObject("Diretor");
        var d = dirGo.AddComponent<IntroCutsceneDirector>();
        d.cam = cam; d.overlay = overlay; d.fade = fade; d.subtitle = subtitle; d.skipHint = skip; d.subtitleBack = subBackSr;
        AudioSource Src(string n) { var a = new GameObject(n).AddComponent<AudioSource>(); a.transform.SetParent(dirGo.transform); a.playOnAwake = false; return a; }
        d.voice = Src("Voz"); d.music = Src("Trilha"); d.ambience = Src("Ambiente"); d.sfx = Src("Efeitos");
        d.music.clip = Clip("Assets/Audio/Casual & Relaxing Game Music/Darkness.wav");
        d.ambience.clip = Sfx("amb_lab");
        d.duration = 112f;
        d.nextScene = "Principal";

        IntroCutsceneDirector.Shot S(string id, float start, Transform g, float x0, float y0, float s0, float x1, float y1, float s1, float fadeIn = 0f, float shake = 0f) =>
            new IntroCutsceneDirector.Shot { id = id, start = start, group = g, camFrom = new Vector2(g.position.x + x0, y0), sizeFrom = s0, camTo = new Vector2(g.position.x + x1, y1), sizeTo = s1, fadeIn = fadeIn, shake = shake };

        d.shots = new[]
        {
            S("s01_tape",     0.0f, lab,  -2.2f, -0.28f, 0.75f, -2.2f, -0.28f, 0.68f, fadeIn: 0.6f),
            S("s01_wide",     1.8f, lab,  -2.2f, -0.28f, 0.68f,  0f,    0.2f,  5.4f),
            S("s02_med",      7.0f, lab,  -2.0f,  0.6f,  3.3f,  -1.7f,  0.5f,  3.1f),
            S("s02_lens",     9.8f, lab,   2.6f,  0.1f,  1.3f,   2.6f,  0.1f,  1.15f),
            S("s02_wire",    12.0f, lab,  -0.6f, -0.4f,  1.2f,  -0.4f, -0.4f,  1.1f),
            S("s02_board",   13.8f, lab,   0.5f,  0.15f, 1.1f,   0.5f,  0.15f, 1.0f),
            S("s02_crt",     15.6f, lab,   5.7f,  0.77f, 1.25f,  5.7f,  0.77f, 0.95f),
            S("s03_empty",   21.0f, lab,   0.3f,  0.6f,  4.4f,   0.3f,  0.6f,  4.2f, fadeIn: 0.25f),
            S("s03_figure",  24.2f, lab,  -1.8f,  0.8f,  3.8f,  -1.8f,  0.8f,  3.4f),
            S("s03_push",    26.6f, lab,  -0.6f,  0.5f,  2.6f,  -0.6f,  0.5f,  2.4f, shake: 0.08f),
            S("s03_floor",   27.4f, floor, 0f,    0f,    5.0f,   0f,    0f,    4.8f, shake: 0.12f),
            S("s04_comic",   30.0f, comic, 0f,    0.2f,  5.4f,   0f,    0.2f,  5.2f),
            S("s04_piece",   35.0f, comic, 0f,    0.3f,  5.4f,   0f,    0.3f,  4.7f),
            S("s04_crt",     36.3f, lab,   5.7f,  0.77f, 0.95f,  5.7f,  0.77f, 0.95f),
            S("s05_pull",    37.0f, rec,  -5.4f,  2.567f, 0.95f, 0f,    0.3f,  5.2f),
            S("s05_rec",     40.0f, rec,   0f,    0.3f,  5.2f,   0f,    0.6f,  4.6f),
            S("s06_play",    51.0f, play,  0f,    0f,    5.4f,   0f,    0f,    5.4f, fadeIn: 0.35f),
            S("s07_rec",     68.0f, rec,   1.5f,  1.0f,  4.4f,   2.0f,  1.2f,  4.2f),
            S("s07_meter",   70.6f, play,  0f,    0f,    5.4f,   0f,    0f,    5.4f),
            S("s08_overheat",82.0f, play,  0f,    0f,    5.4f,   0f,    0f,    5.4f),
            S("s09_lost",    90.5f, play,  0f,    0f,    5.4f,   0f,    0f,    5.4f, fadeIn: 0.3f),
            S("s10_end",    100.0f, rec,   0f,    0.6f,  4.6f,   0.6f,  1.2f,  3.9f),
            S("s11_start",  109.0f, play,  0f,    0f,    5.4f,   0f,    0f,    5.4f, fadeIn: 0.6f),
        };

        string[] text =
        {
            "Em 1975, Steve Sasson montou, num laboratório da Kodak, um protótipo capaz de gravar uma foto em fita magnética.",
            "Era o começo da fotografia digital.",
            "Então alguém voltou àquela noite. Destruiu o protótipo,",
            "e o futuro perdeu décadas de avanços.",
            "Algumas peças ficaram presas no tempo.",
            "Se você está ouvindo esta gravação, a câmera que deixei no laboratório chegou até você.",
            "Use-a para encontrar essas peças e reconstruir o protótipo.",
            "Abra o visor e procure onde a imagem falha.",
            "Quando enxergar uma peça, mire e fotografe.",
            "Ela vai continuar visível por um tempo. Chegue perto e recolha antes que desapareça.",
            "O visor gasta bateria e esquenta enquanto está aberto.",
            "Feche entre uma busca e outra para economizar carga e deixar a câmera esfriar.",
            "Se ela superaquecer, vai sumir.",
            "Uma ruptura pode levá-la para outro ponto do laboratório.",
            "Encontre a câmera e recolha-a para continuar.",
            "Traga as peças de volta.",
            "E, se vir alguém nas gravações, preste atenção.",
            "Eu ainda não sei se aquela pessoa foi embora.",
        };
        float[] starts = { 1.0f, 18.0f, 21.8f, 29.0f, 33.3f, 38.4f, 44.4f, 51.6f, 55.0f, 59.6f, 68.8f, 73.4f, 82.6f, 88.2f, 95.0f, 100.2f, 101.9f, 105.5f };
        float[] fallback = { 9.4f, 2.4f, 4.2f, 2.4f, 2.6f, 5.4f, 3.9f, 2.7f, 3.0f, 5.9f, 3.4f, 4.8f, 2.4f, 3.5f, 2.8f, 1.7f, 3.4f, 2.7f };
        d.lines = new IntroCutsceneDirector.Line[text.Length];
        for (int i = 0; i < text.Length; i++)
            d.lines[i] = new IntroCutsceneDirector.Line
            {
                start = starts[i], text = text[i], duration = fallback[i],
                clip = Clip($"{VoiceDir}/voz_{i + 1:D2}.wav") ?? Clip($"{VoiceDir}/voz_{i + 1:D2}.mp3"), // dublagem (ElevenLabs) entra aqui; sem arquivo, só legenda
            };

        AudioClip shutter = Clip("Assets/Audio/Camera/483381__michx123__sony-a7-ii-autofocus-shutter-mechanism-sound.wav");
        AudioClip rewind = Clip("Assets/Audio/UI/mixkit-tape-rewind-cinematic-transition-1088.wav");
        AudioClip glitch = Clip("Assets/Audio/UI/mixkit-glitch-static-1457.wav");
        AudioClip confirm = Clip("Assets/Audio/UI/mixkit-sci-fi-confirmation-914.wav");
        IntroCutsceneDirector.Cue Q(float t, AudioClip c, float v, string note) => new IntroCutsceneDirector.Cue { time = t, clip = c, volume = v, note = note };
        AudioClip click = Sfx("sfx_tool_click"), whoosh = Sfx("sfx_whoosh");
        d.cues = new[]
        {
            Q(0.15f, Sfx("sfx_tape_click"), 0.9f, "clique seco da fita"),
            Q(1.9f, whoosh, 0.3f, "bancada desce"), Q(2.4f, click, 0.55f, "luz de mesa acende"),
            Q(2.6f, whoosh, 0.22f, "prateleira"), Q(3.1f, whoosh, 0.25f, "Steve"), Q(4.6f, whoosh, 0.3f, "corpo do protótipo"),
            Q(5.9f, whoosh, 0.35f, "lente (por último)"), Q(6.35f, click, 0.6f, "lente assenta"),
            Q(8.0f, click, 0.4f, "ferramenta"), Q(8.6f, click, 0.35f, "ferramenta"),
            Q(10.6f, click, 0.6f, "lente encaixa"), Q(11.2f, click, 0.5f, "lente trava"),
            Q(12.5f, click, 0.7f, "conector"), Q(14.3f, click, 0.5f, "chave na placa"), Q(14.9f, click, 0.5f, "chave na placa"),
            Q(15.65f, Sfx("sfx_crt_on"), 0.6f, "CRT liga"), Q(17.6f, Sfx("sfx_discovery"), 0.75f, "nota de descoberta"),
            Q(21.1f, rewind, 0.7f, "relógio recua"), Q(24.5f, glitch, 0.55f, "silhueta"),
            Q(26.7f, whoosh, 0.5f, "golpe"), Q(27.4f, Sfx("sfx_impact"), 1f, "IMPACTO (corte seco)"),
            Q(30.05f, whoosh, 0.35f, "quadro 1"), Q(31.25f, whoosh, 0.35f, "quadro 2"), Q(32.45f, whoosh, 0.35f, "quadro 3"),
            Q(35.1f, Sfx("sfx_reveal"), 0.5f, "peça suspensa"), Q(36.3f, glitch, 0.3f, "CRT 1975 em estática"),
            Q(37.0f, Sfx("sfx_crt_on"), 0.5f, "CRT da gravação"),
            Q(52.3f, click, 0.5f, "tecla C"), Q(52.8f, whoosh, 0.2f, "visor abre"), Q(53.6f, Sfx("sfx_reveal"), 0.6f, "peça aparece"),
            Q(58.3f, shutter, 1f, "obturador"), Q(59.5f, click, 0.5f, "visor fecha"),
            Q(63.95f, Sfx("sfx_collect"), 0.8f, "coleta"), Q(64.6f, confirm, 0.45f, "missão atualizada"),
            Q(71.0f, click, 0.5f, "visor abre"), Q(74.2f, Sfx("sfx_heat_warning"), 0.55f, "aviso de calor"), Q(75.0f, click, 0.5f, "visor fecha"),
            Q(82.05f, click, 0.4f, "visor abre"), Q(84.2f, Sfx("sfx_heat_warning"), 0.6f, "aviso"), Q(85.2f, Sfx("sfx_heat_warning"), 0.7f, "aviso"),
            Q(86.0f, Sfx("sfx_rupture"), 1f, "RUPTURA"), Q(86.4f, shutter, 0.6f, "obturador fecha"),
            Q(88.7f, Sfx("sfx_denied"), 0.6f, "visor não abre"), Q(90.6f, whoosh, 0.3f, "outro ponto do laboratório"),
            Q(96.5f, Sfx("sfx_collect"), 0.8f, "câmera recuperada"),
            Q(103.2f, glitch, 0.55f, "sabotador no monitor"), Q(108.4f, Sfx("sfx_crt_off"), 0.8f, "gravação encerrada"),
            Q(109.3f, Sfx("sfx_reveal"), 0.35f, "objetivo"),
        };

        d.musicVolume = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(1.5f, 0.42f), new Keyframe(20.5f, 0.42f), new Keyframe(26.9f, 0.5f),
            new Keyframe(27.35f, 0f), new Keyframe(29.2f, 0.3f), new Keyframe(37f, 0.3f), new Keyframe(38.6f, 0.16f),
            new Keyframe(99.5f, 0.16f), new Keyframe(102.5f, 0.28f), new Keyframe(108.3f, 0.28f), new Keyframe(108.5f, 0f),
            new Keyframe(109.3f, 0.3f), new Keyframe(112f, 0f));
        d.ambienceVolume = new AnimationCurve(
            new Keyframe(0f, 0.35f), new Keyframe(21f, 0.35f), new Keyframe(27.4f, 0.15f), new Keyframe(37f, 0.3f),
            new Keyframe(51f, 0.15f), new Keyframe(100f, 0.3f), new Keyframe(108.4f, 0f), new Keyframe(109.2f, 0.2f), new Keyframe(112f, 0f));

        EditorSceneManager.SaveScene(scene, ScenePath);
        Log("cena salva: " + ScenePath);
    }

    // ---------- G1: laboratório de 1975 (âncora s01_tape = tempo absoluto) ----------
    static Transform BuildLab(Transform g)
    {
        const string A = "s01_tape";
        string[] steveShots = { "s01_tape", "s01_wide", "s02_med" };
        string[] warm = { "s01_tape", "s01_wide", "s02_med", "s02_lens", "s02_wire", "s02_board", "s02_crt" };
        string[] cold = { "s03_empty", "s03_figure", "s03_push", "s04_crt" };

        L(g, "lab_wall", 0, 0, 0, A, 0f, E.Fade).Do(l => l.enterDuration = 1.6f);
        L(g, "lab_shelf", -6f, 2.8f, 2, A, 2.3f).Do(l => l.settle = 0.08f);
        L(g, "lab_pegboard", 5.6f, 2.7f, 2, A, 2.6f);
        L(g, "hanging_paper", 5.0f, 1.45f, 3, A, 2.8f).Do(l => { l.swayDegrees = 3f; l.swaySpeed = 0.35f; });
        L(g, "lab_clock", 1.4f, 3.7f, 3, A, 2.9f);
        L(g, "clock_hand", 1.4f, 3.7f, 4, A, 2.9f, name: "ponteiro minutos").Only(warm).Do(l => l.spin = -6f);
        L(g, "clock_hand", 1.4f, 3.7f, 4, A, 2.9f, scale: 0.7f, rot: -120f, name: "ponteiro horas").Only(warm).Do(l => l.spin = -0.5f);
        L(g, "clock_hand", 1.4f, 3.7f, 4, "s03_empty", 0f, E.Cut, name: "ponteiro recuando").Only(cold).Do(l => l.spin = 720f);
        L(g, "clock_hand", 1.4f, 3.7f, 4, "s03_empty", 0f, E.Cut, 0.7f, name: "ponteiro horas recuando").Only(cold).Do(l => l.spin = 60f);
        L(g, "cable_hang", -0.7f, 5.5f, 26, A, 3.0f).Do(l => { l.swayDegrees = 2.5f; l.swaySpeed = 0.3f; });

        L(g, "bench", 0f, -3.1f, 10, A, 1.9f).Do(l => { l.dropHeight = 9f; l.settle = 0.05f; });
        L(g, "lamp", -2.9f, -1.0f, 20, A, 2.1f, scale: 0.8f);
        L(g, "lamp_glow", -2.34f, 0.68f, 45, A, 2.4f, E.Fade, 0.75f).Only(warm).Do(l => l.enterDuration = 0.25f);
        L(g, "lamp_glow", -2.34f, 0.68f, 45, A, 0f, E.Cut, 0.75f, name: "luz hesitante").Only(cold).Tint(new Color(0.85f, 0.9f, 1f, 0.75f)).Do(l => l.flicker = 0.6f);
        L(g, "dust", -2.3f, -1.4f, 46, A, 2.6f, E.Fade).Only(warm.Concat(cold).ToArray()).Do(l => { l.bob = 0.08f; l.bobSpeed = 0.15f; l.flicker = 0.12f; });

        L(g, "steve_body", -4.7f, -1.9f, 8, A, 3.1f, scale: 0.85f).Only(steveShots);
        L(g, "steve_head", -4.65f, 1.75f, 9, A, 3.25f, scale: 0.85f).Only(steveShots).Do(l => { l.swayDegrees = 1.2f; l.swaySpeed = 0.25f; });
        L(g, "steve_arm", -3.6f, 1.3f, 25, A, 3.4f, scale: 0.85f, rot: -37f).Only(steveShots).Do(l => { l.swayDegrees = 3f; l.swaySpeed = 0.4f; });

        L(g, "notes", -7.2f, -1.1f, 22, A, 3.7f, rot: 8f).Do(l => { l.bob = 0.015f; l.swayDegrees = 0.6f; });
        L(g, "screwdriver", -6.0f, -0.85f, 23, A, 3.9f, rot: -10f);
        L(g, "solder_iron", -5.3f, -0.6f, 21, A, 4.1f, rot: 4f);
        L(g, "components", -0.9f, -0.9f, 23, A, 4.3f);
        L(g, "pliers", 3.4f, -0.85f, 23, A, 4.0f, rot: 12f);

        // protótipo: rig inclina no golpe do sabotador
        var rig = L(g, null, 0f, 0f, 0, A, -100f, E.Cut, name: "Prototipo").Move(26.9f, 27.35f, 0.6f, -0.3f, -16f).Do(l => l.linear = 0.7f).transform;
        L(rig, "tape_deck", -2.2f, -0.35f, 31, A, 0f, E.Cut);
        L(rig, "tape_reel", -2.5f, -0.283f, 32, A, 0f, E.Cut, name: "carretel esquerdo").Do(l => l.spin = 220f);
        L(rig, "tape_reel", -1.9f, -0.283f, 32, A, 0f, E.Cut, name: "carretel direito").Do(l => l.spin = 220f);
        L(rig, "wire_bundle", -0.9f, -0.45f, 30, A, 5.3f);
        L(rig, "proto_body", 0.9f, 0.05f, 31, A, 4.6f);
        L(rig, "proto_board", 0.5f, 0.15f, 33, A, 5.1f).Do(l => l.swayDegrees = 0.8f);
        L(rig, "wire_plug", -0.15f, -0.35f, 34, A, 5.4f).Move(12.4f, 12.9f, 0.2f);
        L(rig, "proto_lens", 2.6f, 0.1f, 34, A, 5.9f).Move(10.3f, 11.5f, rot: 40f).Do(l => { l.swayDegrees = 1.5f; l.dropHeight = 8f; l.settle = 0.15f; });

        L(g, "crt1975", 5.7f, 0.4f, 12, A, 4.3f);
        L(g, "crt_lines_0", 5.7f, 0.767f, 13, "s02_crt", 0.05f, E.Cut, name: "linhas do CRT").Only("s02_crt").Exit(1.9f, 0.1f).Do(l => { l.frames = Frames("crt_lines_"); l.fps = 10; });
        L(g, "crt_portrait", 5.7f, 0.767f, 14, "s02_crt", 1.7f, E.Fade, name: "retrato no CRT").Only("s02_crt").Do(l => l.enterDuration = 0.7f);
        L(g, "mon_static_0", 5.7f, 0.767f, 14, "s04_crt", 0f, E.Cut, name: "CRT 1975 em estatica").Only("s04_crt").Do(l => { l.frames = Frames("mon_static_"); l.fps = 14; });

        // closes das mãos
        L(g, "hand_close", 3.5f, -0.2f, 40, "s02_lens", 0f, E.Cut, rot: 10f, name: "mao na lente").Only("s02_lens").Move(0f, 0.7f, -0.3f, 0.05f).Do(l => { l.swayDegrees = 3f; l.swaySpeed = 0.8f; });
        L(g, "hand_close", -0.3f, -0.45f, 40, "s02_wire", 0f, E.Cut, rot: -8f, name: "mao no conector").Only("s02_wire").Sz(-1f, 1f).Move(0.4f, 0.9f, 0.2f);
        L(g, "hand_tool", 1.9f, 0.3f, 40, "s02_board", 0f, E.Cut, rot: 15f, name: "mao com chave").Only("s02_board").Do(l => { l.swayDegrees = 6f; l.swaySpeed = 1.2f; });

        // sabotagem
        L(g, "white", 0f, 0f, 60, A, 0f, E.Cut, name: "luz fria").Only(cold).Sz(26f, 14f).Tint(new Color(0.09f, 0.19f, 0.23f, 0.42f));
        L(g, "silhouette", -6.4f, -1.9f, 9, "s03_figure", 0.3f, E.Fade, name: "silhueta").Only("s03_figure").Do(l => { l.enterDuration = 1.0f; l.flicker = 0.25f; });
        L(g, "silhouette", -2.7f, -1.9f, 9, "s03_push", 0f, E.Cut, name: "silhueta golpe").Only("s03_push");
        L(g, "silhouette_arm", -2.25f, 2.0f, 40, "s03_push", 0f, E.Cut, rot: -10f, name: "braço golpe").Only("s03_push").Move(0f, 0.45f, rot: -40f).Do(l => l.linear = 0.8f);
        return g;
    }

    // ---------- G2: estilhaços no chão ----------
    static Transform BuildFloor(Transform g)
    {
        const string A = "s03_floor";
        L(g, "floor", 0, 0, 0, A, 0f, E.Cut);
        (string sprite, float dx, float dy, float rot)[] parts =
        {
            ("proto_body", -1.8f, -0.9f, -110f), ("proto_board", 3.2f, 1.4f, 200f), ("proto_lens", 4.6f, -2.2f, 540f),
            ("tape_deck", -5.0f, 1.2f, -60f), ("tape_reel", -2.5f, -3.0f, 900f), ("tape_reel", 1.4f, -3.4f, -700f),
            ("screw", 2.4f, 2.6f, 400f), ("screw", -3.4f, -1.8f, -300f), ("screw", 0.8f, -2.2f, 250f), ("components", -1.2f, 2.4f, 90f),
        };
        int i = 0;
        foreach (var p in parts)
        {
            float scale = p.sprite == "tape_reel" ? 2.5f : p.sprite == "tape_deck" ? 1.3f : 1.25f;
            L(g, p.sprite, 0f, 0.6f, 10 + i++, A, 0f, E.Cut, scale, name: "estilhaço " + p.sprite).Move(0f, 0.7f + i * 0.04f, p.dx, p.dy, p.rot).Do(l => l.easeOut = true);
        }
        L(g, "dust_puff", 0f, 0.3f, 30, A, 0f, E.Pop).Move(0.2f, 1.8f, 0f, 0.4f, 0f, 1.6f, 0f);
        L(g, "white", 0, 0, 70, A, 0f, E.Cut, name: "clarão").Sz(24f, 14f).Exit(0.05f, 0.15f);
        return g;
    }

    // ---------- G3: motion comic da linha do tempo alterada ----------
    static Transform BuildComic(Transform g)
    {
        const string A = "s04_comic";
        L(g, "comic_bg", 0, 0, 0, A, 0f, E.Cut);
        float[] px = { -6.1f, 0f, 6.1f };
        string[] caps = { "ARQUIVO", "IMAGEM", "TECNOLOGIA" };
        for (int i = 0; i < 3; i++)
        {
            L(g, "panel", px[i], 0.2f, 10, A, i * 1.2f).Only(A).Exit(4.3f, 0.7f);
            Txt(g, caps[i], px[i], 3.05f, 4f, Paper, 14, A, i * 1.2f + 0.3f).Only(A).Exit(4.3f, 0.7f);
        }
        L(g, "cabinet", -6.1f, -1.3f, 12, A, 0.2f).Only(A).Exit(4.3f, 0.7f);
        float[,] ph = { { -6.6f, 0.8f }, { -5.6f, 1.3f }, { -6.2f, 2.0f } };
        for (int i = 0; i < 3; i++)
            L(g, "photo_" + i, ph[i, 0], ph[i, 1], 13, A, 0.35f + i * 0.1f, E.Pop, rot: (i - 1) * 9f).Only(A).Move(0.9f + i * 0.2f, 2.0f + i * 0.2f, 0f, 0.3f, 0f, 1f, 0f).Do(l => l.flicker = 0.35f);
        L(g, "digital_cam", 0f, 0.4f, 12, A, 1.3f).Only(A).Move(2.0f, 2.8f, alphaTo: 0f).Do(l => l.flicker = 0.3f);
        L(g, "film_cam", 0f, 0.4f, 12, A, 2.1f, E.Fade).Only(A).Exit(4.3f, 0.7f).Do(l => l.enterDuration = 0.6f);
        L(g, "timeline", 6.1f, -0.4f, 12, A, 2.5f).Only(A).Exit(4.3f, 0.7f);
        Txt(g, "1975", 6.1f - 1.96f, -1.3f, 3.2f, Paper, 13, A, 2.7f).Only(A).Exit(4.3f, 0.7f);
        Txt(g, "2005", 6.1f + 1.56f, -1.3f, 3.2f, Paper, 13, A, 2.7f).Only(A).Exit(4.3f, 0.7f);
        L(g, "marker", 6.1f - 1.96f, 0.45f, 14, A, 2.8f, E.Pop).Only(A).Exit(4.3f, 0.7f).Move(3.1f, 4.1f, 3.52f);
        Txt(g, "FOTO\nDIGITAL", 6.1f - 1.96f, 1.55f, 2.4f, Paper, 14, A, 2.8f, width: 3f).Only(A).Exit(4.3f, 0.7f).Move(3.1f, 4.1f, 3.52f);

        const string B = "s04_piece";
        L(g, "room_half", -4.8f, 0f, 2, B, 0f, E.Cut, name: "sala 1975").Only(B).Tint(new Color(1f, 0.8f, 0.55f));
        L(g, "room_half", 4.8f, 0f, 2, B, 0f, E.Cut, name: "sala hoje").Only(B).Tint(new Color(0.5f, 0.66f, 0.74f));
        L(g, "tear", 0f, 0f, 6, B, 0f, E.Cut).Only(B);
        L(g, "piece_glow", 0f, 0.3f, 19, B, 0.2f, E.Fade, 1.8f).Only(B).Do(l => { l.flicker = 0.3f; l.bob = 0.12f; l.bobSpeed = 0.4f; });
        L(g, "temporal_piece", 0f, 0.3f, 20, B, 0.2f, E.Pop, 1.6f).Only(B).Do(l => { l.bob = 0.12f; l.bobSpeed = 0.4f; l.swayDegrees = 5f; l.swaySpeed = 0.3f; });
        return g;
    }

    // ---------- G4: gravação do pesquisador ----------
    static Transform BuildRecording(Transform g)
    {
        string[] all = { "s05_pull", "s05_rec", "s07_rec", "s10_end" };
        L(g, "rec_bg", 0, 0, 0, "s05_pull", 0f, E.Cut);
        L(g, "crt_cold", -5.4f, 2.4f, 5, "s05_pull", 0f, E.Cut, name: "monitor 1 (esquema)");
        L(g, "mon_schematic", -5.4f, 2.567f, 6, "s05_pull", 0f, E.Cut);
        L(g, "crt_cold", 5.4f, 2.4f, 5, "s05_pull", 0f, E.Cut, name: "monitor 2 (forma de onda)");
        L(g, "mon_wave_0", 5.4f, 2.567f, 6, "s05_pull", 0f, E.Cut).Do(l => { l.frames = Frames("mon_wave_"); l.fps = 8; });
        L(g, "mon_gauges", 5.4f, 2.567f, 7, "s07_rec", 0f, E.Cut).Only("s07_rec");
        L(g, "crt_cold", 2.9f, 3.2f, 3, "s05_pull", 0f, E.Cut, 0.8f, name: "monitor 3 (estática)");
        L(g, "mon_static_0", 2.9f, 3.333f, 4, "s05_pull", 0f, E.Cut, 0.8f).Do(l => { l.frames = Frames("mon_static_"); l.fps = 14; });
        L(g, "mon_saboteur", 2.9f, 3.333f, 5, "s10_end", 3.2f, E.Cut, 0.8f, name: "sabotador no monitor 3").Only("s10_end").Exit(4.1f, 0.1f).Do(l => l.flicker = 0.45f);

        L(g, "researcher_body", 0f, -2.4f, 10, "s05_pull", 0.8f, E.Fade, 1.0f).Do(l => { l.enterDuration = 0.8f; l.bob = 0.02f; l.bobSpeed = 0.2f; });
        var head = L(g, "researcher_head", 0f, 2.1f, 11, "s10_end", -100f, E.Cut, 0.95f).Move(4.2f, 4.6f, 0.08f, 0f, -10f).Do(l => { l.talkSway = 1.5f; l.bob = 0.02f; l.bobSpeed = 0.25f; });
        L(head.transform, "r_lids", 0f, 0f, 12, "s05_pull", 0f, E.Cut).Do(l => l.blink = true);
        L(head.transform, "r_mouth", 0f, 0f, 12, "s05_pull", 0f, E.Cut).Do(l => l.talk = true);

        // braços como poses de recorte trocadas no tempo (substituição de pose)
        CutoutLayer Arm(string sprite, float x, float y, float rot, string anchor, float enter, float exit, string name, string[] only) =>
            L(g, sprite, x, y, 13, anchor, enter, E.Cut, 0.95f, rot, name).Exit(exit, 0f).Only(only).Do(a => a.talkSway = 2f);
        Arm("r_arm_rest", -2.0f, 1.6f, 12f, "s05_rec", -3f, 2.4f, "braço dir. repouso 1", new[] { "s05_pull", "s05_rec" });
        Arm("r_arm_present", -2.0f, 1.55f, 70f, "s05_rec", 2.4f, 5.0f, "braço dir. apresenta câmera", new[] { "s05_rec" });
        Arm("r_arm_rest", -2.0f, 1.6f, 12f, "s05_rec", 5.0f, 6.6f, "braço dir. repouso 2", new[] { "s05_rec" });
        Arm("r_arm_point", -2.0f, 1.55f, -16f, "s05_rec", 6.6f, 9.6f, "braço dir. aponta esquema", new[] { "s05_rec" });
        Arm("r_arm_rest", -2.0f, 1.6f, 12f, "s05_rec", 9.6f, -1f, "braço dir. repouso 3", new[] { "s05_rec", "s07_rec" });
        Arm("r_arm_rest", -2.0f, 1.6f, 12f, "s10_end", 0f, 7.9f, "braço dir. repouso 4", new[] { "s10_end" });
        Arm("r_arm_present", -2.0f, 1.55f, 66f, "s10_end", 7.9f, -1f, "braço dir. desliga gravação", new[] { "s10_end" }).Move(7.9f, 8.3f, 0f, -0.25f, 8f);
        Arm("r_arm_rest", 2.0f, 1.6f, -12f, "s05_pull", 0f, -1f, "braço esq. repouso", new[] { "s05_pull", "s05_rec", "s10_end" }).Sz(-0.95f, 0.95f);
        Arm("r_arm_rest", 2.0f, 1.6f, -12f, "s07_rec", 0f, 0.3f, "braço esq. repouso s07a", new[] { "s07_rec" }).Sz(-0.95f, 0.95f);
        Arm("r_arm_point", 2.0f, 1.55f, 16f, "s07_rec", 0.3f, 2.6f, "braço esq. aponta medidores", new[] { "s07_rec" }).Sz(-0.95f, 0.95f);
        Arm("r_arm_rest", 2.0f, 1.6f, -12f, "s07_rec", 2.6f, -1f, "braço esq. repouso s07b", new[] { "s07_rec" }).Sz(-0.95f, 0.95f);

        L(g, "rec_bench", 0f, -3.6f, 20, "s05_pull", 0.6f).Do(l => l.settle = 0.05f);
        L(g, "keyboard", -3.4f, -1.55f, 21, "s05_pull", 1.2f);
        L(g, "oscilloscope", 5.6f, -0.6f, 21, "s05_pull", 1.4f);
        L(g, "mon_wave_0", 5.0f, -0.3f, 22, "s05_pull", 1.6f, E.Cut, 0.28f, name: "traço do osciloscópio").Do(l => { l.frames = Frames("mon_wave_"); l.fps = 10; });
        L(g, "tapes", -6.9f, -1.0f, 21, "s05_pull", 1.6f);
        L(g, "rec_wires", 3.4f, -1.5f, 22, "s05_pull", 1.8f, scale: 0.7f).Do(l => { l.swayDegrees = 1.2f; l.swaySpeed = 0.25f; });
        L(g, "schematic_paper", 3.1f, -2.4f, 22, "s05_pull", 2.0f, scale: 0.7f, rot: -6f).Do(l => { l.bob = 0.02f; l.swayDegrees = 0.8f; });
        L(g, "piece_glow", -3.0f, -1.15f, 22, "s05_pull", 2.6f, E.Fade, 0.45f, name: "brilho câmera temporal").Do(l => l.flicker = 0.2f);
        L(g, "temporal_cam", -3.0f, -1.15f, 23, "s05_pull", 2.4f, scale: 0.6f).Do(l => { l.swayDegrees = 1.2f; l.swaySpeed = 0.35f; l.settle = 0.15f; });

        L(g, "white", 0, 0, 70, "s05_pull", 0f, E.Cut, name: "verde-fósforo da gravação").Sz(26f, 14f).Tint(new Color(0.06f, 0.24f, 0.14f, 0.16f));
        Txt(g, "REC  1984-11-16  03:12", -5.2f, 4.7f, 2.6f, Phos, 75, "s05_pull", 0f, E.Cut, 8f, TextAlignmentOptions.Left).Do(l => l.flicker = 0.08f);

        L(g, "white", 0, 0, 80, "s10_end", 8.4f, E.Cut, name: "CRT desliga (preto)").Only("s10_end").Sz(30f, 16f).Tint(Color.black);
        L(g, "white", 0.6f, 1.2f, 81, "s10_end", 8.45f, E.Cut, name: "CRT desliga (linha)").Only("s10_end").Sz(16f, 0.05f).Exit(8.95f, 0.1f).Do(l => { l.moveStart = 8.5f; l.moveEnd = 8.9f; l.fillX = new Vector2(1f, 0f); });
        return g;
    }

    // ---------- G5: demonstração ilustrada (visão do jogador) + reprodução do HUD ----------
    static void Bracket(Transform parent, string key, float x, float y, float size, int order, string anchor, float enter, float exit, string only) =>
        L(parent, "bracket", x, y, order, anchor, enter, E.Pop, size, name: "contorno " + key).Only(only).Exit(exit, 0.15f).Do(l => l.flicker = 0.2f);

    static void Hud(Transform g, string anchor, float open, float close, string only, float closeDur = 0.25f, bool slide = true)
    {
        var top = L(g, "white", 0f, 4.7f, 80, anchor, open, slide ? E.Drop : E.Cut, name: "borda superior").Only(only).Sz(19.2f, 1.4f).Tint(Color.black).Exit(close, closeDur);
        top.dropHeight = 1.4f; top.enterDuration = 0.3f; top.settle = 0f;
        var bottom = L(g, "white", 0f, -4.7f, 80, anchor, open, slide ? E.Drop : E.Cut, name: "borda inferior").Only(only).Sz(19.2f, 1.4f).Tint(Color.black).Exit(close, closeDur);
        bottom.dropHeight = -1.4f; bottom.enterDuration = 0.3f; bottom.settle = 0f;
        L(g, "hud_frame", 0, 0, 81, anchor, open + 0.1f, E.Fade).Only(only).Exit(close, closeDur);
        L(g, "crosshair", 0, 0, 82, anchor, open + 0.1f, E.Fade).Only(only).Exit(close, closeDur);
        L(g, "white", -8.1f, 3.6f, 82, anchor, open + 0.1f, E.Fade, 0.2f, name: "ponto REC").Only(only).Exit(close, closeDur).Tint(AlertC).Do(l => l.flicker = 0.5f);
        Txt(g, "REC", -7.2f, 3.6f, 3f, Color.white, 82, anchor, open + 0.1f, E.Fade, 2f).Only(only).Exit(close, closeDur);
        L(g, "battery_frame", 5.6f, 3.6f, 82, anchor, open + 0.1f, E.Fade).Only(only).Exit(close, closeDur);
    }

    static CutoutLayer Key(Transform g, string label, float x, float y, string anchor, float enter, float exit, string only)
    {
        var k = L(g, "keycap", x, y, 85, anchor, enter, E.Pop, 0.8f, name: "tecla " + label).Only(only).Exit(exit, 0.15f);
        Txt(k.transform, label, 0f, 0.04f, 6f, new Color(0.1f, 0.08f, 0.07f), 86, anchor, enter, E.Cut, 1.3f).Only(only).Exit(exit, 0.15f);
        return k;
    }

    static void Prompt(Transform g, string text, float y, string anchor, float enter, float exit, string only, Color color)
    {
        L(g, "prompt_bg", 0f, y, 84, anchor, enter, E.Pop, name: "fundo " + text).Only(only).Sz(text.Length * 0.21f + 1.4f, 0.9f).Exit(exit, 0.15f);
        Txt(g, text, 0f, y, 4.2f, color, 85, anchor, enter, E.Pop, 12f).Only(only).Exit(exit, 0.15f);
    }

    static Transform BuildGameplay(Transform g)
    {
        Color steps = new Color(0.95f, 0.9f, 0.78f);

        // --- Localize, fotografe, colete ---
        const string A = "s06_play";
        var pan = L(g, null, 2.2f, 0f, 0, A, 0f, E.Cut, name: "rig pan (mira)").Only(A).Move(4.5f, 5.8f, -2.2f).transform;
        var zoom = L(pan, null, 0f, 0f, 0, A, 0f, E.Cut, name: "rig aproximação").Only(A).Move(10.0f, 11.5f, scaleTo: 1.6f).transform;
        L(zoom, "pov_bench", 0, 0, 1, A, 0f, E.Cut).Only(A);
        L(zoom, "temporal_piece", 0f, -0.5f, 2, A, 2.6f, E.Fade, 0.8f, name: "peça (falha na imagem)").Only(A).Exit(7.3f, 0f).Do(l => { l.enterDuration = 0.3f; l.flicker = 0.55f; });
        L(zoom, "temporal_piece", 0f, -0.5f, 2, A, 7.3f, E.Cut, 0.8f, name: "peça revelada").Only(A).Exit(13.0f, 0.25f);
        Bracket(zoom, "piece", 0f, -0.5f, 0.65f, 3, A, 2.6f, 13.0f, A);
        L(zoom, "timer_ring", 1.3f, 0.4f, 4, A, 8.6f, E.Pop, 0.5f).Only(A).Exit(13.0f, 0.15f).Do(l => l.spin = -90f);
        Hud(g, A, 1.8f, 8.5f, A);
        L(g, "battery_bars", 5.7f, 3.6f, 83, A, 1.9f, E.Fade).Only(A).Exit(8.5f, 0.25f);
        Key(g, "C", 0f, -2.6f, A, 1.3f, 2.3f, A);
        L(g, "mouse_icon", -1.4f, -2.6f, 85, A, 6.0f, E.Pop, 0.7f).Only(A).Exit(7.6f, 0.15f);
        Txt(g, "Clique esquerdo", 0.9f, -2.6f, 3.2f, Color.white, 85, A, 6.0f, E.Pop, 5f).Only(A).Exit(7.6f, 0.15f);
        L(g, "white", 0, 0, 90, A, 7.3f, E.Cut, name: "flash da foto").Only(A).Sz(20f, 12f).Exit(7.32f, 0.25f);
        Key(g, "C", 0f, -2.6f, A, 8.2f, 9.0f, A);
        Key(g, "E", -2.9f, -2.2f, A, 11.8f, 13.0f, A);
        Prompt(g, "E para coletar", -2.2f, A, 11.8f, 13.0f, A, Color.white);
        Txt(g, "Localize", -4.2f, 3.1f, 6f, steps, 87, A, 1.3f).Only(A);
        Txt(g, ">  Fotografe", 0f, 3.1f, 6f, steps, 87, A, 6.0f).Only(A);
        Txt(g, ">  Colete", 4.0f, 3.1f, 6f, steps, 87, A, 11.8f).Only(A);
        L(g, "prompt_bg", -6.2f, -3.6f, 84, A, 0.5f, E.Fade, name: "painel de missões").Only(A).Sz(5.6f, 0.9f);
        Txt(g, "Coletar peças [0/5]", -6.2f, -3.6f, 3.2f, Color.white, 85, A, 0.5f, E.Fade, 5.4f).Only(A).Exit(13.6f, 0f);
        Txt(g, "Coletar peças [1/5]", -6.2f, -3.6f, 3.2f, Phos, 85, A, 13.6f, E.Pop, 5.4f).Only(A);

        // --- bateria e calor ---
        const string B = "s07_meter";
        L(g, "pov_bench", 0, 0, 1, B, 0f, E.Cut).Only(B);
        L(g, "piece_glow", -5.6f, -0.9f, 2, B, 7.4f, E.Fade, 0.6f, name: "brilho da pilha").Only(B).Do(l => l.flicker = 0.25f);
        L(g, "battery_pickup", -5.6f, -0.9f, 3, B, 7.4f, E.Drop, 0.9f).Only(B).Do(l => { l.swayDegrees = 2f; l.dropHeight = 4f; });
        Bracket(g, "battery", -5.6f, -0.9f, 0.85f, 4, B, 7.8f, 11.4f, B);
        Txt(g, "Pilhas recarregam a bateria", -5.6f, 0.9f, 3.2f, Phos, 86, B, 8.0f, E.Pop, 8f).Only(B);
        Hud(g, B, 0f, 4.4f, B, slide: false);
        L(g, "battery_bars", 5.7f, 3.6f, 83, B, 0f, E.Cut, name: "bateria gastando").Only(B).Exit(4.4f, 0f).Do(l => { l.moveStart = 0.5f; l.moveEnd = 4.0f; l.fillX = new Vector2(1f, 0.6f); l.linear = 1f; });
        L(g, "battery_frame", 5.6f, 3.6f, 82, B, 4.4f, E.Cut, name: "bateria (visor fechado)").Only(B);
        L(g, "battery_bars", 5.7f, 3.6f, 83, B, 4.4f, E.Cut, name: "bateria guardada").Only(B).Do(l => l.fillX = new Vector2(0.6f, 0.6f));
        L(g, "heat_frame", 5.6f, 2.9f, 82, B, 0f, E.Cut).Only(B);
        Txt(g, "CALOR", 4.6f, 2.9f, 2.4f, Color.white, 82, B, 0f, E.Cut, 2f).Only(B);
        L(g, "white_left", 5.65f, 2.9f, 83, B, 0f, E.Cut, name: "calor subindo").Only(B).Sz(3.1f, 0.3f).Tint(AlertC).Exit(4.4f, 0f)
            .Do(l => { l.moveStart = 0.5f; l.moveEnd = 4.0f; l.fillX = new Vector2(0f, 0.7f); l.linear = 1f; });
        L(g, "white_left", 5.65f, 2.9f, 83, B, 4.4f, E.Cut, name: "calor recuando").Only(B).Sz(3.1f, 0.3f).Tint(AlertC)
            .Do(l => { l.moveStart = 4.6f; l.moveEnd = 7.9f; l.fillX = new Vector2(0.7f, 0f); l.linear = 1f; });
        Key(g, "C", 0f, -2.6f, B, 4.0f, 4.8f, B);
        Txt(g, "Feche o visor entre as buscas", 0f, -3.6f, 3.2f, Color.white, 86, B, 4.4f, E.Pop, 10f).Only(B).Exit(7.3f, 0.2f);

        // --- superaquecimento e ruptura ---
        const string C = "s08_overheat";
        L(g, "pov_bench", 0, 0, 1, C, 0f, E.Cut).Only(C);
        L(g, "pov_bench", 0, 0, 2, C, 2.0f, E.Fade, name: "desvio vermelho").Only(C).Tint(new Color(1f, 0.3f, 0.3f, 0.45f)).Exit(4.0f, 0f).Move(2.0f, 4.0f, 0.15f).Do(l => { l.enterDuration = 2.0f; l.linear = 1f; });
        L(g, "pov_bench", 0, 0, 2, C, 2.0f, E.Fade, name: "desvio ciano").Only(C).Tint(new Color(0.3f, 0.9f, 1f, 0.35f)).Exit(4.0f, 0f).Move(2.0f, 4.0f, -0.15f).Do(l => { l.enterDuration = 2.0f; l.linear = 1f; });
        Hud(g, C, 0f, 4.5f, C, closeDur: 0f, slide: false);
        L(g, "battery_bars", 5.7f, 3.6f, 83, C, 0f, E.Cut).Only(C).Exit(4.5f, 0f).Do(l => l.fillX = new Vector2(0.5f, 0.5f));
        L(g, "heat_frame", 5.6f, 2.9f, 82, C, 0f, E.Cut).Only(C).Exit(4.5f, 0f);
        L(g, "white_left", 5.65f, 2.9f, 83, C, 0f, E.Cut, name: "calor no limite").Only(C).Sz(3.1f, 0.3f).Tint(AlertC).Exit(4.5f, 0f)
            .Do(l => { l.moveStart = 0.3f; l.moveEnd = 4.0f; l.fillX = new Vector2(0.6f, 1f); l.linear = 1f; });
        L(g, "white", 0, 0, 85, C, 4.0f, E.Cut, name: "clarão da ruptura").Only(C).Sz(20f, 12f).Exit(4.08f, 0.2f);
        L(g, "mon_static_0", 0, 0, 84, C, 4.0f, E.Cut, name: "estática da ruptura").Only(C).Sz(9.6f, 7.1f).Exit(4.5f, 0.05f).Do(l => { l.frames = Frames("mon_static_"); l.fps = 20; });
        var shutTop = L(g, "white", 0f, 2.7f, 88, C, 4.35f, E.Drop, name: "obturador cima").Only(C).Sz(19.2f, 5.4f).Tint(Color.black).Exit(4.9f, 0.9f);
        shutTop.dropHeight = 5.4f; shutTop.enterDuration = 0.25f; shutTop.settle = 0f;
        var shutBottom = L(g, "white", 0f, -2.7f, 88, C, 4.35f, E.Drop, name: "obturador baixo").Only(C).Sz(19.2f, 5.4f).Tint(Color.black).Exit(4.9f, 0.9f);
        shutBottom.dropHeight = -5.4f; shutBottom.enterDuration = 0.25f; shutBottom.settle = 0f;
        Key(g, "C", -1.0f, -2.6f, C, 6.2f, 7.8f, C);
        L(g, "cross_x", 0.3f, -2.6f, 87, C, 6.5f, E.Pop, 0.6f).Only(C).Exit(7.8f, 0.15f).Do(l => { l.swayDegrees = 8f; l.swaySpeed = 3f; });
        Txt(g, "Câmera perdida", 0f, -3.6f, 3.4f, AlertC, 86, C, 6.5f, E.Pop, 8f).Only(C).Exit(7.8f, 0.15f);

        // --- recuperação em outro ponto do laboratório ---
        const string D = "s09_lost";
        var zl = L(g, null, 0f, 0f, 0, D, 0f, E.Cut, name: "rig aproximação câmera perdida").Only(D).Move(2.5f, 4.5f, scaleTo: 1.45f).transform;
        L(zl, "pov_corner", 0, 0, 1, D, 0f, E.Cut).Only(D);
        L(zl, "piece_glow", 0f, -2.6f, 2, D, 0f, E.Cut, 0.7f, name: "brilho câmera perdida").Only(D).Exit(6.0f, 0.2f).Do(l => l.flicker = 0.3f);
        L(zl, "temporal_cam", 0f, -2.6f, 3, D, 0f, E.Cut, 0.55f, 12f, name: "câmera perdida no chão").Only(D).Exit(6.0f, 0.2f);
        Bracket(zl, "lostcam", 0f, -2.6f, 0.75f, 4, D, 0.6f, 6.0f, D);
        Txt(g, "Outro ponto do laboratório", 0f, 3.6f, 3.6f, Color.white, 86, D, 0.3f, E.Fade, 12f).Only(D).Exit(3.0f, 0.3f);
        Key(g, "E", -4.3f, -2.2f, D, 4.6f, 6.0f, D);
        Prompt(g, "E para recuperar a camera", -2.2f, D, 4.6f, 6.0f, D, Color.white);
        Txt(g, "Câmera recuperada", 0f, -2.2f, 3.6f, Phos, 86, D, 6.1f, E.Pop, 10f).Only(D);

        // --- primeira cena jogável ---
        const string F = "s11_start";
        L(g, "comic_bg", 0, 0, 1, F, 0f, E.Cut, name: "fundo final").Only(F).Tint(new Color(0.35f, 0.35f, 0.35f));
        L(g, "prompt_bg", 0f, -2.6f, 84, F, 0.4f, E.Pop, name: "fundo objetivo").Only(F).Sz(15.5f, 1.3f);
        Txt(g, "Recupere as peças e reconstrua o protótipo", 0f, -2.6f, 5.6f, Paper, 85, F, 0.4f, E.Pop, 12.5f).Only(F);
        return g;
    }

    // ------------------------------------------------------------------ prévia

    static void Export(float[] stills)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var d = UnityEngine.Object.FindFirstObjectByType<IntroCutsceneDirector>();
        d.Init();
        string outDir = Path.GetFullPath(PreviewDir);
        string frameDir = Path.Combine(outDir, stills == null ? "frames" : "stills");
        if (Directory.Exists(frameDir)) Directory.Delete(frameDir, true);
        Directory.CreateDirectory(frameDir);

        var rt = d.cam.targetTexture; // a mesma tela de 640x360 do jogo
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (d.skipHint != null) d.skipHint.alpha = 0f;
        float[] times = stills ?? Enumerable.Range(0, Mathf.CeilToInt(d.duration * 30f)).Select(i => i / 30f).ToArray();
        for (int i = 0; i < times.Length; i++)
        {
            if (i % 30 == 0 && EditorUtility.DisplayCancelableProgressBar("Prévia da cutscene", $"{times[i]:0.0}s", i / (float)times.Length)) break;
            float t = times[i];
            d.Evaluate(t);
            if (t > d.duration - d.exitFade)
                d.fade.color = new Color(0, 0, 0, Mathf.Clamp01((t - (d.duration - d.exitFade)) / d.exitFade));
            foreach (var x in texts) if (x.isActiveAndEnabled) x.ForceMeshUpdate();
            d.cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            string name = stills == null ? $"f_{i:D5}.png" : $"t_{t:000.0}s.png".Replace(',', '.');
            File.WriteAllBytes(Path.Combine(frameDir, name), tex.EncodeToPNG());
        }
        rt.Release();
        UnityEngine.Object.DestroyImmediate(tex);
        if (stills == null) MixAudio(d, Path.Combine(outDir, "mix.wav"));
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); // descarta o estado avaliado
        Log("prévia exportada em " + frameDir + (stills == null ? $" ({times.Length} quadros a 30 fps). Codifique: ffmpeg -framerate 30 -i frames/f_%05d.png -i mix.wav -vf scale=1920:1080:flags=neighbor -c:v libx264 -pix_fmt yuv420p -crf 18 -c:a aac -shortest cutscene_preview_1080p30.mp4" : ""));
    }

    // Mixagem offline da prévia: voz, efeitos, trilha e ambiente nas curvas do diretor.
    static void MixAudio(IntroCutsceneDirector d, string path)
    {
        const int rate = 44100;
        var mix = new float[(int)(d.duration * rate)];
        void Add(AudioClip c, float at, float vol, Func<float, float> curve = null, bool loop = false)
        {
            if (c == null) return;
            try
            {
                var data = new float[c.samples * c.channels];
                if (!c.GetData(data, 0)) { Log("sem dados de áudio (ajuste Load Type): " + c.name); return; }
                double step = c.frequency / (double)rate;
                int start = (int)(at * rate);
                for (int i = start; i < mix.Length; i++)
                {
                    double src = (i - start) * step;
                    int s = (int)src;
                    if (s >= c.samples) { if (!loop) break; s %= c.samples; }
                    float v = 0f;
                    for (int ch = 0; ch < c.channels; ch++) v += data[s * c.channels + ch];
                    v /= c.channels;
                    mix[i] += v * vol * (curve != null ? curve(i / (float)rate) : 1f);
                }
            }
            catch (Exception e) { Log("falha ao mixar " + c.name + ": " + e.Message); }
        }
        Add(d.music.clip, 0f, 1f, t => d.musicVolume.Evaluate(t), true);
        Add(d.ambience.clip, 0f, 1f, t => d.ambienceVolume.Evaluate(t), true);
        foreach (var c in d.cues) Add(c.clip, c.time, c.volume);
        foreach (var l in d.lines) Add(l.clip, l.start, 1f);
        for (int i = 0; i < mix.Length; i++)
        {
            float t = i / (float)rate;
            if (t > d.duration - d.exitFade) mix[i] *= Mathf.Clamp01((d.duration - t) / d.exitFade);
            mix[i] = (float)Math.Tanh(mix[i]);
        }
        IntroCutsceneAudio.WriteWav(path, mix);
    }
}
