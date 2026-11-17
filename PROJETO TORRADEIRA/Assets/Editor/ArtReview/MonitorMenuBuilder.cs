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
    private const string ArtFolder = "Assets/UI/PixelArt/";
    private const string MaterialFolder = "Assets/Materials/Menu";
    private const string PixelFontPath = "Assets/Font/VCR_OSD_MONO SDF.asset";
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

    [MenuItem("Ferramentas/Pixel Art/Montar menu em monitores CRT")]
    public static void Build()
    {
        menuCamera = FindMenuCamera();

        if (menuCamera == null)
        {
            Debug.LogError("MonitorMenuBuilder: abra a cena Menu (nenhuma camera encontrada).");
            return;
        }

        ConfigureTextures();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);

        Button play = FindButton("PlayButton", "Play");
        Button credits = FindButton("CreditsButton", "Credits");
        Button quit = FindButton("QuitButton", "Leave");

        // Tira os botoes do cenario antigo antes de recria-lo.
        Canvas overlay = FindOverlayCanvas();
        foreach (Button b in new[] { play, credits, quit })
        {
            if (b != null && overlay != null)
                Undo.SetTransformParent(b.transform, overlay.transform, "Menu");
        }

        foreach (string old in new[] { SetName, "MonitorWall" })
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

        BuildRoom(root);
        BuildLeftRack(root, play, credits, quit);
        BuildRightRack(root);
        BuildBackWall(root);
        BuildConsole(root);
        BuildCables(root);
        BuildLights(root);

        if (overlay != null)
        {
            Transform oldPanel = overlay.transform.Find("MenuPanel");
            if (oldPanel != null)
                Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        ConfigureCamera();
        ConfigureAtmosphere();
        ConfigureVolume();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("MonitorMenuBuilder: sala do menu montada.");
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
        BuildMenuScreen(tv, play, credits, quit);

        // Prateleira do meio: osciloscopio com estatica e caixa de contador.
        Transform scope = Device(rack, "Oscilloscope", new Vector3(-0.32f, 0.49f, -0.08f), new Vector3(0.5f, 0.3f, 0.5f));
        AddQuad(scope, new Vector3(-0.06f, 0.02f, -0.251f), new Vector2(0.24f, 0.18f), ScreenMaterial("CrtStaticSmall", 0.8f)).AddComponent<CrtNoiseScreen>();
        Led(scope, new Vector3(0.15f, 0.06f, -0.252f), "08.5", 0.55f);

        Transform counter = Device(rack, "Counter", new Vector3(0.36f, 0.49f, -0.05f), new Vector3(0.48f, 0.18f, 0.5f));
        Led(counter, new Vector3(0f, 0f, -0.252f), "-01975", 0.7f);

        // Prateleira de baixo: modulos com contadores vermelhos.
        Transform module = Device(rack, "Module", new Vector3(-0.15f, 0.13f, -0.05f), new Vector3(1.0f, 0.22f, 0.55f));
        Led(module, new Vector3(-0.25f, 0f, -0.277f), "47093", 0.75f);
        Led(module, new Vector3(0.25f, 0f, -0.277f), "00:17", 0.75f);
    }

    // ---------- Estante da direita: camera de seguranca ----------

    private static void BuildRightRack(Transform root)
    {
        Transform rack = Rack(root, "Rack_Feed", new Vector3(1.1f, Floor, 3.05f), 1.25f, 1.0f, 1.02f, new[] { 0.06f, 0.30f, 1.02f }, -4f);

        Transform feedTv = Tv(rack, "Monitor_Feed", new Vector3(0f, 1.05f, 0f), 1.35f);
        BuildFeed(feedTv);

        Transform staticTv = Tv(rack, "Monitor_Static", new Vector3(0.05f, 0.33f, 0f), 0.85f);
        StaticScreen(staticTv, 0.8f);

        Device(rack, "Recorder", new Vector3(-0.38f, 0.40f, -0.1f), new Vector3(0.4f, 0.16f, 0.45f));
        Led(rack.Find("Recorder"), new Vector3(0f, 0f, -0.227f), "REC", 0.6f);

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
        bulb.GetComponent<MeshRenderer>().sharedMaterial = GlowMaterial("MenuRedBulb", new Color(1f, 0.04f, 0.02f) * 4f);
    }

    // ---------- Primeiro plano: console ----------

    private static void BuildConsole(Transform root)
    {
        Transform console = Group(root, "Console");
        console.localPosition = new Vector3(1.05f, Floor, 1.4f);
        console.localRotation = Quaternion.Euler(0f, FaceCamera(console.localPosition) - 25f, 0f);

        Material metal = MetalMaterial();
        Box(console, "Body", new Vector3(0f, 0.52f, 0f), new Vector3(1.0f, 1.04f, 0.7f), metal);

        Transform top = Box(console, "Panel", new Vector3(0f, 1.08f, 0.05f), new Vector3(1.0f, 0.06f, 0.62f), metal).transform;
        top.localRotation = Quaternion.Euler(-18f, 0f, 0f);

        Transform dial = Device(console, "Dials", new Vector3(-0.2f, 1.13f, 0.02f), new Vector3(0.35f, 0.05f, 0.22f));
        dial.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        Led(console, new Vector3(0.22f, 0.86f, -0.352f), "-01005", 0.7f);
        Led(console, new Vector3(-0.22f, 0.86f, -0.352f), "88:04", 0.7f);

        // Luz fria fraca vinda do monitor ao lado, para o console nao sumir no preto.
        PointLight(console, "ConsoleGlow", new Vector3(-0.3f, 1.5f, -0.6f), new Color(0.75f, 0.82f, 0.95f), 1.4f, 2.2f);
    }

    // ---------- Cabos ----------

    private static void BuildCables(Transform root)
    {
        Transform cables = Group(root, "Cables");
        Material cable = UnlitMaterial("MenuCable", new Color(0.015f, 0.015f, 0.018f));

        float y = Floor + 0.015f;
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

    // ---------- Luzes ----------

    private static void BuildLights(Transform root)
    {
        Transform lights = Group(root, "Lights");

        // Vermelho forte no fundo, por tras das estantes.
        Light red = PointLight(lights, "RedBacklight", new Vector3(-1.3f, 1.3f, 5.75f), new Color(1f, 0.04f, 0.02f), 16f, 7f);
        red.shadows = LightShadows.Soft;
        PointLight(lights, "RedFill", new Vector3(-2.6f, 0.2f, 3.8f), new Color(1f, 0.04f, 0.02f), 5f, 4.5f);

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

        AddCanvasOverlay(screen);

        // Luz da tela iluminando a estante e a cintilacao do tubo.
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
        GameObject quad = AddQuad(tv, TvScreenCenter + new Vector3(0f, 0f, 0.002f), TvScreenSize, ScreenMaterial("CrtFeed", 1.3f));
        quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        // Camera de seguranca filmando a propria sala do alto.
        GameObject camGo = new GameObject("SecurityCamera", typeof(Camera));
        camGo.transform.SetParent(tv.root, false);
        Transform set = tv.root;
        camGo.transform.position = set.TransformPoint(new Vector3(2.6f, 1.6f, 0.3f));
        camGo.transform.rotation = Quaternion.LookRotation(set.TransformPoint(new Vector3(-0.5f, -0.8f, 4.5f)) - camGo.transform.position);

        Camera feed = camGo.GetComponent<Camera>();
        feed.fieldOfView = 70f;
        feed.nearClipPlane = 0.05f;
        feed.farClipPlane = 20f;
        feed.enabled = false;
        feed.GetUniversalAdditionalCameraData().renderPostProcessing = false;

        CrtCameraFeed component = quad.AddComponent<CrtCameraFeed>();
        SerializedObject so = new SerializedObject(component);
        so.FindProperty("feedCamera").objectReferenceValue = feed;
        so.ApplyModifiedPropertiesWithoutUndo();

        RectTransform overlay = CreateScreenCanvas(tv, "Overlay", out _, 0.004f);
        Object.DestroyImmediate(overlay.GetComponent<GraphicRaycaster>());
        Object.DestroyImmediate(overlay.Find("Background").gameObject);
        AddCanvasOverlay(overlay);

        TextMeshProUGUI rec = Text(overlay, "Rec", "CAM 03  REC", 26f, LedRed);
        rec.alignment = TextAlignmentOptions.TopLeft;
        Stretch(rec.rectTransform, new Vector2(24f, 0f), new Vector2(0f, -18f));

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
            AssetDatabase.CreateFolder("Assets/Materials", "Menu");

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
