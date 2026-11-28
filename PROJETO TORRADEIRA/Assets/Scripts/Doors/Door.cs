using UnityEngine;

// Porta com mola:
// - abre para o lado oposto ao jogador (ele empurra a porta na direcao em que esta indo);
// - abre rapido, passa um pouco do ponto e assenta; fecha batendo no batente com um leve rebote;
// - pode ser interrompida no meio (apertar E de novo inverte).
public class Door : MonoBehaviour, IInteractable
{
    [Header("Configuracao")]
    [SerializeField] private float openAngle = 90f;

    [Header("Mola ao abrir")]
    [SerializeField] private float openStiffness = 55f;
    [SerializeField] private float openDamping = 10f;

    [Header("Mola ao fechar")]
    [SerializeField] private float closeStiffness = 70f;
    [SerializeField] private float closeDamping = 13f;
    [Tooltip("Quanto da velocidade volta quando a porta bate no batente.")]
    [SerializeField, Range(0f, 0.6f)] private float closeRebound = 0.25f;

    private bool isOpen;
    private float angle;          // 0 = fechada, openAngle = aberta (sempre positivo)
    private float angularVelocity;
    private float side = 1f;      // +1 ou -1: para que lado abre
    private Quaternion closedRotation;
    private Vector3 leafLocal;    // do eixo (dobradica) ate o centro da folha, em espaco local
    private Collider[] doorColliders;
    private CharacterController playerController;

    private void Start()
    {
        closedRotation = transform.rotation;
        doorColliders = GetComponentsInChildren<Collider>();
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) playerController = pm.GetComponent<CharacterController>();

        // Centro da folha da porta em relacao a dobradica (o pivo do objeto).
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);
            leafLocal = Quaternion.Inverse(closedRotation) * (b.center - transform.position);
        }
        leafLocal.y = 0f;
        if (leafLocal.sqrMagnitude < 0.01f)
            leafLocal = Vector3.right * 0.5f;
    }

    public bool IsOpen => isOpen;

    public bool CanInteract()
    {
        return true;
    }

    public string GetInteractionMessage()
    {
        return isOpen ? "E para fechar" : "E para abrir";
    }

    public void Interact()
    {
        isOpen = !isOpen;
        // So escolhe o lado quando a porta esta (quase) fechada: aberta, ela volta pelo mesmo caminho.
        if (isOpen && angle < 5f)
            side = SideAwayFromPlayer();
    }

    private float SideAwayFromPlayer()
    {
        Transform player = null;
        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null) player = movement.transform;
        else if (Camera.main != null) player = Camera.main.transform;
        if (player == null)
            return 1f;

        Vector3 axis = closedRotation * Vector3.up;
        Vector3 leaf = closedRotation * leafLocal;
        // Para onde a folha anda girando um pouco no sentido positivo.
        Vector3 move = Quaternion.AngleAxis(10f, axis) * leaf - leaf;
        Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - transform.position, axis);
        return Vector3.Dot(move, toPlayer) > 0f ? -1f : 1f;
    }

    private void Update()
    {
        float target = isOpen ? openAngle : 0f;
        float k = isOpen ? openStiffness : closeStiffness;
        float c = isOpen ? openDamping : closeDamping;

        // Mola em passos pequenos (estavel mesmo com quadro longo).
        float remaining = Mathf.Min(Time.deltaTime, 0.1f);
        while (remaining > 0f)
        {
            float dt = Mathf.Min(remaining, 1f / 120f);
            angularVelocity += (k * (target - angle) - c * angularVelocity) * dt;
            angle += angularVelocity * dt;
            remaining -= dt;

            // Batente: a porta nao atravessa o batente; bate e volta um pouco.
            if (angle < 0f)
            {
                angle = 0f;
                angularVelocity = isOpen ? 0f : -angularVelocity * closeRebound;
            }
        }

        if (!isOpen && angle < 0.02f && Mathf.Abs(angularVelocity) < 0.5f)
        {
            angle = 0f;
            angularVelocity = 0f;
        }

        Quaternion previous = transform.rotation;
        transform.rotation = closedRotation * Quaternion.Euler(0f, angle * side, 0f);

        // A porta e girada por transform: se ela entrar no CharacterController, o Unity
        // empurra o jogador para fora com forca (o bug de "arremessar" o player).
        // Agora o jogador bloqueia a porta: ela para onde encostou nele.
        if (previous != transform.rotation && OverlapsPlayer())
        {
            transform.rotation = previous;
            angle = Mathf.Abs(Quaternion.Angle(closedRotation, previous));
            angularVelocity = 0f;
        }
    }

    private bool OverlapsPlayer()
    {
        if (playerController == null || doorColliders == null) return false;
        Transform pt = playerController.transform;
        foreach (Collider c in doorColliders)
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            if (Physics.ComputePenetration(c, c.transform.position, c.transform.rotation,
                    playerController, pt.position, pt.rotation, out _, out float dist) && dist > 0.001f)
                return true;
        }
        return false;
    }
}
