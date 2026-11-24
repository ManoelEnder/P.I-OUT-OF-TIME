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

        // 1) Ponto do NavMesh a 3-12 m do inimigo, no mesmo andar, com algo solido entre os dois.
        Vector3 eye = enemy.transform.position + Vector3.up * 1.6f;
        bool placed = false;
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        for (int t = 0; t < tri.indices.Length && !placed; t += 3)
        {
            Vector3 center = (tri.vertices[tri.indices[t]] + tri.vertices[tri.indices[t + 1]] + tri.vertices[tri.indices[t + 2]]) / 3f;
            Vector3 flat = center - enemy.transform.position;
            if (Mathf.Abs(flat.y) > 1f)
                continue;
            flat.y = 0f;
            if (flat.magnitude < 3f || flat.magnitude > 12f)
                continue;
            if (!NavMesh.SamplePosition(center, out NavMeshHit spot, 0.5f, NavMesh.AllAreas))
                continue;

            // Cabeca e peito escondidos.
            if (!Physics.Linecast(eye, spot.position + Vector3.up * 1.55f, solid, QueryTriggerInteraction.Ignore) ||
                !Physics.Linecast(eye, spot.position + Vector3.up * 0.9f, solid, QueryTriggerInteraction.Ignore))
                continue;

            // Inimigo olhando direto para o ponto (so a parede no meio).
            enemy.transform.rotation = Quaternion.LookRotation(flat.normalized);
            PerfBenchmark.Teleport(player, spot.position, Quaternion.LookRotation(-flat).eulerAngles.y);
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
        enemy.ActivateNow();
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        Vector3 last = enemy.transform.position;
        int crossings = 0, offMesh = 0, frames = 0;
        float end = Time.realtimeSinceStartup + 25f;
        float moved = 0f;

        while (Time.realtimeSinceStartup < end && !DefeatController.IsDefeated)
        {
            yield return null;
            // O jumpscare tira o inimigo do NavMesh de proposito.
            if (DefeatController.IsDefeated)
                break;
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
        Check(DefeatController.IsDefeated || agent.isOnNavMesh, "agente continua no NavMesh");
    }
}
