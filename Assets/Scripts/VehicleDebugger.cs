using UnityEngine;
using TMPro;

/// <summary>
/// Diagnostic tool to debug vehicle system issues
/// Attach to the Car and check the console for debug info
/// </summary>
public class VehicleDebugger : MonoBehaviour
{
    private VehicleController vehicleController;
    private PrometeoCarController prometeoController;
    private VehicleInteraction vehicleInteraction;

    void Start()
    {
        Debug.Log("===== VEHICLE SYSTEM DEBUG =====");
        
        vehicleController = GetComponent<VehicleController>();
        prometeoController = GetComponent<PrometeoCarController>();
        vehicleInteraction = GetComponent<VehicleInteraction>();

        // Check Vehicle Controller
        Debug.Log($"✓ VehicleController found: {vehicleController != null}");
        if (vehicleController != null)
        {
            Debug.Log($"  - Exit Point assigned: {vehicleController.exitPoint != null}");
            Debug.Log($"  - Vehicle Rigidbody assigned: {vehicleController.vehicleRigidbody != null}");
            Debug.Log($"  - Main Camera assigned: {vehicleController.mainCamera != null}");
            Debug.Log($"  - Fuel: {vehicleController.GetFuel()}");
        }

        // Check Prometeo Car Controller (driving physics)
        Debug.Log($"✓ PrometeoCarController found: {prometeoController != null}");
        if (prometeoController != null)
        {
            Debug.Log($"  - Front Left Collider assigned: {prometeoController.frontLeftCollider != null}");
            Debug.Log($"  - Front Right Collider assigned: {prometeoController.frontRightCollider != null}");
            Debug.Log($"  - Rear Left Collider assigned: {prometeoController.rearLeftCollider != null}");
            Debug.Log($"  - Rear Right Collider assigned: {prometeoController.rearRightCollider != null}");
            Debug.Log($"  - Enabled (should be false until entered): {prometeoController.enabled}");
        }

        // Check Vehicle Interaction
        Debug.Log($"✓ VehicleInteraction found: {vehicleInteraction != null}");
        if (vehicleInteraction != null)
        {
            Debug.Log($"  - Interaction Range: {vehicleInteraction.interactionRange}");
            Debug.Log($"  - Interaction Prompt assigned: {vehicleInteraction.interactionPrompt != null}");
        }

        // Check Player
        Debug.Log("✓ Searching for player...");
        GameObject player = GameObject.Find("PlayerArmature");
        if (player == null)
        {
            Debug.LogWarning("  ✗ PlayerArmature not found by name!");
            // Search ALL objects including inactive
            foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
            {
                if (go.CompareTag("Player"))
                {
                    player = go;
                    Debug.LogWarning($"  ⚠ Found by tag but may be inactive!");
                    break;
                }
            }
            
            if (player == null)
            {
                // Try active search only
                GameObject[] playerTagged = GameObject.FindGameObjectsWithTag("Player");
                Debug.Log($"  - Active objects with 'Player' tag: {playerTagged.Length}");
                foreach (GameObject go in playerTagged)
                {
                    Debug.Log($"    • {go.name}");
                }
            }
        }
        
        if (player != null)
        {
            Debug.Log($"  ✓ Found: {player.name}");
            Debug.Log($"    - Active in Hierarchy: {player.activeInHierarchy}");
            Debug.Log($"    - Active Self: {player.activeSelf}");
            Debug.Log($"    - Tag: {player.tag}");
            Debug.Log($"    - Animator: {player.GetComponent<Animator>() != null}");
            Debug.Log($"    - CharacterController: {player.GetComponent<CharacterController>() != null}");
            Debug.Log($"    - Rigidbody: {player.GetComponent<Rigidbody>() != null}");
            Debug.Log($"    - Collider: {player.GetComponent<Collider>() != null}");
        }
        else
        {
            Debug.LogError("  ✗✗ PLAYER NOT FOUND AT ALL! Check the tag spelling!");
        }

        Debug.Log("===== END DEBUG =====");
    }
}