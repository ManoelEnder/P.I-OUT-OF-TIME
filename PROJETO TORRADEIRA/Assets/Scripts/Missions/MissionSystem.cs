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

    private void Start()
    {
        photos = 0;
        pieces = 0;
        photographedLocations.Clear();

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

        Debug.Log("RegisterPhoto chamado. Lugares na cena: " + PhotoLocation.All.Count);

        foreach (PhotoLocation location in PhotoLocation.All)
        {
            if (photographedLocations.Contains(location.Id))
                continue;

            if (location.IsInFrame(cam))
            {
                photographedLocations.Add(location.Id);
                location.MarkPhotographed();
                Debug.Log("Lugar fotografado: " + location.DisplayName);
            }
        }

        UpdateMissionText();
        CheckMissions();
    }

    private void UpdateMissionText()
    {
        if (missionText == null)
            return;

        string photoMission =
            photos >= photosRequired
                ? "[X] Tirar fotos [" + photosRequired + "/" + photosRequired + "]"
                : "Tirar 10 fotos [" + photos + "/" + photosRequired + "]";

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

        missionText.text =
            photoMission + "\n" +
            pieceMission + "\n" +
            discoverMission + "\n" +
            locationMission;
    }

    private void CheckMissions()
    {
        if (changingScene)
            return;

        if (photos >= photosRequired &&
            pieces >= piecesRequired &&
            photographedLocations.Count >= locationsRequired)
        {
            StartCoroutine(FadeAndLoad());
        }
    }

    private IEnumerator FadeAndLoad()
    {
        changingScene = true;

        if (fadeImage == null)
        {
            SceneManager.LoadScene(finalSceneName);
            yield break;
        }

        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;

            float alpha = Mathf.Clamp01(time / fadeTime);

            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;

            yield return null;
        }

        SceneManager.LoadScene(finalSceneName);
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
}