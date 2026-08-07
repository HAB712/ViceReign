using System;
using UnityEngine;

/// <summary>
/// Generic, reusable mission start marker. Works for ANY mission via the
/// <see cref="missionId"/> field, so future missions can reuse it without new
/// code. While the player stands inside the trigger and the mission is
/// Available, it shows a prompt and starts the mission on key press, then
/// raises <see cref="OnMissionStarted"/> for a mission controller to react.
///
/// This is intentionally separate from Mission 1's MissionStartPoint so that
/// Mission 1 is left completely untouched.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionStartTrigger : MonoBehaviour
{
    [Header("Mission")]
    [Tooltip("Which mission this marker starts (e.g. 2 for the Delivery Job).")]
    public int missionId = 2;
    [Tooltip("Friendly name used in debug logs.")]
    public string missionName = "Delivery Job";

    [Header("Interaction")]
    public KeyCode startKey = KeyCode.M;
    [Tooltip("Prompt shown while the player is inside the marker.")]
    public string promptMessage = "Press M to start Delivery Job";

    /// <summary>Raised once when this mission is successfully started.</summary>
    public event Action OnMissionStarted;

    private bool playerInside = false;
    private bool interactable = true;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        // FORCE a Kinematic Rigidbody so Unity reliably fires OnTriggerEnter against CharacterControllers
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
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
            if (MissionManager.Instance.GetState(missionId) == MissionState.Available && MissionManager.Instance.ui != null)
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
        if (playerInside && MissionManager.Instance.GetState(missionId) == MissionState.Available)
        {
            if (Input.GetKeyDown(startKey))
            {
                if (MissionManager.Instance.TryStartMission(missionId, missionName))
                {
                    if (MissionManager.Instance.ui != null) MissionManager.Instance.ui.HideStartPrompt();
                    OnMissionStarted?.Invoke();
                }
            }
        }
    }



    /// <summary>Permanently disables this marker (called once the mission completes).</summary>
    public void DisableInteraction()
    {
        interactable = false;
        playerInside = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        var mm = MissionManager.Instance;
        if (mm != null && mm.ui != null) mm.ui.HideStartPrompt();

        // Hide the visual marker (first child) if present.
        if (transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(false);
    }
}
