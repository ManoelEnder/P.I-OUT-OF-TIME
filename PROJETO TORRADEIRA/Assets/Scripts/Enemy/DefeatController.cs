using UnityEngine;

public class DefeatController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject defeatPanel;

    [Header("Player")]
    [SerializeField] private GameObject player;

    [Header("Respawn")]
    [SerializeField] private Transform currentSpawnPoint;

    private bool isDefeated;

    private void Start()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }
    }

    public void TriggerDefeat()
    {
        if (isDefeated)
        {
            return;
        }

        isDefeated = true;

        ShowDefeatPanel();
    }

    private void ShowDefeatPanel()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        if (currentSpawnPoint != null)
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

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }
    }

    public void SetSpawnPoint(Transform spawnPoint)
    {
        currentSpawnPoint = spawnPoint;
    }
}