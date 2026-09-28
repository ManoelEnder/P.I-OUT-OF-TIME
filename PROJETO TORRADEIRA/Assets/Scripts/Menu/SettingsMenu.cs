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

        UpdateInterface();
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
    }

    public void ResetSettings()
    {
        audioSettings.ResetVolume();
        brightnessSettings.ResetBrightness();
        sensitivitySettings.ResetSensitivity();

        UpdateInterface();
    }
}