using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI interactText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Hide();
    }

    public void Show(string message)
    {
        if (interactText == null)
        {
            Debug.LogError("InteractionUI | TextMeshProUGUI não foi associado no Inspector.", this);
            return;
        }

        interactText.text = message;
        interactText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (interactText == null)
        {
            return;
        }

        interactText.gameObject.SetActive(false);
    }
}