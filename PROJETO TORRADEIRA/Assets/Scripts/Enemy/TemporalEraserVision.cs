using UnityEngine;

// Visao do TemporalEraser: cone de visao + visao periferica curta, bloqueada por qualquer
// superficie solida (paredes, portas, moveis). Testa a cabeca e o peito do jogador.
public class TemporalEraserVision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform eyePoint;

    [Header("Vision")]
    [SerializeField] private float detectionDistance = 10f;
    [SerializeField] private float fieldOfView = 120f;

    [Header("Peripheral Vision")]
    [SerializeField] private float peripheralDetectionDistance = 4f;
    [SerializeField] private float peripheralFieldOfView = 240f;

    [Header("Proximity")]
    [Tooltip("Muito perto ele sente o jogador mesmo de costas (se nada solido estiver no meio).")]
    [SerializeField] private float senseDistance = 1.6f;

    [Header("Obstacles")]
    [Tooltip("Camadas que bloqueiam a visao. O jogador e o proprio inimigo sao ignorados.")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    private static readonly float[] TargetHeights = { 1.55f, 0.9f };
    private readonly RaycastHit[] hits = new RaycastHit[8];

    public bool CanSeePlayer()
    {
        if (player == null || eyePoint == null)
            return false;

        Vector3 eye = eyePoint.position;
        Vector3 toPlayer = player.position - eye;
        toPlayer.y = 0f;
        float flatDistance = toPlayer.magnitude;

        if (flatDistance > detectionDistance)
            return false;

        Vector3 forward = eyePoint.forward;
        forward.y = 0f;
        float angle = flatDistance > 0.01f ? Vector3.Angle(forward, toPlayer) : 0f;

        bool inside =
            angle <= fieldOfView * 0.5f ||
            (flatDistance <= peripheralDetectionDistance && angle <= peripheralFieldOfView * 0.5f) ||
            flatDistance <= senseDistance;

        if (!inside)
            return false;

        // Basta a cabeca ou o peito estarem a vista.
        foreach (float height in TargetHeights)
        {
            if (Visible(eye, player.position + Vector3.up * height))
                return true;
        }

        return false;
    }

    private bool Visible(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;

        int count = Physics.RaycastNonAlloc(from, direction / distance, hits, distance, obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].transform;

            // Ignora o proprio inimigo e o jogador.
            if (hit.IsChildOf(transform) || hit == player || hit.IsChildOf(player))
                continue;

            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (eyePoint == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyePoint.position, detectionDistance);

        Vector3 left = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * eyePoint.forward;
        Vector3 right = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * eyePoint.forward;

        Gizmos.DrawRay(eyePoint.position, left * detectionDistance);
        Gizmos.DrawRay(eyePoint.position, right * detectionDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(eyePoint.position, senseDistance);
    }
}
