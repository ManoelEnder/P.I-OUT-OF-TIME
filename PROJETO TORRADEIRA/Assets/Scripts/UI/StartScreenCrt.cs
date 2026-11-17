using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Tela inicial na sala do laboratorio: "PRESSIONE QUALQUER TECLA" na TV principal.
// Ao apertar, uma camera dispara, a tela escurece e o menu carrega.
public class StartScreenCrt : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private TMP_Text prompt;
    [SerializeField] private PhotoFlash shutter;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    private bool started;

    private IEnumerator Start()
    {
        if (fade != null)
        {
            fade.alpha = 1f;
            fade.blocksRaycasts = false;
        }

        // Espera o aviso de fotossensibilidade sair antes de clarear a sala.
        while (PhotosensitivityWarning.IsShowing)
            yield return null;

        yield return Fade(1f, 0f, fadeInDuration);
    }

    private void Update()
    {
        if (started || PhotosensitivityWarning.BlocksInput)
            return;

        if (AnyPressed())
            StartCoroutine(Go());
    }

    private static bool AnyPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        Gamepad pad = Gamepad.current;
        return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
    }

    private IEnumerator Go()
    {
        started = true;

        if (prompt != null)
        {
            MenuBlinker blinker = prompt.GetComponent<MenuBlinker>();
            if (blinker != null)
                blinker.enabled = false;

            prompt.alpha = 1f;
            prompt.text = "CARREGANDO FITA...";
        }

        if (shutter != null)
            shutter.Fire();

        yield return new WaitForSecondsRealtime(0.4f);
        yield return Fade(0f, 1f, fadeOutDuration);

        SceneManager.LoadScene(menuSceneName);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fade == null)
            yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        fade.alpha = to;
    }
}
