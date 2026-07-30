public interface IInteractable
{
    string GetInteractionMessage();

    bool CanInteract();

    void Interact();
}