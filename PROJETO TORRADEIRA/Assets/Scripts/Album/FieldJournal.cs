using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Diario de campo: o album ganha abas no topo (ALBUM | MISSOES). TAB desliza entre as paginas,
// como um inventario por paginas. A pagina de missoes mostra cada objetivo com marcador e barra.
// Montado em tempo de execucao dentro do painel do album (nao precisa mexer na cena).
public class FieldJournal : MonoBehaviour
{
    private const float SlideSeconds = 0.32f;
    private const float PageOffset = 1400f;

    private static readonly Color Ink = new Color(0.93f, 0.93f, 0.9f, 1f);
    private static readonly Color Dim = new Color(0.55f, 0.55f, 0.53f, 1f);
    private static readonly Color Done = new Color(0.62f, 0.62f, 0.6f, 1f);
    private static readonly Color Accent = new Color(0.85f, 0.12f, 0.12f, 1f);

    private RectTransform panel;
    private RectTransform albumPage;
    private CanvasGroup albumGroup;
    private RectTransform missionsPage;
    private CanvasGroup missionsGroup;
    private RectTransform tabMarker;
    private TextMeshProUGUI albumTab;
    private TextMeshProUGUI missionsTab;
    private MissionSystem missions;
    private TMP_FontAsset font;

    private Row[] rows;
    private TextMeshProUGUI albumCounter;
    private int page;
    private int openedFrame = -1;
    private Coroutine slide;
    private bool built;

    public bool OnMissionsPage => page == 1;

    private class Row
    {
        public TextMeshProUGUI Check;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Count;
        public Image[] Segments;
    }

    public void Init(GameObject albumPanel, Transform photoContainer, TMP_FontAsset textFont)
    {
        panel = albumPanel != null ? albumPanel.transform as RectTransform : null;
        albumPage = photoContainer as RectTransform;
        font = textFont;
        missions = FindFirstObjectByType<MissionSystem>();
    }

    // ---------- Abrir / fechar / trocar ----------

    public void OnOpened(int startPage)
    {
        Build();
        if (!built)
            return;

        openedFrame = Time.frameCount;
        SetPage(startPage, false);
        Refresh();
    }

    public void TogglePage()
    {
        // Ignora o mesmo TAB que acabou de abrir o diario.
        if (!built || Time.frameCount == openedFrame)
            return;

        SetPage(1 - page, true);
    }

    private void SetPage(int target, bool animate)
    {
        page = Mathf.Clamp(target, 0, 1);
        Refresh();

        if (slide != null)
            StopCoroutine(slide);

        if (animate && isActiveAndEnabled)
            slide = StartCoroutine(Slide());
        else
            ApplySlide(page);
    }

