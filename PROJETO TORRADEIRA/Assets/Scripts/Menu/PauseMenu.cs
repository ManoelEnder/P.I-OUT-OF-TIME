using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject settingsMenu;

    [Header("Player")]
    [SerializeField] private GameObject armsObject;

    [Header("Other UI")]
    [SerializeField] private GraphicRaycaster otherCanvasRaycaster;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public static bool IsPaused { get; private set; }

    private void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            HandleEscape();
    }

    private void HandleEscape()
    {
        if (!IsPaused)
        {
            PauseGame();
            return;
        }

        if (settingsMenu.activeSelf)
        {
            CloseSettings();
            return;
        }

        ResumeGame();
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        pauseMenu.SetActive(true);
        settingsMenu.SetActive(false);

        if (armsObject != null)
            armsObject.SetActive(false);

        if (otherCanvasRaycaster != null)
            otherCanvasRaycaster.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        pauseMenu.SetActive(false);
        settingsMenu.SetActive(false);

        if (armsObject != null)
            armsObject.SetActive(true);

        if (otherCanvasRaycaster != null)
            otherCanvasRaycaster.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void GoToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (otherCanvasRaycaster != null)
            otherCanvasRaycaster.enabled = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }
}