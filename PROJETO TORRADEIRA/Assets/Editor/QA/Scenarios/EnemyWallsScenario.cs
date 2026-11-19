using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// TemporalEraser x paredes:
// 1) jogador logo atras de uma parede: o inimigo NAO ve;
// 2) patrulhando e perseguindo por 25 s: nunca atravessa parede entre um quadro e outro
//    e fica sempre em cima do NavMesh.
public class EnemyWallsScenario : QAScenario
{
    public override string Name => "EnemyWalls";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        yield return WaitGameplay();

        TemporalEraserController enemy = Require<TemporalEraserController>();
        TemporalEraserVision vision = Require<TemporalEraserVision>();
        if (enemy == null || vision == null)
            yield break;

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        int solid = ~LayerMask.GetMask("Ignore Raycast", "UI", "Arms", "PhotoCameraEffect", "GlobalVolume", "Enemy", "Temporal", "TransparentFX");

        // 1) Procura um ponto perto do inimigo com parede no meio (anda pelo NavMesh ate achar).
        Vector3 eye = enemy.transform.position + Vector3.up * 1.6f;
        bool placed = false;
        for (int i = 0; i < 360 && !placed; i++)
        {
            float angle = i * 360f / 72f;
            float range = 6f + (i / 72) * 4f;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            if (!Physics.Raycast(eye, dir, out RaycastHit wall, range, solid, QueryTriggerInteraction.Ignore))
                continue;

            NavMeshHit spot = default;
            bool found = false;
            foreach (float back in new[] { 0.8f, 1.2f, 1.8f, 2.5f })
            {
                if (NavMesh.SamplePosition(wall.point + dir * back, out spot, 0.5f, NavMesh.AllAreas))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                continue;
            if (!Physics.Linecast(eye, spot.position + Vector3.up * 1.2f, solid, QueryTriggerInteraction.Ignore))
                continue;

            // Vira o inimigo para a parede, com o jogador logo atras dela.
            enemy.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
            PerfBenchmark.Teleport(player, spot.position, angle + 180f);
            placed = true;
        }

        Check(placed, "achou um ponto atras de uma parede perto do inimigo");
        if (placed)
        {
            yield return null;
            yield return null;
            Check(!vision.CanSeePlayer(), "inimigo nao ve o jogador atraves da parede");
        }

        // 2) Deixa a IA rodar (patrulha/perseguicao) e vigia cada passo.
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        Vector3 last = enemy.transform.position;
        int crossings = 0, offMesh = 0, frames = 0;
        float end = Time.realtimeSinceStartup + 25f;
        float moved = 0f;

        while (Time.realtimeSinceStartup < end && !DefeatController.IsDefeated)
        {
            yield return null;
            Vector3 now = enemy.transform.position;
            moved += Vector3.Distance(now, last);

            // Passo do quadro atravessando algo solido na altura do joelho.
            if (Physics.Linecast(last + Vector3.up * 0.5f, now + Vector3.up * 0.5f, out RaycastHit hit, solid, QueryTriggerInteraction.Ignore) &&
                !hit.transform.IsChildOf(enemy.transform) && !hit.transform.IsChildOf(player.root) && hit.collider.GetComponentInParent<Door>() == null)
            {
                crossings++;
                Debug.Log("QA TRACE atravessou " + hit.collider.name + " em " + hit.point);
            }

            if (!NavMesh.SamplePosition(now, out _, 0.3f, NavMesh.AllAreas))
                offMesh++;

            last = now;
            frames++;
        }

        Check(moved > 3f, "inimigo andou durante o teste (" + moved.ToString("0.0") + " m)");
        Check(crossings == 0, "inimigo nunca atravessou parede (" + crossings + " vezes em " + frames + " quadros)");
        Check(offMesh == 0, "inimigo sempre em cima do NavMesh (" + offMesh + " quadros fora)");
        Check(agent.isOnNavMesh, "agente continua no NavMesh");
    }
}
