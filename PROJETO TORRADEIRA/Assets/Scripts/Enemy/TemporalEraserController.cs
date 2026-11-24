using UnityEngine;
using UnityEngine.AI;

// IA do TemporalEraser: patrulha -> persegue quando ve o jogador -> procura onde o viu por ultimo.
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

    public bool IsChasing => currentState == State.Chasing;
    public bool IsStunned => Time.time < stunnedUntil;

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
    }

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
            StartSearching();
        }

        bool sees = vision.CanSeePlayer();

        if (sees)
        {
            lastKnownPlayerPosition = player.position;
            lastSeenTime = Time.time;
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

        agent.speed = chaseSpeed;
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

        agent.speed = roamingSpeed;
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
    public void ResetToStart()
    {
        if (agent == null || !agent.isActiveAndEnabled)
            return;

        agent.Warp(startPosition);
        transform.rotation = startRotation;
        lastSeenTime = -999f;
        activateAt = Time.time + activationDelay;

        if (agent.isOnNavMesh)
            ReturnToRoaming();
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

    private void SetRoamingDestination()
    {
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
