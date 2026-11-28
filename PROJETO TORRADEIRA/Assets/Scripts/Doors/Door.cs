using UnityEngine;

// Porta com mola:
// - abre para o lado oposto ao jogador (ele empurra a porta na direcao em que esta indo);
// - abre rapido, passa um pouco do ponto e assenta; fecha batendo no batente com um leve rebote;
// - pode ser interrompida no meio (apertar E de novo inverte).
public class Door : MonoBehaviour, IInteractable, IInteractionAnchor
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

    public Vector3 IconPosition(Vector3 viewer)
    {
        Vector3 leafNow = transform.rotation * leafLocal;          // dobradica -> centro da folha
        Vector3 handle = transform.position + leafNow * 1.75f;     // perto da borda livre
        float floor = transform.position.y;
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        if (rs.Length > 0) floor = rs[0].bounds.min.y;
        handle.y = floor + 1.0f;
        // Lado da porta onde o jogador esta.
        Vector3 normal = Vector3.Cross(leafNow.normalized, Vector3.up);
        if (Vector3.Dot(viewer - handle, normal) < 0f) normal = -normal;
        return handle + normal * 0.06f;
    }

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
        if (isOpen) PlayCreak();
        else creakFading = true;   // fechando: o rangido some rapido (nao fica tocando por cima)
        // So escolhe o lado quando a porta esta (quase) fechada: aberta, ela volta pelo mesmo caminho.
        if (isOpen && angle < 5f)
            side = SideAwayFromPlayer();
    }

    // ---------- Sons ----------
    // Abrir: rangido (som do projeto). Fechar: batida de madeira + trinco (gerado em codigo),
    // mais forte quanto mais rapido a porta bate.
    private AudioSource doorAudio;
    private float nextSlam;
    private float lastCreak = -10f;
    private bool creakFading;
    private static AudioClip slamClip;

    private AudioSource DoorAudio()
    {
        if (doorAudio != null) return doorAudio;
        GameObject go = new GameObject("SomPorta");
        go.transform.SetParent(transform, false);
        Vector3 leafNow = transform.rotation * leafLocal;
        go.transform.position = transform.position + leafNow + Vector3.up * 0.2f;
        doorAudio = go.AddComponent<AudioSource>();
        doorAudio.playOnAwake = false;
        doorAudio.spatialBlend = 1f;
        doorAudio.rolloffMode = AudioRolloffMode.Linear;
        doorAudio.minDistance = 1.5f;
        doorAudio.maxDistance = 20f;
        doorAudio.dopplerLevel = 0f;
        return doorAudio;
    }

    private void PlayCreak()
    {
        // Abrindo e fechando rapido: nao reinicia o rangido toda hora.
        if (Time.time - lastCreak < 0.35f) return;
        AudioClip clip = Resources.Load<AudioClip>("Sfx/door_open");   // ~0,75 s, igual a animacao
        if (clip == null) return;
        lastCreak = Time.time;
        AudioSource s = DoorAudio();
        s.Stop();
        s.clip = clip;
        s.pitch = Random.Range(0.92f, 1.1f);   // cada porta range um pouco diferente
        s.volume = 0.45f;
        creakFading = false;
        s.Play();
    }

    private void PlaySlam(float strength)
    {
        AudioSource s = DoorAudio();
        if (s.clip != null && s.isPlaying) s.Stop();   // corta o rangido
        s.volume = 1f;
        creakFading = false;
        if (slamClip == null) slamClip = BuildSlam();
        s.pitch = Random.Range(0.92f, 1.06f);
        s.PlayOneShot(slamClip, Mathf.Lerp(0.35f, 0.9f, strength));
    }

    private static AudioClip BuildSlam()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.6f);
        float[] d = new float[n];
        var rng = new System.Random(17);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float x = (float)i / rate;
            // Corpo de madeira: tom grave que cai rapido.
            float body = Mathf.Sin(2f * Mathf.PI * (95f - 30f * x) * x) * Mathf.Exp(-x * 14f);
            // Impacto: ruido curto filtrado.
            float noise = (float)(rng.NextDouble() * 2 - 1);
            lp += (noise - lp) * 0.25f;
            float hit = lp * Mathf.Exp(-x * 45f);
            // Trinco metalico um instante depois.
            float lx = x - 0.045f;
            float latch = lx > 0f ? Mathf.Sin(2f * Mathf.PI * 2300f * lx) * Mathf.Exp(-lx * 90f) * 0.35f : 0f;
            d[i] = Mathf.Clamp(body * 0.9f + hit * 1.2f + latch, -1f, 1f) * 0.8f;
        }
        AudioClip c = AudioClip.Create("PortaBatendo", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
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
                if (!isOpen && angularVelocity < -25f && Time.time > nextSlam)
                {
                    nextSlam = Time.time + 0.3f;
                    PlaySlam(Mathf.Clamp01(-angularVelocity / 250f));
                }
                angle = 0f;
                angularVelocity = isOpen ? 0f : -angularVelocity * closeRebound;
            }
        }

        if (creakFading && doorAudio != null && doorAudio.isPlaying && doorAudio.clip != null)
        {
            doorAudio.volume = Mathf.MoveTowards(doorAudio.volume, 0f, Time.deltaTime * 4f);
            if (doorAudio.volume <= 0.001f) { doorAudio.Stop(); creakFading = false; }
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
