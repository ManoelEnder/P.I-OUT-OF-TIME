using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Monta o menu principal dentro de uma parede de monitores CRT na sala 3D do menu.
// Pode ser executado de novo: recria a parede e reaproveita os botoes existentes.
public static class MonitorMenuBuilder
{
    private const string PsxFolder = "Assets/ThirdParty/PSXNostalgia-FBX/PSXNostalgia-FBX/";
    private const string ArtFolder = "Assets/UI/PixelArt/";
    private const string MaterialFolder = "Assets/Materials/Menu";
    private const string PixelFontPath = "Assets/Font/VCR_OSD_MONO SDF.asset";
    private const string MenuProfilePath = "Assets/Scenes/Menu/MenuVolume.asset";
    private const string WallName = "MonitorWall";

    // Area da tela no modelo TV.fbx (espaco local; a tela olha para +Z).
    private static readonly Vector3 TvScreenCenter = new Vector3(-0.06f, 0.40f, 0.186f);
    private static readonly Vector2 TvScreenSize = new Vector2(0.50f, 0.40f);

    // Area da tela no modelo PcScreen.fbx.
    private static readonly Vector3 PcScreenCenter = new Vector3(0f, 0.30f, 0.142f);
    private static readonly Vector2 PcScreenSize = new Vector2(0.38f, 0.29f);

    private static readonly Color ScreenBlack = new Color(0.03f, 0.03f, 0.035f, 1f);
    private static readonly Color Phosphor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color TextIdle = new Color(0.72f, 0.72f, 0.72f, 1f);
    private static readonly Color Beige = new Color(0.86f, 0.8f, 0.64f, 1f);

    private enum ScreenKind { Menu, Static, Feed, Title }

    private struct Slot
    {
        public string Name;
        public string Model;
        public Vector3 Position;
        public float Yaw;
        public ScreenKind Kind;
        public bool Stand;
    }

    // Posicoes relativas a camera do menu (x direita, y cima, z frente).
    private static readonly Slot[] Slots =
    {
        new Slot { Name = "Monitor_Menu", Model = "TVSet/TV.fbx", Position = new Vector3(-0.50f, -0.26f, 1.42f), Yaw = 20f, Kind = ScreenKind.Menu },
        new Slot { Name = "Monitor_StaticA", Model = "TVSet/TV.fbx", Position = new Vector3(-0.56f, -0.99f, 1.56f), Yaw = 16f, Kind = ScreenKind.Static, Stand = true },
        new Slot { Name = "Monitor_Feed", Model = "TVSet/TV.fbx", Position = new Vector3(0.62f, -0.10f, 2.00f), Yaw = -22f, Kind = ScreenKind.Feed },
        new Slot { Name = "Monitor_StaticB", Model = "TVSet/TV.fbx", Position = new Vector3(1.30f, -0.70f, 2.40f), Yaw = -30f, Kind = ScreenKind.Static, Stand = true },
        new Slot { Name = "Monitor_Title", Model = "ComputerSet/PcScreen.fbx", Position = new Vector3(0.30f, -0.66f, 1.30f), Yaw = -12f, Kind = ScreenKind.Title },
    };

    [MenuItem("Ferramentas/Pixel Art/Montar menu em monitores CRT")]
    public static void Build()
    {
        Camera camera = FindMenuCamera();

        if (camera == null)
        {
            Debug.LogError("MonitorMenuBuilder: abra a cena Menu (nenhuma camera encontrada).");
            return;
        }

        ConfigureSprites();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);

        Button play = FindButton("PlayButton", "Play");
        Button credits = FindButton("CreditsButton", "Credits");
        Button quit = FindButton("QuitButton", "Leave");

        // Tira os botoes da parede antiga antes de recria-la.
        Canvas overlay = FindOverlayCanvas();
        foreach (Button b in new[] { play, credits, quit })
        {
            if (b != null && overlay != null)
                Undo.SetTransformParent(b.transform, overlay.transform, "Menu");
        }

        GameObject oldWall = GameObject.Find(WallName);
        if (oldWall != null)
            Undo.DestroyObjectImmediate(oldWall);

