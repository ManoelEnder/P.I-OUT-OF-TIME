using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Recria o menu principal em pixel art e aplica o mesmo estilo aos botoes da cena aberta.
public static class MenuPixelArtSetup
{
    private const string ArtFolder = "Assets/UI/PixelArt/";
    private const string PixelFontPath = "Assets/Font/VCR_OSD_MONO SDF.asset";
    private const string MenuProfilePath = "Assets/Scenes/Menu/MenuVolume.asset";

    // 1 pixel da arte = 4 unidades de UI nos botoes e paineis.
    private const float PixelScale = 0.25f;

    private static readonly Color Phosphor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color PhosphorShadow = new Color(0.04f, 0.16f, 0.08f, 1f);
    private static readonly Color Beige = new Color(0.86f, 0.8f, 0.64f, 1f);
    private static readonly Color BeigeDark = new Color(0.63f, 0.58f, 0.46f, 1f);
    private static readonly Color RecRed = new Color(0.92f, 0.2f, 0.25f, 1f);

    [MenuItem("Ferramentas/Pixel Art/Estilizar botoes da cena aberta")]
    public static void StyleButtonsMenu()
    {
        ConfigureArt();
        int count = StyleButtons(LoadFont());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("MenuPixelArtSetup: " + count + " botoes estilizados.");
    }

    [MenuItem("Ferramentas/Pixel Art/Recriar menu principal")]
    public static void RebuildMainMenu()
    {
        ConfigureArt();
        TMP_FontAsset font = LoadFont();

        Canvas canvas = FindCanvasWithButtons();

        if (canvas == null)
        {
            Debug.LogError("MenuPixelArtSetup: nenhum Canvas com botoes na cena aberta.");
            return;
        }

        SetupCanvas(canvas);
        BuildLayout(canvas, font);
        int buttons = StyleButtons(font);
        LightenPostProcessing();
        RenameMenuObjects();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("MenuPixelArtSetup: menu recriado, " + buttons + " botoes.");
    }

    // ---------- Arte ----------

    private static void ConfigureArt()
    {
        ConfigureSprite("ui_button_normal.png", new Vector4(3, 3, 3, 3));
        ConfigureSprite("ui_button_hover.png", new Vector4(3, 3, 3, 3));
        ConfigureSprite("ui_button_pressed.png", new Vector4(3, 3, 3, 3));
        ConfigureSprite("ui_panel.png", new Vector4(5, 5, 5, 5));
    }

