using UnityEngine;

public class LostCameraPickup : MonoBehaviour, IInteractable
{
    private CameraOverheat overheatSystem;

    public void Initialize(CameraOverheat system)
    {
        overheatSystem = system;
    }

    public bool CanInteract()
    {
        return overheatSystem != null &&
               overheatSystem.IsCameraLost;
    }

    public string GetInteractionMessage()
    {
        return "E para recuperar a camera";
    }

    public void Interact()
    {
        if (!CanInteract())
            return;

        overheatSystem.RecoverCamera();
    }
}