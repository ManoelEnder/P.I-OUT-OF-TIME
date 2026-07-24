using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // 🔹 Importante

public class StartScreenHandler : MonoBehaviour
{
    [Header("Cena do Menu")]
    public string menuSceneName; // Nome da cena de menu

    private bool hasStarted = false;

    private void Update()
    {
        if (hasStarted) return;

        // Detecta qualquer tecla do teclado
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            hasStarted = true;
            LoadMenu();
        }

        // Detecta clique do mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            hasStarted = true;
            LoadMenu();
        }
    }

    private void LoadMenu()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