    private static void ConfigureSprite(string file, Vector4 border)
    {
        string path = ArtFolder + file;
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static Sprite Sprite(string file)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + file);
    }

    private static TMP_FontAsset LoadFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);
    }

    // ---------- Botoes ----------

    private static int StyleButtons(TMP_FontAsset font)
    {
        Sprite normal = Sprite("ui_button_normal.png");
        Sprite hover = Sprite("ui_button_hover.png");
        Sprite pressed = Sprite("ui_button_pressed.png");
        int count = 0;

        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();

            // Botoes sem moldura (ex.: slots do album) ficam como estao.
            if (image == null || button.GetComponentInParent<ScrollRect>(true) != null)
                continue;

            Undo.RecordObject(image, "Botao pixel art");
            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = PixelScale;
            image.color = Color.white;
            image.material = null;

            Undo.RecordObject(button, "Botao pixel art");
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = hover,
                selectedSprite = hover,
                pressedSprite = pressed,
                disabledSprite = normal,
            };

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                Undo.RecordObject(label, "Botao pixel art");
                Undo.RecordObject(label.rectTransform, "Botao pixel art");

                if (font != null)
                {
                    label.font = font;
                    label.fontSharedMaterial = font.material;
                }

                label.text = label.text.Trim().Trim('\'').Trim();
                label.color = Beige;
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = false;
                label.fontSize = Mathf.Clamp(label.fontSize, 24f, 40f);
                label.textWrappingMode = TextWrappingModes.NoWrap;

                RectTransform rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;

                PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            }

            ButtonHoverEffect hoverEffect = button.GetComponent<ButtonHoverEffect>();

            if (hoverEffect != null)
            {
                SerializedObject so = new SerializedObject(hoverEffect);
                so.FindProperty("hoverScale").floatValue = 1.03f;

                SerializedProperty labelProp = so.FindProperty("label");
                if (labelProp != null)
                    labelProp.objectReferenceValue = label;

                so.ApplyModifiedProperties();
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            count++;
        }

        return count;
    }

    // ---------- Layout do menu ----------

    private static Canvas FindCanvasWithButtons()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.GetComponentInChildren<Button>(true) != null)
                return canvas;
        }

        return null;
    }

    private static void SetupCanvas(Canvas canvas)
    {
        // Overlay: a interface fica fora do pos-processamento (nitida e mais leve).
        Undo.RecordObject(canvas, "Canvas");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Canvas");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private static void BuildLayout(Canvas canvas, TMP_FontAsset font)
    {
        Transform root = canvas.transform;

        Button play = FindButton(root, "Play", "PlayButton");
        Button credits = FindButton(root, "Credits", "CreditsButton");
        Button quit = FindButton(root, "Leave", "QuitButton");

        RectTransform panel = FindOrCreateImage(root, "MenuPanel", Sprite("ui_panel.png"));
        panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.sizeDelta = new Vector2(640f, 660f);
        panel.anchoredPosition = new Vector2(120f, 0f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.type = Image.Type.Sliced;
        panelImage.pixelsPerUnitMultiplier = PixelScale;
        panelImage.raycastTarget = false;
        panel.SetAsFirstSibling();

        TextMeshProUGUI shadow = FindOrCreateText(panel, "TitleShadow", font);
        TextMeshProUGUI title = FindOrCreateText(panel, "Title", font);

        foreach (TextMeshProUGUI t in new[] { shadow, title })
        {
            t.text = "OUT OF TIME";
            t.fontSize = 82f;
            t.alignment = TextAlignmentOptions.Center;
            t.characterSpacing = 4f;
            Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(560f, 100f), new Vector2(0f, -55f));
        }

        shadow.color = PhosphorShadow;
        shadow.rectTransform.anchoredPosition += new Vector2(5f, -5f);
        title.color = Phosphor;

        TextMeshProUGUI subtitle = FindOrCreateText(panel, "Subtitle", font);
        subtitle.text = "LABORATÓRIO  1975";
        subtitle.fontSize = 28f;
        subtitle.color = Beige;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.characterSpacing = 6f;
        Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(560f, 40f), new Vector2(0f, -175f));

        RectTransform divider = FindOrCreateImage(panel, "Divider", null);
        divider.GetComponent<Image>().color = BeigeDark;
        divider.GetComponent<Image>().raycastTarget = false;
        Place(divider, new Vector2(0.5f, 1f), new Vector2(480f, 4f), new Vector2(0f, -222f));

        float y = -300f;

        foreach (Button button in new[] { play, credits, quit })
        {
            if (button == null)
                continue;

            RectTransform rect = (RectTransform)button.transform;
            Undo.SetTransformParent(rect, panel, "Menu");
            Undo.RecordObject(rect, "Menu");
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            Place(rect, new Vector2(0.5f, 1f), new Vector2(460f, 80f), new Vector2(0f, y));
            y -= 104f;
        }

        SetLabel(play, "PLAY");
        SetLabel(credits, "CREDITS");
        SetLabel(quit, "QUIT");

        BuildRecIndicator(root, font);

        Transform oldTitle = root.Find("Main Title");

        if (oldTitle != null)
            Undo.DestroyObjectImmediate(oldTitle.gameObject);
    }

    private static void BuildRecIndicator(Transform root, TMP_FontAsset font)
    {
        Transform rec = root.Find("REC");

        if (rec == null)
            return;

        RectTransform recRect = (RectTransform)rec;
        Undo.RecordObject(recRect, "REC");
        recRect.anchorMin = recRect.anchorMax = Vector2.one;
        recRect.pivot = Vector2.one;
        recRect.sizeDelta = new Vector2(150f, 50f);
        recRect.anchoredPosition = new Vector2(-60f, -50f);
        recRect.localScale = Vector3.one;

        Transform dot = rec.Find("Dot");

        if (dot != null)
        {
            RectTransform dotRect = (RectTransform)dot;
            Undo.RecordObject(dotRect, "REC");
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.pivot = new Vector2(0f, 0.5f);
            dotRect.sizeDelta = new Vector2(24f, 24f);
            dotRect.anchoredPosition = Vector2.zero;
            dotRect.localScale = Vector3.one;

            Image dotImage = dot.GetComponent<Image>();
            if (dotImage != null)
            {
                Undo.RecordObject(dotImage, "REC");
                dotImage.color = RecRed;
            }
        }

        TextMeshProUGUI label = FindOrCreateText(rec, "Label", font);
        label.text = "REC";
        label.fontSize = 36f;
        label.color = RecRed;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(40f, 0f);
        labelRect.offsetMax = Vector2.zero;
    }

    private static void SetLabel(Button button, string text)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (label == null)
            return;

        Undo.RecordObject(label, "Menu");
        Undo.RecordObject(label.gameObject, "Menu");
        label.text = text;
        label.fontSize = 40f;
        label.gameObject.name = "Label";
    }

    private static Button FindButton(Transform root, params string[] names)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            foreach (string name in names)
            {
                if (button.name == name)
                    return button;
            }
        }

        return null;
    }

    // ---------- Pos-processamento ----------

    private static void LightenPostProcessing()
    {
        Volume volume = Object.FindFirstObjectByType<Volume>(FindObjectsInactive.Include);

        if (volume == null || volume.sharedProfile == null)
            return;

        // Copia o perfil para o menu nao alterar o efeito da intro na Principal.
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MenuProfilePath);

        if (profile == null)
        {
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile), MenuProfilePath);
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MenuProfilePath);
        }

        if (profile == null)
            return;

        Undo.RecordObject(volume, "Volume");
        volume.sharedProfile = profile;

        if (profile.TryGet(out MotionBlur motionBlur)) motionBlur.active = false;
        if (profile.TryGet(out PaniniProjection panini)) panini.active = false;

        if (profile.TryGet(out Bloom bloom))
        {
            bloom.intensity.Override(1.2f);
            bloom.scatter.Override(0.6f);
        }

        if (profile.TryGet(out FilmGrain grain)) grain.intensity.Override(0.35f);
        if (profile.TryGet(out ChromaticAberration chromatic)) chromatic.intensity.Override(0.15f);
        if (profile.TryGet(out LensDistortion lens)) lens.intensity.Override(0.12f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
    }

    private static void RenameMenuObjects()
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string next = t.name switch
            {
                "ChanceScene" => "SceneLoader",
                "Canvas" when t.parent == null => "MenuCanvas",
                _ => t.name,
            };

            if (next == t.name)
                continue;

            Undo.RecordObject(t.gameObject, "Renomear");
            t.name = next;
        }
    }

    // ---------- Utilitarios ----------

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, anchor.y);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static RectTransform FindOrCreateImage(Transform parent, string name, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        GameObject go;

        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Criar " + name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
        }

        Image image = go.GetComponent<Image>();
        Undo.RecordObject(image, "Imagem");
        image.sprite = sprite;
        return (RectTransform)go.transform;
    }

    private static TextMeshProUGUI FindOrCreateText(Transform parent, string name, TMP_FontAsset font)
    {
        Transform existing = parent.Find(name);
        GameObject go;

        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "Criar " + name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
        }

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        Undo.RecordObject(text, "Texto");

        if (font != null)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }
}
