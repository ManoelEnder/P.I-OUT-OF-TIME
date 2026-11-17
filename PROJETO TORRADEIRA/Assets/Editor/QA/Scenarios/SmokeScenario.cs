using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Cenario minimo: o Menu carrega, tem um unico AudioListener e aguenta input sem erro.
public class SmokeScenario : QAScenario
{
    public override string Name => "Smoke";

    public override IEnumerator Run()
    {
        yield return LoadScene("Menu");
        yield return WaitSeconds(3f);

        int listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length;
        Check(listeners == 1, "um AudioListener no Menu (achou " + listeners + ")");

        yield return QAInput.Tap(Key.Space);
        yield return WaitSeconds(1f);
    }
}
