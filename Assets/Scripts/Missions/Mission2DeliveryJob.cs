using UnityEngine;

/// <summary>
/// Mission 2 - "Delivery Job".
///
/// Flow: become Available automatically once Mission 1 is Completed (the
/// MissionManager already unlocks it) -> player starts at the purple marker ->
/// pick up the parcel -> deliver the parcel -> mission complete + reward.
///
/// This controller owns ONLY Mission 2's specific logic (the parcel state and
/// objective text). All shared state/reward/UI go through the existing
/// MissionManager and MoneyManager - nothing is duplicated, and Mission 1 is
/// not touched. The same pattern can be copied for future delivery missions.
/// </summary>
public class Mission2DeliveryJob : MonoBehaviour
{
    [Header("Mission 2 Config")]
    [Tooltip("Reward (Rs) granted on successful delivery.")]
    public int rewardAmount = 500;
    public string missionName = "Delivery Job";

    [Header("References (assigned by editor automation / inspector)")]
    public MissionStartTrigger startTrigger;
    public MissionPointTrigger pickupPoint;
    public MissionPointTrigger deliveryPoint;
    [Tooltip("Optional visual that represents the parcel the player is carrying.")]
    public GameObject carriedParcelVisual;

    // Simple parcel state tracking.
    public bool hasParcel { get; private set; }

    private bool availableLogged = false;

    private void Start()
    {
        // React to the mission being started at the marker.
        if (startTrigger != null)
            startTrigger.OnMissionStarted += HandleMissionStarted;

        // Bind the trigger points to this controller.
        if (pickupPoint != null) pickupPoint.Init(this, MissionPointTrigger.PointType.Pickup);
        if (deliveryPoint != null) deliveryPoint.Init(this, MissionPointTrigger.PointType.Delivery);

        // Pickup / delivery only become live once the mission is Active.
        SetActiveSafe(pickupPoint, false);
        SetActiveSafe(deliveryPoint, false);
        if (carriedParcelVisual != null) carriedParcelVisual.SetActive(false);

        hasParcel = false;
    }

    private void OnDestroy()
    {
        if (startTrigger != null)
            startTrigger.OnMissionStarted -= HandleMissionStarted;
    }

    private void Update()
    {
        // Announce availability exactly once, when Mission 1 completion unlocks us.
        if (!availableLogged && MissionManager.Instance != null &&
            MissionManager.Instance.GetState(MissionManager.MISSION_2) == MissionState.Available)
        {
            availableLogged = true;
            Debug.Log("[Mission] Mission 2 'Delivery Job' is now AVAILABLE.");
        }
    }

    // ------------------------------------------------------------------
    // Flow callbacks
    // ------------------------------------------------------------------

    private void HandleMissionStarted()
    {
        hasParcel = false;

        // Objective: pick up the parcel.
        var ui = MissionManager.Instance != null ? MissionManager.Instance.ui : null;
        if (ui != null) ui.ShowObjectiveNoProgress("Pick up the parcel.");

        // Activate pickup (and delivery, but delivery stays guarded by hasParcel).
        SetActiveSafe(pickupPoint, true);
        SetActiveSafe(deliveryPoint, true);

        // GPS arrow: point at the pickup point.
        if (MissionNavigator.Instance != null && pickupPoint != null)
            MissionNavigator.Instance.PointTo(pickupPoint.transform, "Pick up the parcel");

        Debug.Log("[Mission] Delivery Job - go to the pickup point.");
    }

    /// <summary>Called by the pickup trigger when the player reaches it.</summary>
    public void OnReachedPickup()
    {
        if (!IsActive() || hasParcel) return;

        hasParcel = true;
        Debug.Log("[Mission] Parcel picked up.");

        if (carriedParcelVisual != null) carriedParcelVisual.SetActive(true);
        if (pickupPoint != null) pickupPoint.Consume();

        var ui = MissionManager.Instance != null ? MissionManager.Instance.ui : null;
        if (ui != null) ui.ShowObjectiveNoProgress("Deliver the parcel.");

        // GPS arrow: point at the delivery point.
        if (MissionNavigator.Instance != null && deliveryPoint != null)
            MissionNavigator.Instance.PointTo(deliveryPoint.transform, "Deliver the parcel");
    }

    /// <summary>Called by the delivery trigger when the player reaches it.</summary>
    public void OnReachedDelivery()
    {
        if (!IsActive()) return;

        // Rule: cannot deliver without first picking up the parcel.
        if (!hasParcel)
        {
            Debug.Log("[Mission] Cannot deliver - no parcel yet. Pick it up first.");
            return;
        }

        hasParcel = false; // parcel is consumed on delivery
        Debug.Log("[Mission] Parcel delivered.");

        if (carriedParcelVisual != null) carriedParcelVisual.SetActive(false);
        if (deliveryPoint != null) deliveryPoint.Consume();

        // Complete via the shared manager (state + reward + UI + unlock-next).
        if (MissionManager.Instance != null)
            MissionManager.Instance.CompleteMission(MissionManager.MISSION_2, rewardAmount, missionName);

        Debug.Log("Mission 2 Completed - Reward Given: Rs " + rewardAmount);

        // GPS arrow: nothing left to point at.
        if (MissionNavigator.Instance != null)
            MissionNavigator.Instance.ClearTarget();

        // Mission cannot restart after completion.
        if (startTrigger != null) startTrigger.DisableInteraction();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private bool IsActive()
    {
        return MissionManager.Instance != null &&
               MissionManager.Instance.IsMissionActive(MissionManager.MISSION_2);
    }

    private static void SetActiveSafe(MissionPointTrigger p, bool active)
    {
        if (p != null) p.gameObject.SetActive(active);
    }
}