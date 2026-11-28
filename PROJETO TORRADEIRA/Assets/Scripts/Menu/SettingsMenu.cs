using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider sensitivitySlider;

    [SerializeField] private TMP_Text volumePercentageText;
    [SerializeField] private TMP_Text brightnessPercentageText;
    [SerializeField] private TMP_Text sensitivityPercentageText;

    [Header("Flashes (fotossensibilidade)")]
    [SerializeField] private Button flashToggleButton;
    [SerializeField] private TMP_Text flashValueText;

    private AudioSettings audioSettings;
    private BrightnessSettings brightnessSettings;
    private SensitivitySettings sensitivitySettings;

    private void Awake()
    {
        audioSettings = GetComponent<AudioSettings>();
        brightnessSettings = GetComponent<BrightnessSettings>();
        sensitivitySettings = GetComponent<SensitivitySettings>();
    }

    private void Start()
    {
        ConfigureSlider(volumeSlider);
        ConfigureSlider(brightnessSlider);
        ConfigureSlider(sensitivitySlider);

        volumeSlider.onValueChanged.RemoveAllListeners();
        brightnessSlider.onValueChanged.RemoveAllListeners();
        sensitivitySlider.onValueChanged.RemoveAllListeners();

        volumeSlider.onValueChanged.AddListener(ChangeVolume);
        brightnessSlider.onValueChanged.AddListener(ChangeBrightness);
        sensitivitySlider.onValueChanged.AddListener(ChangeSensitivity);

        if (flashToggleButton != null)
        {
            flashToggleButton.onClick.RemoveAllListeners();
            flashToggleButton.onClick.AddListener(ToggleFlashes);
            EnsureLanguageRow();
        }

        UpdateInterface();
    }

    // ---------- Idioma ----------
    // Linha nova copiada da linha "Flashes": IDIOMA  [ Portugues (Brasil) ] / [ English ] / [ Espanol ].
    private TMP_Text languageValueText;

    // Publico: o PauseMenuView chama antes de montar o layout (o painel comeca desligado).
    public void EnsureLanguageRow()
    {
        if (flashToggleButton == null) return;
        Transform row = flashToggleButton.transform;
        if (row.parent != null && row.parent.Find("Idioma") != null) return;
        GameObject copy = Instantiate(row.gameObject, row.parent);
        copy.name = "Idioma";
        RectTransform r = (RectTransform)copy.transform;
        RectTransform src = (RectTransform)row;
        r.anchoredPosition = src.anchoredPosition - new Vector2(0f, Mathf.Max(60f, src.rect.height + 20f));
        copy.transform.SetSiblingIndex(row.GetSiblingIndex() + 1);

        foreach (TMP_Text t in copy.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.name == "Name") t.text = "IDIOMA";
            else languageValueText = t;
        }
        Button b = copy.GetComponent<Button>();
        if (b != null)
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(Localization.Next);
        }
        Localization.Changed += UpdateLanguageText;
        UpdateLanguageText();

        // A dica "[ESC] Voltar" desce uma linha para nao ficar por cima.
        Transform hint = row.parent.Find("BackHint");
        if (hint is RectTransform h)
            h.anchoredPosition = new Vector2(h.anchoredPosition.x, Mathf.Min(h.anchoredPosition.y, r.anchoredPosition.y - 60f));
    }

    private void UpdateLanguageText()
    {
        if (languageValueText != null)
            languageValueText.text = "[ " + Localization.NativeName(Localization.Current) + " ]";
    }

    private void OnDestroy()
    {
        Localization.Changed -= UpdateLanguageText;
    }

    public void ToggleFlashes()
    {
        FlashSettings.Toggle();
        UpdateFlashText();
    }

    private void UpdateFlashText()
    {
        if (flashValueText != null)
            flashValueText.text = "[ " + FlashSettings.Label + " ]";
    }

    private void ConfigureSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = true;
    }

    public void ChangeVolume(float value)
    {
        audioSettings.SetVolume(value);

        volumePercentageText.text =
            Mathf.RoundToInt(value * 100f) + "%";
    }

    public void ChangeBrightness(float value)
    {
        brightnessSettings.SetBrightness(value);

        brightnessPercentageText.text =
            Mathf.RoundToInt(value * 100f) + "%";
    }

    public void ChangeSensitivity(float value)
    {
        sensitivitySettings.SetSensitivity(value);

        sensitivityPercentageText.text =
            Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateInterface()
    {
        volumeSlider.SetValueWithoutNotify(audioSettings.Volume);

        brightnessSlider.SetValueWithoutNotify(
            brightnessSettings.Brightness
        );

        sensitivitySlider.SetValueWithoutNotify(
            sensitivitySettings.Sensitivity
        );

        volumePercentageText.text =
            Mathf.RoundToInt(audioSettings.Volume * 100f) + "%";

        brightnessPercentageText.text =
            Mathf.RoundToInt(brightnessSettings.Brightness * 100f) + "%";

        sensitivityPercentageText.text =
            Mathf.RoundToInt(sensitivitySettings.Sensitivity * 100f) + "%";

        UpdateFlashText();
    }

    public void ResetSettings()
    {
        audioSettings.ResetVolume();
        brightnessSettings.ResetBrightness();
        sensitivitySettings.ResetSensitivity();
        FlashSettings.Enabled = true;

        UpdateInterface();
    }
}