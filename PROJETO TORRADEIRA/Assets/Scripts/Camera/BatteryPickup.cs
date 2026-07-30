using UnityEngine;
using UnityEngine.InputSystem;

public class BatteryPickup : MonoBehaviour
{
    [Header("Battery")]
    [SerializeField] private int amount = 3;
    [SerializeField] private float distance = 3f;

    private PhotoCamera playerCamera;
    private Camera mainCamera;

    private bool canInteract = false;

    private void Start()
    {
        mainCamera = Camera.main;

        playerCamera = FindObjectOfType<PhotoCamera>();

        if (playerCamera == null)
        {
            Debug.LogError(
                $"{name} | PhotoCamera não foi encontrada na cena.",
                this
            );
        }
    }

    private void Update()
    {
        CheckLook();

        if (canInteract &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            CollectBattery();
        }
    }

    private void CheckLook()
    {
        canInteract = false;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
            {
                HideInteraction();
                return;
            }
        }

        Ray ray = mainCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            BatteryPickup battery =
                hit.collider.GetComponentInParent<BatteryPickup>();

            if (battery == this)
            {
                canInteract = true;

                if (playerCamera == null)
                {
                    playerCamera = FindObjectOfType<PhotoCamera>();
                }

                if (playerCamera != null)
                {
                    if (playerCamera.IsBatteryFull())
                    {
                        InteractionUI.Instance?.Show("Bateria cheia");
                    }
                    else
                    {
                        InteractionUI.Instance?.Show("E para coletar bateria");
                    }
                }

                return;
            }
        }

        HideInteraction();
    }

    private void CollectBattery()
    {
        if (!canInteract)
        {
            return;
        }

        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<PhotoCamera>();
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                $"{name} | PhotoCamera não encontrada.",
                this
            );

            return;
        }

        if (playerCamera.IsBatteryFull())
        {
            InteractionUI.Instance?.Show("Bateria cheia");

            return;
        }

        playerCamera.AddBattery(amount);

        Debug.Log(
            $"{name} | Bateria coletada! Quantidade adicionada: {amount}",
            this
        );

        canInteract = false;

        InteractionUI.Instance?.Hide();

        Destroy(gameObject);
    }

    private void HideInteraction()
    {
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.Hide();
        }
    }
}