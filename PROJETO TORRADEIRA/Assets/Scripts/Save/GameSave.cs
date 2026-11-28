using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Save de progresso da cena Principal (para o "CONTINUAR" do menu).
// Salva sozinho: quando algo muda (foto, peca, local, bateria, album), a cada 45 s, ao voltar
// ao menu pela pausa e ao fechar o jogo. Nunca salva durante a intro nem morto.
// Guarda: posicao do jogador, missoes, pecas e baterias coletadas, bateria da camera,
// fotos do album (PNG) e quantas vezes o cientista ja te salvou.
// Ao terminar o jogo (entregar as pecas) o save e apagado.
public class GameSave : MonoBehaviour
{
    [Serializable]
    private class Data
    {
        public int version = 1;
        public float px, py, pz, ry;
        public int photos, pieces, battery = -1, photoCount, deaths;
        public List<string> locations = new List<string>();
        public List<string> collectedPieces = new List<string>();
        public List<string> collectedBatteries = new List<string>();
        public List<string> album = new List<string>(); // PNG em base64
    }

    private const string GameScene = "Principal";
    private static string FilePath => Path.Combine(Application.persistentDataPath, "progresso.json");

    public static bool Exists => File.Exists(FilePath);
    public static bool Continuing { get; private set; }

    private static GameSave instance;
    private float nextPeriodic;
    private string lastSignature;
    private bool ready;

    // ---------- API ----------
    public static void ContinueGame()
    {
        Continuing = true;
        LoadingScreen.Load(GameScene);
    }

    public static void Delete()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); }
        catch (Exception e) { Debug.LogWarning("GameSave: " + e.Message); }
    }

    public static void SaveNow()
    {
        if (instance != null) instance.Save(false);
    }

    // ---------- Instalacao ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => TryInstall(s);
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void TryInstall(Scene s)
    {
        if (s.name != GameScene)
        {
            Continuing = false;
            return;
        }
        if (FindFirstObjectByType<GameSave>() != null) return;
        new GameObject("GameSave").AddComponent<GameSave>();
    }

    private IEnumerator Start()
    {
        instance = this;
        // Espera os sistemas da cena iniciarem (eles zeram os contadores no Start).
        yield return null;
        yield return null;
        if (Continuing)
        {
            Load();
            Continuing = false;
        }
        lastSignature = Signature();
        nextPeriodic = Time.unscaledTime + 45f;
        ready = true;
    }

    private void Update()
    {
        if (!ready || !CanSave()) return;
        string sig = Signature();
        if (sig != lastSignature || Time.unscaledTime >= nextPeriodic)
            Save(sig != lastSignature);
    }

    private void OnApplicationQuit()
    {
        if (ready) Save(false);
    }

    private static bool CanSave()
    {
        if (DefeatController.IsDefeated || PauseMenu.IsPaused) return false;
        MissionSystem m = FindFirstObjectByType<MissionSystem>();
        if (m != null && m.Delivered) return false;
        TimeTravelIntro intro = FindFirstObjectByType<TimeTravelIntro>();
        if (intro != null && intro.enabled && Time.timeScale <= 0f) return false;
        return FindFirstObjectByType<PlayerMovement>() != null;
    }

    // Resumo do progresso: se mudou, salva na hora.
    private static string Signature()
    {
        MissionSystem m = FindFirstObjectByType<MissionSystem>();
        AlbumController a = FindFirstObjectByType<AlbumController>();
        CameraBattery b = FindFirstObjectByType<CameraBattery>();
        int pieces = 0, batteries = 0;
        foreach (TemporalObjectPickup p in FindObjectsByType<TemporalObjectPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (p.IsCollected) pieces++;
        foreach (BatteryPickup p in FindObjectsByType<BatteryPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!p.gameObject.activeSelf) batteries++;
        return (m != null ? m.GetPhotoCount() + "/" + m.GetPieceCount() + "/" + m.GetLocationCount() : "-") + "|" +
               pieces + "|" + batteries + "|" + (a != null ? a.Count : 0) + "|" + (b != null ? b.Current : 0);
    }

    // ---------- Salvar ----------
    private void Save(bool showIndicator)
    {
        if (!CanSave()) return;
        try
        {
            Data d = new Data();
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            Vector3 p = player.transform.position;
            d.px = p.x; d.py = p.y; d.pz = p.z;
            d.ry = player.transform.eulerAngles.y;

            MissionSystem m = FindFirstObjectByType<MissionSystem>();
            if (m != null)
            {
                d.photos = m.GetPhotoCount();
                d.pieces = m.GetPieceCount();
                d.locations.AddRange(m.PhotographedLocationIds);
            }
            foreach (TemporalObjectPickup t in FindObjectsByType<TemporalObjectPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.IsCollected) d.collectedPieces.Add(Id(t.transform));
            foreach (BatteryPickup t in FindObjectsByType<BatteryPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!t.gameObject.activeSelf) d.collectedBatteries.Add(Id(t.transform));

            CameraBattery b = FindFirstObjectByType<CameraBattery>();
            if (b != null) d.battery = b.Current;
            CameraPhotoData pd = FindFirstObjectByType<CameraPhotoData>();
            if (pd != null) d.photoCount = pd.PhotoCount;
            AlbumController a = FindFirstObjectByType<AlbumController>();
            if (a != null)
                foreach (AlbumPhoto ph in a.Photos)
                    if (ph != null && ph.Texture != null && ph.Texture.isReadable)
                        d.album.Add(Convert.ToBase64String(ph.Texture.EncodeToPNG()));
            d.deaths = PlayerRespawn.Deaths;

            File.WriteAllText(FilePath, JsonUtility.ToJson(d));
            lastSignature = Signature();
            nextPeriodic = Time.unscaledTime + 45f;
            if (showIndicator) SaveIndicator.Show();
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameSave: nao foi possivel salvar: " + e.Message);
        }
    }

    // ---------- Carregar ----------
    private void Load()
    {
        Data d;
        try { d = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)); }
        catch (Exception e) { Debug.LogWarning("GameSave: save invalido: " + e.Message); return; }
        if (d == null) return;

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(d.px, d.py, d.pz), Quaternion.Euler(0f, d.ry, 0f));
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
        }

        HashSet<string> pieces = new HashSet<string>(d.collectedPieces);
        foreach (TemporalObjectPickup t in FindObjectsByType<TemporalObjectPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (pieces.Contains(Id(t.transform))) t.RestoreCollected();
        HashSet<string> batteries = new HashSet<string>(d.collectedBatteries);
        foreach (BatteryPickup t in FindObjectsByType<BatteryPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (batteries.Contains(Id(t.transform))) t.gameObject.SetActive(false);

        MissionSystem m = FindFirstObjectByType<MissionSystem>();
        if (m != null) m.RestoreProgress(d.photos, d.pieces, d.locations);

        CameraBattery b = FindFirstObjectByType<CameraBattery>();
        if (b != null && d.battery >= 0) b.SetCurrent(d.battery);
        CameraPhotoData pd = FindFirstObjectByType<CameraPhotoData>();
        if (pd != null) pd.SetPhotoCount(d.photoCount);

        AlbumController a = FindFirstObjectByType<AlbumController>();
        if (a != null)
            foreach (string png in d.album)
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(Convert.FromBase64String(png), false))
                {
                    tex.filterMode = FilterMode.Point;
                    a.AddPhoto(tex);
                }
            }

        PlayerRespawn.RestoreDeaths(d.deaths);

        // O inimigo comeca longe e com um tempinho antes de cacar.
        TemporalEraserController enemy = FindFirstObjectByType<TemporalEraserController>();
        if (enemy != null) enemy.ResetToStart(6f);
    }

    // Identificador estavel: nome + posicao inicial (as pecas e baterias nao se movem).
    private static string Id(Transform t)
    {
        Vector3 p = t.position;
        return t.name + "@" + Mathf.RoundToInt(p.x * 10f) + "," + Mathf.RoundToInt(p.y * 10f) + "," + Mathf.RoundToInt(p.z * 10f);
    }
}

