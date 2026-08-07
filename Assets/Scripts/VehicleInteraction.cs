using UnityEngine;
using TMPro;

public class VehicleInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 5f;
    public KeyCode enterKey = KeyCode.E;
    public TextMeshProUGUI interactionPrompt;
    
    [Header("Manual Player Assignment (Fallback)")]
    public Transform manualPlayerTransform;

    private VehicleController vehicleController;
    private Transform player;
    private Animator playerAnimator;

    void Start()
    {
        vehicleController = GetComponent<VehicleController>();
        
        // Check if player was manually assigned in inspector
        if (manualPlayerTransform != null)
        {
            player = manualPlayerTransform;
            playerAnimator = player.GetComponent<Animator>();
            Debug.Log($"[VehicleInteraction] ✓ Using manually assigned player: {player.name}");
            return;
        }
        
        // Find player - try multiple strategies
        GameObject playerGO = null;
        
        // Strategy 1: Find PlayerArmature by name (even if inactive)
        playerGO = GameObject.Find("PlayerArmature");
        if (playerGO != null)
        {
            Debug.Log($"[VehicleInteraction] Found player by name: {playerGO.name} (Active: {playerGO.activeInHierarchy})");
        }
        
        // Strategy 2: Find the first object tagged "Player" (only searches active)
        if (playerGO == null)
        {
            playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                Debug.Log($"[VehicleInteraction] Found player by tag: {playerGO.name}");
        }
        
        // Strategy 3: Search through ALL objects including inactive (fallback)
        if (playerGO == null)
        {
            foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
            {
                if (go.CompareTag("Player"))
                {
                    playerGO = go;
                    Debug.LogWarning($"[VehicleInteraction] Found player by full search: {playerGO.name} (was inactive!)");
                    break;
                }
            }
        }
        
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerAnimator = playerGO.GetComponent<Animator>();
            Debug.Log($"[VehicleInteraction] ✓ SUCCESS! Player assigned: {playerGO.name}");
            if (playerAnimator == null)
                Debug.LogWarning($"[VehicleInteraction] Warning: Player {playerGO.name} has no Animator component!");
        }
        else
        {
            Debug.LogError("[VehicleInteraction] ✗ FAILED to find player!");
            Debug.LogError("   FIX: Drag your PlayerArmature into the 'Manual Player Transform' field in the Inspector!");
            Debug.LogError("   Or verify: 1) Player is tagged 'Player', 2) Player is active in hierarchy");
        }
        
        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null || vehicleController == null)
        {
            if (player == null && vehicleController != null)
                Debug.LogWarning("[VehicleInteraction] Player not found!", this);
            return;
        }
        
        float dist = Vector3.Distance(transform.position, player.position);
        bool isVehicleOccupied = vehicleController.IsOccupied();
        bool inRange = dist <= interactionRange && !isVehicleOccupied;
        
        if (inRange)
        {
            if (interactionPrompt != null) 
                interactionPrompt.gameObject.SetActive(true);
            
            if (Input.GetKeyDown(enterKey))
            {
                Debug.Log($"[VehicleInteraction] Attempting to enter vehicle. Distance: {dist:F2}");
                vehicleController.EnterVehicle(player, playerAnimator);
                if (interactionPrompt != null) 
                    interactionPrompt.gameObject.SetActive(false);
            }
        }
        else
        {
            if (interactionPrompt != null) 
                interactionPrompt.gameObject.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}