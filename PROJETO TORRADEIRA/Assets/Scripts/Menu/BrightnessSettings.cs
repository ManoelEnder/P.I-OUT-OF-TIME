using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BrightnessSettings : MonoBehaviour
{
    [SerializeField] private Volume globalVolume;

    private const string BrightnessKey = "GameBrightness";
    private const float DefaultBrightness = 1f;

    private ColorAdjustments colorAdjustments;

    private float defaultExposure;
    private float minimumExposure;

    public float Brightness { get; private set; }

    private void Awake()
    {
        SetupVolume();
        Load();
    }

    private void SetupVolume()
    {
        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
        }

        if (globalVolume == null)
        {
            Debug.LogError("Global Volume não encontrado.");
            return;
        }

        VolumeProfile profile = globalVolume.profile;

        if (!profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        }

        colorAdjustments.active = true;
        colorAdjustments.postExposure.overrideState = true;

        defaultExposure = colorAdjustments.postExposure.value;
        minimumExposure = defaultExposure - 3f;
    }

    public void SetBrightness(float value)
    {
        Brightness = Mathf.Clamp01(value);

        ApplyBrightness();

        PlayerPrefs.SetFloat(BrightnessKey, Brightness);
        PlayerPrefs.Save();
    }

    private void ApplyBrightness()
    {
        if (colorAdjustments == null)
            return;

        float exposure = Mathf.Lerp(
            minimumExposure,
            defaultExposure,
            Brightness
        );

        colorAdjustments.postExposure.value = exposure;
    }

    public void Load()
    {
        Brightness = PlayerPrefs.GetFloat(
            BrightnessKey,
            DefaultBrightness
        );

        Brightness = Mathf.Clamp01(Brightness);

        ApplyBrightness();
    }

    public void ResetBrightness()
    {
        SetBrightness(DefaultBrightness);
    }
}