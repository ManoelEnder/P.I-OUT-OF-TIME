using UnityEngine;
using UnityEngine.AI;

// IA do TemporalEraser: patrulha -> persegue quando ve o jogador -> procura onde o viu por ultimo.
// Mais esperto: guarda o rastro do jogador (migalhas a cada 1,5 m). Ao perder o jogador segue o rastro
// fresco por alguns metros; na patrulha prefere lugares por onde o jogador passou; ouve corrida por perto.
// O NavMeshAgent so calcula o caminho; a rotacao e suavizada aqui para ele nao "girar no eixo".
[RequireComponent(typeof(NavMeshAgent))]
public class TemporalEraserController : MonoBehaviour
{
    private enum State
    {
        Roaming,
        Chasing,
        Searching
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private TemporalEraserVision vision;

    [Header("Movement")]
    [SerializeField] private float roamingSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float roamingRadius = 15f;
    [SerializeField] private float destinationTolerance = 1f;
    [SerializeField] private float turnSpeed = 300f;
    [SerializeField] private float acceleration = 8f;

    [Header("Patrulha")]
    [Tooltip("Distancia minima ate o proximo ponto de patrulha (evita andar em circulos).")]
    [SerializeField] private float minRoamDistance = 4f;
    [Tooltip("Pausa ao chegar num ponto de patrulha, parado 'escutando'.")]
    [SerializeField] private Vector2 roamPause = new Vector2(1.2f, 3.2f);

    [Header("Percepcao")]
    [Tooltip("Tempo vendo o jogador antes de comecar a perseguir.")]
    [SerializeField] private float reactionTime = 0.35f;
    [Tooltip("Depois de perder o jogador de vista, ainda corre ate onde o viu por ultimo.")]
    [SerializeField] private float loseSightGrace = 1.2f;
    [SerializeField] private float repathInterval = 0.15f;

    [Header("Inicio")]
    [Tooltip("Segundos de jogo (pausa e intro nao contam) antes de ele comecar a se mover.")]
    [SerializeField] private float activationDelay = 12f;

    [Header("Search")]
    [SerializeField] private float searchDuration = 5f;
    [SerializeField] private float searchRadius = 8f;
    [SerializeField] private int searchAttempts = 3;
    [SerializeField] private float searchSpeed = 3f;

    [Header("Rastro e audicao")]
    [Tooltip("Segundos depois de perder de vista em que o rastro ainda esta 'fresco'.")]
    [SerializeField] private float trailFreshness = 7f;
    [SerializeField] private int trailMaxSteps = 10;
    [Tooltip("Chance de patrulhar um lugar por onde o jogador passou (0..1).")]
    [SerializeField, Range(0f, 1f)] private float trailRoamChance = 0.6f;
    [SerializeField] private float trailMemory = 150f;
    [SerializeField] private float hearSprint = 11f;
    [SerializeField] private float hearWalk = 3.5f;

    private NavMeshAgent agent;
    private State currentState;

    private Vector3 lastKnownPlayerPosition;

    private float searchTimer;
    private int currentSearchAttempt;

    private float sightTimer;
    private float lastSeenTime;
    private float nextRepath;
    private float pauseUntil;
    private float stuckTimer;

    private float activateAt;
    private float stunnedUntil;
    private bool wasStunned;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private readonly System.Collections.Generic.List<Vector3> trail = new System.Collections.Generic.List<Vector3>();
    private readonly System.Collections.Generic.List<float> trailTime = new System.Collections.Generic.List<float>();
    private int trailIndex = -1;
    private int trailSteps;
    private float nextHear;
    private PlayerMovement playerMovement;

    public bool IsChasing => currentState == State.Chasing;
    public bool IsStunned => Time.time < stunnedUntil;

