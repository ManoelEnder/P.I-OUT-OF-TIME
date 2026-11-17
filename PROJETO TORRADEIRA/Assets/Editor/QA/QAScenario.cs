using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
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

    // Ponto de piso a ~2 m do alvo, com espaco para o corpo e linha de visao (sem depender de NavMesh,
    // que so cobre o andar do inimigo). Devolve false se nao houver.
    public static string LastStandFailure = "";

    protected static bool TryFindStandPoint(Collider target, float eyeHeight, float radius, out Vector3 feet)
    {
        var why = new System.Text.StringBuilder();
        Vector3 center = target.bounds.center;

        foreach (float distance in new[] { 2f, 1.6f, 2.4f })
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                Vector3 above = center + dir * distance + Vector3.up * 1.5f;

                if (!Physics.Raycast(above, Vector3.down, out RaycastHit floor, 4f, ~0, QueryTriggerInteraction.Ignore))
                { why.Append(" sem-piso"); continue; }

                feet = floor.point;
                Collider[] inside = Physics.OverlapCapsule(feet + Vector3.up * (radius + 0.05f), feet + Vector3.up * (eyeHeight - 0.1f), radius, ~0, QueryTriggerInteraction.Ignore);
                if (inside.Length > 0)
                { why.Append(" corpo:" + inside[0].name); continue; }

                if (Physics.Linecast(feet + Vector3.up * eyeHeight, center, out RaycastHit block, ~0, QueryTriggerInteraction.Ignore) && !IsPartOf(block.collider, target))
                { why.Append(" visao:" + block.collider.name); continue; }

                return true;
            }
        }

        LastStandFailure = why.ToString();
        feet = Vector3.zero;
        return false;
    }

    protected IEnumerator Approach(Transform player, Camera view, Collider target)
    {
        float eye = view.transform.position.y - player.position.y;
        CharacterController body = player.GetComponent<CharacterController>();

        if (!TryFindStandPoint(target, eye, body != null ? body.radius : 0.4f, out Vector3 feet))
        {
            Check(false, "achou ponto com visao para " + target.name + " (" + LastStandFailure.Substring(0, System.Math.Min(300, LastStandFailure.Length)) + ")");
            yield break;
        }

        Vector3 flat = target.bounds.center - feet;
        flat.y = 0f;
        PerfBenchmark.Teleport(player, feet + Vector3.up * 0.05f, Quaternion.LookRotation(flat).eulerAngles.y);
        yield return WaitSeconds(0.5f);
    }

    // Espera a animacao de abrir/fechar o visor terminar (o fechamento inclui a piscada).
    protected IEnumerator WaitTransition(PhotoCamera photo)
    {
        float end = Time.realtimeSinceStartup + 6f;
        yield return null;

        while (photo.IsTransitioning && Time.realtimeSinceStartup < end)
            yield return null;

        yield return WaitSeconds(0.1f);
    }

    // Altura do piso logo abaixo de um colisor (ignorando o proprio colisor).
    protected static float GroundUnder(Collider body)
    {
        Vector3 from = new Vector3(body.bounds.center.x, body.bounds.max.y + 0.5f, body.bounds.center.z);

        foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore)
                     .OrderBy(h => h.distance))
        {
            if (!IsPartOf(hit.collider, body))
                return hit.point.y;
        }

        return float.NegativeInfinity;
    }

    // Poe o jogador a ~5 m do inimigo, num ponto do NavMesh com caminho livre ate ele, olhando para ele.
    protected bool PlaceInFrontOf(Transform player, Transform enemy)
    {
        foreach (float angle in new[] { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f, 180f })
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * enemy.forward;
            if (!NavMesh.SamplePosition(enemy.position + dir * 5f, out NavMeshHit spot, 1.5f, NavMesh.AllAreas))
                continue;

            if (NavMesh.Raycast(enemy.position, spot.position, out _, NavMesh.AllAreas))
                continue;

            Vector3 back = enemy.position - spot.position;
            back.y = 0f;
            PerfBenchmark.Teleport(player, spot.position, Quaternion.LookRotation(back).eulerAngles.y);
            return true;
        }

        Check(false, "achou ponto livre na frente do inimigo");
        return false;
    }

    // Ajusta a mira com o mouse virtual ate o centro da tela cair sobre o alvo.
    protected IEnumerator Aim(Camera view, Collider target)
    {
        for (int i = 0; i < 120; i++)
        {
            Ray ray = view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 5f) && IsPartOf(hit.collider, target))
                yield break;

            Vector3 vp = view.WorldToViewportPoint(target.bounds.center);
            yield return QAInput.Look(new Vector2((vp.x - 0.5f) * 40f, (vp.y - 0.5f) * 40f), 1);
        }

        Ray last = view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        string seen = Physics.Raycast(last, out RaycastHit h, 5f) ? h.collider.name + " a " + h.distance.ToString("0.00") + "m" : "nada";
        Check(false, "mira chegou em " + target.name + " (centro da tela acerta: " + seen + ")");
    }

    private static bool IsPartOf(Collider hit, Collider target)
    {
        return hit == target || hit.transform.IsChildOf(target.transform) || target.transform.IsChildOf(hit.transform);
    }

    protected T Require<T>() where T : Object
    {
        T found = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        if (found == null)
            Failures.Add("nao encontrou " + typeof(T).Name);

        return found;
    }
}
