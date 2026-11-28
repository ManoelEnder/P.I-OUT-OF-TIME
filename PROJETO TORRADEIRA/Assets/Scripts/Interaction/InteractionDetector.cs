using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    private IInteractable currentInteractable;
    private Vector3 lastHitPoint;

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
        if (PauseMenu.IsPaused || Time.timeScale <= 0f)
            return;

        DetectInteraction();

        // Interagir = botao esquerdo do mouse (com o visor fechado).
        if (currentInteractable != null && InteractInput.Pressed)
        {
            currentInteractable.Interact();
        }
    }

    private void DetectInteraction()
    {
        IInteractable detectedInteractable = null;

        // Visor aberto: clique e para foto, nada de interagir.
        if (RadioMessageUI.CameraOpen)
        {
            currentInteractable = null;
            HideInteraction();
            return;
        }

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
            lastHitPoint = hit.point;
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

        // Em vez da legenda embaixo: iconezinho de mouse no proprio objeto.
        if (currentInteractable is Component c)
            InteractionIcon.Show(c, lastHitPoint);
        if (InteractionUI.Instance != null)
            InteractionUI.Instance.Hide();
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
        InteractionIcon.Hide();
    }
}