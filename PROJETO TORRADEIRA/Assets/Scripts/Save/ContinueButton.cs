using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Botao "CONTINUAR" no menu principal (so aparece se existir save).
// E uma copia do botao PLAY, colocada acima dele; os botoes descem um pouco para caber.
// Continuar vai direto para o laboratorio: sem cutscene e sem a intro da viagem,
// so a tela de carregamento e o fade de entrada.
public static class ContinueButton
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => TryInstall(s);
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void TryInstall(Scene s)
    {
        if (s.name != "Menu" || !GameSave.Exists) return;

        Button play = null;
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            TMP_Text label = b.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.text.Trim().ToUpperInvariant() is "PLAY" or "JUGAR") { play = b; break; }
        }
        if (play == null || play.transform.parent.Find("Continuar") != null) return;

        Transform parent = play.transform.parent;
        GameObject copy = Object.Instantiate(play.gameObject, parent);
        copy.name = "Continuar";
        Button cont = copy.GetComponent<Button>();
        cont.onClick = new Button.ButtonClickedEvent(); // tira o LoadScene do PLAY copiado
        cont.onClick.AddListener(GameSave.ContinueGame);
        foreach (TMP_Text t in copy.GetComponentsInChildren<TMP_Text>(true))
            t.text = "CONTINUAR";

        // Reorganiza: CONTINUAR, PLAY, CREDITS, QUIT com espacamento menor para caber na tela.
        RectTransform first = (RectTransform)play.transform;
        float top = first.anchoredPosition.y;
        System.Collections.Generic.List<RectTransform> rows = new System.Collections.Generic.List<RectTransform> { (RectTransform)copy.transform };
        foreach (Transform child in parent)
        {
            if (child == copy.transform || child.GetComponent<Button>() == null) continue;
            rows.Add((RectTransform)child);
        }
        rows.Sort((a, b) => a == (RectTransform)copy.transform ? -1 : b == (RectTransform)copy.transform ? 1 : b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
        float step = 82f; // antes 104: quatro botoes precisam caber na tela da TV
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].sizeDelta = new Vector2(rows[i].sizeDelta.x, Mathf.Min(rows[i].sizeDelta.y, 74f));
            rows[i].anchoredPosition = new Vector2(rows[i].anchoredPosition.x, top - i * step);
        }

        // Navegacao por teclado comeca no CONTINUAR.
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(copy);
    }
}
