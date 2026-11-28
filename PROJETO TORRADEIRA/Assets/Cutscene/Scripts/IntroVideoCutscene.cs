using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Toca a cutscene renderizada no Remotion (640x360, ampliada sem filtro) e inicia a jogabilidade.
public class IntroVideoCutscene : MonoBehaviour
{
    public VideoPlayer player;
    public RawImage screen;
    public Image fade;
    public TMP_Text skipHint;
    public string nextScene = "Principal";
    public float exitFade = 0.4f;
    public float skipConfirmWindow = 3f;

    private bool exiting;
    private float skipHintUntil = -1f;
    private CursorLockMode savedLock;
    private bool savedVisible;
    private RenderTexture renderTarget;

    private void Start()
    {
        Time.timeScale = 1f;
        savedLock = Cursor.lockState;
        savedVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (MusicManager.instance != null) MusicManager.instance.FadeOut(1.5f);
        if (skipHint != null) skipHint.alpha = 0f;

        if (player == null || screen == null)
        {
            Debug.LogError("IntroVideoCutscene: configure VideoPlayer e RawImage.", this);
            SceneManager.LoadScene(nextScene);
            return;
        }

        renderTarget = new RenderTexture(640, 360, 0) { filterMode = FilterMode.Point };
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = renderTarget;
        screen.texture = renderTarget;
        player.loopPointReached += _ => Finish();
        player.errorReceived += (_, msg) => { Debug.LogError("Cutscene: " + msg); Finish(); };
        player.Play();
    }

    private void Update()
    {
        if (exiting) return;

        bool pressed =
            (Keyboard.current != null &&
             (Keyboard.current.escapeKey.wasPressedThisFrame ||
              Keyboard.current.spaceKey.wasPressedThisFrame ||
              Keyboard.current.enterKey.wasPressedThisFrame)) ||
            (Gamepad.current != null &&
             (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame));

        if (pressed)
        {
            if (Time.unscaledTime < skipHintUntil) { Finish(); return; }
            skipHintUntil = Time.unscaledTime + skipConfirmWindow;
        }
        if (skipHint != null)
            skipHint.alpha = Time.unscaledTime < skipHintUntil ? Mathf.Clamp01((skipHintUntil - Time.unscaledTime) * 2f) : 0f;
    }

    // Mesma saída para o fim do vídeo e para o pulo: a jogabilidade começa uma única vez.
    public void Finish()
    {
        if (exiting) return;
        exiting = true;
        StartCoroutine(Exit());
    }

    private IEnumerator Exit()
    {
        float v0 = player.GetDirectAudioVolume(0);
        for (float e = 0f; e < exitFade; e += Time.unscaledDeltaTime)
        {
            float k = e / exitFade;
            if (fade != null) fade.color = new Color(0f, 0f, 0f, k);
            player.SetDirectAudioVolume(0, v0 * (1f - k));
            yield return null;
        }
        player.Stop();
        Cursor.lockState = savedLock;
        Cursor.visible = savedVisible;
        // Tela ja esta preta: entra direto na tela de carregamento (com dica) e a cena principal
        // aparece com fade, em vez do corte seco de antes.
        LoadingScreen.Load(nextScene, fadeIn: false);
    }

    private void OnDestroy()
    {
        if (renderTarget == null) return;
        if (player != null && player.targetTexture == renderTarget) player.targetTexture = null;
        renderTarget.Release();
        Destroy(renderTarget);
    }
}
