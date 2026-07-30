using UnityEngine;

public class BatteryPickup : MonoBehaviour, IInteractable
{
    [Header("Battery")]
    [SerializeField] private int amount = 1;

    private PhotoCamera playerCamera;

    private void Start()
    {
        playerCamera = FindObjectOfType<PhotoCamera>();

        if (playerCamera == null)
        {
            Debug.LogError(
                $"{name} | PhotoCamera não encontrada na cena!",
                this
            );
        }
    }

    public bool CanInteract()
    {
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<PhotoCamera>();
        }

        if (playerCamera == null)
        {
            return false;
        }

        return true;
    }

    public string GetInteractionMessage()
    {
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<PhotoCamera>();
        }

        if (playerCamera == null)
        {
            return "";
        }

        if (playerCamera.IsBatteryFull())
        {
            return "Bateria cheia";
        }

        return "E para coletar";
    }

    public void Interact()
    {
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<PhotoCamera>();
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                $"{name} | Não foi possível encontrar a PhotoCamera!",
                this
            );

            return;
        }

        if (playerCamera.IsBatteryFull())
        {
            Debug.Log(
                $"{name} | Bateria cheia. Não é possível coletar.",
                this
            );

            return;
        }

        playerCamera.AddBattery(amount);

        Debug.Log(
            $"{name} | Bateria coletada! Quantidade adicionada: {amount}",
            this
        );

        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.Hide();
        }

        Destroy(gameObject);
    }
}