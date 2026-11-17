using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Aplica o padrao pixel art na cena aberta: HUD do visor (calor e bateria), radar,
// particulas das pecas, fonte VCR e hierarquia em ingles. Pode ser executado de novo.
public static class PixelArtSceneSetup
{
    private const string ArtFolder = "Assets/UI/PixelArt/";
    private const string PixelFontPath = "Assets/Font/VCR_OSD_MONO SDF.asset";
    private const string ParticleMaterialPath = "Assets/Materials/GlitchParticleMaterial.mat";

    // Escala da arte: 1 pixel do sprite = 3 unidades de UI.
    private const float Px = 3f;

    private static readonly Color HeatRed = new Color(0.92f, 0.2f, 0.25f, 1f);
    private static readonly Color SparkColor = new Color(0.7f, 1f, 0.85f, 1f);

    [MenuItem("Ferramentas/Pixel Art/Aplicar padrao na cena aberta")]
    public static void Apply()
    {
        ConfigureArt();

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);

        int fonts = font != null ? UnifyFonts(font) : 0;
        bool hud = BuildCameraHud(font);
        bool radar = BuildRadar();
        int particles = FixPieceParticles();
        int renamed = RenameToEnglish();
        OrganizeRoots();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log(
            "PixelArtSceneSetup: fontes " + fonts + ", HUD " + hud + ", radar " + radar +
            ", particulas " + particles + ", nomes " + renamed + "."
        );
    }

    // ---------- Importacao ----------

    private static void ConfigureArt()
    {
        ConfigureSprite("hud_heat_frame.png");
        ConfigureSprite("hud_heat_fill.png");
        ConfigureSprite("hud_battery_frame.png");
        ConfigureSprite("hud_battery_cell.png");
        ConfigureSprite("hud_radar_bg.png");
        ConfigureSprite("hud_radar_sweep.png");
        ConfigureSprite("hud_radar_blip.png");
        ConfigureTexture("fx_temporal_spark.png");
    }

    private static TextureImporter BaseImport(string file)
    {
        string path = ArtFolder + file;
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return null;

        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        return importer;
    }

    private static void ConfigureSprite(string file)
    {
        TextureImporter importer = BaseImport(file);

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.SaveAndReimport();
    }

    private static void ConfigureTexture(string file)
    {
        TextureImporter importer = BaseImport(file);

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string file)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + file);
    }

    // ---------- Fontes ----------

    private static int UnifyFonts(TMP_FontAsset font)
    {
        int changed = 0;

        foreach (TMP_Text text in FindAll<TMP_Text>())
        {
            if (text.font == font)
                continue;

            Undo.RecordObject(text, "Fonte pixel art");
            text.font = font;
            text.fontSharedMaterial = font.material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            changed++;
        }

        return changed;
    }

    // ---------- HUD do visor ----------

    private static bool BuildCameraHud(TMP_FontAsset font)
    {
        CameraOverheat overheat = FindFirst<CameraOverheat>();
        CameraBattery battery = FindFirst<CameraBattery>();

        if (overheat == null || battery == null)
            return false;

        SerializedObject batterySO = new SerializedObject(battery);
        SerializedProperty bars = batterySO.FindProperty("batteryBars");

        Image firstBar = bars.arraySize > 0
            ? bars.GetArrayElementAtIndex(0).objectReferenceValue as Image
            : null;

        if (firstBar == null)
            return false;

        RectTransform batteryGroup = (RectTransform)firstBar.rectTransform.parent;
        RectTransform hudRoot = (RectTransform)batteryGroup.parent;

        BuildBattery(batteryGroup, bars);
        BuildHeat(hudRoot, overheat, font);
        return true;
    }

    private static void BuildBattery(RectTransform group, SerializedProperty bars)
    {
        Undo.RecordObject(group.gameObject, "Bateria");

        foreach (LayoutGroup layout in group.GetComponents<LayoutGroup>())
            Undo.DestroyObjectImmediate(layout);

        foreach (ContentSizeFitter fitter in group.GetComponents<ContentSizeFitter>())
            Undo.DestroyObjectImmediate(fitter);

        // O HUD do visor usa ancoras no centro (tela de 1920x1080): o indicador fica
        // onde as barras originais estavam.
        Vector2 center = Vector2.zero;
        int found = 0;

        for (int i = 0; i < bars.arraySize; i++)
        {
            Image bar = bars.GetArrayElementAtIndex(i).objectReferenceValue as Image;

            if (bar != null && bar.rectTransform.parent == group && group.name != "BatteryIndicator")
            {
                center += bar.rectTransform.anchoredPosition;
                found++;
            }
        }

        Undo.RecordObject(group, "Bateria");
        group.anchorMin = group.anchorMax = new Vector2(0.5f, 0.5f);
        group.pivot = new Vector2(0.5f, 0.5f);
        group.localScale = Vector3.one;

        if (found > 0)
            group.anchoredPosition = center / found;

        Image frame = GetOrAdd<Image>(group.gameObject);
        Undo.RecordObject(frame, "Bateria");
        frame.sprite = LoadSprite("hud_battery_frame.png");
        frame.type = Image.Type.Simple;
        frame.color = Color.white;
        frame.raycastTarget = false;

        group.sizeDelta = new Vector2(30f * Px, 14f * Px);
        group.gameObject.name = "BatteryIndicator";

        Sprite cell = LoadSprite("hud_battery_cell.png");

        for (int i = 0; i < bars.arraySize; i++)
        {
            Image bar = bars.GetArrayElementAtIndex(i).objectReferenceValue as Image;

            if (bar == null)
                continue;

            Undo.RecordObject(bar, "Celula");
            Undo.RecordObject(bar.rectTransform, "Celula");
            Undo.RecordObject(bar.gameObject, "Celula");

            bar.gameObject.name = "Cell_" + (i + 1).ToString("00");
            bar.sprite = cell;
            bar.type = Image.Type.Simple;
            bar.raycastTarget = false;

            RectTransform rect = bar.rectTransform;
            rect.SetParent(group, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(4f * Px, 8f * Px);
            rect.anchoredPosition = new Vector2((3f + i * 5f) * Px, 3f * Px);
        }
    }

    private static void BuildHeat(RectTransform hudRoot, CameraOverheat overheat, TMP_FontAsset font)
    {
        Transform old = hudRoot.Find("HeatIndicator");

        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        GameObject root = NewUI("HeatIndicator", hudRoot);
        RectTransform rect = (RectTransform)root.transform;
        // Centro-baixo da tela, na mesma linha do texto [CAM] e do contador de fotos.
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(80f * Px, 16f * Px);
        rect.anchoredPosition = new Vector2(0f, -425f);

        Image frame = root.AddComponent<Image>();
        frame.sprite = LoadSprite("hud_heat_frame.png");
        frame.raycastTarget = false;

        GameObject fillGo = NewUI("Fill", rect);
        RectTransform fillRect = (RectTransform)fillGo.transform;
        fillRect.anchorMin = fillRect.anchorMax = Vector2.zero;
        fillRect.pivot = Vector2.zero;
        fillRect.sizeDelta = new Vector2(60f * Px, 4f * Px);
        fillRect.anchoredPosition = new Vector2(16f * Px, 6f * Px);

        Image fill = fillGo.AddComponent<Image>();
        fill.sprite = LoadSprite("hud_heat_fill.png");
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;

        GameObject warningGo = NewUI("HeatWarning", rect);
        RectTransform warningRect = (RectTransform)warningGo.transform;
        warningRect.anchorMin = warningRect.anchorMax = new Vector2(0.5f, 1f);
        warningRect.pivot = new Vector2(0.5f, 0f);
        warningRect.sizeDelta = new Vector2(400f, 30f);
        warningRect.anchoredPosition = new Vector2(0f, 4f);

        TextMeshProUGUI warning = warningGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            warning.font = font;
            warning.fontSharedMaterial = font.material;
        }
        warning.text = "SUPERAQUECENDO";
        warning.fontSize = 24f;
        warning.color = HeatRed;
        warning.alignment = TextAlignmentOptions.Bottom;
        warning.textWrappingMode = TextWrappingModes.NoWrap;
        warning.raycastTarget = false;
        warningGo.SetActive(false);

        SerializedObject so = new SerializedObject(overheat);
        so.FindProperty("heatBar").objectReferenceValue = fill;
        so.FindProperty("heatWarning").objectReferenceValue = warningGo;
        so.ApplyModifiedProperties();
    }

    // ---------- Radar ----------

    private static bool BuildRadar()
    {
        RadarPecas blip = FindFirst<RadarPecas>();
        LinhaRadar sweep = FindFirst<LinhaRadar>();

        if (blip == null)
            return false;

        RectTransform screen = (RectTransform)blip.transform.parent;
        Image background = FindRadarBackground(screen);

        if (background != null)
        {
            Undo.RecordObject(background, "Radar");
            background.sprite = LoadSprite("hud_radar_bg.png");
            background.color = Color.white;
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(background);
        }

        float size = Mathf.Min(screen.rect.width, screen.rect.height);
        float unit = size / 64f;
        float innerRadius = 27f * unit;

        Image blipImage = blip.GetComponent<Image>();
        Undo.RecordObject(blipImage, "Radar");
        Undo.RecordObject(blip.transform, "Radar");
        Undo.RecordObject(blip.gameObject, "Radar");
        Undo.RecordObject(blip, "Radar");
        blip.gameObject.name = "RadarBlip";
        blipImage.sprite = LoadSprite("hud_radar_blip.png");
        blipImage.preserveAspect = true;
        ((RectTransform)blip.transform).sizeDelta = Vector2.one * 5f * unit;
        blip.transform.localScale = Vector3.one;
        blip.transform.localRotation = Quaternion.identity;
        blip.raioRadar = innerRadius - 3f * unit;
        PrefabUtility.RecordPrefabInstancePropertyModifications(blipImage);
        PrefabUtility.RecordPrefabInstancePropertyModifications(blip);

        if (sweep != null)
        {
            Image sweepImage = sweep.GetComponent<Image>();
            Undo.RecordObject(sweepImage, "Radar");
            Undo.RecordObject(sweep.transform, "Radar");
            Undo.RecordObject(sweep.gameObject, "Radar");
            Undo.RecordObject(sweep, "Radar");
            sweep.gameObject.name = "RadarSweep";
            sweepImage.sprite = LoadSprite("hud_radar_sweep.png");
            sweepImage.color = Color.white;
            sweepImage.preserveAspect = false;
            ((RectTransform)sweep.transform).sizeDelta = new Vector2(2f * unit, innerRadius);
            sweep.transform.localScale = Vector3.one;
            sweep.comprimento = innerRadius;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sweepImage);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sweep);
        }

        return true;
    }

    private static Image FindRadarBackground(RectTransform screen)
    {
        for (Transform t = screen; t != null; t = t.parent)
        {
            Image image = t.GetComponent<Image>();

            if (image != null && image.sprite != null)
                return image;

            if (t.GetComponent<Canvas>() != null)
                break;
        }

        foreach (Image image in screen.root.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite != null && image.sprite.name.Contains("Radar"))
                return image;
        }

        return null;
    }

    // ---------- Particulas das pecas ----------

    private static int FixPieceParticles()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
        Texture2D spark = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "fx_temporal_spark.png");

        if (material != null && spark != null)
        {
            Undo.RecordObject(material, "Particulas");

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", spark);

            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", spark);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        int count = 0;

        foreach (ParticleSystem system in FindAll<ParticleSystem>())
        {
            if (!system.name.StartsWith("TemporalHintParticles"))
                continue;

            Undo.RecordObject(system, "Particulas");

            ParticleSystem.MainModule main = system.main;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(SparkColor, Color.white);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.maxParticles = 40;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.radius = Mathf.Min(shape.radius, 0.35f);

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
            );
            color.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();

            if (renderer != null)
            {
                Undo.RecordObject(renderer, "Particulas");
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.maxParticleSize = 0.05f;

                if (material != null)
                    renderer.sharedMaterial = material;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(system);
            count++;
        }

        return count;
    }

    // ---------- Hierarquia ----------

    private static readonly Dictionary<string, string> ExactNames = new Dictionary<string, string>
    {
        { "Lampada (1)", "Lamp" },
        { "Lampada", "Lamp" },
        { "Baterys", "Batteries" },
        { "Peças", "Pieces" },
        { "Detectador", "PieceDetector" },
        { "todas", "PieceModel" },
        { "Sensitivy", "Sensitivity" },
        { "Brigthnnes", "Brightness" },
        { "ResumeBttn", "ResumeButton" },
        { "TXT", "ModeLabel" },
        { "SaveTXT", "SaveText" },
        { "RadioTXT", "RadioText" },
        { "InteractionTXT", "InteractionText" },
        { "MissionTXT", "MissionText" },
        { "FADE", "Fade" },
        { "CanvasMenu", "PauseCanvas" },
        { "PontoRadar", "RadarBlip" },
        { "LinhaRada", "RadarSweep" },
        { "LinhaRadar", "RadarSweep" },
    };

    private static int RenameToEnglish()
    {
        int changed = 0;

        foreach (Transform t in FindAll<Transform>())
        {
            string current = t.gameObject.name;
            string next = TranslateName(t, current.Trim());

            if (next == current)
                continue;

            Undo.RecordObject(t.gameObject, "Renomear");
            t.gameObject.name = next;
            PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
            changed++;
        }

        return changed;
    }

    private static string TranslateName(Transform t, string name)
    {
        // Nomes genericos so mudam na raiz da cena.
        if (t.parent == null && name == "Canvas") return "HUDCanvas";
        if (t.parent == null && name == "Light") return "Lights";
        if (t.parent != null && (name == "Canvas" || name == "HUDCanvas") && t.GetComponentInParent<RadarPecas>(true) == null
            && t.GetComponentInChildren<RadarPecas>(true) != null)
            return "RadarCanvas";

        if (ExactNames.TryGetValue(name, out string mapped))
            return mapped;

        Match piece = Regex.Match(name, @"^[Pp]eça\s*(\d+)$");
        if (piece.Success)
            return "Piece_" + int.Parse(piece.Groups[1].Value).ToString("00");

        if (Regex.IsMatch(name, @"^[Mm]arcador\s*\d*$"))
            return "DetectorMarker";

        if (Regex.IsMatch(name, @"^TemporalHintParticles( \(\d+\))?$"))
            return "TemporalHintParticles";

        // Pilhas coletaveis no mapa (fora do HUD).
        if (Regex.IsMatch(name, @"^Battery \(\d+\)$") && t.GetComponentInParent<Canvas>(true) == null)
            return "BatteryPickup";

        // Paineis dentro do canvas de pausa.
        Canvas canvas = t.GetComponentInParent<Canvas>(true);
        if (canvas != null && t.parent == canvas.transform && canvas.name.Contains("Pause"))
        {
            if (name == "PauseMenu") return "PausePanel";
            if (name == "SettingsMenu") return "SettingsPanel";
            if (name == "ControlsMenu") return "ControlsPanel";
        }

        if (name == "Text (TMP)" && t.parent != null && t.parent.name == "ControlsPanel")
            return "ControlsText";

        return name;
    }

    private static readonly string[][] Groups =
    {
        new[] { "Systems", "EventSystem", "SaveSystem", "PauseMenu", "MissionSystem", "GlobalVolume", "Audio", "Radio", "PieceDetector" },
        new[] { "Environment", "Directional Light", "Lights", "Map", "EnemyNavigationArea" },
        new[] { "Gameplay", "Player", "TemporalEraser", "Batteries", "Pieces" },
        new[] { "UI", "HUDCanvas", "PauseCanvas", "TimeTravelIntroCanvas", "Radar" },
    };

    private static void OrganizeRoots()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var roots = new Dictionary<string, GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
            roots[root.name] = root;

        int sibling = 1; // GameManager fica na raiz e no topo (usa DontDestroyOnLoad).

        foreach (string[] group in Groups)
        {
            if (!roots.TryGetValue(group[0], out GameObject parent))
            {
                parent = new GameObject(group[0]);
                Undo.RegisterCreatedObjectUndo(parent, "Agrupar");
            }

            parent.transform.SetSiblingIndex(sibling++);

            for (int i = 1; i < group.Length; i++)
            {
                if (!roots.TryGetValue(group[i], out GameObject child) || child == parent)
                    continue;

                Undo.SetTransformParent(child.transform, parent.transform, "Agrupar");
            }
        }

        if (roots.TryGetValue("GameManager", out GameObject manager))
            manager.transform.SetSiblingIndex(0);
    }

    // ---------- Utilitarios ----------

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        Undo.RegisterCreatedObjectUndo(go, "Criar " + name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static T FindFirst<T>() where T : Object
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return found.Length > 0 ? found[0] : null;
    }

    private static T[] FindAll<T>() where T : Object
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }
}