        GameObject wall = new GameObject(WallName);
        Undo.RegisterCreatedObjectUndo(wall, "Menu CRT");
        wall.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);

        foreach (Slot slot in Slots)
            BuildMonitor(wall.transform, slot, camera, font, play, credits, quit);

        BuildLights(wall.transform);

        if (overlay != null)
        {
            Transform oldPanel = overlay.transform.Find("MenuPanel");
            if (oldPanel != null)
                Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        TuneDepthOfField();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("MonitorMenuBuilder: menu montado em " + Slots.Length + " monitores.");
    }

    // ---------- Monitores ----------

    private static void BuildMonitor(Transform wall, Slot slot, Camera camera, TMP_FontAsset font, Button play, Button credits, Button quit)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PsxFolder + slot.Model);

        if (model == null)
            return;

        GameObject monitor = (GameObject)PrefabUtility.InstantiatePrefab(model);
        Undo.RegisterCreatedObjectUndo(monitor, "Monitor");
        monitor.name = slot.Name;
        monitor.transform.SetParent(wall, false);
        monitor.transform.localPosition = slot.Position;
        monitor.transform.localRotation = Quaternion.Euler(0f, 180f + slot.Yaw, 0f);

        if (slot.Stand)
        {
            GameObject standModel = AssetDatabase.LoadAssetAtPath<GameObject>(PsxFolder + "TVSet/TVStand.fbx");

            if (standModel != null)
            {
                GameObject stand = (GameObject)PrefabUtility.InstantiatePrefab(standModel);
                stand.name = "Stand";
                stand.transform.SetParent(monitor.transform, false);
                stand.transform.localPosition = new Vector3(0f, -0.455f, -0.15f);
            }
        }

        bool isTv = slot.Model.EndsWith("TV.fbx");
        Vector3 center = isTv ? TvScreenCenter : PcScreenCenter;
        Vector2 size = isTv ? TvScreenSize : PcScreenSize;

        switch (slot.Kind)
        {
            case ScreenKind.Menu:
                BuildMenuScreen(monitor.transform, center, size, camera, font, play, credits, quit);
                break;
            case ScreenKind.Static:
                AddQuad(monitor.transform, center, size, ScreenMaterial("CrtStatic")).AddComponent<CrtNoiseScreen>();
                break;
            case ScreenKind.Feed:
                BuildFeed(monitor.transform, center, size, camera);
                break;
            case ScreenKind.Title:
                BuildTitleScreen(monitor.transform, center, size, camera, font);
                break;
        }
    }

    private static GameObject AddQuad(Transform parent, Vector3 center, Vector2 size, Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.name = "Screen";
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = center + new Vector3(0f, 0f, 0.002f);
        // O Quad olha para -Z; gira para acompanhar a tela (+Z).
        quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return quad;
    }

    private static void BuildFeed(Transform monitor, Vector3 center, Vector2 size, Camera menuCamera)
    {
        GameObject quad = AddQuad(monitor, center, size, ScreenMaterial("CrtFeed"));

        GameObject camGo = new GameObject("SecurityCamera", typeof(Camera));
        camGo.transform.SetParent(monitor.parent, false);
        camGo.transform.localPosition = new Vector3(0.8f, 0.6f, -0.6f);
        camGo.transform.localRotation = Quaternion.Euler(18f, -150f, 0f);

        Camera feed = camGo.GetComponent<Camera>();
        feed.fieldOfView = 75f;
        feed.nearClipPlane = 0.05f;
        feed.farClipPlane = menuCamera.farClipPlane;
        feed.enabled = false;
        feed.GetUniversalAdditionalCameraData().renderPostProcessing = false;

        CrtCameraFeed component = quad.AddComponent<CrtCameraFeed>();
        SerializedObject so = new SerializedObject(component);
        so.FindProperty("feedCamera").objectReferenceValue = feed;
        so.ApplyModifiedPropertiesWithoutUndo();

        AddScreenOverlay(monitor, center, size, menuCamera);
    }

    private static void BuildTitleScreen(Transform monitor, Vector3 center, Vector2 size, Camera camera, TMP_FontAsset font)
    {
        RectTransform screen = CreateScreenCanvas(monitor, "TitleScreen", center, size, camera, out _);

        TextMeshProUGUI title = Text(screen, "Title", font, "OUT OF\nTIME", 64f, Phosphor);
        title.alignment = TextAlignmentOptions.Center;
        title.lineSpacing = -10f;
        Stretch(title.rectTransform, new Vector2(10f, 70f), new Vector2(-10f, -20f));

        TextMeshProUGUI year = Text(screen, "Year", font, "LAB 1975", 26f, Beige);
        year.alignment = TextAlignmentOptions.Bottom;
        Stretch(year.rectTransform, new Vector2(10f, 22f), new Vector2(-10f, -200f));

        AddCanvasOverlay(screen);
    }

    private static void BuildMenuScreen(Transform monitor, Vector3 center, Vector2 size, Camera camera, TMP_FontAsset font, Button play, Button credits, Button quit)
    {
        RectTransform screen = CreateScreenCanvas(monitor, "MenuScreen", center, size, camera, out CanvasGroup group);

        TextMeshProUGUI header = Text(screen, "Header", font, "> OUT OF TIME_", 30f, Phosphor);
        header.alignment = TextAlignmentOptions.TopLeft;
        Stretch(header.rectTransform, new Vector2(40f, 0f), new Vector2(-20f, -28f));

        float y = -110f;

        foreach (Button button in new[] { play, credits, quit })
        {
            if (button == null)
                continue;

            RectTransform rect = (RectTransform)button.transform;
            Undo.SetTransformParent(rect, screen, "Menu");
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.localPosition = Vector3.zero;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(440f, 70f);
            rect.anchoredPosition = new Vector2(40f, y);
            y -= 82f;

            StyleScreenButton(button, font);
        }

        SetLabel(play, "PLAY");
        SetLabel(credits, "CREDITS");
        SetLabel(quit, "QUIT");

        AddCanvasOverlay(screen);

        // Luz da tela iluminando o movel e a cintilacao do tubo.
        GameObject lightGo = new GameObject("ScreenGlow", typeof(Light));
        lightGo.transform.SetParent(monitor, false);
        lightGo.transform.localPosition = center + new Vector3(0f, 0f, 0.35f);

        Light glow = lightGo.GetComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.85f, 0.88f, 0.95f);
        glow.intensity = 0.8f;
        glow.range = 1.6f;
        glow.shadows = LightShadows.None;

        CrtFlicker flicker = screen.gameObject.AddComponent<CrtFlicker>();
        SerializedObject so = new SerializedObject(flicker);
        so.FindProperty("screenLight").objectReferenceValue = glow;
        so.FindProperty("canvasGroup").objectReferenceValue = group;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StyleScreenButton(Button button, TMP_FontAsset font)
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

            label.fontSize = 54f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
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
            so.FindProperty("hoverScale").floatValue = 1f;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("normalTextColor").colorValue = TextIdle;
            so.FindProperty("hoverTextColor").colorValue = Phosphor;
            so.FindProperty("hoverPrefix").stringValue = "> ";
            so.ApplyModifiedProperties();
        }
    }

    private static RectTransform CreateScreenCanvas(Transform monitor, string name, Vector3 center, Vector2 size, Camera camera, out CanvasGroup group)
    {
        const float unitsPerMeter = 1000f;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        go.transform.SetParent(monitor, false);
        go.transform.localPosition = center + new Vector3(0f, 0f, 0.003f);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        go.transform.localScale = Vector3.one / unitsPerMeter;

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 4f;

        RectTransform rect = (RectTransform)go.transform;
        rect.sizeDelta = size * unitsPerMeter;

        group = go.GetComponent<CanvasGroup>();

        Image background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        background.transform.SetParent(go.transform, false);
        background.color = ScreenBlack;
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

    private static void AddScreenOverlay(Transform monitor, Vector3 center, Vector2 size, Camera camera)
    {
        RectTransform overlay = CreateScreenCanvas(monitor, "Overlay", center + new Vector3(0f, 0f, 0.001f), size, camera, out _);
        Object.DestroyImmediate(overlay.GetComponent<GraphicRaycaster>());
        Object.DestroyImmediate(overlay.Find("Background").gameObject);
        AddCanvasOverlay(overlay);

        TextMeshProUGUI rec = Text(overlay, "Rec", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath), "CAM 03  REC", 26f, new Color(0.92f, 0.2f, 0.25f, 1f));
        rec.alignment = TextAlignmentOptions.TopLeft;
        Stretch(rec.rectTransform, new Vector2(20f, 0f), new Vector2(0f, -16f));
    }

    // ---------- Luzes ----------

    private static void BuildLights(Transform wall)
    {
        Light red = NewLight(wall, "RedBacklight", new Vector3(0.3f, 0.6f, 3.2f), new Color(0.9f, 0.1f, 0.08f), 3f, 5f);
        red.shadows = LightShadows.None;

        Light cool = NewLight(wall, "FeedGlow", new Vector3(0.6f, 0.1f, 1.5f), new Color(0.75f, 0.85f, 1f), 0.6f, 1.8f);
        cool.shadows = LightShadows.None;
    }

    private static Light NewLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name, typeof(Light));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;

        Light light = go.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        return light;
    }

    // ---------- Pos-processamento ----------

    private static void TuneDepthOfField()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MenuProfilePath);

        if (profile == null || !profile.TryGet(out DepthOfField dof))
            return;

        // Monitores nitidos (ate ~3 m); a sala ao fundo continua desfocada.
        dof.gaussianStart.Override(3.5f);
        dof.gaussianEnd.Override(25f);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
    }

    // ---------- Utilitarios ----------

    private static void ConfigureSprites()
    {
        foreach (string file in new[] { "crt_scanlines.png", "crt_vignette.png" })
        {
            string path = ArtFolder + file;
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
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
    }

    private static Material ScreenMaterial(string name)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Menu");

        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = Color.white;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, TMP_FontAsset font, string value, float size, Color color)
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
            if (camera.targetTexture == null)
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