// "● SALVANDO" pequeno no canto, estilo OSD de fita.
public class SaveIndicator : MonoBehaviour
{
    private CanvasGroup group;
    private float t;

    public static void Show()
    {
        SaveIndicator old = FindFirstObjectByType<SaveIndicator>();
        if (old != null) { old.t = 0f; return; }

        GameObject go = new GameObject("IndicadorSave", typeof(RectTransform));
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 31100;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        SaveIndicator s = go.AddComponent<SaveIndicator>();
        s.group = go.AddComponent<CanvasGroup>();
        s.group.alpha = 0f;

        TextMeshProUGUI text = new GameObject("Texto", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(go.transform, false);
        LoadingScreenArt art = Resources.Load<LoadingScreenArt>("LoadingScreenArt");
        if (art != null && art.font != null) text.font = art.font;
        text.text = "<color=#D23A32>●</color> SALVANDO";
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.outlineWidth = 0.12f;
        text.outlineColor = new Color32(0, 0, 0, 230);
        text.raycastTarget = false;
        RectTransform r = text.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
        r.anchoredPosition = new Vector2(-40f, 36f);
        r.sizeDelta = new Vector2(400f, 40f);
    }

    private void Update()
    {
        t += Time.unscaledDeltaTime;
        bool blink = !FlashSettings.Enabled || Mathf.Repeat(t * 2f, 1f) < 0.7f;
        group.alpha = t < 1.6f ? (blink ? 1f : 0.35f) : 1f - (t - 1.6f) / 0.4f;
        if (t > 2f) Destroy(gameObject);
    }
}
