using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MissionSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI missionText;

    [SerializeField] private int photosRequired = 10;
    [SerializeField] private int piecesRequired = 5;
    [SerializeField] private int locationsRequired = 3;

    [SerializeField] private string finalSceneName = "Final";

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeTime = 1.5f;

    private int photos;
    private int pieces;

    private readonly HashSet<string> photographedLocations = new HashSet<string>();

    private bool changingScene;

    // Missao final: com tudo pronto, entregar as pecas no ponto de montagem (DeliveryPoint).
    public bool ReadyToDeliver { get; private set; }
    public bool Delivered { get; private set; }
    public event System.Action OnReadyToDeliver;

    // Quantas pecas o jogador levou para a sala de montagem.
    public static int DeliveredPieces = 4;

    private void Start()
    {
        photos = 0;
        pieces = 0;
        photographedLocations.Clear();

        ValidateRequirements();

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }

        UpdateMissionText();
    }

    public void AddFoto()
    {
        if (photos >= photosRequired)
            return;

        photos++;

        UpdateMissionText();
        CheckMissions();
    }

    public void AddPeca()
    {
        if (pieces >= piecesRequired)
            return;

        pieces++;

        UpdateMissionText();
        CheckMissions();
    }

    public void DescobriuPeca()
    {
        AddPeca();
    }

    public void RegisterPhoto(Camera cam)
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("MissionSystem: nenhuma câmera para RegisterPhoto");
            return;
        }

        foreach (PhotoLocation location in PhotoLocation.All)
        {
            if (photographedLocations.Contains(location.Id))
                continue;

            if (location.IsInFrame(cam))
            {
                photographedLocations.Add(location.Id);
                location.MarkPhotographed();
            }
        }

        UpdateMissionText();
        CheckMissions();
    }

    // A missao nao pode exigir mais pecas ou lugares do que existem na cena.
    private void ValidateRequirements()
    {
        int availablePieces = FindObjectsByType<TemporalObjectPickup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        ).Length;

        HashSet<string> locationIds = new HashSet<string>();

        foreach (PhotoLocation location in FindObjectsByType<PhotoLocation>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None))
        {
            locationIds.Add(location.Id);
        }

        if (availablePieces < piecesRequired)
        {
            Debug.LogWarning(
                "MissionSystem: a missao pede " + piecesRequired +
                " pecas, mas a cena tem " + availablePieces +
                ". Ajustando para " + availablePieces + ".",
                this
            );

            piecesRequired = availablePieces;
        }

        if (locationIds.Count < locationsRequired)
        {
            Debug.LogWarning(
                "MissionSystem: a missao pede " + locationsRequired +
                " lugares, mas a cena tem " + locationIds.Count +
                ". Ajustando para " + locationIds.Count + ".",
                this
            );

            locationsRequired = locationIds.Count;
        }

        // Todos os lugares marcados contam (os extras sao criados pelo PhotoSpots ao carregar a fase).
        if (locationIds.Count > locationsRequired)
            locationsRequired = locationIds.Count;
    }

    private void UpdateMissionText()
    {
        if (missionText == null)
            return;

        string photoMission =
            photos >= photosRequired
                ? "[X] Tirar fotos [" + photosRequired + "/" + photosRequired + "]"
                : "Tirar " + photosRequired + " fotos [" + photos + "/" + photosRequired + "]";

        string pieceMission =
            pieces >= piecesRequired
                ? "[X] Coletar peças [" + piecesRequired + "/" + piecesRequired + "]"
                : "Coletar peças [" + pieces + "/" + piecesRequired + "]";

        string discoverMission =
            pieces >= 1
                ? "[X] Descobrir uma peça"
                : "[ ] Descobrir uma peça";

        int found = photographedLocations.Count;

        string locationMission =
            found >= locationsRequired
                ? "[X] Fotografar lugares [" + locationsRequired + "/" + locationsRequired + "]"
                : "Fotografar lugares [" + found + "/" + locationsRequired + "]";

        string deliverMission =
            Delivered
                ? "[X] Entregar as peças no ponto de montagem"
                : ReadyToDeliver
                    ? "[ ] FINAL: entregue as peças no ponto de montagem (laboratório inicial)"
                    : "[ ] Entregar as peças no ponto de montagem";

        missionText.text =
            photoMission + "\n" +
            pieceMission + "\n" +
            discoverMission + "\n" +
            locationMission + "\n" +
            deliverMission;
    }

    private void CheckMissions()
    {
        if (changingScene)
            return;

        if (!ReadyToDeliver &&
            photos >= photosRequired &&
            pieces >= piecesRequired &&
            photographedLocations.Count >= locationsRequired)
        {
            ReadyToDeliver = true;
            UpdateMissionText();
            OnReadyToDeliver?.Invoke();
        }
    }

    // Chamado pelo ponto de montagem: encerra a fase e toca a cutscene final.
    public void Deliver()
    {
        if (!ReadyToDeliver || Delivered || changingScene)
            return;

        Delivered = true;
        DeliveredPieces = Mathf.Max(1, pieces);
        UpdateMissionText();
        // A garra mecanica da sala de montagem monta o prototipo e depois chama LoadFinalScene.
        if (AssemblyStation.Instance != null)
            AssemblyStation.Instance.Begin(DeliveredPieces, this);
        else
            StartCoroutine(FadeAndLoad());
    }

    public void LoadFinalScene(string sceneName)
    {
        if (changingScene)
            return;
        finalSceneName = sceneName;
        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        changingScene = true;

        if (fadeImage == null)
        {
            LoadingScreen.Load(finalSceneName);
            yield break;
        }

        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.unscaledDeltaTime;

            float alpha = Mathf.Clamp01(time / fadeTime);

            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;

            yield return null;
        }

        LoadingScreen.Load(finalSceneName);
    }

    public int GetPhotoCount()
    {
        return photos;
    }

    public int GetPieceCount()
    {
        return pieces;
    }

    public int GetLocationCount()
    {
        return photographedLocations.Count;
    }

    public bool HasDiscoveredPiece()
    {
        return pieces >= 1;
    }

    // Lidos pela pagina de missoes do album.
    public int PhotosRequired => photosRequired;
    public int PiecesRequired => piecesRequired;
    public int LocationsRequired => locationsRequired;

    // Texto atual das missoes (o aviso discreto do HUD compara com o anterior).
    public string CurrentText => missionText != null ? missionText.text : string.Empty;
}