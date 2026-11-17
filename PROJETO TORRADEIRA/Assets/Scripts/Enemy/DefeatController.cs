using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class DefeatController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject defeatPanel;

    [Header("Player")]
    [SerializeField] private GameObject player;

    [Header("Respawn")]
    [SerializeField] private Transform currentSpawnPoint;

    [Header("Scene")]
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Events")]
    [SerializeField] private UnityEvent onRetry;

    private bool isDefeated;

    // Outros scripts (movimento, camera) param enquanto a tela de derrota esta aberta.
    public static bool IsDefeated { get; private set; }

    private void Start()
    {
        IsDefeated = false;

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (isDefeated)
        {
            IsDefeated = false;
            Time.timeScale = 1f;
        }
    }

    public void TriggerDefeat()
    {
        if (isDefeated || PauseMenu.IsPaused)
        {
            return;
        }

        isDefeated = true;
        IsDefeated = true;

        ShowDefeatPanel();
    }

    private void ShowDefeatPanel()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
        }

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (currentSpawnPoint != null && player != null)
        {
            CharacterController characterController =
                player.GetComponent<CharacterController>();

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            player.transform.position =
                currentSpawnPoint.position;

            player.transform.rotation =
                currentSpawnPoint.rotation;

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        isDefeated = false;
        IsDefeated = false;

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }

        onRetry?.Invoke();
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        isDefeated = false;
        IsDefeated = false;

        SceneManager.LoadScene(menuSceneName);
    }

    public void SetSpawnPoint(Transform spawnPoint)
    {
        currentSpawnPoint = spawnPoint;
    }
}
