using UnityEngine;
using StarterAssets;

namespace CombatSystem
{
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private float punchDamage = 10f;
        [SerializeField] private float kickDamage  = 15f;
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private float maxAttackWindow = 1.5f;

        [Header("Attack Detection")]
        [SerializeField] private AttackDetection leftPunchCollider;
        [SerializeField] private AttackDetection rightPunchCollider;
        [SerializeField] private AttackDetection leftKickCollider;
        [SerializeField] private AttackDetection rightKickCollider;

        private Animator animator;
        private StarterAssetsInputs input;
        private HealthSystem healthSystem;

        private float lastAttackTime = -1f;
        private float attackWindowOpenedAt = -1f;
        private bool isAttacking = false;
        private bool punchArmed = false;
        private bool kickArmed  = false;

        private void Awake()
        {
            animator     = GetComponent<Animator>();
            input        = GetComponent<StarterAssetsInputs>();
            healthSystem = GetComponent<HealthSystem>();
        }

        private void Update()
        {
            if (healthSystem != null && healthSystem.IsDead()) return;

            // Safety disarm if animation event OnEnd was never called
            if ((punchArmed || kickArmed) && Time.time - attackWindowOpenedAt > maxAttackWindow)
            {
                Debug.LogWarning("[PlayerCombat] Safety-disarming - animation end event missed.");
                ForceDisarmAll();
            }

            if (input.punch && CanAttack()) Punch();
            if (input.kick  && CanAttack()) Kick();
        }

        private bool CanAttack() => (Time.time - lastAttackTime) >= attackCooldown;

        private void Punch()
        {
            lastAttackTime = Time.time;
            isAttacking    = true;
            animator.SetTrigger("Punch");
            Debug.Log("[PlayerCombat] Punch triggered");
        }

        private void Kick()
        {
            lastAttackTime = Time.time;
            isAttacking    = true;
            animator.SetTrigger("Kick");
            Debug.Log("[PlayerCombat] Kick triggered");
        }

        public void OnPunchStart()
        {
            punchArmed = true;
            attackWindowOpenedAt = Time.time;
            if (leftPunchCollider  != null) { leftPunchCollider.SetDamage(punchDamage);  leftPunchCollider.StartAttack(); }
            if (rightPunchCollider != null) { rightPunchCollider.SetDamage(punchDamage); rightPunchCollider.StartAttack(); }
            Debug.Log("[PlayerCombat] OnPunchStart fired");
        }

        public void OnPunchEnd()
        {
            punchArmed  = false;
            isAttacking = false;
            if (leftPunchCollider  != null) leftPunchCollider.StopAttack();
            if (rightPunchCollider != null) rightPunchCollider.StopAttack();
            Debug.Log("[PlayerCombat] OnPunchEnd fired");
        }

        public void OnKickStart()
        {
            kickArmed = true;
            attackWindowOpenedAt = Time.time;
            if (leftKickCollider  != null) { leftKickCollider.SetDamage(kickDamage);  leftKickCollider.StartAttack(); }
            if (rightKickCollider != null) { rightKickCollider.SetDamage(kickDamage); rightKickCollider.StartAttack(); }
            Debug.Log("[PlayerCombat] OnKickStart fired");
        }

        public void OnKickEnd()
        {
            kickArmed   = false;
            isAttacking = false;
            if (leftKickCollider  != null) leftKickCollider.StopAttack();
            if (rightKickCollider != null) rightKickCollider.StopAttack();
            Debug.Log("[PlayerCombat] OnKickEnd fired");
        }

        private void ForceDisarmAll()
        {
            punchArmed = kickArmed = isAttacking = false;
            if (leftPunchCollider  != null) leftPunchCollider.StopAttack();
            if (rightPunchCollider != null) rightPunchCollider.StopAttack();
            if (leftKickCollider   != null) leftKickCollider.StopAttack();
            if (rightKickCollider  != null) rightKickCollider.StopAttack();
        }

        public bool IsAttacking() => isAttacking;
    }
}
