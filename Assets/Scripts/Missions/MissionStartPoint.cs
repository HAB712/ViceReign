using UnityEngine;

/// <summary>
/// Green circular start marker for a mission. When the player stands inside its
/// trigger and the mission is Available, it shows a "Press M" prompt and starts
/// the mission on key press. Interaction is disabled once the mission completes.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionStartPoint : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Key the player presses to start the mission.")]
    public KeyCode startKey = KeyCode.M;
    [Tooltip("Prompt shown while the player is inside the marker.")]
    public string promptMessage = "Press M to start Lost Package mission";

    private bool playerInside = false;
    private bool interactable = true;

    private void Reset()
    {
        // Make sure the collider is a trigger when added in the editor.
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (!interactable) return;
        if (MissionManager.Instance == null) return;

        // 100% foolproof distance check instead of unreliable Unity physics triggers
        bool isInside = false;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Check distance (ignoring Y axis height differences so standing on top always works)
            Vector3 p1 = transform.position; p1.y = 0;
            Vector3 p2 = player.transform.position; p2.y = 0;
            if (Vector3.Distance(p1, p2) <= 5.0f)
            {
                isInside = true;
            }
        }

        // Handle Enter/Exit state changes
        if (isInside && !playerInside)
        {
            playerInside = true;
            if (MissionManager.Instance.GetState(MissionManager.MISSION_1) == MissionState.Available && MissionManager.Instance.ui != null)
            {
                MissionManager.Instance.ui.ShowStartPrompt(promptMessage);
            }
        }
        else if (!isInside && playerInside)
        {
            playerInside = false;
            if (MissionManager.Instance.ui != null)
            {
                MissionManager.Instance.ui.HideStartPrompt();
            }
        }

        // Input check
        if (playerInside && MissionManager.Instance.GetState(MissionManager.MISSION_1) == MissionState.Available)
        {
            if (Input.GetKeyDown(startKey))
            {
                MissionManager.Instance.StartMission1();
            }
        }
    }

    /// <summary>Permanently stops this marker from offering the mission again.</summary>
    public void DisableInteraction()
    {
        interactable = false;
        playerInside = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (MissionManager.Instance != null && MissionManager.Instance.ui != null)
            MissionManager.Instance.ui.HideStartPrompt();

        // Hide the visual marker (first child) if present.
        if (transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(false);
    }
}
