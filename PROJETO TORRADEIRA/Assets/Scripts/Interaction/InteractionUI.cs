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
            return;
        }

        // Evita reconstruir a malha do TMP a cada quadro com o mesmo texto.
        if (interactText.text != message)
            interactText.text = message;

        if (!interactText.gameObject.activeSelf)
            interactText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (interactText == null)
        {
            return;
        }

        if (interactText.gameObject.activeSelf)
            interactText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}