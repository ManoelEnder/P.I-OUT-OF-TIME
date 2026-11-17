using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Base dos testes automaticos. Cenarios de play mode dirigem o jogo com QAInput;
// checagens de editor (NeedsPlayMode = false) rodam direto, sem Play.
public abstract class QAScenario
{
    public abstract string Name { get; }

    public virtual bool NeedsPlayMode => true;

    public readonly List<string> Failures = new List<string>();

    public abstract IEnumerator Run();

    protected void Check(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log("QA ok " + message);
            return;
        }

        Failures.Add(message);
        Debug.Log("QA CHECK FAIL " + message);
    }

    protected IEnumerator LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        yield return WaitForScene(sceneName, 30f);
    }

    protected IEnumerator WaitForScene(string sceneName, float timeout)
    {
        float end = Time.realtimeSinceStartup + timeout;

        while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < end)
            yield return null;

        Check(SceneManager.GetActiveScene().name == sceneName, "cena " + sceneName + " carregou");
        yield return null;
    }

    protected IEnumerator WaitSeconds(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;

        while (Time.realtimeSinceStartup < end)
            yield return null;
    }

    // Sem janela em foco (batch) o Unity nao segura o Cursor.lockState; nesse caso os testes
    // conferem a outra metade do pedido do jogo (cursor escondido).
    private static bool? lockSupported;

    protected static bool CursorLockSupported
    {
        get
        {
            if (lockSupported == null)
            {
                CursorLockMode previous = Cursor.lockState;
                Cursor.lockState = CursorLockMode.Locked;
                lockSupported = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = previous;
                Debug.Log("QA ambiente segura Cursor.lockState: " + lockSupported);
            }

            return lockSupported.Value;
        }
    }

    // Cursor "do jogo": travado, ou escondido quando o ambiente nao consegue travar.
    protected static bool CursorCaptured =>
        CursorLockSupported ? Cursor.lockState == CursorLockMode.Locked : !Cursor.visible;

    // A intro de viagem no tempo da fase congela o jogo; espera o jogo ficar livre por 2 s.
    protected IEnumerator WaitGameplay()
    {
        float end = Time.realtimeSinceStartup + 60f;
        float free = 0f;

        while (free < 2f && Time.realtimeSinceStartup < end)
        {
            free = Time.timeScale > 0f ? free + Time.unscaledDeltaTime : 0f;
            yield return null;
        }

        Check(free >= 2f, "jogo liberado depois da intro");
    }

    protected T Require<T>() where T : Object
    {
        T found = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        if (found == null)
            Failures.Add("nao encontrou " + typeof(T).Name);

        return found;
    }
}