    private IEnumerator Slide()
    {
        float from = albumPage != null ? -albumPage.anchoredPosition.x / PageOffset : page;
        float time = 0f;

        while (time < SlideSeconds)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / SlideSeconds);
            float e = 1f - Mathf.Pow(1f - t, 3f);
            ApplySlide(Mathf.Lerp(from, page, e));
            yield return null;
        }

        ApplySlide(page);
        slide = null;
    }

    // 0 = album na tela, 1 = missoes na tela.
    private void ApplySlide(float k)
    {
        if (albumPage != null)
            albumPage.anchoredPosition = new Vector2(-k * PageOffset, albumPage.anchoredPosition.y);
        if (albumGroup != null)
            albumGroup.alpha = 1f - k;

        if (missionsPage != null)
            missionsPage.anchoredPosition = new Vector2((1f - k) * PageOffset, 0f);
        if (missionsGroup != null)
            missionsGroup.alpha = k;

        if (tabMarker != null)
            tabMarker.anchoredPosition = new Vector2(Mathf.Lerp(-110f, 110f, k), -60f);

        if (albumTab != null)
            albumTab.color = Color.Lerp(Ink, Dim, k);
        if (missionsTab != null)
            missionsTab.color = Color.Lerp(Dim, Ink, k);
    }

    // ---------- Montagem ----------

    private void Build()
    {
        if (built || panel == null)
            return;

        if (albumPage != null)
        {
            albumGroup = albumPage.GetComponent<CanvasGroup>();
            if (albumGroup == null)
                albumGroup = albumPage.gameObject.AddComponent<CanvasGroup>();
        }

        // Abas no topo.
        RectTransform header = NewRect("JournalTabs", panel);
        header.anchorMin = header.anchorMax = new Vector2(0.5f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(520f, 70f);
        header.anchoredPosition = new Vector2(0f, -24f);

        albumTab = Label(header, "AlbumTab", "ÁLBUM", 30f, Ink, TextAlignmentOptions.Center);
        Place(albumTab.rectTransform, new Vector2(-110f, 0f), new Vector2(200f, 44f));
        missionsTab = Label(header, "MissionsTab", "MISSÕES", 30f, Dim, TextAlignmentOptions.Center);
        Place(missionsTab.rectTransform, new Vector2(110f, 0f), new Vector2(200f, 44f));

        TextMeshProUGUI tabKey = Label(header, "TabKey", "[TAB]", 18f, Dim, TextAlignmentOptions.Center);
        Place(tabKey.rectTransform, new Vector2(0f, 0f), new Vector2(60f, 30f));

        Image marker = NewImage("Marker", header, Accent);
        tabMarker = marker.rectTransform;
        tabMarker.anchorMin = tabMarker.anchorMax = new Vector2(0.5f, 1f);
        tabMarker.pivot = new Vector2(0.5f, 0.5f);
        tabMarker.sizeDelta = new Vector2(150f, 3f);

        // Pagina de missoes.
        missionsPage = NewRect("MissionsPage", panel);
        missionsPage.anchorMin = missionsPage.anchorMax = new Vector2(0.5f, 0.5f);
        missionsPage.pivot = new Vector2(0.5f, 0.5f);
        missionsPage.sizeDelta = new Vector2(860f, 520f);
        missionsGroup = missionsPage.gameObject.AddComponent<CanvasGroup>();
        missionsGroup.blocksRaycasts = false;

        TextMeshProUGUI title = Label(missionsPage, "Title", "OBJETIVOS  //  LAB 1975", 22f, Dim, TextAlignmentOptions.Left);
        Place(title.rectTransform, new Vector2(0f, 230f), new Vector2(860f, 34f));

        rows = new Row[5];
        for (int i = 0; i < rows.Length; i++)
            rows[i] = BuildRow(missionsPage, i, 168f - i * 88f);

        TextMeshProUGUI footer = Label(panel, "JournalHint", "[SETAS] escolher foto    [X] apagar foto    [TAB] missões    [ESC] fechar", 18f, Dim, TextAlignmentOptions.Center);
        albumCounter = Label(panel, "AlbumCounter", "", 22f, Ink, TextAlignmentOptions.Right);
        RectTransform ac = albumCounter.rectTransform;
        ac.anchorMin = ac.anchorMax = new Vector2(1f, 1f);
        ac.pivot = new Vector2(1f, 1f);
        ac.sizeDelta = new Vector2(420f, 30f);
        ac.anchoredPosition = new Vector2(-30f, -22f);
        RectTransform f = footer.rectTransform;
        f.anchorMin = f.anchorMax = new Vector2(0.5f, 0f);
        f.pivot = new Vector2(0.5f, 0f);
        f.sizeDelta = new Vector2(700f, 30f);
        f.anchoredPosition = new Vector2(0f, 18f);

        built = true;
    }

    private Row BuildRow(RectTransform parent, int index, float y)
    {
        RectTransform rowRect = NewRect("Mission_" + (index + 1), parent);
        rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(860f, 90f);
        rowRect.anchoredPosition = new Vector2(0f, y);

        var row = new Row
        {
            Check = Label(rowRect, "Check", "[ ]", 30f, Ink, TextAlignmentOptions.Left),
            Title = Label(rowRect, "Title", "", 30f, Ink, TextAlignmentOptions.Left),
            Count = Label(rowRect, "Count", "", 26f, Dim, TextAlignmentOptions.Right),
        };

        Place(row.Check.rectTransform, new Vector2(-380f, 16f), new Vector2(80f, 40f));
        Place(row.Title.rectTransform, new Vector2(30f, 16f), new Vector2(740f, 40f));
        Place(row.Count.rectTransform, new Vector2(330f, 16f), new Vector2(180f, 40f));

        // Barra segmentada de progresso.
        const int segments = 10;
        row.Segments = new Image[segments];
        for (int s = 0; s < segments; s++)
        {
            Image seg = NewImage("Seg", rowRect, Dim);
            RectTransform r = seg.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.sizeDelta = new Vector2(70f, 8f);
            r.anchoredPosition = new Vector2(-340f + s * 76f, -22f);
            row.Segments[s] = seg;
        }

        return row;
    }

    // ---------- Dados ----------

    // Contador de fotos do album (atualiza tambem depois de apagar uma foto).
    private void LateUpdate()
    {
        if (built && panel != null && panel.gameObject.activeInHierarchy)
            RefreshCounter();
    }

    private void Refresh()
    {
        RefreshCounter();
        RefreshMissions();
    }

    private void RefreshCounter()
    {
        AlbumController album = GetComponent<AlbumController>();
        if (albumCounter != null && album != null)
        {
            albumCounter.text = album.IsFull
                ? "<color=#D23A32>ÁLBUM CHEIO " + album.Count + "/" + album.Max + "</color>  apague com [X]"
                : "FOTOS " + album.Count + "/" + album.Max;
            albumCounter.alpha = page == 0 ? 1f : 0f;
        }
    }

    private void RefreshMissions()
    {

        if (!built || rows == null)
            return;

        if (missions == null)
            missions = FindFirstObjectByType<MissionSystem>();

        if (missions == null)
            return;

        SetRow(rows[0], "Tirar fotos", missions.GetPhotoCount(), missions.PhotosRequired);
        SetRow(rows[1], "Coletar peças da câmera", missions.GetPieceCount(), missions.PiecesRequired);
        SetRow(rows[2], "Descobrir uma peça com o visor", missions.HasDiscoveredPiece() ? 1 : 0, 1);
        SetRow(rows[3], "Fotografar lugares marcados", missions.GetLocationCount(), missions.LocationsRequired);
        SetRow(rows[4], missions.ReadyToDeliver ? "FINAL: entregar as peças no ponto de montagem" : "Entregar as peças no ponto de montagem", missions.Delivered ? 1 : 0, 1);
    }

    private static void SetRow(Row row, string title, int value, int required)
    {
        required = Mathf.Max(1, required);
        value = Mathf.Clamp(value, 0, required);
        bool complete = value >= required;

        row.Check.text = complete ? "[X]" : "[ ]";
        row.Check.color = complete ? Accent : Ink;
        row.Title.text = complete ? "<s>" + title + "</s>" : title;
        row.Title.color = complete ? Done : Ink;
        row.Count.text = value + " / " + required;

        float filled = (float)value / required * row.Segments.Length;
        for (int i = 0; i < row.Segments.Length; i++)
            row.Segments[i].color = i < Mathf.RoundToInt(filled) ? (complete ? Done : Ink) : new Color(0.25f, 0.25f, 0.25f, 1f);
    }

    // ---------- Utilitarios de UI ----------

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = align;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
