using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    private IInteractable currentInteractable;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (InteractionUI.Instance == null)
        {
            Debug.LogError(
                "InteractionDetector | Nenhum InteractionUI encontrado na cena!",
                this
            );
        }
    }

    private void Update()
    {
        DetectInteraction();

        if (currentInteractable != null &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            currentInteractable.Interact();
        }
    }

    private void DetectInteraction()
    {
        IInteractable detectedInteractable = null;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                HideInteraction();
                return;
            }
        }

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore))
        {
            detectedInteractable =
                hit.collider.GetComponentInParent<IInteractable>();
        }

        if (detectedInteractable == null)
        {
            currentInteractable = null;
            HideInteraction();
            return;
        }

        if (!detectedInteractable.CanInteract())
        {
            currentInteractable = null;
            HideInteraction();
            return;
        }

        currentInteractable = detectedInteractable;

        ShowInteraction(
            currentInteractable.GetInteractionMessage()
        );
    }

    private void ShowInteraction(string message)
    {
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.Show(message);
        }
    }

    private void HideInteraction()
    {
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.Hide();
        }
    }
}