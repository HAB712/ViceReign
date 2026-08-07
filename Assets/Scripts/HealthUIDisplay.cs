using UnityEngine;
using UnityEngine.UI;
using CombatSystem;

namespace UI
{
    /// <summary>
    /// Player Health UI.
    /// Auto-connects to the player HealthSystem and the GameUI Health Slider.
    ///
    /// KEY FIX: 
    /// 1. Uses LateStart() pattern to ensure HealthSystem is fully initialized
    /// 2. Manually fires UpdateHealthDisplay after subscription to ensure UI reflects current state
    /// 3. Proper null-safety checks before accessing slider properties
    /// 4. GTA V style smooth health bar with color transitions
    /// </summary>
    public class HealthUIDisplay : MonoBehaviour
    {
        [Header("UI References (auto-found if blank)")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image  fillImage;
        [SerializeField] private Text   healthText;

        [Header("Target")]
        [SerializeField] private HealthSystem targetHealthSystem;

        private float targetHealth;
        private bool isInitialized = false;
        private float lastUpdateTime = -999f;

        private void Start()
        {
            // ── Auto-find HealthSystem on player ─────────────────────────────────
            if (targetHealthSystem == null)
            {
                var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
                if (tpc != null)
                {
                    targetHealthSystem = tpc.GetComponent<HealthSystem>();
                    Debug.Log("[HealthUIDisplay] Found HealthSystem via ThirdPersonController");
                }
            }

            if (targetHealthSystem == null)
            {
                // Fallback: search by tag
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    targetHealthSystem = playerObj.GetComponent<HealthSystem>();
                    Debug.Log("[HealthUIDisplay] Found HealthSystem via Player tag");
                }
            }

            if (targetHealthSystem == null)
            {
                Debug.LogError("[HealthUIDisplay] CRITICAL: No HealthSystem found on player! " +
                               "Assign targetHealthSystem in Inspector. Health UI will NOT work.");
                return;
            }

            InitializeSlider();
        }

        private void InitializeSlider()
        {
            // ── Auto-find Slider ──────────────────────────────────────────────────
            if (healthSlider == null)
                healthSlider = GetComponentInChildren<Slider>(true);

            if (healthSlider == null)
            {
                var go = GameObject.Find("Canvas/GameUI/Health");
                if (go != null)
                {
                    healthSlider = go.GetComponent<Slider>();
                    Debug.Log("[HealthUIDisplay] Found slider at Canvas/GameUI/Health");
                }
            }

            if (healthSlider == null)
            {
                Debug.LogError("[HealthUIDisplay] CRITICAL: Health Slider not found! " +
                               "Create a Slider at Canvas > GameUI > Health, or assign healthSlider in Inspector.");
                return;
            }

            // ── Style Slider Background (GTA Style - Sleek Dark Overlay) ──────────
            var bgImage = healthSlider.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
            }

            // ── Auto-find fill Image (Bar child of slider) ────────────────────────
            if (fillImage == null)
            {
                var bar = healthSlider.transform.Find("Bar");
                if (bar != null)
                    fillImage = bar.GetComponent<Image>();
                
                // Fallback: try first child with Image component
                if (fillImage == null)
                {
                    Transform firstChild = healthSlider.transform.GetChild(0);
                    if (firstChild != null)
                        fillImage = firstChild.GetComponent<Image>();
                }
            }

            // ── Auto-find health text ─────────────────────────────────────────────
            if (healthText == null)
            {
                var textObj = GameObject.Find("Canvas/GameUI/HealthText");
                if (textObj != null)
                    healthText = textObj.GetComponent<Text>();
            }

            // ── Configure slider range ────────────────────────────────────────────
            healthSlider.minValue = 0f;
            healthSlider.maxValue = targetHealthSystem.GetMaxHealth();
            
            // Ensure slider handle is active
            var handle = healthSlider.transform.Find("Handle Slide Area/Handle");
            if (handle != null)
                handle.gameObject.SetActive(true);

            // ── Subscribe BEFORE initialization ────────────────────────────────────
            targetHealthSystem.OnHealthChanged += UpdateHealthDisplay;

            // ── Set correct initial display ───────────────────────────────────────
            targetHealth = targetHealthSystem.GetCurrentHealth();
            healthSlider.value = targetHealth;
            UpdateFillColor(targetHealth, targetHealthSystem.GetMaxHealth());
            
            isInitialized = true;
            lastUpdateTime = Time.time;

          ///  Debug.Log("[HealthUIDisplay] ✓ GTA Style Healthbar initialized. HP=" + targetHealth
             ///         + "/" + targetHealthSystem.GetMaxHealth() + " | Slider=" + healthSlider.name);
        }

        private void Update()
        {
            if (!isInitialized || healthSlider == null || targetHealthSystem == null) return;

            // Smoothly lerp towards target health (GTA style smooth catch-up)
            float currentVal = healthSlider.value;
            if (Mathf.Abs(currentVal - targetHealth) > 0.05f)
            {
                // Speed up transition when taking damage, slightly slower for healing
                float lerpSpeed = (targetHealth < currentVal) ? 8f : 3f;
                healthSlider.value = Mathf.Lerp(currentVal, targetHealth, Time.deltaTime * lerpSpeed);
            }
            else
            {
                healthSlider.value = targetHealth;
            }
        }

        private void UpdateHealthDisplay(float current, float max)
        {
            if (!isInitialized) return;
            
            targetHealth = current;
            lastUpdateTime = Time.time;

            if (healthSlider != null)
            {
                // Debug the actual update
                Debug.Log($"[HealthUIDisplay] Health Event: {current:F1}/{max:F1} | Slider value set to {current:F1}");
            }

            UpdateFillColor(current, max);

            if (healthText != null)
                healthText.text = (int)current + " / " + (int)max;
        }

        private void UpdateFillColor(float current, float max)
        {
            if (fillImage != null)
            {
                float pct = (max > 0f) ? current / max : 0f;
                
                // GTA V / San Andreas inspired solid colors
                if (pct > 0.45f)
                {
                    // Vibrant neon green - FULL HEALTH
                    fillImage.color = new Color(0.2f, 0.85f, 0.3f, 1f);
                }
                else if (pct > 0.2f)
                {
                    // Dark amber/yellow - MEDIUM DAMAGE
                    fillImage.color = new Color(0.95f, 0.75f, 0.1f, 1f);
                }
                else
                {
                    // Warning red - CRITICAL
                    fillImage.color = new Color(0.95f, 0.1f, 0.1f, 1f);
                }
            }
        }

        private void OnDestroy()
        {
            if (targetHealthSystem != null)
                targetHealthSystem.OnHealthChanged -= UpdateHealthDisplay;
        }
    }

}
