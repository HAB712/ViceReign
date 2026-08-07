using UnityEngine;
using UnityEngine.UI;
using CombatSystem;
using StarterAssets;

namespace Diagnostics
{
    /// <summary>
    /// Diagnostic tool to verify Player Health System setup.
    /// Attach to an empty GameObject and check console output.
    /// </summary>
    public class HealthSystemDiagnostics : MonoBehaviour
    {
        [SerializeField] private bool runDiagnosticsOnStart = true;

        private void Start()
        {
            if (runDiagnosticsOnStart)
                RunFullDiagnostics();
        }

        public void RunFullDiagnostics()
        {
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  🏥 PLAYER HEALTH SYSTEM DIAGNOSTICS");
            Debug.Log("═══════════════════════════════════════════════════════════");

            DiagnosePlayer();
            DiagnoseHealthUI();
            DiagnoseAttackDetection();
            DiagnoseCombatSetup();

            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  ✓ Diagnostics Complete");
            Debug.Log("═══════════════════════════════════════════════════════════");
        }

        private void DiagnosePlayer()
        {
            Debug.Log("\n📍 PLAYER SETUP:");
            Debug.Log("─────────────────────────────────────────────────────────");

            // Find player
            var playerTPC = FindObjectOfType<ThirdPersonController>();
            if (playerTPC == null)
            {
                Debug.LogError("  ❌ Player ThirdPersonController NOT FOUND!");
                return;
            }
            Debug.Log("  ✓ Player found: " + playerTPC.gameObject.name);

            // Check HealthSystem
            var playerHealth = playerTPC.GetComponent<HealthSystem>();
            if (playerHealth == null)
            {
                Debug.LogError("  ❌ HealthSystem NOT on Player!");
                return;
            }
            Debug.Log("  ✓ HealthSystem found on player");

            // Check isPlayer flag
            if (playerHealth.GetType().GetField("isPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
            {
                var field = playerHealth.GetType().GetField("isPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bool isPlayer = (bool)field.GetValue(playerHealth);
                
                if (isPlayer)
                    Debug.Log("  ✓ isPlayer = TRUE ✓ (Correct!)");
                else
                    Debug.LogWarning("  ⚠️  isPlayer = FALSE (Should be TRUE for player!)");
            }

            Debug.Log($"  ✓ Max Health: {playerHealth.GetMaxHealth()}");
            Debug.Log($"  ✓ Current Health: {playerHealth.GetCurrentHealth()}");
            Debug.Log($"  ✓ Health Percent: {playerHealth.GetHealthPercent():P0}");
            Debug.Log($"  ✓ Is Dead: {playerHealth.IsDead()}");
        }

        private void DiagnoseHealthUI()
        {
            Debug.Log("\n🎨 HEALTH UI SETUP:");
            Debug.Log("─────────────────────────────────────────────────────────");

            // Find Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("  ❌ Canvas NOT FOUND!");
                return;
            }
            Debug.Log("  ✓ Canvas found: " + canvas.gameObject.name);

            // Find Slider
            var slider = GameObject.Find("Canvas/GameUI/Health");
            if (slider == null)
            {
                Debug.LogWarning("  ⚠️  Slider not found at 'Canvas/GameUI/Health'");
                // Try to find any slider
                slider = FindObjectOfType<Slider>()?.gameObject;
                if (slider == null)
                {
                    Debug.LogError("  ❌ No Slider found in scene!");
                    return;
                }
                Debug.Log("  ⚠️  Found slider at: " + GetGameObjectPath(slider));
            }
            else
            {
                Debug.Log("  ✓ Slider found at: Canvas/GameUI/Health");
            }

            var sliderComponent = slider.GetComponent<Slider>();
            if (sliderComponent != null)
            {
                Debug.Log($"  ✓ Slider minValue: {sliderComponent.minValue}");
                Debug.Log($"  ✓ Slider maxValue: {sliderComponent.maxValue}");
                Debug.Log($"  ✓ Slider current value: {sliderComponent.value}");
            }

            // Find Bar Image
            Transform bar = slider.transform.Find("Bar");
            if (bar != null)
            {
                var fillImage = bar.GetComponent<Image>();
                if (fillImage != null)
                    Debug.Log("  ✓ Bar Image found and configured");
                else
                    Debug.LogWarning("  ⚠️  Bar exists but has no Image component");
            }
            else
            {
                Debug.LogWarning("  ⚠️  Bar child not found on slider");
            }

            // Find Health Text
            var textObj = GameObject.Find("Canvas/GameUI/HealthText");
            if (textObj != null)
            {
                Debug.Log("  ✓ HealthText found: " + textObj.name);
            }
            else
            {
                Debug.LogWarning("  ⚠️  HealthText not found at 'Canvas/GameUI/HealthText'");
            }

            // Find HealthUIDisplay
            var healthUIDisplay = FindObjectOfType<UI.HealthUIDisplay>();
            if (healthUIDisplay != null)
            {
                Debug.Log("  ✓ HealthUIDisplay component found");
            }
            else
            {
                Debug.LogWarning("  ⚠️  HealthUIDisplay component NOT found in scene!");
                Debug.LogWarning("     Add HealthUIDisplay script to: GameUI or Canvas");
            }
        }

        private void DiagnoseAttackDetection()
        {
            Debug.Log("\n⚔️  ATTACK DETECTION SETUP:");
            Debug.Log("─────────────────────────────────────────────────────────");

            var detectors = FindObjectsOfType<AttackDetection>();
            Debug.Log($"  ✓ Found {detectors.Length} AttackDetection components");

            foreach (var detector in detectors)
            {
                Debug.Log($"    - {detector.gameObject.name} (Owner: {detector.transform.root.gameObject.name})");

                var collider = detector.GetComponent<SphereCollider>();
                if (collider != null)
                {
                    if (collider.isTrigger)
                        Debug.Log($"      ✓ Collider is Trigger: YES ✓");
                    else
                        Debug.LogWarning($"      ❌ Collider is Trigger: NO (Should be Trigger!)");

                    Debug.Log($"      ✓ Radius: {collider.radius}");
                }
                else
                {
                    Debug.LogError($"      ❌ No SphereCollider on {detector.gameObject.name}!");
                }
            }
        }

        private void DiagnoseCombatSetup()
        {
            Debug.Log("\n⚡ COMBAT SETUP:");
            Debug.Log("─────────────────────────────────────────────────────────");

            var playerCombat = FindObjectOfType<PlayerCombat>();
            if (playerCombat != null)
            {
                Debug.Log("  ✓ PlayerCombat found");
            }
            else
            {
                Debug.LogWarning("  ⚠️  PlayerCombat NOT found");
            }

            var npcCombats = FindObjectsOfType<NPCCombatAI>();
            Debug.Log($"  ✓ Found {npcCombats.Length} NPCCombatAI components");

            foreach (var npc in npcCombats)
            {
                var health = npc.GetComponent<HealthSystem>();
                if (health != null)
                {
                    Debug.Log($"    - {npc.gameObject.name}: Health={health.GetCurrentHealth()}/{health.GetMaxHealth()}");
                }
            }
        }

        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.gameObject.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        // Test methods for manual debugging
        [ContextMenu("Test: Damage Player 10 HP")]
        public void TestDamagePlayer()
        {
            var playerHealth = FindObjectOfType<ThirdPersonController>()?.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(10);
                Debug.Log("✓ Test: Damaged player 10 HP");
            }
            else
            {
                Debug.LogError("❌ Player HealthSystem not found!");
            }
        }

        [ContextMenu("Test: Heal Player 20 HP")]
        public void TestHealPlayer()
        {
            var playerHealth = FindObjectOfType<ThirdPersonController>()?.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.Heal(20);
                Debug.Log("✓ Test: Healed player 20 HP");
            }
            else
            {
                Debug.LogError("❌ Player HealthSystem not found!");
            }
        }

        [ContextMenu("Test: Reset Player Health")]
        public void TestResetHealth()
        {
            var playerHealth = FindObjectOfType<ThirdPersonController>()?.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
                Debug.Log("✓ Test: Player health reset to full");
            }
            else
            {
                Debug.LogError("❌ Player HealthSystem not found!");
            }
        }
    }
}
