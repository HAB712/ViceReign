using UnityEngine;
using CombatSystem;

public class NPCDebugger : MonoBehaviour
{
    private NPCCombatAI ai;
    private float lastLogTime = 0f;

    void Start()
    {
        ai = GetComponent<NPCCombatAI>();
        Debug.Log("[NPCDebugger] Started on " + gameObject.name);
    }

    void Update()
    {
        if (ai == null) return;

        if (Time.time - lastLogTime >= 1f)
        {
            lastLogTime = Time.time;
            
            // Use reflection to read the private currentState
            var type = ai.GetType();
            var stateField = type.GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var state = stateField != null ? stateField.GetValue(ai).ToString() : "UNKNOWN";

            var player = GameObject.Find("PlayerArmature");
            float dist = -1f;
            if (player != null)
            {
                dist = Vector3.Distance(transform.position, player.transform.position);
            }

            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool isStopped = agent != null ? agent.isStopped : false;

            // // Debug.Log(string.Format("[NPCDebugger] {0}: State={1}, Dist={2:F2}, AgentStopped={3}, isPlaying={4}", 
            // //    gameObject.name, state, dist, isStopped, Application.isPlaying));
        }
    }
}
