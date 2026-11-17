using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Aviso de fotossensibilidade: primeira coisa que aparece ao abrir o jogo.
// Monta a propria UI em tempo de execucao; [F] liga/desliga os flashes, ENTER continua.
public class PhotosensitivityWarning : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private static bool shownThisSession;
    private static int closedFrame = -10;

    private CanvasGroup group;
    private TextMeshProUGUI flashText;
    private TextMeshProUGUI continueText;
    private bool closing;

    public static bool IsShowing { get; private set; }

    // Bloqueia o "pressione qualquer tecla" enquanto o aviso esta na tela
    // e no frame em que ele fecha (o mesmo ENTER nao pode pular a tela inicial).
    public static bool BlocksInput => IsShowing || Time.frameCount <= closedFrame + 1;

    private static readonly Color White = Color.white;
    private static readonly Color Grey = new Color(0.72f, 0.72f, 0.72f, 1f);
    private static readonly Color Red = new Color(0.85f, 0.12f, 0.12f, 1f);

    private void Awake()
    {
        if (shownThisSession)
        {
            Destroy(gameObject);
            return;
        }

        shownThisSession = true;
        IsShowing = true;
        Build();
    }

    private void OnDestroy()
    {
        if (group != null)
            IsShowing = false;
    }

    private void Update()
    {
        if (closing)
            return;

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        if ((keyboard != null && keyboard.fKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame))
        {
            FlashSettings.Toggle();
            RefreshFlashText();
        }

        if ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame))
        {
            StartCoroutine(Close());
            return;
        }

        // Pulso lento (nao e flash): ~0,8 Hz entre 35% e 100%.
        if (continueText != null)
        {
            float a = Mathf.Lerp(0.35f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f));
            continueText.alpha = a;
        }
    }

    private IEnumerator Close()
    {
        closing = true;
        IsShowing = false;
        closedFrame = Time.frameCount;

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeOutDuration);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Build()
    {
        if (font == null)
            font = TMP_Settings.defaultFontAsset;

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();

        Image background = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        background.transform.SetParent(transform, false);
        background.color = Color.black;
        Stretch(background.rectTransform);

        Text("Title", "AVISO DE FOTOSSENSIBILIDADE", 34f, Red, new Vector2(0f, 190f), new Vector2(1000f, 50f));

        Text("Body",
            "Este jogo contém luzes piscando e flashes de câmera que podem\n" +
            "provocar crises em pessoas com epilepsia fotossensível.\n\n" +
            "Se sentir tontura, visão alterada, espasmos ou desorientação,\n" +
            "pare de jogar imediatamente e procure um médico.",
            22f, Grey, new Vector2(0f, 40f), new Vector2(1100f, 220f));

        flashText = Text("Flashes", "", 24f, White, new Vector2(0f, -110f), new Vector2(900f, 40f));
        RefreshFlashText();

        Text("Hint", "Você pode mudar isso depois em CONFIGURAÇÕES.", 18f,
            new Color(0.72f, 0.72f, 0.72f, 0.6f), new Vector2(0f, -150f), new Vector2(900f, 30f));

        continueText = Text("Continue", "ENTER para continuar", 26f, White, new Vector2(0f, -240f), new Vector2(900f, 40f));
    }

    private void RefreshFlashText()
    {
        if (flashText != null)
            flashText.text = "[F]  FLASHES:  " + FlashSettings.Label;
    }

    private TextMeshProUGUI Text(string name, string value, float size, Color color, Vector2 position, Vector2 box)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(transform, false);

        if (font != null)
            text.font = font;

        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = box;
        rect.anchoredPosition = position;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
