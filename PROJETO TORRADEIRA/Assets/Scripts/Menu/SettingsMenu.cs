using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private AudioSettings audioSettings;
    [SerializeField] private BrightnessSettings brightnessSettings;
    [SerializeField] private SensitivitySettings sensitivitySettings;

    [Header("Sliders")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider sensitivitySlider;

    [Header("Percentage Texts")]
    [SerializeField] private TMP_Text volumePercentageText;
    [SerializeField] private TMP_Text brightnessPercentageText;
    [SerializeField] private TMP_Text sensitivityPercentageText;

    private void Start()
    {
        ConfigureSliders();
        ConfigureEvents();
        UpdateInterface();
    }

    private void ConfigureSliders()
    {
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;

        brightnessSlider.minValue = 0f;
        brightnessSlider.maxValue = 1f;
        brightnessSlider.wholeNumbers = false;

        sensitivitySlider.minValue = 0.1f;
        sensitivitySlider.maxValue = 2f;
        sensitivitySlider.wholeNumbers = false;
    }

    private void ConfigureEvents()
    {
        volumeSlider.onValueChanged.AddListener(ChangeVolume);

        brightnessSlider.onValueChanged.AddListener(ChangeBrightness);

        sensitivitySlider.onValueChanged.AddListener(ChangeSensitivity);
    }

    private void UpdateInterface()
    {
        volumeSlider.SetValueWithoutNotify(
            audioSettings.Volume
        );

        brightnessSlider.SetValueWithoutNotify(
            brightnessSettings.Brightness
        );

        sensitivitySlider.SetValueWithoutNotify(
            sensitivitySettings.Sensitivity
        );

        UpdatePercentageTexts();
    }

    private void UpdatePercentageTexts()
    {
        volumePercentageText.text =
            Mathf.RoundToInt(audioSettings.Volume * 100f) + "%";

        brightnessPercentageText.text =
            Mathf.RoundToInt(brightnessSettings.Brightness * 100f) + "%";

        sensitivityPercentageText.text =
            Mathf.RoundToInt(sensitivitySettings.Sensitivity * 100f) + "%";
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

    public void ResetSettings()
    {
        audioSettings.ResetVolume();
        brightnessSettings.ResetBrightness();
        sensitivitySettings.ResetSensitivity();

        UpdateInterface();
    }
}