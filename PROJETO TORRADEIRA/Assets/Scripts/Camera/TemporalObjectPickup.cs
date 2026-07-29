using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TemporalObjectPickup : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private TextMeshProUGUI interactText;

    private Camera playerCamera;
    private bool isRevealed;
    private bool isCollected;

    private void Start()
    {
        playerCamera = Camera.main;

        if (interactText != null)
        {
            interactText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isCollected || !isRevealed)
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
            return hit.collider.transform == transform ||
                   hit.collider.transform.IsChildOf(transform);
        }

        return false;
    }

    private void ShowInteractionText()
    {
        if (interactText == null)
        {
            return;
        }

        interactText.text = "E para coletar";
        interactText.gameObject.SetActive(true);
    }

    private void HideInteractionText()
    {
        if (interactText != null)
        {
            interactText.gameObject.SetActive(false);
        }
    }

    private void Collect()
    {
        isCollected = true;

        HideInteractionText();

        gameObject.SetActive(false);
    }
}