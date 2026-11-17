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

    protected T Require<T>() where T : Object
    {
        T found = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        if (found == null)
            Failures.Add("nao encontrou " + typeof(T).Name);

        return found;
    }
}
