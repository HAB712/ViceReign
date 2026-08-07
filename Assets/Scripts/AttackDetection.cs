using System.Collections.Generic;
using UnityEngine;

namespace CombatSystem
{
    /// <summary>
    /// Detects attack hits using trigger colliders on hands/feet.
    /// 
    /// Uses two detection methods for reliability:
    ///   1. OnTriggerEnter  - fires when the collider physically overlaps something
    ///   2. OverlapSphere   - manual check every FixedUpdate while the hitbox is armed
    ///      (catches cases where the collider moves through a target too fast for physics events)
    ///
    /// Self-hits are prevented by comparing root GameObject instances (not tags).
    /// Multi-hits within the same swing are prevented via a per-swing HashSet.
    /// </summary>
    public class AttackDetection : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float damageAmount = 10f;

        [Header("Hit Detection")]
        [Tooltip("Multiplier applied to the sphere radius for the OverlapSphere sweep. 1.0 = exact collider size.")]
        [SerializeField] private float overlapRadiusMultiplier = 1.2f;

        // The root GameObject that owns this hitbox collider (resolved once in Awake)
        private GameObject ownerRoot;

        // Runtime state
        private bool canDamage = false;
        /// <summary>Read by NPCCombatAI to check if this collider was already armed by an animation event.</summary>
        public bool IsArmed => canDamage;

        private SphereCollider sphereCol;

        // Per-swing hit tracking (HashSet prevents multi-hits on same target)
        private readonly HashSet<HealthSystem> hitThisSwing = new HashSet<HealthSystem>();

        private void Awake()
        {
            sphereCol = GetComponent<SphereCollider>();
            if (sphereCol == null)
                Debug.LogError("[AttackDetection] " + gameObject.name + " has no SphereCollider!");

            // Resolve owner root once. Self-hit guard uses instance comparison (NOT tags),
            // so it works correctly regardless of how GameObjects are tagged.
            // e.g. NPC root tagged "Player" no longer causes NPC→Player hits to be skipped.
            ownerRoot = transform.root.gameObject;
            Debug.Log("[AttackDetection] " + gameObject.name + " owner root resolved to: '" + ownerRoot.name + "' (tag: " + ownerRoot.tag + ")");
        }

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>Called by animation event at the start of the attack impact window.</summary>
        public void StartAttack()
        {
            canDamage = true;
            hitThisSwing.Clear();
            Debug.Log("[AttackDetection] (" + ownerRoot.name + ") " + gameObject.name + " ARMED");
        }

        /// <summary>Called by animation event at the end of the attack impact window.</summary>
        public void StopAttack()
        {
            canDamage = false;
            hitThisSwing.Clear();
            Debug.Log("[AttackDetection] (" + ownerRoot.name + ") " + gameObject.name + " DISARMED");
        }

        /// <summary>Override damage amount (called per-attack by PlayerCombat / NPCCombatAI).</summary>
        public void SetDamage(float damage)
        {
            damageAmount = damage;
        }

        // ── Physics Trigger (primary) ────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!canDamage) return;
            TryHit(other.GetComponentInParent<HealthSystem>(), "OnTriggerEnter:" + other.name);
        }

        // ── OverlapSphere fallback (catches fast sweeps) ─────────────────────────────

        private void FixedUpdate()
        {
            if (!canDamage || sphereCol == null) return;

            float worldRadius = sphereCol.radius * overlapRadiusMultiplier
                                * Mathf.Max(transform.lossyScale.x,
                                            transform.lossyScale.y,
                                            transform.lossyScale.z);

            Vector3 worldCenter = transform.TransformPoint(sphereCol.center);

            Collider[] hits = Physics.OverlapSphere(worldCenter, worldRadius);
            Debug.Log("[AttackDetection] (" + ownerRoot.name + ") " + gameObject.name + " OverlapSphere found " + hits.Length + " colliders.");

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue; // skip self-collider
                HealthSystem hs = hit.GetComponentInParent<HealthSystem>();
                TryHit(hs, "OverlapSphere:" + hit.name);
            }
        }

        // ── Shared hit logic ─────────────────────────────────────────────────────────

        private void TryHit(HealthSystem targetHealth, string source)
        {
            if (targetHealth == null)
            {
                Debug.Log("[AttackDetection] " + gameObject.name + " overlap via " + source + " — no HealthSystem in parent chain.");
                return;
            }

            // ── SELF-HIT GUARD (instance-based, NOT tag-based) ──────────────────────
            if (targetHealth.transform.root.gameObject == ownerRoot)
            {
                Debug.Log("[AttackDetection] " + gameObject.name + " — skipping self-hit on " + targetHealth.gameObject.name);
                return;
            }

            // Already hit this target this swing
            if (hitThisSwing.Contains(targetHealth))
            {
                Debug.Log("[AttackDetection] " + gameObject.name + " — already hit " + targetHealth.gameObject.name + " this swing.");
                return;
            }

            // Dead targets
            if (targetHealth.IsDead())
            {
                Debug.Log("[AttackDetection] " + gameObject.name + " — target " + targetHealth.gameObject.name + " is already dead.");
                return;
            }

            hitThisSwing.Add(targetHealth);

            targetHealth.lastAttacker = ownerRoot;
            targetHealth.TakeDamage(damageAmount);

            Debug.Log("[AttackDetection] ✓ HIT! " + gameObject.name + " → " + targetHealth.gameObject.name
                      + " for " + damageAmount + " dmg  [via " + source + "]");

            // Trigger NPC aggression after confirmed damage (NPC hit by player)
            NPCCombatAI npcAI = targetHealth.GetComponent<NPCCombatAI>();
            if (npcAI != null && !targetHealth.IsDead())
                npcAI.BecomeAggressive();

            // Police assault crime — only when Player is the attacker
            if (ownerRoot.CompareTag("Player"))
            {
                NPCCombatAI police = targetHealth.GetComponent<NPCCombatAI>();
                if (police != null && !police.firstHitReported)
                {
                    police.firstHitReported = true;
                    if (WantedSystem.Instance != null)
                        WantedSystem.Instance.AddWantedLevel(1);
                }
            }
        }
    }
}