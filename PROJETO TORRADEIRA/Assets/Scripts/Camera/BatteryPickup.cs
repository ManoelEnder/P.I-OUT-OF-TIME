using UnityEngine;

public class BatteryPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int amount = 1;

    private PhotoCamera playerCamera;

    private void Start()
    {
        FindPlayerCamera();
    }

    private void OnEnable()
    {
        FindPlayerCamera();
    }

    public bool CanInteract()
    {
        FindPlayerCamera();

        return playerCamera != null;
    }

    public string GetInteractionMessage()
    {
        FindPlayerCamera();

        if (playerCamera == null)
        {
            return string.Empty;
        }

        if (playerCamera.IsBatteryFull())
        {
            return "Bateria cheia";
        }

        return "E para coletar";
    }

    public void Interact()
    {
        FindPlayerCamera();

        if (playerCamera == null)
        {
            return;
        }

        if (playerCamera.IsBatteryFull())
        {
            return;
        }

        playerCamera.AddBattery(amount);

        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.Hide();
        }

        gameObject.SetActive(false);
    }

    private void FindPlayerCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<PhotoCamera>();
        }
    }
}
