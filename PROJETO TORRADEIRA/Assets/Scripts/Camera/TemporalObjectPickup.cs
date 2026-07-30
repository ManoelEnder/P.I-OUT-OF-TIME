using UnityEngine;
using UnityEngine.InputSystem;

public class TemporalObjectPickup : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;

    private Camera playerCamera;

    private bool isRevealed;
    private bool isCollected;

    private void Start()
    {
        playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError(
                $"{name}: Player Camera não encontrada.",
                this
            );
        }
    }

    private void Update()
    {
        if (isCollected)
        {
            HideInteractionText();
            return;
        }

        if (!isRevealed)
        {
            HideInteractionText();
            return;
        }

        CheckInteraction();

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            CanInteract())
        {
            Collect();
        }
    }

    public void SetRevealed(bool revealed)
    {
        isRevealed = revealed;

        Debug.Log(
            $"{name} | SetRevealed chamado: {revealed}",
            this
        );

        if (!isRevealed)
        {
            HideInteractionText();
        }
    }

    private void CheckInteraction()
    {
        if (CanInteract())
        {
            ShowInteractionText();
        }
        else
        {
            HideInteractionText();
        }
    }

    private bool CanInteract()
    {
        if (playerCamera == null)
        {
            return false;
        }

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }

    private void ShowInteractionText()
    {
        if (InteractionUI.Instance == null)
        {
            return;
        }

        InteractionUI.Instance.Show("E para coletar");
    }

    private void HideInteractionText()
    {
        if (InteractionUI.Instance == null)
        {
            return;
        }

        InteractionUI.Instance.Hide();
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

        HideInteractionText();

        gameObject.SetActive(false);
    }
}