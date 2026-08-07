using System.Collections.Generic;
using UnityEngine;
using CombatSystem;

public class PoliceManager : MonoBehaviour
{
    public static PoliceManager Instance;

    [Header("Police Detection")]
[SerializeField] private Transform player;
[SerializeField] private float policeAlertRange = 25f;

    [Header("All Police NPCs")]
    public List<NPCCombatAI> policeNPCs = new List<NPCCombatAI>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
{
    if (player == null)
    {
        var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (tpc != null) player = tpc.transform;
    }

    // NEW: auto-register any police NPC in the scene so you don't have
    // to manually drag every officer into the Inspector list.
    NPCCombatAI[] allCombatants = FindObjectsOfType<NPCCombatAI>();
    foreach (var npc in allCombatants)
    {
        if (npc.IsPolice && !policeNPCs.Contains(npc))
            policeNPCs.Add(npc);
    }

    if (WantedSystem.Instance != null)
    {
        WantedSystem.Instance.OnWantedLevelChanged += OnWantedLevelChanged;
    }
}

    private void OnDestroy()
    {
        if (WantedSystem.Instance != null)
        {
            WantedSystem.Instance.OnWantedLevelChanged -= OnWantedLevelChanged;
        }
    }

   private void OnWantedLevelChanged(int level)
{
    Debug.Log("Wanted Level Changed: " + level);

    switch (level)
    {
        case 0:
            CalmAllPolice();
            break;

        case 1:
            ActivatePolice(25f);
            break;

        case 2:
            ActivatePolice(50f);
            break;

        case 3:
            ActivateAllPolice(); // NEW: every police NPC, no distance check
            break;
    }
}

// NEW
private void ActivateAllPolice()
{
    foreach (NPCCombatAI police in policeNPCs)
    {
        if (police != null)
            police.BecomeAggressive();
    }
    Debug.Log("Police Activated - ALL OFFICERS (3 Stars)");
}

   private void ActivatePolice(float range)
{
    if (player == null)
        return;

    foreach (NPCCombatAI police in policeNPCs)
    {
        if (police == null)
            continue;

        float distance = Vector3.Distance(player.position, police.transform.position);

        if (distance <= range)
        {
            police.BecomeAggressive();
        }
    }

    Debug.Log("Police Activated - Range : " + range);
}
public void CalmAllPolice()
{
    foreach (NPCCombatAI police in policeNPCs)
    {
        if (police != null)
        {
            police.CalmDown();
        }
    }

    Debug.Log("All Police Returned To Patrol");
}
}