using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject controlsMenu;

    [Header("Player")]
    [SerializeField] private GameObject armsObject;
    [SerializeField] private GameObject crosshair;

    [Header("Other UI")]
    [SerializeField] private GraphicRaycaster otherCanvasRaycaster;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public static bool IsPaused { get; private set; }

    private void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (settingsMenu != null)
            settingsMenu.SetActive(false);

        if (controlsMenu != null)
            controlsMenu.SetActive(false);

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

        if (settingsMenu != null && settingsMenu.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (controlsMenu != null && controlsMenu.activeSelf)
        {
            CloseControls();
            return;
        }

        ResumeGame();
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (pauseMenu != null)
            pauseMenu.SetActive(true);

        if (settingsMenu != null)
            settingsMenu.SetActive(false);

        if (controlsMenu != null)
            controlsMenu.SetActive(false);

        if (armsObject != null)
            armsObject.SetActive(false);

        if (crosshair != null)
            crosshair.SetActive(false);

        if (otherCanvasRaycaster != null)
            otherCanvasRaycaster.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (settingsMenu != null)
            settingsMenu.SetActive(false);

        if (controlsMenu != null)
            controlsMenu.SetActive(false);

        if (armsObject != null)
            armsObject.SetActive(true);

        if (crosshair != null)
            crosshair.SetActive(true);

        if (otherCanvasRaycaster != null)
            otherCanvasRaycaster.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (settingsMenu != null)
            settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsMenu != null)
            settingsMenu.SetActive(false);

        if (pauseMenu != null)
            pauseMenu.SetActive(true);
    }

    public void OpenControls()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (controlsMenu != null)
            controlsMenu.SetActive(true);
    }

    public void CloseControls()
    {
        if (controlsMenu != null)
            controlsMenu.SetActive(false);

        if (pauseMenu != null)
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