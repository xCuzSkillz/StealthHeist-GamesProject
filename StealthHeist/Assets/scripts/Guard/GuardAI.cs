using UnityEngine;
using UnityEngine.AI;

public enum GuardAIState
{
    Patrolling,
    Chasing,
    Investigating,
    WaitingAtWaypoint   // new state
}

public class GuardAI : MonoBehaviour
{
    public Transform player;
    public Transform[] waypoints;
    public float visionRange = 20f;
    public float visionAngle = 60f;
    public float waitTimeAtWaypoint = 2f;   // seconds to wait

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private GuardAIState state = GuardAIState.Patrolling;
    private float defaultAgentSpeed;
    private float waitTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        defaultAgentSpeed = agent.speed;
        GoToNextWaypoint();
    }

    void Update()
    {
        // Detection
        float distance = DistanceToPlayer();
        if (distance >= 0 && distance <= visionRange)
        {
            // Player is visible, interrupt waiting/patrolling
            state = (distance > visionRange / 2f) ? GuardAIState.Investigating : GuardAIState.Chasing;
            agent.SetDestination(player.position);
            agent.speed = (state == GuardAIState.Chasing) ? defaultAgentSpeed * 2f : defaultAgentSpeed;
            return; // skip patrol logic
        }

        switch (state)
        {
            case GuardAIState.Patrolling:
                // If reached waypoint, start waiting
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    state = GuardAIState.WaitingAtWaypoint;
                    waitTimer = waitTimeAtWaypoint;
                    agent.isStopped = true;
                }
                break;

            case GuardAIState.WaitingAtWaypoint:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    // waiting finished, move to next waypoint
                    agent.isStopped = false;
                    GoToNextWaypoint();
                    state = GuardAIState.Patrolling;
                }
                break;

            case GuardAIState.Chasing:
            case GuardAIState.Investigating:
                // If arrived at destination (player last known position) without seeing player again
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    ResumePatrol();
                }
                break;
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    float DistanceToPlayer()
    {
        if (player == null) return -1;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        if (distanceToPlayer > visionRange) return -1;

        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > visionAngle / 2) return -1;

        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, visionRange))
        {
            if (hit.transform == player) return hit.distance;
        }

        return -1;
    }

    public void HearSound(Vector3 soundPosition)
    {
        if (state == GuardAIState.Chasing) return;
        // interrupt waiting or patrolling
        if (state == GuardAIState.WaitingAtWaypoint)
        {
            agent.isStopped = false;   // start moving again
        }
        agent.SetDestination(soundPosition);
        state = GuardAIState.Investigating;
        CancelInvoke();
        Invoke(nameof(ResumePatrol), 5f);
    }

    void ResumePatrol()
    {
        state = GuardAIState.Patrolling;
        agent.speed = defaultAgentSpeed;
        // go to next waypoint (ignore current position)
        GoToNextWaypoint();
        agent.isStopped = false;
    }
}