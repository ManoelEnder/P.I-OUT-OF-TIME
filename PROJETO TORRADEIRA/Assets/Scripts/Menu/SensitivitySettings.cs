using UnityEngine;

public class SensitivitySettings : MonoBehaviour
{
    private const string SensitivityKey = "MouseSensitivity";
    private const float DefaultSensitivity = 1f;

    public float Sensitivity { get; private set; }

    private void Awake()
    {
        Load();
    }

    public void SetSensitivity(float value)
    {
        Sensitivity = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            SensitivityKey,
            Sensitivity
        );

        PlayerPrefs.Save();
    }

    public void Load()
    {
        Sensitivity = PlayerPrefs.GetFloat(
            SensitivityKey,
            DefaultSensitivity
        );

        Sensitivity = Mathf.Clamp01(Sensitivity);
    }

    public void ResetSensitivity()
    {
        SetSensitivity(DefaultSensitivity);
    }
}