using UnityEngine;

// Ataque do TemporalEraser: o jogador precisa estar perto E sem parede no meio
// (o trigger sozinho alcancava atraves de paredes finas).
public class TemporalEraserAttack : MonoBehaviour
{
    [SerializeField] private DefeatController defeatController;
    [SerializeField] private float reach = 1.3f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        if (DefeatController.IsDefeated || !other.CompareTag("Player"))
            return;

        if (defeatController == null)
        {
            Debug.LogError($"{name}: DefeatController reference is missing.", this);
            return;
        }

        TemporalEraserController owner = GetComponentInParent<TemporalEraserController>();
        Transform enemy = owner != null ? owner.transform : transform;
        Transform playerRoot = other.GetComponentInParent<CharacterController>() != null
            ? other.GetComponentInParent<CharacterController>().transform
            : other.transform;
        Vector3 chest = enemy.position + Vector3.up * 1.2f;
        Vector3 target = other.transform.position + Vector3.up * 1.0f;

        Vector3 flat = target - chest;
        flat.y = 0f;
        if (flat.magnitude > reach)
            return;

        // Nada solido entre o inimigo e o jogador.
        Vector3 direction = target - chest;
        foreach (RaycastHit hit in Physics.RaycastAll(chest, direction.normalized, direction.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(enemy) || hit.transform.IsChildOf(playerRoot))
                continue;

            return;
        }

        defeatController.TriggerDefeat();
    }
}
