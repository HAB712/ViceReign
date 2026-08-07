using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CombatSystem
{
    /// <summary>
    /// NPC Combat AI System
    /// NPC only becomes aggressive when it RECEIVES damage - never from detecting player animations.
    /// </summary>
    public class NPCCombatAI : MonoBehaviour
    {

        [HideInInspector]
public bool firstHitReported = false;
[HideInInspector]
public bool killReported = false;


        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 50f;
        [SerializeField] private float attackRange = 3f;
        [SerializeField] private float stopChaseDistance = 0.5f;

        [Header("Combat Settings")]
        [SerializeField] private float punchDamage = 8f;
        [SerializeField] private float kickDamage = 12f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float attackRandomness = 0.5f;

        [Header("AI Settings")]
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private float chaseSpeed = 4f;
      /// <summary>
      ///  [SerializeField] private Vector3[] patrolPoints = new Vector3[0];
      /// </summary>

       
        [Header("Police / Initial Aggression")]
[Tooltip("Enable for NPCs that should be aggressive from the start (NPC_Police)...")]
[SerializeField] private bool startAggressive = false;

// NEW — lets PoliceManager identify which NPCs are police without new tags
public bool IsPolice => startAggressive;


[Header("Attack Detection")]
        [SerializeField] private AttackDetection leftPunchCollider;
        [SerializeField] private AttackDetection rightPunchCollider;
        [SerializeField] private AttackDetection leftKickCollider;
        [SerializeField] private AttackDetection rightKickCollider;

        [Header("References")]
        [SerializeField] private Transform playerTransform;

        // Components
        private Animator animator;
        private NavMeshAgent navMeshAgent;
        private HealthSystem healthSystem;
        private NavMeshAgent agent;

        // Patrol
        public Transform waypoint;
        private int currentWaypointIndex = 0;
        [Header("Patrol Route")]
        public Transform route;
        private List<Transform> routeWaypoints = new List<Transform>();


        // AI State
        private enum AIState { Patrolling, Chasing, Attacking, Dead }
        private AIState currentState = AIState.Patrolling;
        private float lastAttackTime = -999f;
        private bool isAggressive = false;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            agent = navMeshAgent;
            healthSystem = GetComponent<HealthSystem>();

            if (healthSystem != null)
                healthSystem.OnDeath += OnDeath;

            // Auto-find player if not assigned in Inspector
            if (playerTransform == null)
            {
                var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
                if (tpc != null) playerTransform = tpc.transform;
            }

            ResetAIState();
        }

private void Start()
        {
        if (route != null)
        {
        routeWaypoints.Clear();

        foreach (Transform child in route)
        {
            routeWaypoints.Add(child);
        }
      if (routeWaypoints.Count > 0)
        {
        currentWaypointIndex = Random.Range(0, routeWaypoints.Count);
        }
        }
            // NPC_Police (or any NPC with startAggressive = true) skips patrolling
            // and immediately chases the player.
            if (startAggressive)
            {
                BecomeAggressive();
                Debug.Log(gameObject.name + " [NPC_Police] started in aggressive mode - chasing player immediately.");
            }
            else
            {
            SetAgentDestination();
            }
        }

        // ---- PUBLIC API ----

        /// <summary>
        /// Called by AttackDetection ONLY when this NPC actually receives damage.
        /// This is the sole trigger for NPC aggression.
        /// </summary>
public void BecomeAggressive()
        {
            if (healthSystem != null && healthSystem.IsDead()) return;

            isAggressive = true;
            currentState = AIState.Chasing;

            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.speed     = chaseSpeed;   // ensure chase speed is set immediately
            }

            Debug.Log(gameObject.name + " became aggressive - now chasing player!");
        }


        public void CalmDown()
{
    if (healthSystem != null && healthSystem.IsDead())
        return;

    isAggressive = false;
    currentState = AIState.Patrolling;

    if (navMeshAgent != null)
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.speed = patrolSpeed;
    }

    SetAgentDestination();

    Debug.Log(gameObject.name + " returned to patrol.");
}

        public void ResetAIState()
        {
            currentState = AIState.Patrolling;
            isAggressive = false;
            lastAttackTime = -999f;
            currentWaypointIndex = 0;
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true;
                navMeshAgent.isStopped = false;
            }

            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.ResetTrigger("Punch");
                animator.ResetTrigger("Kick");
                animator.ResetTrigger("Death");
            }
        }

        // ---- UPDATE ----

        private void Update()
        {
            if (healthSystem == null || healthSystem.IsDead()) return;

            switch (currentState)
            {
                case AIState.Patrolling: PatrolBehavior(); break;
                case AIState.Chasing:   ChaseBehavior();  break;
                case AIState.Attacking: AttackBehavior(); break;
            }
        }

        // ---- PATROL ----

        private void PatrolBehavior()
        {
        PatrolWithWaypoints();
        }

        private void PatrolWithWaypoints()
        {
            if (waypoint == null)
                SetAgentDestination();

            if (waypoint == null) return;

            float dist = Vector3.Distance(transform.position, waypoint.position);
            if (dist <= 2f)
                SetAgentDestination();
            else
            {
                navMeshAgent.SetDestination(waypoint.position);
                animator.SetFloat("Speed", patrolSpeed);
            }
        }


        // ---- CHASE ----