    // Reacao a urina (PeeReaction): foge com nojo ou fica furioso.
    private float fleeUntil, rageUntil, nextFleePath;
    public bool IsFleeing => Time.time < fleeUntil;
    public bool IsEnraged => Time.time < rageUntil;
    public void Flee(float seconds) { fleeUntil = Time.time + seconds; nextFleePath = 0f; }
    public void Enrage(float seconds) { rageUntil = Time.time + seconds; fleeUntil = 0f; }
    public void ClearPeeStates() { fleeUntil = 0f; rageUntil = 0f; wasFleeing = false; }

    private bool FindFleePoint(out Vector3 best)
    {
        best = transform.position;
        float bestScore = float.MinValue;
        NavMeshPath path = new NavMeshPath();
        Vector3 me = transform.position, pl = player.position;
        for (int i = 0; i < 16; i++)
        {
            Vector2 c = Random.insideUnitCircle.normalized * Random.Range(8f, 18f);
            Vector3 p = me + new Vector3(c.x, 0f, c.y);
            if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;
            if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            // Caminho nao pode passar perto do jogador.
            float closest = float.MaxValue;
            foreach (Vector3 corner in path.corners) closest = Mathf.Min(closest, Vector3.Distance(corner, pl));
            if (closest < 1.5f && Vector3.Distance(me, pl) > 3f) continue;
            float score = Vector3.Distance(hit.position, pl) - Vector3.Distance(hit.position, me) * 0.3f;
            if (score > bestScore) { bestScore = score; best = hit.position; }
        }
        return bestScore > float.MinValue;
    }

    private bool wasFleeing;

    private bool UpdateFleeing()
    {
        if (!IsFleeing)
        {
            // Acabou a fuga: volta atras do jogador (procura onde ele esta agora).
            if (wasFleeing)
            {
                wasFleeing = false;
                agent.speed = searchSpeed;
                lastKnownPlayerPosition = player.position;
                lastSeenTime = Time.time;
                StartSearching();
            }
            return false;
        }
        wasFleeing = true;
        agent.isStopped = false;
        agent.speed = chaseSpeed * 1.15f;
        // Destino so e trocado quando chega, quando trava ou a cada 3 s (antes trocava toda hora
        // e ele ficava indeciso, parado no lugar "andando").
        bool arrived = !agent.pathPending && agent.remainingDistance <= destinationTolerance + 0.5f;
        bool stuck = agent.velocity.sqrMagnitude < 0.05f && Time.time > nextFleePath - 2.4f;
        if (Time.time >= nextFleePath || arrived || stuck)
        {
            nextFleePath = Time.time + 3f;
            if (FindFleePoint(out Vector3 dest))
                agent.SetDestination(dest);
            else
            {
                // Sem ponto bom: so se afasta na direcao oposta ao jogador.
                Vector3 away = transform.position - player.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = -transform.forward;
                if (NavMesh.SamplePosition(transform.position + away.normalized * 10f, out NavMeshHit h, 8f, NavMesh.AllAreas))
                    agent.SetDestination(h.position);
            }
        }
        UpdateRotation();
        return true;
    }

    // Flash da camera de perto: fica atordoado, parado, e depois procura onde o jogador estava.
    public void Stun(float seconds)
    {
        stunnedUntil = Time.time + seconds;
        sightTimer = 0f;
        if (player != null)
            lastKnownPlayerPosition = player.position;

        TemporalEraserPresence presence = GetComponent<TemporalEraserPresence>();
        if (presence != null)
            presence.Shock(Mathf.Min(seconds, 1.2f));
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        startPosition = transform.position;
        startRotation = transform.rotation;

        if (vision == null)
            vision = GetComponent<TemporalEraserVision>();

        ApplyThreatTuning();
    }

