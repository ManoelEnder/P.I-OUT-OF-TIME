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
        Sensitivity = Mathf.Clamp(value, 0.1f, 2f);

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
    }

    public void ResetSensitivity()
    {
        SetSensitivity(DefaultSensitivity);
    }
}