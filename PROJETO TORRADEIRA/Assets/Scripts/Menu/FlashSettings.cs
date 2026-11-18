using System;
using UnityEngine;

// Opcao de acessibilidade (fotossensibilidade): liga/desliga todo clarao de tela.
public static class FlashSettings
{
    private const string Key = "FlashEnabled";

    public static event Action<bool> Changed;

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(Key, 1) == 1;
        set
        {
            if (Enabled == value)
                return;

            PlayerPrefs.SetInt(Key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke(value);
        }
    }

    public static void Toggle()
    {
        Enabled = !Enabled;
    }

    public static string Label => Enabled ? "LIGADO" : "DESLIGADO";

    // Shaders que pulsam (ex.: Retro/TemporalRim) leem este global: 1 = flashes ligados.
    private static readonly int ShaderFlag = Shader.PropertyToID("_RetroFlashes");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SyncShaders()
    {
        Shader.SetGlobalFloat(ShaderFlag, Enabled ? 1f : 0f);
        Changed -= OnChanged;
        Changed += OnChanged;
    }

    private static void OnChanged(bool enabled)
    {
        Shader.SetGlobalFloat(ShaderFlag, enabled ? 1f : 0f);
    }
}
