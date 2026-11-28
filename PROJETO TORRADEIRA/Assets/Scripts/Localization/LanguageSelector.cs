using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Seletor de idioma no canto do menu principal: "IDIOMA  < Portugues (Brasil) >".
// Clique troca para o proximo idioma (o nome de cada um aparece na propria lingua).
// No jogo, a mesma opcao fica em Pausa > Configuracoes.
public class LanguageSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly string[] Scenes = { "Menu" };
    private TextMeshProUGUI value;
    private bool hover;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => TryInstall(s);
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void TryInstall(Scene s)
    {
        if (System.Array.IndexOf(Scenes, s.name) < 0 || FindFirstObjectByType<LanguageSelector>() != null) return;

        GameObject root = new GameObject("SeletorIdioma", typeof(RectTransform));
        Canvas c = root.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 500;
        CanvasScaler cs = root.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        GameObject btn = new GameObject("Idioma", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(root.transform, false);
        RectTransform r = (RectTransform)btn.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-40f, -36f);
        r.sizeDelta = new Vector2(560f, 56f);
        btn.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        LanguageSelector sel = btn.AddComponent<LanguageSelector>();
        LoadingScreenArt art = Resources.Load<LoadingScreenArt>("LoadingScreenArt");
        sel.value = new GameObject("Valor", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        sel.value.transform.SetParent(btn.transform, false);
        RectTransform vr = sel.value.rectTransform;
        vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = vr.offsetMax = Vector2.zero;
        if (art != null && art.font != null) sel.value.font = art.font;
        sel.value.fontSize = 30f;
        sel.value.alignment = TextAlignmentOptions.MidlineRight;
        sel.value.textWrappingMode = TextWrappingModes.NoWrap;
        sel.value.raycastTarget = false;

        btn.GetComponent<Button>().onClick.AddListener(Localization.Next);
        Localization.Changed += sel.Refresh;
        sel.Refresh();
    }

    private void Refresh()
    {
        if (value == null) return;
        // "IDIOMA" e traduzido pelo TextAutoLocalizer; o nome do idioma fica sempre na propria lingua.
        string label = Localization.T("IDIOMA");
        string arrows = hover ? "<color=#D23A32>" : "<color=#8C8C88>";
        value.text = label + "   " + arrows + "◄</color> " + Localization.NativeName(Localization.Current) + " " + arrows + "►</color>";
    }

    public void OnPointerEnter(PointerEventData e) { hover = true; Refresh(); }
    public void OnPointerExit(PointerEventData e) { hover = false; Refresh(); }

    private void OnDestroy() => Localization.Changed -= Refresh;
}
