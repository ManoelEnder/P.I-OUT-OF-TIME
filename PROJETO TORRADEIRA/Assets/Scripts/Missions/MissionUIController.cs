using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

// HUD de missoes discreto:
// - o painel fixo some; as missoes ficam no album (aba MISSOES);
// - TAB abre o album direto na pagina de missoes;
// - quando uma missao avanca, aparece um aviso curto no canto, que some sozinho;
// - dica "[TAB] MISSOES" pequena e meio apagada (some com o visor aberto).
public class MissionUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform missionPanel;
    [SerializeField] private TextMeshProUGUI tabHint;

    [Header("Aviso de progresso")]
    [SerializeField] private float toastSeconds = 3.5f;
    [SerializeField, Range(0f, 1f)] private float hintAlpha = 0.55f;

    [Header("Camera")]
    [SerializeField] private PhotoCamera photoCamera;

    private MissionSystem missions;
    private AlbumController album;
    private TextMeshProUGUI toast;
    private CanvasGroup toastGroup;
    private Coroutine toastRoutine;
    private string lastText;
    private bool hiddenByCamera;

    private void Start()
    {
        missions = FindFirstObjectByType<MissionSystem>();
        album = FindFirstObjectByType<AlbumController>();

        // O texto continua sendo escrito no painel (a missao usa ele), so nao aparece.
        if (missionPanel != null)
        {
            CanvasGroup group = missionPanel.GetComponent<CanvasGroup>();
            if (group == null)
                group = missionPanel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        if (tabHint != null)
        {
            tabHint.text = "[TAB] MISSÕES";
            tabHint.alpha = hintAlpha;
            tabHint.gameObject.SetActive(true);
        }

        CreateToast();
        lastText = missions != null ? missions.CurrentText : string.Empty;
    }

    private void Update()
    {
        UpdateCameraVisibility();
        WatchProgress();

        if (hiddenByCamera || Keyboard.current == null || PauseMenu.IsPaused)
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame && album != null && !album.IsAlbumOpen)
            album.OpenMissions();
    }

    public void ShowMissions()
    {
        if (album != null && !album.IsAlbumOpen)
            album.OpenMissions();
    }

    public void HideMissions()
    {
        if (album != null && album.IsAlbumOpen)
            album.CloseAlbum();
    }

    // ---------- Aviso de progresso ----------

    private void WatchProgress()
    {
        if (missions == null)
            return;

        string current = missions.CurrentText;
        if (current == lastText)
            return;

        string changed = FirstChangedLine(lastText, current);
        lastText = current;

        if (!string.IsNullOrEmpty(changed))
            ShowToast(changed);
    }

    private static string FirstChangedLine(string before, string after)
    {
        string[] a = (before ?? string.Empty).Split('\n');
        string[] b = (after ?? string.Empty).Split('\n');

        for (int i = 0; i < b.Length; i++)
        {
            if (i >= a.Length || a[i] != b[i])
                return b[i].Trim();
        }

        return string.Empty;
    }

    private void CreateToast()
    {
        Transform parent = tabHint != null ? tabHint.transform.parent : (missionPanel != null ? missionPanel.parent : null);
        if (parent == null)
            return;

        GameObject go = new GameObject("MissionToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        toast = go.GetComponent<TextMeshProUGUI>();
        toastGroup = go.GetComponent<CanvasGroup>();
        if (tabHint != null)
        {
            toast.font = tabHint.font;
            toast.fontSize = tabHint.fontSize;
        }
        toast.color = Color.white;
        toast.alignment = TextAlignmentOptions.TopLeft;
        toast.raycastTarget = false;
        toast.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rect = (RectTransform)go.transform;
        if (tabHint != null)
        {
            RectTransform hint = tabHint.rectTransform;
            rect.anchorMin = hint.anchorMin;
            rect.anchorMax = hint.anchorMax;
            rect.pivot = hint.pivot;
            rect.sizeDelta = new Vector2(800f, hint.sizeDelta.y);
            rect.anchoredPosition = hint.anchoredPosition + new Vector2(0f, -hint.rect.height - 6f);
        }

        toastGroup.alpha = 0f;
    }

    private void ShowToast(string line)
    {
        if (toast == null)
            return;

        if (toastRoutine != null)
            StopCoroutine(toastRoutine);

        toast.text = "> " + line.Replace("[X]", "").Replace("[ ]", "").Trim();
        toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        RectTransform rect = toast.rectTransform;
        Vector2 home = rect.anchoredPosition;

        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.25f;
            toastGroup.alpha = k * (hiddenByCamera ? 0f : 1f);
            rect.anchoredPosition = home + new Vector2(-24f * (1f - k), 0f);
            yield return null;
        }

        rect.anchoredPosition = home;
        toastGroup.alpha = hiddenByCamera ? 0f : 1f;

        yield return new WaitForSecondsRealtime(toastSeconds);

        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            toastGroup.alpha = 1f - t / 0.6f;
            yield return null;
        }

        toastGroup.alpha = 0f;
        toastRoutine = null;
    }

    // ---------- Visor ----------

    private void UpdateCameraVisibility()
    {
        if (photoCamera == null)
            photoCamera = FindFirstObjectByType<PhotoCamera>();

        bool cameraOpen = photoCamera != null && (photoCamera.IsCameraMode || photoCamera.IsTransitioning);
        bool albumOpen = album != null && album.IsAlbumOpen;
        bool hide = cameraOpen || albumOpen;

        if (hide == hiddenByCamera)
            return;

        hiddenByCamera = hide;

        if (tabHint != null)
            tabHint.gameObject.SetActive(!hide);

        if (hide && toastGroup != null)
            toastGroup.alpha = 0f;
    }
}
