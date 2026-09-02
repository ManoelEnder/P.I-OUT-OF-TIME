using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject settingsMenu;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Menu";

    private bool isPaused;

    private void Start()
    {
        pauseMenu.SetActive(false);

        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        pauseMenu.SetActive(true);

        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;

        pauseMenu.SetActive(false);

        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (settingsMenu == null)
        {
            return;
        }

        pauseMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsMenu == null)
        {
            return;
        }

        settingsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }
}