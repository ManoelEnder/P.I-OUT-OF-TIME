using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Traduz automaticamente TODO texto TextMeshPro do jogo (cenas e textos montados por script):
// - escuta o evento de "texto mudou" do TMP e anota o texto;
// - no LateUpdate (antes da interface ser desenhada) troca o portugues pelo idioma atual.
//   Trocar DENTRO do evento nao funciona: o evento acontece no meio da reconstrucao da
//   interface e o Unity ignora mudancas nessa hora (era por isso que alguns textos ficavam
//   em portugues);
// - guarda o original de cada texto para poder trocar de idioma de novo a qualquer momento.
public class TextAutoLocalizer : MonoBehaviour
{
    private class Entry { public string original; public string shown; }

    private static readonly Dictionary<TMP_Text, Entry> texts = new Dictionary<TMP_Text, Entry>();
    private static readonly HashSet<TMP_Text> pending = new HashSet<TMP_Text>();
    private static bool writing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        GameObject go = new GameObject("TextAutoLocalizer");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<TextAutoLocalizer>();

        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        SceneManager.sceneLoaded += (s, m) => ScanAll();
        Localization.Changed += RefreshAll;
    }

    private static void OnTextChanged(Object obj)
    {
        if (!writing && obj is TMP_Text t)
            pending.Add(t);
    }

    private void LateUpdate()
    {
        if (pending.Count == 0) return;
        TMP_Text[] list = new TMP_Text[pending.Count];
        pending.CopyTo(list);
        pending.Clear();
        foreach (TMP_Text t in list)
            if (t != null) Apply(t);
    }

    private static void Apply(TMP_Text t)
    {
        string current = t.text;
        if (!texts.TryGetValue(t, out Entry e))
        {
            e = new Entry { original = current, shown = current };
            texts[t] = e;
        }
        else if (current != e.shown)
        {
            // Um script escreveu um texto novo (em portugues): esse vira o original.
            e.original = current;
        }

        string translated = Localization.T(e.original);
        e.shown = translated;
        if (translated == current) return;
        writing = true;
        t.text = translated;
        writing = false;
    }

    private static void ScanAll()
    {
        List<TMP_Text> dead = new List<TMP_Text>();
        foreach (TMP_Text k in texts.Keys) if (k == null) dead.Add(k);
        foreach (TMP_Text d in dead) texts.Remove(d);

        foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Apply(t);
    }

    private static void RefreshAll()
    {
        foreach (var kv in texts)
        {
            if (kv.Key == null) continue;
            Entry e = kv.Value;
            e.shown = Localization.T(e.original);
            writing = true;
            kv.Key.text = e.shown;
            writing = false;
        }
        ScanAll();
    }
}