private void ChaseBehavior()
        {
            if (playerTransform == null || healthSystem.IsDead())
            {
                currentState = AIState.Patrolling;
                isAggressive = false;
                return;
            }

            
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist > detectionRange * 1.5f)
{
    // NEW: police officers stay committed to the chase while the wanted
    // level is still active, even if they briefly lose direct range.
    bool policePersist = startAggressive
                          && WantedSystem.Instance != null
                          && WantedSystem.Instance.GetWantedLevel() > 0;

    if (!policePersist)
    {
        currentState = AIState.Patrolling;
        isAggressive = false;
        navMeshAgent.isStopped = false;
        navMeshAgent.velocity = Vector3.zero;
        return;
    }
    // else: fall through, keep chasing toward the player's position
}


            if (dist < attackRange)
            {
                currentState = AIState.Attacking;
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero;
                animator.SetFloat("Speed", 0f);
                return;
            }

            navMeshAgent.isStopped = false;
            navMeshAgent.speed = chaseSpeed;
            navMeshAgent.SetDestination(playerTransform.position);
            animator.SetFloat("Speed", chaseSpeed);
        }

        // ---- ATTACK ----

private void AttackBehavior()
        {
            if (playerTransform == null || healthSystem.IsDead())
            {
                currentState = AIState.Patrolling;
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            // Player moved out of attack range - resume chase
            if (dist > attackRange * 1.5f)
            {
                navMeshAgent.isStopped = false;  // FIX: resume agent when leaving attack state
                navMeshAgent.speed = chaseSpeed;
                currentState = AIState.Chasing;
                return;
            }

            // Keep NavMesh stopped while in melee range
            if (navMeshAgent.enabled && !navMeshAgent.isStopped)
                navMeshAgent.isStopped = true;

            // Face the player
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                                                       Quaternion.LookRotation(dir),
                                                       Time.deltaTime * 10f);

            // Attack on cooldown
            if ((Time.time - lastAttackTime) >= attackCooldown)
            {
                if (Random.value < attackRandomness)
                    PerformPunch();
                else
                    PerformKick();
            }
        }

        private void PerformPunch()
        {
            lastAttackTime = Time.time;
            animator.SetTrigger("Punch");
            Debug.Log("[NPCCombatAI] " + gameObject.name + " PerformPunch called (Punch trigger set)");
        }

        private void PerformKick()
        {
            lastAttackTime = Time.time;
            animator.SetTrigger("Kick");
            Debug.Log("[NPCCombatAI] " + gameObject.name + " PerformKick called (Kick trigger set)");
        }

        // ---- ANIMATION EVENTS ----

        public void OnPunchStart()
        {
            Debug.Log("[NPCCombatAI] " + gameObject.name + " OnPunchStart event received");
            if (leftPunchCollider  != null) { leftPunchCollider.SetDamage(punchDamage);  leftPunchCollider.StartAttack(); }
            if (rightPunchCollider != null) { rightPunchCollider.SetDamage(punchDamage); rightPunchCollider.StartAttack(); }
        }

        public void OnPunchEnd()
        {
            Debug.Log("[NPCCombatAI] " + gameObject.name + " OnPunchEnd event received");
            if (leftPunchCollider  != null) leftPunchCollider.StopAttack();
            if (rightPunchCollider != null) rightPunchCollider.StopAttack();
        }

        public void OnKickStart()
        {
          ////  Debug.Log("[NPCCombatAI] " + gameObject.name + " OnKickStart event received");
            if (leftKickCollider  != null) { leftKickCollider.SetDamage(kickDamage);  leftKickCollider.StartAttack(); }
            if (rightKickCollider != null) { rightKickCollider.SetDamage(kickDamage); rightKickCollider.StartAttack(); }
        }

        public void OnKickEnd()
        {
          ////  Debug.Log("[NPCCombatAI] " + gameObject.name + " OnKickEnd event received");
            if (leftKickCollider  != null) leftKickCollider.StopAttack();
            if (rightKickCollider != null) rightKickCollider.StopAttack();
        }

        // ---- DEATH ----

private void OnDeath()
{
    currentState = AIState.Dead;

    if (navMeshAgent != null)
    {
        navMeshAgent.isStopped = true;
        navMeshAgent.enabled = false;
    }

    // NEW: corpse ko physically "solid" mat rehne do warna player usay push/slide kar sakta hai
    Collider col = GetComponent<Collider>();
    if (col != null) col.enabled = false;

    SnapToGround();
}
[SerializeField] private LayerMask groundLayer;  // Inspector se "Ground" layer assign karo

private void SnapToGround()
{
    Vector3 origin = transform.position + Vector3.up * 2f;
    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, groundLayer, QueryTriggerInteraction.Ignore))
    {
        transform.position = hit.point;
    }
}

        private void OnDestroy()
        {
            if (healthSystem != null)
                healthSystem.OnDeath -= OnDeath;
        }

        // ---- WAYPOINTS ----

     private void SetAgentDestination()
{
    if (routeWaypoints.Count == 0)
        return;

    // Agar last waypoint aa gaya to dobara pehle se start karo
    if (currentWaypointIndex >= routeWaypoints.Count)
        currentWaypointIndex = 0;

    waypoint = routeWaypoints[currentWaypointIndex];

    if (agent != null && agent.enabled)
    {
        agent.SetDestination(waypoint.position);
    }

    // Next waypoint ke liye index badhao
    currentWaypointIndex++;
}
    }
}
