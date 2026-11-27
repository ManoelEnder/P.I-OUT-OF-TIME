using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Pulo: mede altura e tempo no ar (parado e correndo).
public class JumpScenario : QAScenario
{
    public override string Name => "Jump";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();

        PlayerMovement player = Require<PlayerMovement>();
        if (player == null) yield break;
        CharacterController cc = player.controller;

        yield return Measure(player, cc, false);
        yield return WaitSeconds(0.5f);
        yield return Measure(player, cc, true);
    }

    private IEnumerator Measure(PlayerMovement player, CharacterController cc, bool running)
    {
        if (running)
        {
            QAInput.Hold(Key.W, true);
            QAInput.Hold(Key.LeftShift, true);
            yield return WaitSeconds(0.8f);
        }
        float startY = player.transform.position.y;
        Vector3 startPos = player.transform.position;
        yield return QAInput.Tap(Key.Space);
        float t = 0f, maxY = startY;
        bool left = false;
        while (t < 3f)
        {
            t += Time.deltaTime;
            maxY = Mathf.Max(maxY, player.transform.position.y);
            if (!cc.isGrounded) left = true;
            if (left && cc.isGrounded) break;
            yield return null;
        }
        Vector3 d = player.transform.position - startPos;
        d.y = 0f;
        if (running)
        {
            QAInput.Hold(Key.W, false);
            QAInput.Hold(Key.LeftShift, false);
        }
        float h = maxY - startY;
        string label = running ? "correndo" : "parado";
        Debug.Log("QA pulo " + label + ": altura " + h.ToString("F2") + " m, no ar " + t.ToString("F2") + " s, distancia " + d.magnitude.ToString("F2") + " m");
        Check(left, "pulo " + label + " saiu do chao");
        Check(h > 0.6f && h < 1.3f, "pulo " + label + " altura " + h.ToString("F2") + " m");
        Check(t > 0.4f && t < 0.9f, "pulo " + label + " tempo no ar " + t.ToString("F2") + " s");
    }
}
