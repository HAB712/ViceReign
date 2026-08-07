using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CombatSystem;

/// <summary>
/// CommonNPC / Civilian AI
/// - Patrols between GameManager waypoints
/// - Plays Idle when still, Walk when patrolling, runs when fleeing
/// - On hit: plays Hit animation, then runs away from player for returnToPatrolTime seconds
/// - On death: plays Death animation, stops all AI
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HealthSystem))]
public class NPCCivilianAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [HideInInspector]
public bool killReported = false;


    [Header("Patrol Settings")]
    public float patrolSpeed = 2f;
    public float waypointReachDistance = 1.5f;

    [Header("Flee Settings")]
    public float runSpeed = 6f;
    public float runDistance = 15f;
    public float returnToPatrolTime = 8f;

    [Header("Animation Thresholds")]
    public float walkThreshold = 0.5f;
    public float runThreshold = 4f;

    // Components
    private NavMeshAgent agent;
    private Animator animator;
    private HealthSystem healthSystem;

    // State
    private bool isRunningAway = false;
    private bool isDead = false;
    [Header("Patrol Route")]
public Transform route;

private List<Transform> routeWaypoints = new List<Transform>();
private Transform currentWaypoint;
private int currentWaypointIndex = 0;

    // Animator param hashes (faster than string lookup every frame)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int HitHash   = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    // ─── Lifecycle ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        agent        = GetComponent<NavMeshAgent>();
        animator     = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        agent.speed = patrolSpeed;

        // Auto-find player if not assigned
        if (player == null)
        {
            var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
            if (tpc != null) player = tpc.transform;
        }

        // Subscribe to health events
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnHealthChanged;
            healthSystem.OnDeath         += OnDeath;
        }

        if (route != null)
{
    foreach (Transform child in route)
    {
        routeWaypoints.Add(child);
    }

    if (routeWaypoints.Count > 0)
    {
        currentWaypointIndex = Random.Range(0, routeWaypoints.Count);
    }
}

        SelectNewWaypoint();
    }

    private void Update()
    {
        if (isDead) return;

        // Drive speed parameter every frame
        float speed = agent.velocity.magnitude;
        animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);

        if (!isRunningAway)
            Patrol();
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthChanged;
            healthSystem.OnDeath         -= OnDeath;
        }
    }

    // ─── Health Events ────────────────────────────────────────────────────────────

    private void OnHealthChanged(float current, float max)
{
    if (isDead) return;
    if (current <= 0f) return;   // lethal hit — OnDeath ise handle karega, Hit/RunAway mat chalao

    animator.SetTrigger(HitHash);
    RunAway();
}

    private void OnDeath()
    {
        isDead = true;

        // Stop all movement
        agent.isStopped = true;
        agent.enabled = false;

        // Cancel any pending Invoke
        CancelInvoke(nameof(ReturnToPatrol));

        // Play Death
        animator.SetTrigger(DeathHash);

        Debug.Log(gameObject.name + " has died.");
    }

    // ─── Patrol ───────────────────────────────────────────────────────────────────

    private void Patrol()
    {
        if (currentWaypoint == null)
        {
            SelectNewWaypoint();
            return;
        }

        float dist = Vector3.Distance(transform.position, currentWaypoint.position);
        if (dist <= waypointReachDistance)
            SelectNewWaypoint();
    }

   private void SelectNewWaypoint()
{
    if (routeWaypoints.Count == 0)
        return;

    if (currentWaypointIndex >= routeWaypoints.Count)
        currentWaypointIndex = 0;

    currentWaypoint = routeWaypoints[currentWaypointIndex];

    currentWaypointIndex++;

    if (agent.enabled && agent.isOnNavMesh)
    {
        agent.SetDestination(currentWaypoint.position);
    }
}

    // ─── Flee ─────────────────────────────────────────────────────────────────────

    public void RunAway()
    {
        if (isDead || player == null) return;
        if (isRunningAway) return; // already fleeing

        isRunningAway = true;
        agent.speed   = runSpeed;

        // Pick a point directly away from the player
        Vector3 dir          = (transform.position - player.position).normalized;
        Vector3 targetPos    = transform.position + dir * runDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, runDistance, NavMesh.AllAreas))
            agent.SetDestination(hit.position);

        Debug.Log(gameObject.name + " is running away!");

        // Cancel any existing return invoke before scheduling a new one
        CancelInvoke(nameof(ReturnToPatrol));
        Invoke(nameof(ReturnToPatrol), returnToPatrolTime);
    }

    private void ReturnToPatrol()
    {
        if (isDead) return;

        isRunningAway = false;
        agent.speed   = patrolSpeed;

        SelectNewWaypoint();
        Debug.Log(gameObject.name + " returned to patrol.");
    }
}
