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
}
