using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Padroniza o HUD da fase (revisao, Parte 2). Reexecutavel.
// Escala de tamanhos unica (20/24/26/30), fonte VCR, moldura na missao e cores:
// bateria branca/cinza (pilha segmentada) x calor vermelho (barra continua).
public static class HudStyleSetup
{
    public const string ScenePath = "Assets/Scenes/Principal.unity";
    public const string HudCanvas = "HUDCanvas";
    private const string FontPath = "Assets/Fonts/VCR_OSD_MONO SDF.asset";
    private const string PanelSprite = "Assets/Art/UI/PixelArt/ui_panel.png";

    public static readonly HashSet<float> Sizes = new HashSet<float> { 20f, 24f, 26f, 30f };

    // Tamanho de cada texto do HUD; o resto vai para 24.
    private static readonly Dictionary<string, float> SizeByName = new Dictionary<string, float>
    {
        { "ModeLabel", 30f },
        { "PhotoCounter", 30f },
        { "MissionText", 24f },
        { "MissionHint", 24f },
        { "InteractionText", 24f },
        { "HeatWarning", 24f },
        { "RadioText", 26f },
        { "Message", 26f },
        { "SaveText", 20f },
    };

    private static readonly Color HeatRed = new Color(0.92f, 0.2f, 0.25f, 1f);
    private static readonly Color HeatCritical = new Color(1f, 0.85f, 0.85f, 1f);
    private static readonly Color BatteryWhite = new Color(0.92f, 0.92f, 0.92f, 1f);

    [MenuItem("Ferramentas/Visual/Padronizar HUD da fase")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        Canvas hud = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.name == HudCanvas)
                hud = c;

        if (hud == null)
        {
            Debug.LogError("HudStyleSetup: " + HudCanvas + " nao encontrado.");
            return;
        }

        foreach (TMP_Text text in hud.GetComponentsInChildren<TMP_Text>(true))
        {
            Undo.RecordObject(text, "HUD");
            float size = SizeByName.TryGetValue(text.name, out float s) ? s : 24f;

            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }

            // Textos que mudam de tamanho (radio, contador) continuam se ajustando, mas com teto da escala.
            if (text.enableAutoSizing)
            {
                text.fontSizeMin = Mathf.Min(16f, size);
                text.fontSizeMax = size;
            }

            text.fontSize = size;
        }

        AddMissionFrame(hud.transform.Find("MissionsPanel"));
        SetColors();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("HUD padronizado");
    }

    public static void ApplyFromCommandLine()
    {
        Apply();
        EditorApplication.Exit(0);
    }

    // Placa escura semitransparente atras das missoes: leitura em areas claras.
    private static void AddMissionFrame(Transform panel)
    {
        if (panel == null || panel.Find("Frame") != null)
            return;

        GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(frame, "HUD");
        frame.layer = panel.gameObject.layer;
        frame.transform.SetParent(panel, false);
        frame.transform.SetAsFirstSibling();

        RectTransform rect = (RectTransform)frame.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-18f, -14f);
        rect.offsetMax = new Vector2(18f, 14f);

        Image image = frame.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSprite);
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.5f;
        image.color = new Color(0.05f, 0.05f, 0.06f, 0.72f);
        image.raycastTarget = false;
    }

    private static void SetColors()
    {
        CameraOverheat heat = Object.FindFirstObjectByType<CameraOverheat>(FindObjectsInactive.Include);
        if (heat != null)
        {
            var so = new SerializedObject(heat);
            so.FindProperty("barraNormal").colorValue = HeatRed;
            so.FindProperty("barraCritica").colorValue = HeatCritical;
            so.ApplyModifiedProperties();
        }

        CameraBattery battery = Object.FindFirstObjectByType<CameraBattery>(FindObjectsInactive.Include);
        if (battery != null)
        {
            var so = new SerializedObject(battery);
            so.FindProperty("corNormal").colorValue = BatteryWhite;
            so.ApplyModifiedProperties();
        }
    }
}
