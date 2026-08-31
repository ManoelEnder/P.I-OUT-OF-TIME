using UnityEngine;

public class TemporalObjectPickup : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private bool canBeCollected = true;

    [Header("Mission")]
    [SerializeField] private MissionSystem missionSystem;

    private bool isRevealed;
    private bool isCollected;

    private void Start()
    {
        isRevealed = false;
        isCollected = false;

        if (missionSystem == null)
            missionSystem = FindFirstObjectByType<MissionSystem>();
    }

    public void SetRevealed(bool revealed)
    {
        if (isCollected)
            return;

        isRevealed = revealed;
    }

    public bool CanInteract()
    {
        if (isCollected)
            return false;

        if (!isRevealed)
            return false;

        if (!canBeCollected)
            return false;

        return true;
    }

    public string GetInteractionMessage()
    {
        return "E para coletar";
    }

    public void Interact()
    {
        if (!CanInteract())
            return;

        Collect();
    }

    private void Collect()
    {
        if (isCollected)
            return;

        isCollected = true;

        if (missionSystem != null)
            missionSystem.AddPeca();

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.Hide();

        gameObject.SetActive(false);
    }
}