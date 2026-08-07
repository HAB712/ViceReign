using UnityEngine;
using UnityEngine.SceneManagement;

namespace CombatSystem
{
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Settings")]

        [HideInInspector] public GameObject lastAttacker;

        [SerializeField] private float maxHealth = 100f;

        [Header("Death Settings")]
        [SerializeField] private bool isPlayer = false;
        [SerializeField] private float deathDisableDelay = 3f;

        [Header("Player Death UI (Player only)")]
        [Tooltip("Panel shown 'deathDisableDelay' seconds after the player dies (e.g. 'You Died' screen). Leave empty for NPCs.")]
        [SerializeField] private GameObject gameOverPanel;

        [Tooltip("Panel shown 'mainMenuDelay' seconds AFTER the Game Over panel appears (e.g. Main Menu). Leave empty to skip this step.")]
        [SerializeField] private GameObject mainMenuPanel;

        [Tooltip("Seconds to wait after the Game Over panel appears before showing the Main Menu panel.")]
        [SerializeField] private float mainMenuDelay = 3f;

        [Tooltip("Fires whenever health changes (damage/heal). Use for UI updates.")]
        public float lastHealthChangeTime = -1f;

        // Runtime state
        private float currentHealth;
        private bool isDead = false;

        // Components (auto-resolved)
        private Animator animator;

        
        private CharacterController characterController;

        // Reward settings
        [Header("Reward")]
        [SerializeField] private bool giveReward = true;
        [SerializeField] private int rewardMoney = 100;

        // Events
        public delegate void HealthChangedDelegate(float currentHealth, float maxHealth);
        public event HealthChangedDelegate OnHealthChanged;

        public delegate void DeathDelegate();
        public event DeathDelegate OnDeath;

        private void Awake()
        {
           // animator = GetComponent<Animator>();
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            characterController = GetComponent<CharacterController>();
            currentHealth = maxHealth;
        }

        // ---- PUBLIC API ----

        public void TakeDamage(float damageAmount)
        {
            if (isDead) return;

            currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0f, maxHealth);
            lastHealthChangeTime = Time.time;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

           // Debug.Log(gameObject.name + " took " + damageAmount + " damage. HP: " + currentHealth + "/" + maxHealth);

            NPCCivilianAI civilian = GetComponent<NPCCivilianAI>();

            if(civilian != null)
            {
             civilian.RunAway();
            }

            if (currentHealth <= 0f)
                Die();
        }

        public void Heal(float healAmount)
        {
            if (isDead) return;
            currentHealth = Mathf.Clamp(currentHealth + healAmount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            isDead = false;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        // ---- GETTERS ----

        public float GetCurrentHealth() => currentHealth;
        public float GetMaxHealth()     => maxHealth;
        public float GetHealthPercent() => currentHealth / maxHealth;
        public bool  IsDead()           => isDead;

        // ---- DEATH ----

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log(gameObject.name + " died.");

            // Wanted Crime Detection
if (lastAttacker != null && lastAttacker.CompareTag("Player"))
{
    if (GetComponent<NPCCombatAI>() != null)
    {
        WantedSystem.Instance.AddWantedLevel(3);
       /// Debug.Log("Crime: Police Killed (+3 Stars)");
    }
    else if (GetComponent<NPCCivilianAI>() != null)
    {
        WantedSystem.Instance.AddWantedLevel(2);
       /// Debug.Log("Crime: Civilian Killed (+2 Stars)");
    }
}

            // Play death animation
            // NOTE: your Animator Controller's "AnyState -> Death" transition uses the
            // bool parameter "IsDead" (not a Trigger called "Death"), so we must set that.
            if (animator != null)
{
    Debug.Log("Death animator found, firing trigger");
    animator.SetBool("IsDead", true);
    animator.SetTrigger("Death");   // NEW — NPC animator's Death transition needs this trigger
}else Debug.LogError("Animator NULL on " + gameObject.name);

            // Fire death event (NPCCombatAI listens to this)
            OnDeath?.Invoke();

            if (isPlayer)
            {
                // Player death: disable movement and combat, do NOT destroy
                if (characterController != null)
                    characterController.enabled = false;

                // Disable ThirdPersonController so input is ignored
                var tpc = GetComponent<StarterAssets.ThirdPersonController>();
                if (tpc != null) tpc.enabled = false;

                // Disable PlayerCombat so no more attacks
                var pc = GetComponent<PlayerCombat>();
                if (pc != null) pc.enabled = false;


                if (UIManager.Instance != null)
{
    UIManager.Instance.playerDied = true;
}
                // Show the Game Over / Main Menu panel after the death animation has had time to play
                if (gameOverPanel != null)
                    Invoke(nameof(ShowGameOverPanel), deathDisableDelay);
            }
            else
            {
                // NPC death: disable self, destroy after delay
                enabled = false;
                if (characterController != null)
                    characterController.enabled = false;

                  // Give reward to player if applicable  
                if (giveReward && MoneyManager.instance != null)
                    {

                    MoneyManager.instance.AddMoney(rewardMoney);
                    if (RewardPopup.Instance != null)
                    {
                        RewardPopup.Instance.Show("+Rs. " + rewardMoney);
                    }
                } 

                Destroy(gameObject, deathDisableDelay);
            }
        }

        /// <summary>Called via Invoke() a few seconds after the player dies.</summary>
        private void ShowGameOverPanel()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            // Chain to the main menu panel after an additional delay
            if (mainMenuPanel != null)
                Invoke(nameof(ShowMainMenuPanel), mainMenuDelay);
        }

        /// <summary>Called via Invoke() after the Game Over panel has been shown for a bit.</summary>
      [Header("Main Menu Scene")]
[Tooltip("Agar Main Menu ek alag scene hai to yahan scene ka naam daalo.")]
[SerializeField] private string mainMenuSceneName = "MainMenu";

private void ShowMainMenuPanel()
{
    if (gameOverPanel != null)
        gameOverPanel.SetActive(false);

    Time.timeScale = 1f; // scene load se pehle timescale normal karo, warna naye scene mein bhi sab freeze rahega

    if (UIManager.Instance != null)
    {
        UIManager.Instance.isGameStart = false;
    }

    SceneManager.LoadScene(mainMenuSceneName);
}
    }
}