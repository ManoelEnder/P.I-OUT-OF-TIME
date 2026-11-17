using System.Collections;
using UnityEngine;

// Roda as corrotinas dos testes automaticos e do benchmark; sobrevive a troca de cena.
public class QACoroutineHost : MonoBehaviour
{
    private static QACoroutineHost instance;

    public bool Running { get; private set; }

    public static QACoroutineHost Get()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("QAHost");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<QACoroutineHost>();
        }

        return instance;
    }

    public void Run(IEnumerator routine)
    {
        Running = true;
        StartCoroutine(Wrap(routine));
    }

    private IEnumerator Wrap(IEnumerator routine)
    {
        yield return routine;
        Running = false;
    }
}
