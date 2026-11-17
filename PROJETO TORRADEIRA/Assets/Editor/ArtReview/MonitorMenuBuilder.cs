using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Monta o menu principal numa sala escura de vigilancia: estantes de metal no chao com
// monitores CRT, luz vermelha ao fundo, janela fria com feixe de luz, nevoa e pos-processo.
// Pode ser executado de novo: recria o cenario e reaproveita os botoes existentes.
public static class MonitorMenuBuilder
{
    private const string PsxFolder = "Assets/ThirdParty/PSXNostalgia-FBX/PSXNostalgia-FBX/";
    private const string TexFolder = "Assets/ThirdParty/Facility-7 Genetics/Level Textures/";
    private const string ArtFolder = "Assets/Art/UI/PixelArt/";
    private const string MaterialFolder = "Assets/Art/Materials/Menu";
    private const string PixelFontPath = "Assets/Fonts/VCR_OSD_MONO SDF.asset";
    private const string MenuProfilePath = "Assets/Scenes/Menu/MenuVolume.asset";
    private const string SetName = "MenuSet";

    // Altura do chao e do teto em relacao ao olho da camera.
    private const float Floor = -1.55f;
    private const float Ceiling = 1.9f;

    // Vidro da tela no modelo TV.fbx, medido na malha (espaco local; a tela olha para +Z).
    private static readonly Vector3 TvScreenCenter = new Vector3(-0.008f, 0.443f, 0.184f);
    private static readonly Vector2 TvScreenSize = new Vector2(0.672f, 0.49f);

    private static readonly Color ScreenDark = new Color(0.07f, 0.075f, 0.08f, 1f);
    private static readonly Color Phosphor = new Color(0.95f, 0.95f, 0.92f, 1f);
    private static readonly Color TextIdle = new Color(0.62f, 0.62f, 0.6f, 1f);
    private static readonly Color LedRed = new Color(1f, 0.16f, 0.12f, 1f);

    private enum ScreenKind { Menu, Static, Feed, Blown }

    private static TMP_FontAsset font;
    private static Camera menuCamera;
    private static Transform prototypeLens;
    private static Transform setRoot;
    private static PhotoFlash playerFacingCamera;

    // O que a TV principal mostra; o resto da sala e igual nas tres cenas.
    public enum Mode { Menu, Start, Credits }

    private static Mode mode;

    [MenuItem("Ferramentas/Pixel Art/Montar menu em monitores CRT")]
    public static void Build()
    {
        Build(Mode.Menu);
    }

    [MenuItem("Ferramentas/Pixel Art/Montar tela inicial em monitores CRT")]
    public static void BuildStart()
    {
        Build(Mode.Start);
    }

    [MenuItem("Ferramentas/Pixel Art/Montar creditos em monitores CRT")]
    public static void BuildCredits()
    {
        Build(Mode.Credits);
    }

    public static void Build(Mode buildMode)
    {
        mode = buildMode;
        menuCamera = FindMenuCamera();

        if (menuCamera == null)
        {
            Debug.LogError("MonitorMenuBuilder: abra a cena " + (mode == Mode.Start ? "TelaInicio" : mode == Mode.Credits ? "Credits" : "Menu") + " (nenhuma camera encontrada).");
            return;
        }

        ConfigureTextures();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);

        Button play = mode == Mode.Menu ? FindButton("PlayButton", "Play") : null;
        Button credits = mode == Mode.Menu ? FindButton("CreditsButton", "Credits") : null;
        Button quit = mode == Mode.Menu ? FindButton("QuitButton", "Leave") : null;

        // Tira os botoes do cenario antigo antes de recria-lo.
        Canvas overlay = FindOverlayCanvas();
        foreach (Button b in new[] { play, credits, quit })
        {
            if (b != null && overlay != null)
                Undo.SetTransformParent(b.transform, overlay.transform, "Menu");
        }

        foreach (string old in new[] { SetName, "MonitorWall", "StartScreen" })
        {
            GameObject found = GameObject.Find(old);
            if (found != null)
                Undo.DestroyObjectImmediate(found);
        }

        // A sala antiga e o sol deixam de ser usados: o cenario e todo construido aqui.
        Deactivate("Room");
        Deactivate("Directional Light");

        GameObject set = new GameObject(SetName);
        Undo.RegisterCreatedObjectUndo(set, "Menu CRT");
        Vector3 forward = Vector3.ProjectOnPlane(menuCamera.transform.forward, Vector3.up).normalized;
        set.transform.SetPositionAndRotation(menuCamera.transform.position, Quaternion.LookRotation(forward, Vector3.up));
        Transform root = set.transform;
        setRoot = root;

        BuildRoom(root);
        BuildConsole(root);
        BuildLeftRack(root, play, credits, quit);
        BuildRightRack(root);
        BuildBackWall(root);
        BuildCables(root);
        BuildLights(root);
        BuildPhotoCameras(root);

        if (overlay != null)
        {
            Transform oldPanel = overlay.transform.Find("MenuPanel");
            if (oldPanel != null)
                Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        if (mode == Mode.Start)
            SetupStartScene();
        else if (mode == Mode.Credits)
            SetupCreditsScene();

        if (mode != Mode.Menu)
            FrameLikeMenu();

        ConfigureCamera();
        ConfigureAtmosphere();
        ConfigureVolume();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("MonitorMenuBuilder: sala montada (" + mode + ").");
    }

    // ---------- Tela inicial ----------

    private static TextMeshProUGUI startPrompt;

    private static void BuildStartScreen(Transform tv)
    {
        RectTransform screen = CreateScreenCanvas(tv, "StartScreen_TV", out CanvasGroup group);

        TextMeshProUGUI title = Text(screen, "Title", "OUT OF TIME", 70f, Phosphor);
        title.alignment = TextAlignmentOptions.Center;
        Stretch(title.rectTransform, new Vector2(0f, 60f), new Vector2(0f, 0f));

        TextMeshProUGUI sub = Text(screen, "Subtitle", "LAB 1975 // FITA 01", 24f, TextIdle);
        sub.alignment = TextAlignmentOptions.Top;
        Stretch(sub.rectTransform, Vector2.zero, new Vector2(0f, -30f));

        startPrompt = Text(screen, "Prompt", "PRESSIONE QUALQUER TECLA", 32f, Phosphor);
        startPrompt.alignment = TextAlignmentOptions.Center;
        Stretch(startPrompt.rectTransform, new Vector2(0f, 0f), new Vector2(0f, -250f));
        Blinker(startPrompt.gameObject, MenuBlinker.Mode.Blink, 1.2f, 0.65f, null, Color.white, Color.white, null, 0f, startPrompt);

        TextMeshProUGUI footer = Text(screen, "Footer", "PROTOTIPO CCD 100x100 // 1975", 24f, new Color(0.5f, 0.5f, 0.48f));
        footer.alignment = TextAlignmentOptions.Bottom;
        Stretch(footer.rectTransform, new Vector2(0f, 26f), Vector2.zero);

        TextMeshProUGUI rec = Text(screen, "Rec", "[REC]", 24f, LedRed);
        rec.alignment = TextAlignmentOptions.TopRight;
        Stretch(rec.rectTransform, Vector2.zero, new Vector2(-30f, -30f));
        Blinker(rec.gameObject, MenuBlinker.Mode.Blink, 1f, 0.6f, null, Color.white, Color.white, null, 0f, rec);

        AddCanvasOverlay(screen);
        AddScreenGlow(tv, screen, group);
    }

