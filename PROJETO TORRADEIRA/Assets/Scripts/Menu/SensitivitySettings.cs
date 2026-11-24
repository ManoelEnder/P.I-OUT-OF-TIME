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

        // Aplica na hora no jogador (antes o valor era salvo e nunca lido).
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
            player.RefreshSensitivity();
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