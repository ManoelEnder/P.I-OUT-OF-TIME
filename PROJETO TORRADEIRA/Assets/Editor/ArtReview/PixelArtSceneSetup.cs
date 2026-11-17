using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Aplica o padrao pixel art na cena aberta: indicador de temperatura no HUD do visor
// e fonte VCR em todos os textos. Pode ser executado de novo sem duplicar objetos.
public static class PixelArtSceneSetup
{
    private const string FramePath = "Assets/UI/PixelArt/ui_frame_pixel.png";
    private const string FillPath = "Assets/UI/PixelArt/ui_fill_pixel.png";
    private const string PixelFontPath = "Assets/Font/VCR_OSD_MONO SDF.asset";
    private const string HeatRootName = "HeatIndicator";
    private const string WarningName = "HeatWarning";

    private static readonly Color Beige = new Color(0.86f, 0.80f, 0.64f, 1f);
    private static readonly Color Phosphor = new Color(0.45f, 1f, 0.55f, 1f);
    private static readonly Color Danger = new Color(1f, 0.35f, 0.15f, 1f);

    [MenuItem("Ferramentas/Pixel Art/Aplicar padrao na cena aberta")]
    public static void Apply()
    {
        ConfigureSprite(FramePath, new Vector4(2, 2, 2, 2));
        ConfigureSprite(FillPath, Vector4.zero);

        Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
        Sprite fill = AssetDatabase.LoadAssetAtPath<Sprite>(FillPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);

        if (frame == null || fill == null || font == null)
        {
            Debug.LogError("PixelArtSceneSetup: sprites ou fonte VCR nao encontrados.");
            return;
        }

        int fonts = UnifyFonts(font);
        bool heat = BuildHeatIndicator(frame, fill, font);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log(
            "PixelArtSceneSetup: " + fonts + " textos trocados para VCR; indicador de temperatura " +
            (heat ? "configurado." : "nao criado (sem CameraOverheat/CameraBattery na cena).")
        );
    }

    private static void ConfigureSprite(string path, Vector4 border)
    {
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
        importer.SaveAndReimport();
    }

    private static int UnifyFonts(TMP_FontAsset font)
    {
        int changed = 0;

        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text.font == font || EditorUtility.IsPersistent(text))
                continue;

            Undo.RecordObject(text, "Fonte pixel art");
            text.font = font;
            text.fontSharedMaterial = font.material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            changed++;
        }

        return changed;
    }

    private static bool BuildHeatIndicator(Sprite frame, Sprite fill, TMP_FontAsset font)
    {
        CameraOverheat overheat = Find<CameraOverheat>();
        CameraBattery battery = Find<CameraBattery>();

        if (overheat == null || battery == null)
            return false;

        SerializedObject batterySO = new SerializedObject(battery);
        SerializedProperty bars = batterySO.FindProperty("batteryBars");

        Image firstBar = bars.arraySize > 0
            ? bars.GetArrayElementAtIndex(0).objectReferenceValue as Image
            : null;

        if (firstBar == null)
            return false;

        RectTransform batteryGroup = firstBar.rectTransform.parent as RectTransform;
        RectTransform hudRoot = batteryGroup.parent as RectTransform;

        // Canto inferior direito do conjunto de barras, no espaco local do HUD.
        Vector2 barsBottomRight = new Vector2(float.MinValue, float.MaxValue);
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < bars.arraySize; i++)
        {
            Image bar = bars.GetArrayElementAtIndex(i).objectReferenceValue as Image;

            if (bar == null)
                continue;

            bar.rectTransform.GetWorldCorners(corners);

            foreach (Vector3 corner in corners)
            {
                Vector2 local = hudRoot.InverseTransformPoint(corner);
                barsBottomRight.x = Mathf.Max(barsBottomRight.x, local.x);
                barsBottomRight.y = Mathf.Min(barsBottomRight.y, local.y);
            }
        }

        // Indicador ancorado no canto superior direito do HUD, logo abaixo da bateria.
        Rect hudRect = hudRoot.rect;
        RectTransform root = FindOrCreate(hudRoot, HeatRootName, frame, out Image frameImage);
        root.anchorMin = Vector2.one;
        root.anchorMax = Vector2.one;
        root.pivot = Vector2.one;
        root.localScale = Vector3.one;
        root.sizeDelta = new Vector2(150f, 20f);
        root.anchoredPosition = new Vector2(
            barsBottomRight.x - hudRect.xMax,
            barsBottomRight.y - hudRect.yMax - 10f
        );
        root.SetSiblingIndex(batteryGroup.GetSiblingIndex() + 1);
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Beige;
        frameImage.raycastTarget = false;

        RectTransform fillRect = FindOrCreate(root, "Fill", fill, out Image fillImage);
        Stretch(fillRect, 3f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;
        fillImage.color = Phosphor;
        fillImage.raycastTarget = false;

        TextMeshProUGUI label = FindOrCreateText(root, "Label", font);
        label.text = "TEMP";
        label.fontSize = 22f;
        label.color = Beige;
        label.alignment = TextAlignmentOptions.MidlineRight;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(1f, 0.5f);
        labelRect.sizeDelta = new Vector2(90f, 0f);
        labelRect.anchoredPosition = new Vector2(-6f, 0f);

        // Aviso de superaquecimento abaixo da barra, longe da mira.
        TextMeshProUGUI warning = FindOrCreateText(root, WarningName, font);
        warning.text = "! SUPERAQUECENDO";
        warning.fontSize = 24f;
        warning.color = Danger;
        warning.alignment = TextAlignmentOptions.TopRight;
        RectTransform warningRect = warning.rectTransform;
        warningRect.anchorMin = new Vector2(1f, 0f);
        warningRect.anchorMax = new Vector2(1f, 0f);
        warningRect.pivot = new Vector2(1f, 1f);
        warningRect.sizeDelta = new Vector2(320f, 30f);
        warningRect.anchoredPosition = new Vector2(0f, -6f);
        warning.gameObject.SetActive(false);

        SerializedObject overheatSO = new SerializedObject(overheat);
        overheatSO.FindProperty("heatBar").objectReferenceValue = fillImage;
        overheatSO.FindProperty("heatWarning").objectReferenceValue = warning.gameObject;
        overheatSO.ApplyModifiedProperties();

        return true;
    }

    private static T Find<T>() where T : Object
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return found.Length > 0 ? found[0] : null;
    }

    private static RectTransform FindOrCreate(Transform parent, string name, Sprite sprite, out Image image)
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
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
        }

        image = go.GetComponent<Image>();

        if (image == null)
            image = go.AddComponent<Image>();

        Undo.RecordObject(image, "Configurar " + name);
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
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
        }

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSharedMaterial = font.material;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        return text;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
