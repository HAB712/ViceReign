using UnityEngine;
using System.Collections;
using CombatSystem;

public class NPCSpawner : MonoBehaviour
{
    public GameObject npcPrefab;
    public float respawnTime = 10f;

    private GameObject currentNPC;
    private HealthSystem currentNPCHealth;
    private Coroutine respawnCoroutine;

    void Start()
    {
        SpawnNPC();
    }

    void SpawnNPC()
    {
        currentNPC = Instantiate(
            npcPrefab,
            transform.position,
            transform.rotation
        );

        // Get and cache the health system
        currentNPCHealth = currentNPC.GetComponent<HealthSystem>();

        if (currentNPCHealth != null)
        {
            // Subscribe to death event
            currentNPCHealth.OnDeath += OnNPCDeath;
        }

        // Ensure all AI scripts are enabled and reset on the respawned NPC
        NPCCombatAI combatAI = currentNPC.GetComponent<NPCCombatAI>();
        if (combatAI != null)
        {
            combatAI.enabled = true;
            combatAI.ResetAIState();  // Reset AI state for respawn
        }
        

        Debug.Log("NPC Spawned and AI enabled");
    }

    void OnNPCDeath()
    {
        Debug.Log("NPC Death detected, scheduling respawn...");

        // Unsubscribe from the death event to avoid duplicate subscriptions
        if (currentNPCHealth != null)
        {
            currentNPCHealth.OnDeath -= OnNPCDeath;
        }

        // Stop any existing respawn coroutine
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
        }

        // Start new respawn coroutine
        respawnCoroutine = StartCoroutine(RespawnNPC());
    }

    IEnumerator RespawnNPC()
    {
        yield return new WaitForSeconds(respawnTime);

        // Only spawn if the previous NPC has been destroyed
        if (currentNPC == null)
        {
            Debug.Log("Respawning NPC now...");
            SpawnNPC();
        }
        else
        {
            Debug.LogWarning("Previous NPC still exists, skipping respawn");
        }
    }
}