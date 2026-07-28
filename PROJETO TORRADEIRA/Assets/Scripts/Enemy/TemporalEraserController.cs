using UnityEngine;
using UnityEngine.AI;

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

    [Header("Search")]
    [SerializeField] private float searchDuration = 5f;
    [SerializeField] private float searchRadius = 8f;
    [SerializeField] private int searchAttempts = 3;

    private NavMeshAgent agent;

    private State currentState;

    private Vector3 lastKnownPlayerPosition;

    private float searchTimer;
    private int currentSearchAttempt;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (vision == null)
        {
            vision = GetComponent<TemporalEraserVision>();
        }
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError(
                $"{name}: Player reference is missing.",
                this
            );

            enabled = false;
            return;
        }

        if (vision == null)
        {
            Debug.LogError(
                $"{name}: TemporalEraserVision reference is missing.",
                this
            );

            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                $"{name}: NavMeshAgent is not placed on a NavMesh.",
                this
            );

            enabled = false;
            return;
        }

        currentState = State.Roaming;

        agent.speed = roamingSpeed;
        agent.isStopped = false;

        SetRoamingDestination();
    }

    private void Update()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            return;
        }

        switch (currentState)
        {
            case State.Roaming:
                UpdateRoaming();
                break;

            case State.Chasing:
                UpdateChasing();
                break;

            case State.Searching:
                UpdateSearching();
                break;
        }
    }

    private void UpdateRoaming()
    {
        bool canSeePlayer = vision.CanSeePlayer();

        Debug.Log(
            $"{name} | Can See Player: {canSeePlayer}",
            this
        );

        if (canSeePlayer)
        {
            StartChasing();
            return;
        }

        if (HasReachedDestination())
        {
            SetRoamingDestination();
        }
    }

    private void UpdateChasing()
    {
        if (vision.CanSeePlayer())
        {
            lastKnownPlayerPosition = player.position;

            agent.speed = chaseSpeed;
            agent.isStopped = false;

            agent.SetDestination(player.position);

            return;
        }

        StartSearching();
    }

    private void UpdateSearching()
    {
        searchTimer -= Time.deltaTime;

        if (vision.CanSeePlayer())
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
        {
            ReturnToRoaming();
        }
    }

    private void StartChasing()
    {
        currentState = State.Chasing;

        lastKnownPlayerPosition = player.position;

        agent.speed = chaseSpeed;
        agent.isStopped = false;

        agent.SetDestination(player.position);
    }

    private void StartSearching()
    {
        currentState = State.Searching;

        searchTimer = searchDuration;
        currentSearchAttempt = 0;

        agent.speed = roamingSpeed;
        agent.isStopped = false;

        SetSearchDestination();
    }

    private void ReturnToRoaming()
    {
        currentState = State.Roaming;

        agent.speed = roamingSpeed;
        agent.isStopped = false;

        SetRoamingDestination();
    }

    private void SetRoamingDestination()
    {
        if (TryGetRandomNavMeshPosition(
            transform.position,
            roamingRadius,
            out Vector3 destination))
        {
            agent.SetDestination(destination);
        }
    }

    private void SetSearchDestination()
    {
        if (TryGetRandomNavMeshPosition(
            lastKnownPlayerPosition,
            searchRadius,
            out Vector3 destination))
        {
            agent.SetDestination(destination);
        }
        else
        {
            ReturnToRoaming();
        }
    }

    private bool TryGetRandomNavMeshPosition(
        Vector3 center,
        float radius,
        out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPosition =
                center + Random.insideUnitSphere * radius;

            if (NavMesh.SamplePosition(
                randomPosition,
                out NavMeshHit hit,
                2f,
                NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();

                if (agent.CalculatePath(
                    hit.position,
                    path) &&
                    path.status == NavMeshPathStatus.PathComplete)
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
        if (agent == null)
        {
            return false;
        }

        if (!agent.isActiveAndEnabled)
        {
            return false;
        }

        if (!agent.isOnNavMesh)
        {
            return false;
        }

        if (agent.pathPending)
        {
            return false;
        }

        if (agent.remainingDistance == Mathf.Infinity)
        {
            return false;
        }

        return agent.remainingDistance <= destinationTolerance;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            roamingRadius
        );

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            lastKnownPlayerPosition,
            searchRadius
        );
    }
}