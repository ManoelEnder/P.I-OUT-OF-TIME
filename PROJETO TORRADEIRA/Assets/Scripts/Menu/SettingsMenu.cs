using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private AudioSettings audioSettings;
    [SerializeField] private BrightnessSettings brightnessSettings;

    public void VolumeUp()
    {
        audioSettings.IncreaseVolume();
    }

    public void VolumeDown()
    {
        audioSettings.DecreaseVolume();
    }

    public void BrightnessUp()
    {
        brightnessSettings.IncreaseBrightness();
    }

    public void BrightnessDown()
    {
        brightnessSettings.DecreaseBrightness();
    }

    public void ResetSettings()
    {
        audioSettings.ResetVolume();
        brightnessSettings.ResetBrightness();
    }
}