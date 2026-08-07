using UnityEngine;
using CombatSystem;

public class ClinicHeal : MonoBehaviour
{
    public int healCost = 100;

    private bool playerInside = false;
    private HealthSystem playerHealth;

    private void Update()
    {
        if (!playerInside)
            return;

        if (Input.GetKeyDown(KeyCode.H))
        {
            HealPlayer();
        }
    }

    private void HealPlayer()
    {
        if (playerHealth == null)
            return;

        // Already Full Health
        if (playerHealth.GetCurrentHealth() >= playerHealth.GetMaxHealth())
        {
            ClinicUI.Instance.ShowMessage("Health Already Full");
            return;
        }

        // Not Enough Money
        if (MoneyManager.instance.CurrentMoney < healCost)
        {
            ClinicUI.Instance.ShowMessage("Not Enough Money");
            return;
        }

        // Deduct Money
        MoneyManager.instance.RemoveMoney(healCost);

        // Heal To Full
        float healAmount = playerHealth.GetMaxHealth() - playerHealth.GetCurrentHealth();
        playerHealth.Heal(healAmount);

        ClinicUI.Instance.ShowMessage("Health Restored");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        playerHealth = other.GetComponent<HealthSystem>();

        ClinicUI.Instance.ShowPrompt("Press H To Heal\nCost : Rs100");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        playerHealth = null;

        ClinicUI.Instance.HidePrompt();
    }
}