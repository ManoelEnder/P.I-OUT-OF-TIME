using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private AudioSettings audioSettings;
    [SerializeField] private BrightnessSettings brightnessSettings;

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
    }

    private void ConfigureEvents()
    {
        volumeSlider.onValueChanged.RemoveListener(ChangeVolume);
        brightnessSlider.onValueChanged.RemoveListener(ChangeBrightness);

        volumeSlider.onValueChanged.AddListener(ChangeVolume);
        brightnessSlider.onValueChanged.AddListener(ChangeBrightness);
    }

    private void UpdateInterface()
    {
        if (audioSettings != null)
        {
            volumeSlider.SetValueWithoutNotify(
                audioSettings.Volume
            );
        }

        if (brightnessSettings != null)
        {
            brightnessSlider.SetValueWithoutNotify(
                brightnessSettings.Brightness
            );
        }
    }

    public void ChangeVolume(float value)
    {
        if (audioSettings != null)
        {
            audioSettings.SetVolume(value);
        }
    }

    public void ChangeBrightness(float value)
    {
        if (brightnessSettings != null)
        {
            brightnessSettings.SetBrightness(value);
        }
    }

    public void ResetSettings()
    {
        if (audioSettings != null)
        {
            audioSettings.ResetVolume();
        }

        if (brightnessSettings != null)
        {
            brightnessSettings.ResetBrightness();
        }

        UpdateInterface();
    }
}