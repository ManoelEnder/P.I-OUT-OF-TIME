using System.Collections;
using UnityEngine;

// TemporalEraser: ve o jogador, persegue, derrota; "tentar de novo" volta jogador e inimigo ao inicio.
public class EnemyScenario : QAScenario
{
    public override string Name => "Enemy";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        TemporalEraserController enemy = Require<TemporalEraserController>();
        Vector3 enemyStart = enemy != null ? enemy.transform.position : Vector3.zero;
        yield return WaitGameplay();

        DefeatController defeat = Require<DefeatController>();
        if (enemy == null || defeat == null)
            yield break;

        Camera eye = Require<PhotoCamera>().playerCam;
        foreach (Renderer r in enemy.GetComponentsInChildren<Renderer>(true))
            Check((eye.cullingMask & (1 << r.gameObject.layer)) != 0, "inimigo visivel a olho nu (" + r.name + " na camada " + LayerMask.LayerToName(r.gameObject.layer) + ")");

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        Transform spawn = GameObject.Find("PlayerSpawn").transform;

        enemy.ActivateNow();
        PlaceInFrontOf(player, enemy.transform);

        float end = Time.realtimeSinceStartup + 5f;
        while (!enemy.IsChasing && Time.realtimeSinceStartup < end)
            yield return null;
        Check(enemy.IsChasing, "inimigo ve o jogador e persegue");

        var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        end = Time.realtimeSinceStartup + 10f;
        float nextLog = 0f;
        while (!DefeatController.IsDefeated && Time.realtimeSinceStartup < end)
        {
            if (Time.realtimeSinceStartup > nextLog)
            {
                nextLog = Time.realtimeSinceStartup + 1f;
                Debug.Log("QA TRACE dist=" + Vector3.Distance(player.position, enemy.transform.position).ToString("0.00") + " enemy=" + enemy.transform.position + " player=" + player.position + " chasing=" + enemy.IsChasing + " vel=" + agent.velocity.magnitude.ToString("0.0") + " rem=" + agent.remainingDistance.ToString("0.0") + " stopped=" + agent.isStopped + " ts=" + Time.timeScale + " dt=" + Time.deltaTime.ToString("0.000"));
            }
            yield return null;
        }
        Check(DefeatController.IsDefeated, "inimigo alcanca e derrota o jogador");
        Check(Time.timeScale == 0f, "derrota congela o jogo");
        Check(GameObject.Find("DefeatCanvas").transform.Find("DefeatPanel").gameObject.activeSelf, "tela de derrota aparece");
        Check(Cursor.visible, "cursor livre na derrota");

        defeat.Retry();
        yield return null;
        yield return null;

        Check(!DefeatController.IsDefeated && Time.timeScale == 1f, "tentar de novo retoma o jogo");
        Check(Vector3.Distance(player.position, spawn.position) < 0.5f, "jogador volta ao PlayerSpawn");
        Check(Vector3.Distance(enemy.transform.position, enemyStart) < 1f, "inimigo volta ao inicio");
        Check(!enemy.IsChasing, "inimigo esquece o jogador");
        Check(CursorCaptured, "cursor volta ao jogo");
    }
}
