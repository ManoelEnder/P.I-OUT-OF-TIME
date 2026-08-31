using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MissionSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI missionText;

    [SerializeField] private int photosRequired = 10;
    [SerializeField] private int piecesRequired = 5;

    [SerializeField] private string finalSceneName = "Final";

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeTime = 1.5f;

    private int photos;
    private int pieces;

    private bool changingScene;

    private void Start()
    {
        photos = 0;
        pieces = 0;

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
                ? "[X] Coletar pecas [" + piecesRequired + "/" + piecesRequired + "]"
                : "Coletar pecas [" + pieces + "/" + piecesRequired + "]";

        string discoverMission =
            pieces >= 1
                ? "[X] Descobrir uma peca"
                : "[ ] Descobrir uma peca";

        missionText.text =
            photoMission + "\n" +
            pieceMission + "\n" +
            discoverMission;
    }

    private void CheckMissions()
    {
        if (changingScene)
            return;

        if (photos >= photosRequired &&
            pieces >= piecesRequired)
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

            float alpha =
                Mathf.Clamp01(time / fadeTime);

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

    public bool HasDiscoveredPiece()
    {
        return pieces >= 1;
    }
}