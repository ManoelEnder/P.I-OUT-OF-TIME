using UnityEngine;

public class TemporalObjectPickup : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private bool canBeCollected = true;

    private bool isRevealed;
    private bool isCollected;

    private void Start()
    {
        isRevealed = false;
        isCollected = false;
    }

    public void SetRevealed(bool revealed)
    {
        if (isCollected)
        {
            return;
        }

        isRevealed = revealed;

        Debug.Log(
            $"{name} | SetRevealed chamado: {revealed}",
            this
        );
    }

    public bool CanInteract()
    {
        if (isCollected)
        {
            return false;
        }

        if (!isRevealed)
        {
            return false;
        }

        if (!canBeCollected)
        {
            return false;
        }

        return true;
    }

    public string GetInteractionMessage()
    {
        return "E para coletar";
    }

    public void Interact()
    {
        if (!CanInteract())
        {
            return;
        }

        Collect();
    }

    private void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;

        Debug.Log(
            $"{name} | Objeto coletado!",
            this
        );

        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.Hide();
        }

        gameObject.SetActive(false);
    }
}