    // Desliga a UI 2D antiga, monta o fade e o script que leva ao menu.
    private static void SetupStartScene()
    {
        // UI 2D e cenario antigo da tela inicial (escritorio noir e cameras Canon).
        foreach (string old in new[] { "Canvas", "Lights", "Point Light", "Point Light ",
                     "detectives_office_in_noir_style", "camera_canon_eos_400d", "camera_canon_eos_400d (1)" })
            Deactivate(old);


        foreach (StartScreenManager manager in Object.FindObjectsByType<StartScreenManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(manager, "Tela inicial");
            manager.enabled = false;
        }

        foreach (StartScreenHandler handler in Object.FindObjectsByType<StartScreenHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(handler, "Tela inicial");
            handler.enabled = false;
        }

        GameObject go = new GameObject("StartScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        Undo.RegisterCreatedObjectUndo(go, "Tela inicial");
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        Image black = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        black.transform.SetParent(go.transform, false);
        black.color = Color.black;
        black.raycastTarget = false;
        Stretch(black.rectTransform, Vector2.zero, Vector2.zero);

        CanvasGroup fade = go.GetComponent<CanvasGroup>();
        fade.alpha = 0f;
        fade.blocksRaycasts = false;
        fade.interactable = false;

        StartScreenCrt start = go.AddComponent<StartScreenCrt>();
        SerializedObject so = new SerializedObject(start);
        so.FindProperty("fade").objectReferenceValue = fade;
        so.FindProperty("prompt").objectReferenceValue = startPrompt;
        so.FindProperty("shutter").objectReferenceValue = playerFacingCamera;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Mesmo enquadramento da camera do menu: sem rolagem, leve inclinacao para baixo.
    private static void FrameLikeMenu()
    {
        Undo.RecordObject(menuCamera.transform, "Camera");
        menuCamera.transform.rotation = setRoot.rotation * Quaternion.Euler(3.6f, 0f, 0f);
        Undo.RecordObject(menuCamera, "Camera");
        menuCamera.fieldOfView = 60f;
    }

    // ---------- Creditos ----------

    private static readonly System.Collections.Generic.List<PhotoFlash> photoCameras = new System.Collections.Generic.List<PhotoFlash>();
    private static RectTransform creditsViewport;
    private static RectTransform creditsContent;
    private static readonly System.Collections.Generic.List<RectTransform> creditEntries = new System.Collections.Generic.List<RectTransform>();

    // Nomes e funcoes (vindos do texto antigo da cena Credits).
    private static readonly string[,] Credits =
    {
        { "MANOEL SCHEIDT ENDER", "PROGRAMADOR E GAME DESIGNER" },
        { "JÚLLYA DIELLY MARTINS MONTEIRO", "PROGRAMADORA E GAME DESIGNER" },
        { "GABRIEL PAMPLONA", "SONOPLASTA E PROGRAMADOR" },
    };

    private static void BuildCreditsScreen(Transform tv)
    {
        RectTransform screen = CreateScreenCanvas(tv, "CreditsScreen_TV", out CanvasGroup group);

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGo.transform.SetParent(screen, false);
        creditsViewport = (RectTransform)viewportGo.transform;
        Stretch(creditsViewport, new Vector2(90f, 56f), new Vector2(-30f, -56f));

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(creditsViewport, false);
        creditsContent = (RectTransform)contentGo.transform;
        creditsContent.anchorMin = new Vector2(0f, 1f);
        creditsContent.anchorMax = new Vector2(1f, 1f);
        creditsContent.pivot = new Vector2(0.5f, 1f);

        creditEntries.Clear();
        float y = 0f;

        CreditLine(creditsContent, "Title", "OUT OF TIME", 52f, Phosphor, ref y, 90f);
        CreditLine(creditsContent, "Header", "CRÉDITOS", 26f, TextIdle, ref y, 110f);
        CreditLine(creditsContent, "Section", "DESENVOLVEDORES", 24f, LedRed, ref y, 80f);

        for (int i = 0; i < Credits.GetLength(0); i++)
        {
            GameObject entryGo = new GameObject("Credit_" + (i + 1), typeof(RectTransform));
            entryGo.transform.SetParent(creditsContent, false);
            RectTransform entry = (RectTransform)entryGo.transform;
            PlaceRow(entry, y, 150f);

            float inner = 0f;
            TextMeshProUGUI name = CreditLine(entry, "Name", Credits[i, 0], 30f, Phosphor, ref inner, 50f);
            name.enableAutoSizing = true;
            name.fontSizeMin = 20f;
            name.fontSizeMax = 30f;
            CreditLine(entry, "Role", Credits[i, 1], 22f, TextIdle, ref inner, 40f);

            creditEntries.Add(entry);
            y += 150f;
        }

        y += 60f;
        CreditLine(creditsContent, "Thanks", "OBRIGADO POR JOGAR", 34f, Phosphor, ref y, 70f);
        CreditLine(creditsContent, "End", "LAB 1975 // FIM DA FITA", 22f, TextIdle, ref y, 50f);

        // Comeca abaixo da tela e sobe; o CreditsController termina quando y >= altura.
        creditsContent.sizeDelta = new Vector2(0f, y);
        creditsContent.anchoredPosition = new Vector2(0f, -(TvScreenSize.y * 1000f - 112f));

        TextMeshProUGUI back = Text(screen, "BackHint", "[ESC] VOLTAR", 20f, new Color(0.5f, 0.5f, 0.48f));
        back.alignment = TextAlignmentOptions.BottomLeft;
        Stretch(back.rectTransform, new Vector2(90f, 22f), Vector2.zero);

        TextMeshProUGUI rec = Text(screen, "Rec", "[REC]", 24f, LedRed);
        rec.alignment = TextAlignmentOptions.TopRight;
        Stretch(rec.rectTransform, Vector2.zero, new Vector2(-30f, -22f));
        Blinker(rec.gameObject, MenuBlinker.Mode.Blink, 1f, 0.6f, null, Color.white, Color.white, null, 0f, rec);

        AddCanvasOverlay(screen);
        AddScreenGlow(tv, screen, group);
    }

    private static TextMeshProUGUI CreditLine(RectTransform parent, string name, string value, float size, Color color, ref float y, float height)
    {
        TextMeshProUGUI text = Text(parent, name, value, size, color);
        text.alignment = TextAlignmentOptions.Center;
        PlaceRow(text.rectTransform, y, height);
        y += height;
        return text;
    }

    private static void PlaceRow(RectTransform rect, float y, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = new Vector2(0f, -y);
    }

    // Desliga a UI 2D antiga e liga o CreditsController existente ao rolo dentro da TV.
    private static void SetupCreditsScene()
    {
        Deactivate("Canvas");

        CreditsController controller = Object.FindFirstObjectByType<CreditsController>(FindObjectsInactive.Include);

        if (controller == null)
        {
            Debug.LogError("MonitorMenuBuilder: CreditsController nao encontrado na cena Credits.");
            return;
        }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("creditsContent").objectReferenceValue = creditsContent;
        so.FindProperty("scrollSpeed").floatValue = 38f;
        so.FindProperty("endDelay").floatValue = 3f;
        so.ApplyModifiedProperties();

        CreditsSnapshots snapshots = controller.GetComponent<CreditsSnapshots>();
        if (snapshots == null)
            snapshots = Undo.AddComponent<CreditsSnapshots>(controller.gameObject);

        TMP_Text feedLabel = null;
        foreach (TMP_Text text in setRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "Label" && text.transform.parent != null && text.transform.parent.name == "Overlay")
                feedLabel = text;
        }

        // Primeiro a camera do fundo (aparece no feed do prototipo), depois as que miram o jogador.
        photoCameras.Sort((a, b) => Rank(a).CompareTo(Rank(b)));

        SerializedObject snap = new SerializedObject(snapshots);
        snap.FindProperty("viewport").objectReferenceValue = creditsViewport;
        snap.FindProperty("feed").objectReferenceValue = setRoot.GetComponentInChildren<CrtCameraFeed>(true);
        snap.FindProperty("feedLabel").objectReferenceValue = feedLabel;

        SerializedProperty entries = snap.FindProperty("entries");
        entries.arraySize = creditEntries.Count;
        for (int i = 0; i < creditEntries.Count; i++)
            entries.GetArrayElementAtIndex(i).objectReferenceValue = creditEntries[i];

        SerializedProperty cams = snap.FindProperty("cameras");
        cams.arraySize = photoCameras.Count;
        for (int i = 0; i < photoCameras.Count; i++)
            cams.GetArrayElementAtIndex(i).objectReferenceValue = photoCameras[i];

        snap.ApplyModifiedProperties();
    }

    private static int Rank(PhotoFlash camera)
    {
        string rig = camera.transform.parent != null ? camera.transform.parent.name : "";
        return rig.EndsWith("Back") ? 0 : rig.EndsWith("Right") ? 1 : 2;
    }

    // ---------- Sala ----------

    private static void BuildRoom(Transform root)
    {
        Transform room = Group(root, "Room_Set");

        Material floor = LitMaterial("MenuFloor", "SterileTiles", new Color(0.32f, 0.34f, 0.35f), 0.9f, 0f, new Vector2(5f, 5f));
        Material wall = LitMaterial("MenuWall", "SterileTiles", new Color(0.24f, 0.25f, 0.26f), 0.45f, 0f, new Vector2(4f, 2f));
        Material ceiling = LitMaterial("MenuCeiling", "VentMetal", new Color(0.08f, 0.08f, 0.09f), 0.3f, 0.5f, new Vector2(3f, 3f));

        const float left = -3.4f, right = 4.2f, back = 6.2f, front = -1.5f;
        float width = right - left, depth = back - front, height = Ceiling - Floor;
        float cx = (left + right) * 0.5f, cz = (back + front) * 0.5f;

        Box(room, "Floor", new Vector3(cx, Floor - 0.05f, cz), new Vector3(width, 0.1f, depth), floor);
        Box(room, "Ceiling", new Vector3(cx, Ceiling + 0.05f, cz), new Vector3(width, 0.1f, depth), ceiling);
        Box(room, "WallBack", new Vector3(cx, Floor + height * 0.5f, back + 0.05f), new Vector3(width, height, 0.1f), wall);
        Box(room, "WallLeft", new Vector3(left - 0.05f, Floor + height * 0.5f, cz), new Vector3(0.1f, height, depth), wall);
        Box(room, "WallRight", new Vector3(right + 0.05f, Floor + height * 0.5f, cz), new Vector3(0.1f, height, depth), wall);
        Box(room, "WallFront", new Vector3(cx, Floor + height * 0.5f, front - 0.05f), new Vector3(width, height, 0.1f), wall);

        // Rodape e viga no teto quebram as superficies lisas.
        Material metal = MetalMaterial();
        Box(room, "Baseboard", new Vector3(cx, Floor + 0.08f, back - 0.02f), new Vector3(width, 0.16f, 0.06f), metal);
        Box(room, "CeilingBeam", new Vector3(cx, Ceiling - 0.12f, 3.2f), new Vector3(width, 0.24f, 0.3f), metal);
        Box(room, "Pipe", new Vector3(cx, Ceiling - 0.35f, back - 0.25f), new Vector3(width, 0.09f, 0.09f), metal);
    }

    // ---------- Estante da esquerda: TV do menu ----------

    private static void BuildLeftRack(Transform root, Button play, Button credits, Button quit)
    {
        Transform rack = Rack(root, "Rack_Menu", new Vector3(-0.8f, Floor, 1.6f), 1.45f, 0.95f, 0.84f, new[] { 0.10f, 0.46f, 0.84f }, 6f);

        Transform tv = Tv(rack, "Monitor_Menu", new Vector3(0f, 0.87f, 0f), 1.75f);
        if (mode == Mode.Start)
            BuildStartScreen(tv);
        else if (mode == Mode.Credits)
            BuildCreditsScreen(tv);
        else
            BuildMenuScreen(tv, play, credits, quit);

        // Prateleira do meio: osciloscopio com estatica e caixa de contador.
        Transform scope = Device(rack, "Oscilloscope", new Vector3(-0.32f, 0.49f, -0.08f), new Vector3(0.5f, 0.3f, 0.5f));
        AddQuad(scope, new Vector3(-0.06f, 0.02f, -0.251f), new Vector2(0.24f, 0.18f), ScreenMaterial("CrtStaticSmall", 0.8f)).AddComponent<CrtNoiseScreen>();
        Led(scope, new Vector3(0.15f, 0.06f, -0.252f), "08.5", 0.55f);
        LedRow(scope, new Vector3(0.08f, -0.09f, -0.252f), 4, 0.035f, LedRed, MenuBlinker.Mode.Random, 0.6f);

        Transform counter = Device(rack, "Counter", new Vector3(0.36f, 0.49f, -0.05f), new Vector3(0.48f, 0.18f, 0.5f));
        Led(counter, new Vector3(0f, 0.02f, -0.252f), "-01975", 0.7f);
        LedRow(counter, new Vector3(-0.15f, -0.06f, -0.252f), 6, 0.06f, Color.white, MenuBlinker.Mode.Random, 0.35f);

        // Prateleira de baixo: modulos com contadores vermelhos.
        Transform module = Device(rack, "Module", new Vector3(-0.15f, 0.13f, -0.05f), new Vector3(1.0f, 0.22f, 0.55f));
        Led(module, new Vector3(-0.25f, 0f, -0.277f), "47093", 0.75f);
        Led(module, new Vector3(0.25f, 0f, -0.277f), "00:17", 0.75f);
        LedRow(module, new Vector3(-0.45f, -0.08f, -0.277f), 12, 0.075f, LedRed, MenuBlinker.Mode.Random, 0.5f);
    }

    // ---------- Estante da direita: camera de seguranca ----------

    private static void BuildRightRack(Transform root)
    {
        Transform rack = Rack(root, "Rack_Feed", new Vector3(1.1f, Floor, 3.05f), 1.25f, 1.0f, 1.02f, new[] { 0.06f, 0.30f, 1.02f }, -4f);

        Transform feedTv = Tv(rack, "Monitor_Feed", new Vector3(0f, 1.05f, 0f), 1.35f);
        BuildFeed(feedTv);

        Transform staticTv = Tv(rack, "Monitor_Static", new Vector3(0.05f, 0.33f, 0f), 0.85f);
        StaticScreen(staticTv, 0.8f);

        TapeDeck(rack, new Vector3(0f, 0.08f, -0.1f));

        // TV extra no pedestal, cortada pela borda da tela.
        GameObject standModel = AssetDatabase.LoadAssetAtPath<GameObject>(PsxFolder + "TVSet/TVStand.fbx");
        Transform tvC = Tv(root, "Monitor_Side", new Vector3(2.35f, Floor + 0.455f, 3.5f), 1.0f);
        tvC.localRotation = Quaternion.Euler(0f, 180f + FaceCamera(tvC.localPosition) - 10f, 0f);
        StaticScreen(tvC, 0.9f);

        if (standModel != null)
        {
            GameObject stand = (GameObject)PrefabUtility.InstantiatePrefab(standModel);
            stand.name = "Stand";
            stand.transform.SetParent(tvC, false);
            stand.transform.localPosition = new Vector3(0f, -0.455f, -0.15f);
        }
    }

    // ---------- Fundo: janela fria e monitores estourados ----------

    private static void BuildBackWall(Transform root)
    {
        Transform back = Group(root, "Back_Set");

        // Janela alta com luz fria (vai estourar no bloom).
        Material window = GlowMaterial("MenuWindow", new Color(0.75f, 0.85f, 1f) * 2.2f);
        GameObject pane = AddQuad(back, new Vector3(0.35f, 1.3f, 6.14f), new Vector2(1.0f, 0.6f), window);
        pane.name = "Window";
        pane.transform.localRotation = Quaternion.identity;

        Material metal = MetalMaterial();
        Box(back, "WindowFrameTop", new Vector3(0.35f, 1.63f, 6.12f), new Vector3(1.15f, 0.07f, 0.1f), metal);
        Box(back, "WindowFrameBottom", new Vector3(0.35f, 0.97f, 6.12f), new Vector3(1.15f, 0.07f, 0.1f), metal);
        for (int i = 0; i < 3; i++)
            Box(back, "WindowBar", new Vector3(0.02f + i * 0.33f, 1.3f, 6.1f), new Vector3(0.04f, 0.65f, 0.04f), metal);

        // Feixe de luz da janela ate o chao.
        LightShaft(back, new Vector3(0.35f, 1.25f, 6.0f), new Vector3(0.55f, Floor, 3.6f), 1.0f, 0.4f);
        LightShaft(back, new Vector3(0.6f, 1.25f, 6.0f), new Vector3(1.2f, Floor, 4.0f), 0.6f, 0.3f);

        // Prateleira alta com monitores estourados de branco, como no fundo da sala.
        Transform shelf = Rack(back, "Rack_Back", new Vector3(-0.9f, Floor, 5.5f), 1.9f, 0.7f, 1.6f, new[] { 0.5f, 1.05f, 1.6f }, 0f);
        Transform a = Tv(shelf, "Monitor_BackA", new Vector3(-0.5f, 1.08f, 0f), 0.8f);
        BlownScreen(a);
        Transform b = Tv(shelf, "Monitor_BackB", new Vector3(0.45f, 1.08f, 0f), 0.75f);
        StaticScreen(b, 0.8f);
        Transform c = Tv(shelf, "Monitor_BackC", new Vector3(0f, 0.53f, 0f), 0.8f);
        StaticScreen(c, 0.7f);

        // Lampada vermelha que pinta a parede do fundo.
        GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(bulb.GetComponent<Collider>());
        bulb.name = "RedBulb";
        bulb.transform.SetParent(back, false);
        bulb.transform.localPosition = new Vector3(-1.3f, 1.45f, 6.05f);
        bulb.transform.localScale = Vector3.one * 0.12f;
        bulb.GetComponent<MeshRenderer>().sharedMaterial = GlowMaterial("MenuLed", Color.white);
        Blinker(bulb, MenuBlinker.Mode.Pulse, 2.6f, 0.5f, bulb.GetComponent<Renderer>(), new Color(1f, 0.04f, 0.02f) * 4f, new Color(1f, 0.04f, 0.02f) * 0.8f);

        // Luminoso "GRAVANDO" piscando, como luz de estudio.
        Transform sign = Box(back, "RecordingSign", new Vector3(1.75f, 1.42f, 6.1f), new Vector3(0.62f, 0.16f, 0.06f), GlowMaterial("MenuLed", Color.white)).transform;
        Blinker(sign.gameObject, MenuBlinker.Mode.Blink, 1.4f, 0.65f, sign.GetComponent<Renderer>(), new Color(1f, 0.05f, 0.03f) * 3f, new Color(0.25f, 0.02f, 0.01f));
        WallText(back, "RecordingLabel", new Vector3(1.75f, 1.42f, 6.065f), "GRAVANDO", 0.9f, new Color(0.04f, 0f, 0f));

        // Placa pintada na parede.
        WallText(back, "LabSign", new Vector3(1.75f, 1.17f, 6.14f), "LAB 1975", 1.8f, new Color(0.75f, 0.74f, 0.7f));
        WallText(back, "LabSub", new Vector3(1.75f, 1.02f, 6.14f), "PROJETO: CAMERA DIGITAL", 0.9f, new Color(0.6f, 0.6f, 0.58f));
        WallText(back, "LabWarn", new Vector3(1.75f, 0.92f, 6.14f), "NAO EXPOR A FITA A LUZ", 0.7f, new Color(0.6f, 0.08f, 0.06f));

        // LEDs na lateral da estante do fundo.
        LedRow(shelf, new Vector3(0.93f, 0.7f, -0.36f), 8, 0.05f, LedRed, MenuBlinker.Mode.Random, 0.4f, true);
    }

    // ---------- Primeiro plano: console ----------

    private static void BuildConsole(Transform root)
    {
        Transform console = Group(root, "Console");
        console.localPosition = new Vector3(1.05f, Floor, 1.4f);
        console.localRotation = Quaternion.Euler(0f, FaceCamera(console.localPosition) - 25f, 0f);

        Material metal = MetalMaterial();
        Box(console, "Body", new Vector3(0f, 0.52f, 0f), new Vector3(1.0f, 1.04f, 0.7f), metal);

        Transform top = Group(console, "Panel");
        top.localPosition = new Vector3(0f, 1.07f, -0.2f);
        top.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        Box(top, "Plate", Vector3.zero, new Vector3(1.0f, 0.05f, 0.32f), metal);
        LedRow(top, new Vector3(-0.42f, 0.03f, -0.05f), 10, 0.07f, LedRed, MenuBlinker.Mode.Random, 0.45f);

        Led(console, new Vector3(0.22f, 0.86f, -0.352f), "-01005", 0.7f);
        Led(console, new Vector3(-0.22f, 0.86f, -0.352f), "88:04", 0.7f);
        LedRow(console, new Vector3(-0.4f, 0.72f, -0.352f), 12, 0.07f, Color.white, MenuBlinker.Mode.Random, 0.3f);

        BuildPrototype(console, new Vector3(0.05f, 1.04f, 0.15f));

        // Luz fria fraca vinda do monitor ao lado, para o console nao sumir no preto.
        PointLight(console, "ConsoleGlow", new Vector3(-0.3f, 1.5f, -0.6f), new Color(0.75f, 0.82f, 0.95f), 1.4f, 2.2f);
    }

    // ---------- Cabos ----------

    private static void BuildCables(Transform root)
    {
        Transform cables = Group(root, "Cables");
        Material cable = UnlitMaterial("MenuCable", new Color(0.015f, 0.015f, 0.018f));

        float y = Floor + 0.015f;
        Cable(cables, cable, 0.03f, new Vector3(0.95f, y, 1.75f), new Vector3(1.3f, y, 2.2f), new Vector3(0.9f, y, 2.6f), new Vector3(1.1f, y, 2.95f));
        Cable(cables, cable, 0.025f, new Vector3(-0.6f, y, 2.2f), new Vector3(0.1f, y, 3.2f), new Vector3(0.7f, y, 3.4f), new Vector3(1.2f, y, 3.5f));
        Cable(cables, cable, 0.02f, new Vector3(-1.1f, y, 2.3f), new Vector3(-0.9f, y, 3.8f), new Vector3(-0.3f, y, 4.6f), new Vector3(-0.6f, y, 5.6f));
        Cable(cables, cable, 0.03f, new Vector3(1.4f, y, 3.4f), new Vector3(1.9f, y, 4.4f), new Vector3(1.3f, y, 5.2f), new Vector3(2.2f, y, 6.1f));
        Cable(cables, cable, 0.02f, new Vector3(0.6f, y, 1.6f), new Vector3(0.3f, y, 2.4f), new Vector3(-0.2f, y, 2.8f), new Vector3(-0.7f, y, 2.4f));

        // Cabos pendurados do teto.
        Hanging(cables, cable, new Vector3(-0.4f, Ceiling, 2.8f), new Vector3(0.9f, Ceiling, 4.6f), 1.1f);
        Hanging(cables, cable, new Vector3(0.3f, Ceiling, 3.2f), new Vector3(1.8f, Ceiling, 3.6f), 0.7f);
        Hanging(cables, cable, new Vector3(-1.6f, Ceiling, 3.0f), new Vector3(-0.2f, Ceiling, 5.0f), 1.4f);
    }

    private static void Cable(Transform parent, Material material, float width, params Vector3[] points)
    {
        GameObject go = new GameObject("Cable", typeof(LineRenderer));
        go.transform.SetParent(parent, false);
        LineRenderer line = go.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.sharedMaterial = material;
        line.widthMultiplier = width;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.numCornerVertices = 2;

        // Suaviza os pontos com Catmull-Rom.
        const int steps = 8;
        var smooth = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 p0 = points[Mathf.Max(i - 1, 0)], p1 = points[i], p2 = points[i + 1], p3 = points[Mathf.Min(i + 2, points.Length - 1)];
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps;
                smooth.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t));
            }
        }
        smooth.Add(points[points.Length - 1]);

        line.positionCount = smooth.Count;
        line.SetPositions(smooth.ToArray());
    }

    private static void Hanging(Transform parent, Material material, Vector3 a, Vector3 b, float sag)
    {
        const int count = 16;
        var points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (count - 1f);
            points[i] = Vector3.Lerp(a, b, t) + Vector3.down * sag * 4f * t * (1f - t);
        }

        GameObject go = new GameObject("HangingCable", typeof(LineRenderer));
        go.transform.SetParent(parent, false);
        LineRenderer line = go.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.sharedMaterial = material;
        line.widthMultiplier = 0.018f;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.positionCount = count;
        line.SetPositions(points);
    }

    // ---------- Cameras fotograficas com flash ----------

    private static void BuildPhotoCameras(Transform root)
    {
        photoCameras.Clear();
        Transform cameras = Group(root, "PhotoCameras");

        // Duas apontadas para o jogador (a da esquerda fica atras da TV e so ilumina a sala)
        // e uma para o prototipo no console.
        PhotoCamera(cameras, "PhotoCamera_Left", new Vector3(-2.15f, Floor, 3.1f), 1.38f, Vector3.zero);
        Transform right = PhotoCamera(cameras, "PhotoCamera_Right", new Vector3(2.75f, Floor, 3.2f), 1.3f, Vector3.zero);
        playerFacingCamera = right.GetComponentInChildren<PhotoFlash>();
        PhotoCamera(cameras, "PhotoCamera_Back", new Vector3(-0.05f, Floor, 4.5f), 1.2f, new Vector3(1.0f, Floor + 1.15f, 1.5f));
    }

    // Camera de tripe dos anos 70 com flash "martelo" ao lado. A frente (-Z) mira em target.
    private static Transform PhotoCamera(Transform parent, string name, Vector3 position, float height, Vector3 target)
    {
        Transform rig = Group(parent, name);
        rig.localPosition = position;
        rig.localRotation = Quaternion.Euler(0f, FaceCamera(position - target), 0f);

        Material metal = MetalMaterial();
        Material chrome = LitMaterial("MenuChassis", "VentMetal", new Color(0.55f, 0.55f, 0.53f), 0.6f, 0.75f, Vector2.one);
        Material leather = LitMaterial("MenuDarkPlastic", "VentMetal", new Color(0.05f, 0.05f, 0.055f), 0.4f, 0.1f, Vector2.one);

        // Tripe: tres pernas abertas e coluna central.
        Vector3 head = new Vector3(0f, height, 0f);
        for (int i = 0; i < 3; i++)
        {
            float angle = (90f + i * 120f) * Mathf.Deg2Rad;
            Vector3 foot = new Vector3(Mathf.Cos(angle) * 0.4f, 0f, Mathf.Sin(angle) * 0.4f);
            Vector3 leg = head - foot;
            GameObject legGo = Box(rig, "Leg", (head + foot) * 0.5f, new Vector3(0.025f, leg.magnitude, 0.025f), metal);
            legGo.transform.localRotation = Quaternion.FromToRotation(Vector3.up, leg.normalized);
        }

        Box(rig, "Column", head + new Vector3(0f, 0.04f, 0f), new Vector3(0.035f, 0.1f, 0.035f), chrome);
        Box(rig, "HeadPlate", head + new Vector3(0f, 0.095f, 0f), new Vector3(0.09f, 0.012f, 0.09f), metal);

        // Corpo da camera, inclinado para mirar no alvo.
        Transform body = Group(rig, "Camera");
        body.localPosition = head + new Vector3(0f, 0.17f, 0f);
        Vector3 aim = rig.InverseTransformPoint(setRoot.TransformPoint(target)) - body.localPosition;
        body.localRotation = Quaternion.LookRotation(-aim.normalized, Vector3.up);
        body.localScale = Vector3.one * 1.5f;

        Box(body, "Body", Vector3.zero, new Vector3(0.2f, 0.11f, 0.09f), leather);
        Box(body, "TopPlate", new Vector3(0f, 0.065f, 0f), new Vector3(0.2f, 0.022f, 0.09f), chrome);
        Box(body, "BottomPlate", new Vector3(0f, -0.06f, 0f), new Vector3(0.2f, 0.012f, 0.09f), chrome);
        Box(body, "Prism", new Vector3(0f, 0.095f, 0f), new Vector3(0.07f, 0.045f, 0.07f), chrome);
        Box(body, "Winder", new Vector3(0.07f, 0.082f, 0.01f), new Vector3(0.025f, 0.012f, 0.025f), leather);

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(lens.GetComponent<Collider>());
        lens.name = "Lens";
        lens.transform.SetParent(body, false);
        lens.transform.localPosition = new Vector3(0f, 0f, -0.09f);
        lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        lens.transform.localScale = new Vector3(0.07f, 0.05f, 0.07f);
        lens.GetComponent<MeshRenderer>().sharedMaterial = leather;

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        ring.name = "FocusRing";
        ring.transform.SetParent(body, false);
        ring.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = new Vector3(0.076f, 0.012f, 0.076f);
        ring.GetComponent<MeshRenderer>().sharedMaterial = chrome;

        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(glass.GetComponent<Collider>());
        glass.name = "Glass";
        glass.transform.SetParent(body, false);
        glass.transform.localPosition = new Vector3(0f, 0f, -0.141f);
        glass.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        glass.transform.localScale = new Vector3(0.055f, 0.002f, 0.055f);
        glass.GetComponent<MeshRenderer>().sharedMaterial = LitMaterial("MenuLensGlass", "VentMetal", new Color(0.02f, 0.03f, 0.05f), 0.95f, 0.9f, Vector2.one);

        // Flash "martelo": suporte ao lado, cabo vertical e cabeca com refletor.
        Box(body, "FlashBracket", new Vector3(0.14f, -0.06f, 0f), new Vector3(0.1f, 0.012f, 0.04f), chrome);
        Box(body, "FlashHandle", new Vector3(0.19f, 0.04f, 0f), new Vector3(0.04f, 0.2f, 0.045f), leather);
        Box(body, "FlashHead", new Vector3(0.19f, 0.18f, -0.005f), new Vector3(0.13f, 0.075f, 0.07f), leather);
        Box(body, "Reflector", new Vector3(0.19f, 0.18f, -0.038f), new Vector3(0.12f, 0.064f, 0.006f), chrome);

        GameObject tube = AddQuad(body, new Vector3(0.19f, 0.18f, -0.043f), new Vector2(0.105f, 0.05f), GlowMaterial("MenuFlash", Color.white));
        tube.name = "FlashTube";

        GameObject led = Box(body, "ReadyLed", new Vector3(0.235f, 0.222f, -0.03f), Vector3.one * 0.012f, GlowMaterial("MenuLed", Color.white));
        led.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        GameObject lightGo = new GameObject("FlashLight", typeof(Light));
        lightGo.transform.SetParent(body, false);
        lightGo.transform.localPosition = new Vector3(0.19f, 0.18f, -0.08f);
        lightGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Light flash = lightGo.GetComponent<Light>();
        flash.type = LightType.Spot;
        flash.color = new Color(0.95f, 0.97f, 1f);
        flash.intensity = 0f;
        flash.range = 10f;
        flash.spotAngle = 110f;
        flash.innerSpotAngle = 60f;
        flash.shadows = LightShadows.None;
        flash.enabled = false;

        AudioSource audio = body.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0.7f;
        audio.minDistance = 1.5f;
        audio.maxDistance = 15f;

        PhotoFlash photo = body.gameObject.AddComponent<PhotoFlash>();
        SerializedObject so = new SerializedObject(photo);
        so.FindProperty("autoFire").boolValue = mode != Mode.Credits;
        photoCameras.Add(photo);
        so.FindProperty("flashLight").objectReferenceValue = flash;
        so.FindProperty("flashRenderer").objectReferenceValue = tube.GetComponent<Renderer>();
        so.FindProperty("readyLed").objectReferenceValue = led.GetComponent<Renderer>();
        so.FindProperty("audioSource").objectReferenceValue = audio;
        so.ApplyModifiedPropertiesWithoutUndo();

        return rig;
    }

    // ---------- Luzes ----------

    private static void BuildLights(Transform root)
    {
        Transform lights = Group(root, "Lights");

        // Vermelho forte no fundo, por tras das estantes.
        Light red = PointLight(lights, "RedBacklight", new Vector3(-1.3f, 1.3f, 5.75f), new Color(1f, 0.04f, 0.02f), 16f, 7f);
        red.shadows = LightShadows.Soft;
        Blinker(red.gameObject, MenuBlinker.Mode.Pulse, 2.6f, 0.5f, null, Color.white, Color.white, red, red.intensity * 0.45f);
        PointLight(lights, "RedFill", new Vector3(-2.6f, 0.2f, 3.8f), new Color(1f, 0.04f, 0.02f), 5f, 4.5f);

        // Luz do luminoso GRAVANDO.
        Light sign = PointLight(lights, "RecordingSignLight", new Vector3(1.75f, 1.42f, 5.85f), new Color(1f, 0.05f, 0.03f), 3f, 2.2f);
        Blinker(sign.gameObject, MenuBlinker.Mode.Blink, 1.4f, 0.65f, null, Color.white, Color.white, sign, 0f);

        // Luz fria entrando pela janela.
        GameObject spotGo = new GameObject("WindowLight", typeof(Light));
        spotGo.transform.SetParent(lights, false);
        spotGo.transform.localPosition = new Vector3(0.35f, 1.4f, 5.9f);
        spotGo.transform.localRotation = Quaternion.LookRotation(new Vector3(0.55f, Floor, 3.4f) - spotGo.transform.localPosition);
        Light spot = spotGo.GetComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(0.72f, 0.82f, 1f);
        spot.intensity = 30f;
        spot.range = 10f;
        spot.spotAngle = 62f;
        spot.innerSpotAngle = 30f;
        spot.shadows = LightShadows.Soft;
        Blinker(spotGo, MenuBlinker.Mode.Flicker, 1.5f, 0.5f, null, Color.white, Color.white, spot, spot.intensity * 0.2f);

        // Preenchimento bem fraco para os volumes nao sumirem no preto.
        PointLight(lights, "AmbientFill", new Vector3(0.2f, 1.4f, 0.4f), new Color(0.55f, 0.62f, 0.7f), 1.2f, 6f);
        PointLight(lights, "FloorBounce", new Vector3(0.55f, Floor + 0.4f, 3.4f), new Color(0.7f, 0.8f, 1f), 2.5f, 3.2f);
    }

    private static Light PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name, typeof(Light));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;

        Light light = go.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }

    // ---------- Telas ----------

    private static void BuildMenuScreen(Transform tv, Button play, Button credits, Button quit)
    {
        RectTransform screen = CreateScreenCanvas(tv, "MenuScreen", out CanvasGroup group);

        TextMeshProUGUI header = Text(screen, "Header", "OUT OF TIME", 30f, TextIdle);
        header.alignment = TextAlignmentOptions.Top;
        Stretch(header.rectTransform, new Vector2(0f, 0f), new Vector2(0f, -30f));

        float y = -100f;

        foreach (Button button in new[] { play, credits, quit })
        {
            if (button == null)
                continue;

            RectTransform rect = (RectTransform)button.transform;
            Undo.SetTransformParent(rect, screen, "Menu");
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.localPosition = Vector3.zero;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(580f, 86f);
            rect.anchoredPosition = new Vector2(0f, y);
            y -= 104f;

            StyleScreenButton(button);
        }

        SetLabel(play, "PLAY");
        SetLabel(credits, "CREDITS");
        SetLabel(quit, "QUIT");

        TextMeshProUGUI footer = Text(screen, "Footer", "PROTOTIPO CCD 100x100 // 1975", 24f, new Color(0.5f, 0.5f, 0.48f));
        footer.alignment = TextAlignmentOptions.Bottom;
        Stretch(footer.rectTransform, new Vector2(0f, 26f), Vector2.zero);

        TextMeshProUGUI rec = Text(screen, "Rec", "[REC]", 24f, LedRed);
        rec.alignment = TextAlignmentOptions.TopRight;
        Stretch(rec.rectTransform, Vector2.zero, new Vector2(-30f, -30f));
        Blinker(rec.gameObject, MenuBlinker.Mode.Blink, 1f, 0.6f, null, Color.white, Color.white, null, 0f, rec);

        AddCanvasOverlay(screen);
        AddScreenGlow(tv, screen, group);
    }

    // Luz da tela iluminando a estante e a cintilacao do tubo.
    private static void AddScreenGlow(Transform tv, RectTransform screen, CanvasGroup group)
    {
        GameObject lightGo = new GameObject("ScreenGlow", typeof(Light));
        lightGo.transform.SetParent(tv, false);
        lightGo.transform.localPosition = TvScreenCenter + new Vector3(0f, 0f, 0.4f);

        Light glow = lightGo.GetComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.8f, 0.85f, 0.95f);
        glow.intensity = 1.6f;
        glow.range = 2.4f;
        glow.shadows = LightShadows.None;

        CrtFlicker flicker = screen.gameObject.AddComponent<CrtFlicker>();
        SerializedObject so = new SerializedObject(flicker);
        so.FindProperty("screenLight").objectReferenceValue = glow;
        so.FindProperty("canvasGroup").objectReferenceValue = group;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StyleScreenButton(Button button)
    {
        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            // Area clicavel invisivel: no CRT so o texto aparece.
            image.sprite = null;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
        }

        button.transition = Selectable.Transition.None;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            if (font != null)
            {
                label.font = font;
                label.fontSharedMaterial = font.material;
            }

            label.fontSize = 76f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = TextIdle;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
        }

        ButtonHoverEffect hover = button.GetComponent<ButtonHoverEffect>();

        if (hover != null)
        {
            SerializedObject so = new SerializedObject(hover);
            so.FindProperty("hoverScale").floatValue = 1.04f;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("normalTextColor").colorValue = TextIdle;
            so.FindProperty("hoverTextColor").colorValue = Phosphor;
            so.FindProperty("hoverPrefix").stringValue = "> ";
            so.ApplyModifiedProperties();
        }
    }

    private static void BuildFeed(Transform tv)
    {
        GameObject quad = AddQuad(tv, TvScreenCenter + new Vector3(0f, 0f, 0.002f), TvScreenSize, GrayFeedMaterial());
        quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        // O que o prototipo "ve": camera na ponta da lente, imagem 100x100 em tons de cinza.
        GameObject camGo = new GameObject("PrototypeView", typeof(Camera));
        camGo.transform.SetParent(setRoot, false);

        if (prototypeLens != null)
            camGo.transform.SetPositionAndRotation(prototypeLens.position, prototypeLens.rotation);

        Camera feed = camGo.GetComponent<Camera>();
        feed.fieldOfView = 55f;
        feed.nearClipPlane = 0.05f;
        feed.farClipPlane = 20f;
        feed.enabled = false;
        feed.GetUniversalAdditionalCameraData().renderPostProcessing = false;

        CrtCameraFeed component = quad.AddComponent<CrtCameraFeed>();
        SerializedObject so = new SerializedObject(component);
        so.FindProperty("feedCamera").objectReferenceValue = feed;
        so.FindProperty("width").intValue = 100;
        so.FindProperty("height").intValue = 100;
        so.FindProperty("framesPerSecond").floatValue = 6f;
        so.ApplyModifiedPropertiesWithoutUndo();

        RectTransform overlay = CreateScreenCanvas(tv, "Overlay", out _, 0.004f);
        Object.DestroyImmediate(overlay.GetComponent<GraphicRaycaster>());
        Object.DestroyImmediate(overlay.Find("Background").gameObject);
        AddCanvasOverlay(overlay);

        TextMeshProUGUI label = Text(overlay, "Label", "CCD 100x100", 26f, Phosphor);
        label.alignment = TextAlignmentOptions.TopLeft;
        Stretch(label.rectTransform, new Vector2(28f, 0f), new Vector2(0f, -22f));

        TextMeshProUGUI rec = Text(overlay, "Rec", "[REC]", 26f, LedRed);
        rec.alignment = TextAlignmentOptions.TopRight;
        Stretch(rec.rectTransform, Vector2.zero, new Vector2(-28f, -22f));
        Blinker(rec.gameObject, MenuBlinker.Mode.Blink, 1f, 0.6f, null, Color.white, Color.white, null, 0f, rec);

        TextMeshProUGUI tape = Text(overlay, "Tape", "GRAVANDO NA FITA 00s", 24f, Phosphor);
        tape.alignment = TextAlignmentOptions.Bottom;
        Stretch(tape.rectTransform, new Vector2(0f, 24f), Vector2.zero);
        tape.gameObject.AddComponent<TapeRecordingText>();

        ScreenLight(tv, new Color(0.85f, 0.9f, 1f), 1.4f);
    }

    private static void StaticScreen(Transform tv, float brightness)
    {
        GameObject quad = AddQuad(tv, TvScreenCenter + new Vector3(0f, 0f, 0.002f), TvScreenSize, ScreenMaterial("CrtStatic", brightness));
        quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        quad.AddComponent<CrtNoiseScreen>();
        ScreenLight(tv, new Color(0.8f, 0.85f, 0.9f), 0.5f);
    }

    private static void BlownScreen(Transform tv)
    {
        GameObject quad = AddQuad(tv, TvScreenCenter + new Vector3(0f, 0f, 0.002f), TvScreenSize, GlowMaterial("MenuBlownScreen", new Color(0.85f, 0.9f, 1f) * 2.4f));
        quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        ScreenLight(tv, new Color(0.8f, 0.88f, 1f), 1.2f);
    }

    private static void ScreenLight(Transform tv, Color color, float intensity)
    {
        GameObject go = new GameObject("ScreenGlow", typeof(Light));
        go.transform.SetParent(tv, false);
        go.transform.localPosition = TvScreenCenter + new Vector3(0f, 0f, 0.35f);
        Light light = go.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = 1.8f;
        light.shadows = LightShadows.None;
    }

    private static RectTransform CreateScreenCanvas(Transform tv, string name, out CanvasGroup group, float offset = 0.003f)
    {
        const float unitsPerMeter = 1000f;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        go.transform.SetParent(tv, false);
        go.transform.localPosition = TvScreenCenter + new Vector3(0f, 0f, offset);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        go.transform.localScale = Vector3.one / unitsPerMeter;

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = menuCamera;

        go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

        RectTransform rect = (RectTransform)go.transform;
        rect.sizeDelta = TvScreenSize * unitsPerMeter;

        group = go.GetComponent<CanvasGroup>();

        Image background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        background.transform.SetParent(go.transform, false);
        background.color = ScreenDark;
        background.raycastTarget = false;
        Stretch(background.rectTransform, Vector2.zero, Vector2.zero);

        return rect;
    }

    private static void AddCanvasOverlay(RectTransform screen)
    {
        Image scan = new GameObject("Scanlines", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        scan.transform.SetParent(screen, false);
        scan.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "crt_scanlines.png");
        scan.type = Image.Type.Tiled;
        scan.pixelsPerUnitMultiplier = 0.5f;
        scan.raycastTarget = false;
        Stretch(scan.rectTransform, Vector2.zero, Vector2.zero);

        Image vignette = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        vignette.transform.SetParent(screen, false);
        vignette.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "crt_vignette.png");
        vignette.raycastTarget = false;
        Stretch(vignette.rectTransform, Vector2.zero, Vector2.zero);
    }

    // ---------- Pecas do cenario ----------

    // Estante de metal apoiada no chao. A frente (-Z local) olha para a camera.
    private static Transform Rack(Transform parent, string name, Vector3 position, float width, float depth, float height, float[] shelves, float extraYaw)
    {
        Transform rack = Group(parent, name);
        rack.localPosition = position;
        rack.localRotation = Quaternion.Euler(0f, FaceCamera(position) + extraYaw, 0f);

        Material metal = MetalMaterial();
        const float post = 0.04f;
        float hx = width * 0.5f - post * 0.5f, hz = depth * 0.5f - post * 0.5f;

        foreach (Vector2 corner in new[] { new Vector2(-hx, -hz), new Vector2(hx, -hz), new Vector2(-hx, hz), new Vector2(hx, hz) })
            Box(rack, "Post", new Vector3(corner.x, height * 0.5f, corner.y), new Vector3(post, height, post), metal);

        foreach (float h in shelves)
            Box(rack, "Shelf", new Vector3(0f, h, 0f), new Vector3(width, 0.03f, depth), metal);

        return rack;
    }

    private static Transform Tv(Transform parent, string name, Vector3 localPosition, float scale)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PsxFolder + "TVSet/TV.fbx");
        GameObject tv = (GameObject)PrefabUtility.InstantiatePrefab(model);
        Undo.RegisterCreatedObjectUndo(tv, "Monitor");
        tv.name = name;
        tv.transform.SetParent(parent, false);
        tv.transform.localPosition = localPosition;
        // A tela do modelo olha para +Z; gira para olhar para a frente da estante (-Z).
        tv.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        tv.transform.localScale = Vector3.one * scale;
        return tv.transform;
    }

    // Caixa de equipamento com painel frontal escuro.
    private static Transform Device(Transform parent, string name, Vector3 localPosition, Vector3 size)
    {
        // Grupo sem escala: filhos (telas, leds) usam medidas em metros.
        Transform group = Group(parent, name);
        group.localPosition = localPosition + new Vector3(0f, size.y * 0.5f, 0f);
        Box(group, "Body", Vector3.zero, size, MetalMaterial());
        return group;
    }

    private static void Led(Transform parent, Vector3 localPosition, string value, float size)
    {
        GameObject go = new GameObject("Led", typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;

        TextMeshPro text = go.GetComponent<TextMeshPro>();
        if (font != null)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        text.text = value;
        text.fontSize = size;
        text.color = LedRed;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(0.5f, 0.12f);
    }

    // Prototipo de 1975: caixa de metal do tamanho de uma torradeira, lente de cinema,
    // placas de circuito expostas e gravador de fita cassete acoplado.
    private static void BuildPrototype(Transform console, Vector3 localPosition)
    {
        Transform proto = Group(console, "Prototype_1975");
        proto.localPosition = localPosition;

        proto.localScale = Vector3.one * 1.4f;

        // A lente aponta para o centro da sala, onde cai a luz da janela.
        Vector3 here = setRoot.InverseTransformPoint(proto.position);
        Vector3 aim = new Vector3(0.1f, here.y, 4.6f) - here;
        proto.rotation = Quaternion.LookRotation(-setRoot.TransformDirection(aim.normalized), Vector3.up);

        // Luminaria de bancada iluminando o prototipo.
        GameObject lampGo = new GameObject("DeskLamp", typeof(Light));
        lampGo.transform.SetParent(console, false);
        lampGo.transform.position = proto.position + Vector3.up * 0.9f + setRoot.forward * -0.2f;
        lampGo.transform.rotation = Quaternion.LookRotation(proto.position - lampGo.transform.position);
        Light lamp = lampGo.GetComponent<Light>();
        lamp.type = LightType.Spot;
        lamp.color = new Color(0.85f, 0.88f, 0.95f);
        lamp.intensity = 6f;
        lamp.range = 2.5f;
        lamp.spotAngle = 55f;
        lamp.innerSpotAngle = 25f;
        lamp.shadows = LightShadows.Soft;

        Material chassis = LitMaterial("MenuChassis", "VentMetal", new Color(0.55f, 0.55f, 0.53f), 0.6f, 0.75f, Vector2.one);
        Material dark = LitMaterial("MenuDarkPlastic", "VentMetal", new Color(0.05f, 0.05f, 0.055f), 0.4f, 0.1f, Vector2.one);
        Material board = LitMaterial("MenuCircuit", "VentMetal", new Color(0.12f, 0.06f, 0.04f), 0.5f, 0.2f, Vector2.one);

        Box(proto, "Chassis", new Vector3(0f, 0.1f, 0f), new Vector3(0.24f, 0.2f, 0.34f), chassis);
        Box(proto, "FrontPlate", new Vector3(0f, 0.1f, -0.172f), new Vector3(0.22f, 0.18f, 0.008f), dark);
        Box(proto, "Handle", new Vector3(0f, 0.235f, 0f), new Vector3(0.03f, 0.02f, 0.22f), dark);
        Box(proto, "HandleA", new Vector3(0f, 0.215f, -0.1f), new Vector3(0.03f, 0.03f, 0.02f), dark);
        Box(proto, "HandleB", new Vector3(0f, 0.215f, 0.1f), new Vector3(0.03f, 0.03f, 0.02f), dark);

        // Lente.
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(barrel.GetComponent<Collider>());
        barrel.name = "Lens";
        barrel.transform.SetParent(proto, false);
        barrel.transform.localPosition = new Vector3(0.04f, 0.11f, -0.215f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.transform.localScale = new Vector3(0.075f, 0.045f, 0.075f);
        barrel.GetComponent<MeshRenderer>().sharedMaterial = dark;

        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(glass.GetComponent<Collider>());
        glass.name = "Glass";
        glass.transform.SetParent(proto, false);
        glass.transform.localPosition = new Vector3(0.04f, 0.11f, -0.261f);
        glass.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        glass.transform.localScale = new Vector3(0.055f, 0.002f, 0.055f);
        glass.GetComponent<MeshRenderer>().sharedMaterial = LitMaterial("MenuLensGlass", "VentMetal", new Color(0.02f, 0.03f, 0.05f), 0.95f, 0.9f, Vector2.one);

        // Ponto de onde sai a imagem do feed (+Z para fora da lente).
        prototypeLens = Group(proto, "LensView");
        prototypeLens.localPosition = new Vector3(0.04f, 0.11f, -0.27f);
        prototypeLens.localRotation = Quaternion.Euler(8f, 180f, 0f);

        // Placas de circuito expostas em cima, com chips.
        Box(proto, "BoardA", new Vector3(-0.04f, 0.205f, -0.06f), new Vector3(0.12f, 0.006f, 0.14f), board);
        Box(proto, "BoardB", new Vector3(0.06f, 0.205f, 0.09f), new Vector3(0.09f, 0.006f, 0.12f), board);
        for (int i = 0; i < 4; i++)
            Box(proto, "Chip", new Vector3(-0.07f + (i % 2) * 0.05f, 0.212f, -0.1f + (i / 2) * 0.06f), new Vector3(0.03f, 0.008f, 0.018f), dark);

        // Gravador de fita acoplado na lateral.
        Box(proto, "TapeUnit", new Vector3(-0.15f, 0.07f, 0.04f), new Vector3(0.06f, 0.14f, 0.22f), dark);
        Box(proto, "Cassette", new Vector3(-0.182f, 0.08f, 0.04f), new Vector3(0.006f, 0.08f, 0.13f), LitMaterial("MenuCassette", "VentMetal", new Color(0.7f, 0.68f, 0.62f), 0.3f, 0f, Vector2.one));

        // LED de gravacao na frente.
        GameObject tally = Box(proto, "TallyLed", new Vector3(-0.07f, 0.17f, -0.178f), new Vector3(0.016f, 0.016f, 0.008f), GlowMaterial("MenuLed", Color.white));
        Blinker(tally, MenuBlinker.Mode.Blink, 1f, 0.5f, tally.GetComponent<Renderer>(), LedRed * 4f, new Color(0.15f, 0.01f, 0.01f));

        Light tallyLight = PointLight(proto, "TallyGlow", new Vector3(-0.07f, 0.17f, -0.22f), new Color(1f, 0.05f, 0.03f), 0.4f, 0.4f);
        Blinker(tallyLight.gameObject, MenuBlinker.Mode.Blink, 1f, 0.5f, null, Color.white, Color.white, tallyLight, 0f);

        Led(proto, new Vector3(-0.02f, 0.05f, -0.178f), "01", 0.45f);
    }

    // Deck de fita cassete com dois rolos girando.
    private static void TapeDeck(Transform rack, Vector3 localPosition)
    {
        Transform deck = Device(rack, "TapeDeck", localPosition, new Vector3(0.62f, 0.2f, 0.42f));
        Material dark = LitMaterial("MenuDarkPlastic", "VentMetal", new Color(0.05f, 0.05f, 0.055f), 0.4f, 0.1f, Vector2.one);
        Material reelMat = LitMaterial("MenuReel", "VentMetal", new Color(0.6f, 0.6f, 0.58f), 0.6f, 0.8f, Vector2.one);

        Box(deck, "Window", new Vector3(-0.1f, 0.01f, -0.212f), new Vector3(0.3f, 0.13f, 0.004f), dark);

        foreach (float x in new[] { -0.18f, -0.02f })
        {
            Transform reel = Group(deck, "Reel");
            reel.localPosition = new Vector3(x, 0.01f, -0.216f);

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.name = "Disc";
            disc.transform.SetParent(reel, false);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale = new Vector3(0.09f, 0.003f, 0.09f);
            disc.GetComponent<MeshRenderer>().sharedMaterial = reelMat;

            // Raios para dar para ver o giro.
            Box(reel, "Spoke", new Vector3(0f, 0f, -0.004f), new Vector3(0.08f, 0.012f, 0.004f), dark);
            Box(reel, "Spoke", new Vector3(0f, 0f, -0.004f), new Vector3(0.012f, 0.08f, 0.004f), dark);

            MenuSpin spin = reel.gameObject.AddComponent<MenuSpin>();
            SerializedObject so = new SerializedObject(spin);
            so.FindProperty("axis").vector3Value = Vector3.forward;
            so.FindProperty("degreesPerSecond").floatValue = x < -0.1f ? -140f : -190f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Led(deck, new Vector3(0.18f, 0.03f, -0.212f), "TAPE", 0.55f);
        LedRow(deck, new Vector3(0.12f, -0.05f, -0.212f), 4, 0.04f, LedRed, MenuBlinker.Mode.Blink, 0.5f);
    }

    // Fileira de LEDs piscando. vertical = coluna em vez de linha.
    private static void LedRow(Transform parent, Vector3 start, int count, float spacing, Color color, MenuBlinker.Mode mode, float period, bool vertical = false)
    {
        Material led = GlowMaterial("MenuLed", Color.white);
        Color on = color * 3f;
        Color off = new Color(color.r * 0.08f, color.g * 0.08f, color.b * 0.08f, 1f);

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = vertical ? new Vector3(0f, -i * spacing, 0f) : new Vector3(i * spacing, 0f, 0f);
            GameObject dot = Box(parent, "Led", start + offset, Vector3.one * 0.014f, led);
            dot.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Blinker(dot, mode, period * Random.Range(0.7f, 1.3f), 0.55f, dot.GetComponent<Renderer>(), on, off);
        }
    }

    private static void Blinker(GameObject go, MenuBlinker.Mode mode, float period, float duty, Renderer renderer, Color on, Color off, Light light = null, float lightMin = 0f, TMP_Text text = null)
    {
        MenuBlinker blinker = go.AddComponent<MenuBlinker>();
        SerializedObject so = new SerializedObject(blinker);
        so.FindProperty("mode").enumValueIndex = (int)mode;
        so.FindProperty("period").floatValue = period;
        so.FindProperty("duty").floatValue = duty;
        so.FindProperty("targetRenderer").objectReferenceValue = renderer;
        so.FindProperty("onColor").colorValue = on;
        so.FindProperty("offColor").colorValue = off;
        so.FindProperty("targetLight").objectReferenceValue = light;
        so.FindProperty("lightMin").floatValue = lightMin;
        so.FindProperty("targetText").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Texto pintado na parede do fundo (olha para a camera, -Z).
    private static void WallText(Transform parent, string name, Vector3 localPosition, string value, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        TextMeshPro text = go.GetComponent<TextMeshPro>();
        if (font != null)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(2f, 0.4f);
    }

    private static Material GrayFeedMaterial()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(MaterialFolder + "/CrtGrayFeed.shader");
        string path = MaterialFolder + "/CrtGrayFeed.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetFloat("_Brightness", 1.35f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void LightShaft(Transform parent, Vector3 from, Vector3 to, float width, float strength)
    {
        Material material = ShaftMaterial(strength);
        Vector3 axis = to - from;
        Vector3 mid = (from + to) * 0.5f;

        // Dois planos cruzados para o feixe ter volume visto de lado.
        for (int i = 0; i < 2; i++)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "LightShaft";
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = mid;

            Vector3 side = Vector3.Cross(axis, Vector3.up).normalized;
            Vector3 facing = Quaternion.AngleAxis(i * 90f - 45f, axis.normalized) * side;
            quad.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(facing, axis).normalized, axis.normalized);
            quad.transform.localScale = new Vector3(width, axis.magnitude, 1f);

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static GameObject Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(box.GetComponent<Collider>());
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        return box;
    }

    private static GameObject AddQuad(Transform parent, Vector3 center, Vector2 size, Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.name = "Screen";
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = center;
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return quad;
    }

    private static Transform Group(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    // Yaw que faz a frente (-Z) de um objeto nessa posicao olhar para a camera (origem).
    private static float FaceCamera(Vector3 position)
    {
        return Mathf.Atan2(position.x, position.z) * Mathf.Rad2Deg;
    }

    // ---------- Camera, nevoa e volume ----------

    private static void ConfigureCamera()
    {
        Undo.RecordObject(menuCamera, "Camera");
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = Color.black;
        menuCamera.nearClipPlane = 0.05f;
        menuCamera.farClipPlane = 30f;
        menuCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    private static void ConfigureAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.035f, 0.04f, 0.05f);
        RenderSettings.fogDensity = 0.11f;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.065f, 0.075f);
        RenderSettings.reflectionIntensity = 0.4f;
    }

    private static void ConfigureVolume()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MenuProfilePath);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, MenuProfilePath);
        }

        Bloom bloom = Get<Bloom>(profile);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(0.72f);
        bloom.tint.Override(Color.white);

        Tonemapping tone = Get<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.ACES);

        ColorAdjustments color = Get<ColorAdjustments>(profile);
        color.postExposure.Override(0.25f);
        color.contrast.Override(22f);
        color.saturation.Override(-30f);
        color.colorFilter.Override(Color.white);
        color.hueShift.Override(0f);

        Vignette vignette = Get<Vignette>(profile);
        vignette.color.Override(Color.black);
        vignette.intensity.Override(0.42f);
        vignette.smoothness.Override(0.5f);

        FilmGrain grain = Get<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Medium3);
        grain.intensity.Override(0.45f);
        grain.response.Override(0.7f);

        ChromaticAberration chroma = Get<ChromaticAberration>(profile);
        chroma.intensity.Override(0.22f);

        LensDistortion lens = Get<LensDistortion>(profile);
        lens.intensity.Override(-0.12f);

        DepthOfField dof = Get<DepthOfField>(profile);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(3.2f);
        dof.gaussianEnd.Override(9f);
        dof.gaussianMaxRadius.Override(1.2f);

        // Qualquer outro efeito herdado do profile antigo (tons, mixer, blur...) fica desligado.
        var keep = new System.Collections.Generic.HashSet<System.Type>
        {
            typeof(Bloom), typeof(Tonemapping), typeof(ColorAdjustments), typeof(Vignette),
            typeof(FilmGrain), typeof(ChromaticAberration), typeof(LensDistortion), typeof(DepthOfField),
        };

        foreach (VolumeComponent component in profile.components)
        {
            if (!keep.Contains(component.GetType()))
                component.active = false;
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);

        foreach (Volume volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!volume.isGlobal)
                continue;

            Undo.RecordObject(volume, "Volume");
            volume.sharedProfile = profile;
            volume.weight = 1f;
            volume.priority = 10f;
        }
    }

    private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
        }

        component.active = true;
        return component;
    }

    // ---------- Materiais ----------

    private static Material LoadOrCreate(string name, string shader)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Art/Materials", "Menu");

        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader == null || material.shader.name != shader)
        {
            material.shader = Shader.Find(shader);
        }

        return material;
    }

    private static Material LitMaterial(string name, string texture, Color tint, float smoothness, float metallic, Vector2 tiling)
    {
        Material material = LoadOrCreate(name, "Universal Render Pipeline/Lit");
        Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TexFolder + texture + "_Base_color.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexFolder + texture + "_Normal_DirectX.png");

        material.SetTexture("_BaseMap", baseMap);
        material.SetTextureScale("_BaseMap", tiling);
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);

        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 0.8f);
            material.EnableKeyword("_NORMALMAP");
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MetalMaterial()
    {
        return LitMaterial("MenuMetal", "VentMetal", new Color(0.2f, 0.2f, 0.21f), 0.55f, 0.6f, new Vector2(1f, 1f));
    }

    private static Material UnlitMaterial(string name, Color color)
    {
        Material material = LoadOrCreate(name, "Universal Render Pipeline/Unlit");
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GlowMaterial(string name, Color hdr)
    {
        return UnlitMaterial(name, hdr);
    }

    private static Material ScreenMaterial(string name, float brightness)
    {
        Material material = LoadOrCreate(name, "Universal Render Pipeline/Unlit");
        material.SetColor("_BaseColor", Color.white * brightness);
        EditorUtility.SetDirty(material);
        return material;
    }

    // Material aditivo para o feixe de luz (textura de degrade gerada aqui).
    private static Material ShaftMaterial(float strength)
    {
        Material material = LoadOrCreate("MenuLightShaft", "Universal Render Pipeline/Particles/Unlit");
        material.SetTexture("_BaseMap", ShaftTexture());
        material.SetColor("_BaseColor", new Color(0.7f, 0.8f, 1f, strength));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D ShaftTexture()
    {
        string path = ArtFolder + "fx_light_shaft.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (existing != null)
            return existing;

        const int w = 64, h = 128;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float v = y / (h - 1f);
            // Mais forte perto da janela (topo), some no chao, bordas suaves.
            float along = Mathf.Pow(v, 1.6f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, (1f - v) * 8f));
            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs(x / (w - 1f) - 0.5f) * 2f;
                float across = Mathf.Pow(1f - u, 2f);
                float a = Mathf.Clamp01(along * across);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------- Utilitarios ----------

    private static void ConfigureTextures()
    {
        foreach (string file in new[] { "crt_scanlines.png", "crt_vignette.png" })
        {
            string path = ArtFolder + file;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null || importer.textureType == TextureImporterType.Sprite)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = file.Contains("scanlines") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        foreach (string tex in new[] { "SterileTiles", "VentMetal" })
        {
            TextureImporter normal = AssetImporter.GetAtPath(TexFolder + tex + "_Normal_DirectX.png") as TextureImporter;

            if (normal != null && normal.textureType != TextureImporterType.NormalMap)
            {
                normal.textureType = TextureImporterType.NormalMap;
                normal.maxTextureSize = 1024;
                normal.SaveAndReimport();
            }

            TextureImporter color = AssetImporter.GetAtPath(TexFolder + tex + "_Base_color.png") as TextureImporter;

            if (color != null && color.maxTextureSize > 1024)
            {
                color.maxTextureSize = 1024;
                color.SaveAndReimport();
            }
        }
    }

    private static void Deactivate(string name)
    {
        GameObject go = GameObject.Find(name);

        if (go == null || go.transform.parent != null)
            return;

        Undo.RecordObject(go, "Menu");
        go.SetActive(false);
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string value, float size, Color color)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);

        if (font != null)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetLabel(Button button, string text)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;

        if (label == null)
            return;

        label.text = text;
        label.gameObject.name = "Label";
    }

    private static Camera FindMenuCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (camera.targetTexture == null && camera.enabled)
                return camera;
        }

        return null;
    }

    private static Canvas FindOverlayCanvas()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
                return canvas;
        }

        return null;
    }

    private static Button FindButton(params string[] names)
    {
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (string name in names)
            {
                if (button.name == name)
                    return button;
            }
        }

        return null;
    }
}
