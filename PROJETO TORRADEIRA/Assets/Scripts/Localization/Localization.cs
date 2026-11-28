using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

// Sistema de idiomas. O texto original do jogo e o portugues (Brasil); cada frase em
// Translations.cs tem a versao em ingles e espanhol. A troca vale na hora (TextAutoLocalizer
// re-traduz tudo que esta na tela) e fica salva em PlayerPrefs.
public enum Language { PortugueseBrazil = 0, English = 1, Spanish = 2 }

public static class Localization
{
    private const string Key = "Language";
    public static event Action Changed;

    // Nome de cada idioma na propria lingua (como aparece nas configuracoes).
    public static string NativeName(Language l) => l switch
    {
        Language.English => "English",
        Language.Spanish => "Español",
        _ => "Português (Brasil)",
    };

    public static Language Current
    {
        get => (Language)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)DefaultFromSystem()), 0, 2);
        set
        {
            if (Current == value && PlayerPrefs.HasKey(Key)) return;
            PlayerPrefs.SetInt(Key, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static void Next() => Current = (Language)(((int)Current + 1) % 3);
    public static void Previous() => Current = (Language)(((int)Current + 2) % 3);

    private static Language DefaultFromSystem() => Application.systemLanguage switch
    {
        SystemLanguage.English => Language.English,
        SystemLanguage.Spanish => Language.Spanish,
        _ => Language.PortugueseBrazil,
    };

    // ---------- Traducao ----------
    private static Dictionary<string, string[]> table;
    private static Regex phrases;

    private static void Build()
    {
        if (table != null) return;
        table = new Dictionary<string, string[]>();
        foreach (string[] e in Translations.Entries)
        {
            Add(e[0], e[1], e[2]);
            string up = e[0].ToUpperInvariant();
            if (up != e[0]) Add(up, e[1].ToUpperInvariant(), e[2].ToUpperInvariant());
        }
        // Frases dentro de textos maiores (ex.: "[ LIGADO ]", "DICA:  " + dica, "Coletar peças [2/5]").
        // Fronteira = nao ter letra/numero colado (o "_" de "PAUSADO_" nao atrapalha).
        string alt = string.Join("|", table.Keys.Where(k => k.Length >= 3 && System.Array.IndexOf(Translations.ExactOnly, k) < 0)
            .OrderByDescending(k => k.Length).Select(Regex.Escape));
        phrases = new Regex(@"(?<![\p{L}\p{N}])(?:" + alt + @")(?![\p{L}\p{N}])", RegexOptions.CultureInvariant);
    }

    private static void Add(string pt, string en, string es)
    {
        if (string.IsNullOrEmpty(pt) || table.ContainsKey(pt)) return;
        table[pt] = new[] { pt, en, es };
    }

    // Traduz um texto em portugues para o idioma atual (frase inteira ou pedacos conhecidos).
    public static string T(string pt)
    {
        if (string.IsNullOrEmpty(pt) || Current == Language.PortugueseBrazil) return pt;
        Build();
        int i = (int)Current;
        if (table.TryGetValue(pt, out string[] row)) return row[i];
        string trimmed = pt.Trim();
        if (trimmed != pt && table.TryGetValue(trimmed, out row)) return pt.Replace(trimmed, row[i]);
        return phrases.Replace(pt, m => table[m.Value][i]);
    }
}