    // Valores da cena eram faceis de despistar. Garante um minimo de agressividade
    // (sobrescreve o que estiver serializado so quando for mais "bonzinho").
    private void ApplyThreatTuning()
    {
        // Equilibrio: perigoso, mas da para despistar quebrando a linha de visao e
        // se escondendo. Correndo (6) o jogador abre distancia; andando (3.5) nao.
        chaseSpeed = Mathf.Clamp(chaseSpeed, 4.4f, 4.8f);
        roamingSpeed = Mathf.Max(roamingSpeed, 2.2f);
        searchSpeed = Mathf.Clamp(searchSpeed, 3f, 3.4f);
        reactionTime = Mathf.Clamp(reactionTime, 0.2f, 0.3f);
        loseSightGrace = Mathf.Clamp(loseSightGrace, 1f, 1.4f);
        searchDuration = Mathf.Clamp(searchDuration, 7f, 9f);
        searchAttempts = Mathf.Clamp(searchAttempts, 2, 4);
        hearSprint = Mathf.Clamp(hearSprint, 9f, 12f);
        hearWalk = Mathf.Clamp(hearWalk, 2.5f, 3.5f);
        activationDelay = Mathf.Min(activationDelay, 3f); // antes ficava 12 s parado no comeco
    }

    [Header("Caca")]
    [SerializeField] private float huntInterval = 70f;
    [SerializeField] private float huntRadius = 16f;
    private float nextHunt;
    private Vector3 lastSafePosition;
    private bool hasSafePosition;
    private float lastLockOnTime;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError($"{name}: Player reference is missing.", this);
            enabled = false;
            return;
        }

        if (vision == null)
        {
            Debug.LogError($"{name}: TemporalEraserVision reference is missing.", this);
            enabled = false;
            return;
        }

        agent.updateRotation = false;
        agent.acceleration = acceleration;
        agent.autoBraking = true;

        if (!EnsureOnNavMesh())
        {
            Debug.LogError($"{name}: NavMeshAgent is not placed on a NavMesh.", this);
            enabled = false;
            return;
        }

