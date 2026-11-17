using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Refaz o menu de pausa como um terminal CRT, no mesmo estilo do menu principal.
public static class PauseMenuBuilder
{
    private const string ArtFolder = "Assets/UI/PixelArt/";
    private const string PixelFontPath = "Assets/Font/VCR_OSD_MONO SDF.asset";

    private static readonly Color ScreenBlack = new Color(0.03f, 0.03f, 0.035f, 0.96f);
    private static readonly Color Phosphor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color TextIdle = new Color(0.72f, 0.72f, 0.72f, 1f);
    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);

    [MenuItem("Ferramentas/Pixel Art/Refazer menu de pausa")]
    public static void Build()
    {
        PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);

        if (pause == null)
        {
            Debug.LogError("PauseMenuBuilder: abra a cena Principal (PauseMenu nao encontrado).");
            return;
        }

        SerializedObject so = new SerializedObject(pause);
        GameObject pausePanel = so.FindProperty("pauseMenu").objectReferenceValue as GameObject;
        GameObject settingsPanel = so.FindProperty("settingsMenu").objectReferenceValue as GameObject;
        GameObject controlsPanel = so.FindProperty("controlsMenu").objectReferenceValue as GameObject;

        if (pausePanel == null)
            return;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);

        Canvas canvas = pausePanel.GetComponentInParent<Canvas>(true).rootCanvas;
        Undo.RecordObject(canvas, "Pausa");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        Rename(pausePanel, "PausePanel");
        Rename(settingsPanel, "SettingsPanel");
        Rename(controlsPanel, "ControlsPanel");

        // Visual limpo: sem fundo escuro, so o texto sobre o jogo.
        foreach (GameObject panel in new[] { pausePanel, settingsPanel, controlsPanel })
        {
            if (panel != null)
                RemoveChild(panel.transform, "Dim");
        }

        Button[] buttons = pausePanel.GetComponentsInChildren<Button>(true);
        RectTransform terminal = BuildTerminal(pausePanel.transform, font, buttons.Length);

        float y = -78f;

        foreach (Button button in buttons)
        {
            RectTransform rect = (RectTransform)button.transform;
            Undo.SetTransformParent(rect, terminal, "Pausa");
            Undo.RecordObject(rect, "Pausa");
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(300f, 40f);
            rect.anchoredPosition = new Vector2(34f, y);
            y -= 46f;

            StyleRow(button, font);
        }

        MakeTransparent(terminal);
        BuildSettings(settingsPanel, font);
        BuildControls(controlsPanel, font);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("PauseMenuBuilder: menu de pausa refeito com " + buttons.Length + " opcoes.");
    }

    private static RectTransform BuildTerminal(Transform panel, TMP_FontAsset font, int rows)
    {
        Transform old = panel.Find("Terminal");
        RectTransform terminal;

        if (old != null)
        {
            terminal = (RectTransform)old;
        }
        else
        {
            GameObject go = new GameObject("Terminal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Pausa");
            go.layer = panel.gameObject.layer;
            go.transform.SetParent(panel, false);
            terminal = (RectTransform)go.transform;
        }

        terminal.anchorMin = terminal.anchorMax = new Vector2(0.5f, 0.5f);
        terminal.pivot = new Vector2(0.5f, 0.5f);
        terminal.anchoredPosition = Vector2.zero;
        terminal.sizeDelta = new Vector2(380f, 110f + rows * 46f);
        terminal.SetAsLastSibling();

        Image frame = terminal.GetComponent<Image>();
        frame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "ui_panel.png");
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 0.5f;
        frame.color = Color.white;
        frame.raycastTarget = true;

        Image screen = Child<Image>(terminal, "Screen");
        screen.color = ScreenBlack;
        screen.raycastTarget = false;
        Stretch(screen.rectTransform, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        screen.transform.SetAsFirstSibling();

        TextMeshProUGUI header = Child<TextMeshProUGUI>(terminal, "Header");
        if (font != null)
        {
            header.font = font;
            header.fontSharedMaterial = font.material;
        }
        header.text = "> PAUSADO_";
        header.fontSize = 24f;
        header.color = Phosphor;
        header.alignment = TextAlignmentOptions.TopLeft;
        header.raycastTarget = false;
        header.textWrappingMode = TextWrappingModes.NoWrap;
        Stretch(header.rectTransform, new Vector2(34f, 0f), new Vector2(-20f, -26f));

        return terminal;
    }

    private static void StyleRow(Button button, TMP_FontAsset font)
    {
        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            Undo.RecordObject(image, "Pausa");
            image.sprite = null;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
        }

        Undo.RecordObject(button, "Pausa");
        button.transition = Selectable.Transition.None;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            Undo.RecordObject(label, "Pausa");
            Undo.RecordObject(label.gameObject, "Pausa");
            label.gameObject.name = "Label";

            if (font != null)
            {
                label.font = font;
                label.fontSharedMaterial = font.material;
            }

            label.text = label.text.Trim();
            label.fontSize = 26f;
            label.enableAutoSizing = false;
            label.color = TextIdle;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
            label.rectTransform.localScale = Vector3.one;
        }

        ButtonHoverEffect hover = button.GetComponent<ButtonHoverEffect>();

        if (hover == null)
            hover = Undo.AddComponent<ButtonHoverEffect>(button.gameObject);

        SerializedObject so = new SerializedObject(hover);
        so.FindProperty("hoverScale").floatValue = 1f;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("normalTextColor").colorValue = TextIdle;
        so.FindProperty("hoverTextColor").colorValue = Phosphor;
        so.FindProperty("hoverPrefix").stringValue = "> ";
        so.ApplyModifiedProperties();
    }

    private static void MakeTransparent(RectTransform terminal)
    {
        Image frame = terminal.GetComponent<Image>();

        if (frame != null)
        {
            Undo.RecordObject(frame, "Pausa");
            frame.sprite = null;
            frame.color = new Color(0f, 0f, 0f, 0f);
            frame.raycastTarget = false;
        }

        RemoveChild(terminal, "Screen");
        RemoveChild(terminal, "Scanlines");
        RemoveChild(terminal, "Vignette");
    }

    private static void BuildSettings(GameObject panel, TMP_FontAsset font)
    {
        if (panel == null)
            return;

        Header(panel.transform, font, "> CONFIGURAÇÕES", new Vector2(-110f, 150f));
        Hint(panel.transform, font, new Vector2(-110f, -150f));

        foreach (Slider slider in panel.GetComponentsInChildren<Slider>(true))
        {
            string name = slider.name switch
            {
                "Sensitivity" => "SENSIBILIDADE",
                "Volume" => "VOLUME",
                "Brightness" => "BRILHO",
                _ => slider.name.ToUpperInvariant(),
            };

            TextMeshProUGUI label = Child<TextMeshProUGUI>(slider.transform, "Name");
            SetFont(label, font);
            label.text = name;
            label.fontSize = 24f;
            label.color = TextIdle;
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 40f);
            rect.anchoredPosition = new Vector2(-24f, 0f);

            // Trilho fino, preenchimento em fosforo e pegador quadrado.
            Tint(slider.transform.Find("Background"), new Color(0.8f, 0.8f, 0.8f, 0.25f));
            Tint(slider.fillRect, Phosphor);
            Tint(slider.handleRect, Color.white);

            foreach (TMP_Text value in slider.GetComponentsInChildren<TMP_Text>(true))
            {
                if (value == label)
                    continue;

                Undo.RecordObject(value, "Configuracoes");
                SetFont(value, font);
                value.color = Phosphor;
            }
        }
    }

    private static void BuildControls(GameObject panel, TMP_FontAsset font)
    {
        if (panel == null)
            return;

        Header(panel.transform, font, "> CONTROLES", new Vector2(0f, 180f));
        Hint(panel.transform, font, new Vector2(0f, -185f));

        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name != "Text (TMP)" && text.name != "ControlsText")
                continue;

            Undo.RecordObject(text, "Controles");
            Undo.RecordObject(text.gameObject, "Controles");
            text.gameObject.name = "ControlsText";
            SetFont(text, font);
            text.color = TextIdle;
            text.fontSize = 18f;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Left;
            text.lineSpacing = 28f;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(380f, 330f);
            textRect.anchoredPosition = new Vector2(0f, -2f);

            // Teclas conferidas no codigo (PlayerMovement, PhotoCamera, AlbumController...).
            text.text =
                "<color=#FFFFFF>WASD</color><pos=130>Mover\n" +
                "<color=#FFFFFF>SHIFT</color><pos=130>Correr\n" +
                "<color=#FFFFFF>ESPAÇO</color><pos=130>Pular\n" +
                "<color=#FFFFFF>MOUSE</color><pos=130>Olhar\n" +
                "<color=#FFFFFF>C</color><pos=130>Abrir/fechar visor\n" +
                "<color=#FFFFFF>CLIQUE</color><pos=130>Tirar foto\n" +
                "<color=#FFFFFF>SCROLL</color><pos=130>Zoom do visor\n" +
                "<color=#FFFFFF>E</color><pos=130>Interagir/coletar\n" +
                "<color=#FFFFFF>F</color><pos=130>Álbum de fotos\n" +
                "<color=#FFFFFF>TAB</color><pos=130>Missões\n" +
                "<color=#FFFFFF>ESC</color><pos=130>Pausar";
        }
    }

    private static void Header(Transform panel, TMP_FontAsset font, string text, Vector2 position)
    {
        TextMeshProUGUI header = Child<TextMeshProUGUI>(panel, "Header");
        SetFont(header, font);
        header.text = text;
        header.fontSize = 30f;
        header.color = Phosphor;
        header.alignment = TextAlignmentOptions.Center;
        header.raycastTarget = false;
        header.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rect = header.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 40f);
        rect.anchoredPosition = position;
    }

    private static void Hint(Transform panel, TMP_FontAsset font, Vector2 position)
    {
        TextMeshProUGUI hint = Child<TextMeshProUGUI>(panel, "BackHint");
        SetFont(hint, font);
        hint.text = "[ESC] Voltar";
        hint.fontSize = 20f;
        hint.color = new Color(0.72f, 0.72f, 0.72f, 0.6f);
        hint.alignment = TextAlignmentOptions.Center;
        hint.raycastTarget = false;
        hint.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rect = hint.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 30f);
        rect.anchoredPosition = position;
    }

    private static void Tint(Transform target, Color color)
    {
        Image image = target != null ? target.GetComponent<Image>() : null;

        if (image == null)
            return;

        Undo.RecordObject(image, "Configuracoes");
        image.color = color;
    }

    private static void SetFont(TMP_Text text, TMP_FontAsset font)
    {
        if (font == null)
            return;

        text.font = font;
        text.fontSharedMaterial = font.material;
    }

    private static void RemoveChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);

        if (child != null)
            Undo.DestroyObjectImmediate(child.gameObject);
    }

    private static void AddDim(Transform panel)
    {
        Image dim = Child<Image>(panel, "Dim");
        dim.color = Dim;
        dim.raycastTarget = true;

        // Painel pode ser pequeno; o fundo escuro cobre a tela toda.
        RectTransform rect = dim.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(6000f, 6000f);
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsFirstSibling();
    }

    private static void AddOverlay(RectTransform terminal)
    {
        Image scan = Child<Image>(terminal, "Scanlines");
        scan.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "crt_scanlines.png");
        scan.type = Image.Type.Tiled;
        scan.pixelsPerUnitMultiplier = 1f;
        scan.color = Color.white;
        scan.raycastTarget = false;
        Stretch(scan.rectTransform, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        scan.transform.SetAsLastSibling();

        Image vignette = Child<Image>(terminal, "Vignette");
        vignette.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "crt_vignette.png");
        vignette.color = Color.white;
        vignette.raycastTarget = false;
        Stretch(vignette.rectTransform, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        vignette.transform.SetAsLastSibling();
    }

    private static T Child<T>(Transform parent, string name) where T : Graphic
    {
        Transform existing = parent.Find(name);
        GameObject go;

        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
            Undo.RegisterCreatedObjectUndo(go, "Pausa");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
        }

        T graphic = go.GetComponent<T>();
        Undo.RecordObject(graphic, "Pausa");
        return graphic;
    }

    private static void Rename(GameObject go, string name)
    {
        if (go == null || go.name == name)
            return;

        Undo.RecordObject(go, "Renomear");
        go.name = name;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