        ReturnToRoaming();
        activateAt = Time.time + activationDelay;
        agent.isStopped = true;
    }

    private void Update()
    {
        if (!agent.isActiveAndEnabled)
            return;

        if (!agent.isOnNavMesh && !EnsureOnNavMesh())
            return;

        // Comeco da partida: parado, so "existindo" (Time.time nao anda na intro nem na pausa).
        if (Time.time < activateAt)
        {
            agent.isStopped = true;
            return;
        }

        if (IsStunned)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            wasStunned = true;
            return;
        }

        if (wasStunned)
        {
            wasStunned = false;
            if (!IsFleeing)
                StartSearching();
        }

        RecordTrail();

        if (UpdateFleeing())
            return;

        bool sees = vision.CanSeePlayer() || IsEnraged; // furioso: sabe onde voce esta

        if (!sees && currentState != State.Chasing && Hears())
        {
            // Ouviu: vai ate o barulho (sem saber exatamente onde o jogador esta).
            lastKnownPlayerPosition = player.position + Random.insideUnitSphere * 1.5f;
            lastSeenTime = Time.time;
            StartSearching();
        }

        if (sees)
        {
            lastKnownPlayerPosition = player.position;
            lastSeenTime = Time.time;
        }

        // De tempos em tempos ele "fareja" a regiao do jogador em vez de vagar a esmo.
        if (currentState == State.Roaming && Time.time >= nextHunt)
        {
            nextHunt = Time.time + huntInterval * Random.Range(0.7f, 1.3f);
            if (Time.time > activateAt + 5f && Time.time - lastSeenTime > 45f)
            {
                lastKnownPlayerPosition = player.position + Random.insideUnitSphere * huntRadius;
                lastSeenTime = Time.time;
                StartSearching();
            }
        }

        switch (currentState)
        {
            case State.Roaming:
                UpdateRoaming(sees);
                break;

            case State.Chasing:
                UpdateChasing(sees);
                break;

            case State.Searching:
                UpdateSearching(sees);
                break;
        }

        UpdateRotation();
        UpdateStuck();
    }

    // O agente so anda no NavMesh; se ele estiver desatualizado (fitas/paredes adicionadas
    // depois do bake), o inimigo atravessava. Aqui cada passo e conferido contra colisores
    // solidos: se cruzar um, volta e recorta o NavMesh naquele ponto para recalcular o caminho.
    private void LateUpdate()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        Vector3 pos = transform.position;
        if (!hasSafePosition) { lastSafePosition = pos; hasSafePosition = true; return; }

        Vector3 delta = pos - lastSafePosition;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.0001f) return;
        if (delta.sqrMagnitude > 4f) { lastSafePosition = pos; return; } // teleporte (Warp/reset)

        float radius = Mathf.Max(0.15f, agent.radius * 0.6f);
        Vector3 from = lastSafePosition + Vector3.up * 0.9f;
        RaycastHit[] hits = Physics.SphereCastAll(from, radius, delta.normalized, delta.magnitude + 0.05f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (h.collider == null || h.distance <= 0f) continue;   // ja estava dentro: ignora
            Transform t = h.collider.transform;
            if (t.IsChildOf(transform) || (player != null && t.IsChildOf(player))) continue;
            if (h.collider.attachedRigidbody != null && !h.collider.attachedRigidbody.isKinematic) continue;
            if (h.collider.GetComponentInParent<Door>() != null) continue; // portas tem NavMeshObstacle proprio

            agent.Warp(lastSafePosition);
            BlockAt(h.point, h.collider);
            return;
        }
        lastSafePosition = pos;
    }

    private void BlockAt(Vector3 point, Collider c)
    {
        if (c.GetComponent<NavMeshObstacle>() == null && c.bounds.size.magnitude < 12f)
        {
            NavMeshObstacle o = c.gameObject.AddComponent<NavMeshObstacle>();
            o.carving = true;
            o.carveOnlyStationary = true;
            o.shape = NavMeshObstacleShape.Box;
            o.center = c.transform.InverseTransformPoint(c.bounds.center);
            Vector3 s = c.transform.lossyScale;
            o.size = new Vector3(c.bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(s.x)),
                                 c.bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(s.y)),
                                 c.bounds.size.z / Mathf.Max(0.001f, Mathf.Abs(s.z)));
        }
        else
        {
            // Colisor grande (parede inteira): recorta so um pedaco no ponto do impacto.
            GameObject g = new GameObject("NavBlock");
            g.transform.position = point;
            NavMeshObstacle o = g.AddComponent<NavMeshObstacle>();
            o.carving = true;
            o.shape = NavMeshObstacleShape.Box;
            o.size = new Vector3(0.6f, 2f, 0.6f);
        }
        if (agent.hasPath)
        {
            Vector3 dest = agent.destination;
            agent.ResetPath();
            agent.SetDestination(dest);
        }
    }

    // ---------- Estados ----------

    private void UpdateRoaming(bool sees)
    {
        if (Notice(sees, reactionTime))
        {
            StartChasing();
            return;
        }

        // Viu algo: para e encara o jogador ate decidir (rotacao em UpdateRotation).
        if (sightTimer > 0f)
        {
            agent.isStopped = true;
            pauseUntil = Mathf.Max(pauseUntil, Time.time + 0.6f);
            return;
        }

        if (Time.time < pauseUntil)
            return;

        if (agent.isStopped)
        {
            agent.isStopped = false;
            SetRoamingDestination();
            return;
        }

        if (HasReachedDestination())
        {
            // Para e "escuta" antes de seguir.
            agent.isStopped = true;
            pauseUntil = Time.time + Random.Range(roamPause.x, roamPause.y);
        }
    }

    private void UpdateChasing(bool sees)
    {
        if (!sees && Time.time - lastSeenTime > loseSightGrace)
        {
            StartSearching();
            return;
        }

        agent.speed = IsEnraged ? chaseSpeed * 1.3f : chaseSpeed;
        agent.isStopped = false;

        if (Time.time >= nextRepath)
        {
            nextRepath = Time.time + repathInterval;
            agent.SetDestination(ReachablePoint(lastKnownPlayerPosition));
        }
    }

    private void UpdateSearching(bool sees)
    {
        searchTimer -= Time.deltaTime;

        if (Notice(sees, reactionTime * 0.4f))
        {
            StartChasing();
            return;
        }

        if (HasReachedDestination())
        {
            // Primeiro segue o rastro fresco deixado depois de sumir de vista.
            if (NextTrailPoint(out Vector3 crumb))
            {
                agent.SetDestination(crumb);
                searchTimer = Mathf.Max(searchTimer, 3f);
                return;
            }

            if (currentSearchAttempt < searchAttempts)
            {
                currentSearchAttempt++;
                SetSearchDestination();
            }
            else
            {
                ReturnToRoaming();
            }
        }

        if (searchTimer <= 0f)
            ReturnToRoaming();
    }

    // Precisa ver o jogador por um instante antes de reagir (sem "estalar" ao primeiro pixel).
    private bool Notice(bool sees, float time)
    {
        sightTimer = sees ? sightTimer + Time.deltaTime : Mathf.Max(0f, sightTimer - Time.deltaTime * 0.5f);
        return sightTimer >= time;
    }

    private void StartChasing()
    {
        currentState = State.Chasing;
        sightTimer = 0f;
        pauseUntil = 0f;
        nextRepath = 0f;

        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.SetDestination(ReachablePoint(lastKnownPlayerPosition));
    }

    private void StartSearching()
    {
        currentState = State.Searching;

        searchTimer = searchDuration;
        currentSearchAttempt = 0;
        trailSteps = 0;
        trailIndex = FirstCrumbAfter(lastSeenTime - 0.5f);

        agent.speed = searchSpeed;
        agent.isStopped = false;
        agent.SetDestination(ReachablePoint(lastKnownPlayerPosition));
    }

    private void ReturnToRoaming()
    {
        currentState = State.Roaming;
        sightTimer = 0f;
        pauseUntil = 0f;

        agent.speed = roamingSpeed;
        agent.isStopped = false;

        SetRoamingDestination();
    }

    // Pula a espera do comeco da partida (usado pelos testes automaticos).
    public void ActivateNow()
    {
        activateAt = 0f;
        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    // Chamado no "tentar de novo": volta ao ponto inicial e esquece o jogador.
    public void ResetToStart() => ResetToStart(activationDelay);

    public void ResetToStart(float wakeDelay)
    {
        if (agent == null || !agent.isActiveAndEnabled)
            return;

        agent.Warp(startPosition);
        hasSafePosition = false;
        transform.rotation = startRotation;
        trail.Clear();
        trailTime.Clear();
        trailIndex = -1;
        lastSeenTime = -999f;
        activateAt = Time.time + wakeDelay;
        sightTimer = 0f;
        stunnedUntil = 0f;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            ReturnToRoaming();
        }
    }

    // ---------- Movimento ----------

    private void UpdateRotation()
    {
        Vector3 look = agent.desiredVelocity;

        // Perto do jogador (ou percebendo ele), encara ele em vez da direcao do caminho.
        bool noticing = currentState != State.Chasing && sightTimer > 0f;
        if (noticing || (currentState == State.Chasing && Vector3.Distance(transform.position, player.position) < 2.5f))
            look = player.position - transform.position;

        look.y = 0f;
        if (look.sqrMagnitude < 0.01f)
            return;

        Quaternion target = Quaternion.LookRotation(look.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    // Se ficar parado sem chegar (preso num canto), escolhe outro destino.
    private void UpdateStuck()
    {
        bool shouldMove = !agent.isStopped && agent.hasPath && agent.remainingDistance > destinationTolerance;

        if (shouldMove && agent.velocity.sqrMagnitude < 0.04f)
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;

        if (stuckTimer < 1.5f)
            return;

        stuckTimer = 0f;

        if (currentState == State.Chasing)
            agent.SetDestination(ReachablePoint(lastKnownPlayerPosition));
        else if (currentState == State.Searching)
            SetSearchDestination();
        else
            SetRoamingDestination();
    }

    private bool EnsureOnNavMesh()
    {
        if (agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    // Ponto do NavMesh mais perto do alvo (o jogador pode estar fora dele, ex.: em cima de algo).
    private Vector3 ReachablePoint(Vector3 target)
    {
        return NavMesh.SamplePosition(target, out NavMeshHit hit, 2.5f, NavMesh.AllAreas) ? hit.position : target;
    }

    // ---------- Rastro ----------

    private void RecordTrail()
    {
        if (player == null)
            return;

        Vector3 p = player.position;
        if (trail.Count > 0 && (trail[trail.Count - 1] - p).sqrMagnitude < 2.25f)
            return;
        if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            return;

        trail.Add(hit.position);
        trailTime.Add(Time.time);
        if (trail.Count > 120)
        {
            trail.RemoveAt(0);
            trailTime.RemoveAt(0);
        }
    }

    private int FirstCrumbAfter(float time)
    {
        for (int i = 0; i < trailTime.Count; i++)
            if (trailTime[i] >= time)
                return i;
        return -1;
    }

    private bool NextTrailPoint(out Vector3 point)
    {
        point = Vector3.zero;
        while (trailIndex >= 0 && trailIndex < trail.Count && trailSteps < trailMaxSteps)
        {
            int i = trailIndex++;
            // So o trecho fresco (logo depois de sumir de vista) ainda tem "cheiro".
            if (trailTime[i] > lastSeenTime + trailFreshness)
                break;
            if ((trail[i] - transform.position).sqrMagnitude < 2.25f)
                continue;
            trailSteps++;
            point = trail[i];
            return true;
        }
        trailIndex = -1;
        return false;
    }

    private bool Hears()
    {
        if (Time.time < nextHear || player == null)
            return false;
        nextHear = Time.time + 0.25f;

        if (playerMovement == null)
            playerMovement = player.GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement == null || playerMovement.MoveBlend < 0.1f)
            return false;

        float range = playerMovement.IsSprinting ? hearSprint : hearWalk;
        Vector3 d = player.position - transform.position;
        return Mathf.Abs(d.y) < 2.5f && d.sqrMagnitude < range * range;
    }

    private void SetRoamingDestination()
    {
        // Prefere lugares por onde o jogador passou (nao muito recentes, para nao ir direto nele).
        if (trail.Count > 4 && Random.value < trailRoamChance)
        {
            for (int i = 0; i < 6; i++)
            {
                int k = Random.Range(0, trail.Count);
                float age = Time.time - trailTime[k];
                if (age < 8f || age > trailMemory)
                    continue;
                if (Vector3.Distance(trail[k], transform.position) < minRoamDistance)
                    continue;
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(trail[k], path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(trail[k]);
                    return;
                }
            }
        }

        for (int i = 0; i < 6; i++)
        {
            if (TryGetRandomNavMeshPosition(transform.position, roamingRadius, out Vector3 destination) &&
                Vector3.Distance(destination, transform.position) >= minRoamDistance)
            {
                agent.SetDestination(destination);
                return;
            }
        }

        // Volta para perto do inicio se nao achou nada longe o suficiente.
        agent.SetDestination(ReachablePoint(startPosition));
    }

    private void SetSearchDestination()
    {
        if (TryGetRandomNavMeshPosition(lastKnownPlayerPosition, searchRadius, out Vector3 destination))
            agent.SetDestination(destination);
        else
            ReturnToRoaming();
    }

    private bool TryGetRandomNavMeshPosition(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            Vector3 randomPosition = center + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();

                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    result = hit.position;
                    return true;
                }
            }
        }

        result = Vector3.zero;
        return false;
    }

    private bool HasReachedDestination()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh || agent.pathPending)
            return false;

        if (agent.remainingDistance == Mathf.Infinity)
            return false;

        return agent.remainingDistance <= destinationTolerance;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, roamingRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastKnownPlayerPosition, searchRadius);
    }
}
