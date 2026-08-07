using UnityEngine;

/// <summary>
/// Petrol pump refueling zone. Detects a vehicle (any object with a
/// VehicleController) entering its trigger collider while the player is driving it,
/// and lets the player gradually refuel by pressing R - spending money via the
/// existing MoneyManager and adding fuel via VehicleController.AddFuel.
///
/// Integrates with the existing systems only; it does not duplicate the fuel,
/// vehicle, fuel-UI, or money systems.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PetrolPump : MonoBehaviour
{
    [Header("Pricing")]
    [Tooltip("Rupees charged per 1% of fuel.")]
    public int costPerPercent = 2;

    [Header("Refuel Feel")]
    [Tooltip("How fast the tank fills, in fuel % per second.")]
    public float refuelPercentPerSecond = 12f;

    [Header("Input")]
    public KeyCode refuelKey = KeyCode.R;

    [Header("Messages")]
    public string promptMessage = "Press R to Refuel";
    public string fullMessage = "Fuel Tank Full";
    public string noMoneyMessage = "Not Enough Money";

    // Runtime state
    private VehicleController dockedVehicle;
    private bool isRefueling;
    private float stepAccumulator; // accumulates fractional % toward whole 1% steps

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var vehicle = ResolveVehicle(other);
        if (vehicle == null) return;

        dockedVehicle = vehicle;
        stepAccumulator = 0f;
        Debug.Log("[PetrolPump] Vehicle entered refuel zone.");
        RefreshIdlePrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        var vehicle = ResolveVehicle(other);
        if (vehicle == null || vehicle != dockedVehicle) return;

        StopRefueling(null);
        if (RefuelUI.Instance != null) { RefuelUI.Instance.HidePrompt(); RefuelUI.Instance.HideStatus(); }
        dockedVehicle = null;
        Debug.Log("[PetrolPump] Vehicle left refuel zone.");
    }

    private void Update()
    {
        if (dockedVehicle == null) return;

        // The player must be seated in the car to refuel.
        if (!dockedVehicle.IsOccupied())
        {
            if (isRefueling) StopRefueling(null);
            if (RefuelUI.Instance != null) { RefuelUI.Instance.HidePrompt(); RefuelUI.Instance.HideStatus(); }
            return;
        }

        if (!isRefueling)
        {
            RefreshIdlePrompt();
            if (Input.GetKeyDown(refuelKey)) TryStartRefueling();
        }
        else
        {
            // Toggle off with the same key.
            if (Input.GetKeyDown(refuelKey)) { StopRefueling(null); RefreshIdlePrompt(); return; }
            TickRefueling();
        }
    }



    private void TryStartRefueling()
    {
        float fuel = dockedVehicle.GetFuel();

        if (fuel >= 100f)
        {
            if (RefuelUI.Instance != null) RefuelUI.Instance.ShowMessage(fullMessage);
            return;
        }

        if (!HasMoneyForOnePercent())
        {
            if (RefuelUI.Instance != null) RefuelUI.Instance.ShowMessage(noMoneyMessage);
            Debug.Log("[PetrolPump] Refuel blocked - not enough money.");
            return;
        }

        isRefueling = true;
        stepAccumulator = 0f;
        if (RefuelUI.Instance != null) RefuelUI.Instance.HidePrompt();
        Debug.Log("[PetrolPump] Refueling started at " + fuel.ToString("F0") + "%.");
    }

    private void TickRefueling()
    {
        // Already full?
        if (dockedVehicle.GetFuel() >= 100f) { StopRefueling(fullMessage); return; }

        stepAccumulator += refuelPercentPerSecond * Time.deltaTime;

        // Commit whole 1% steps; each costs exactly costPerPercent rupees so money
        // stays integer-accurate and fuel never overshoots 100%.
        while (stepAccumulator >= 1f)
        {
            if (dockedVehicle.GetFuel() >= 100f) { StopRefueling(fullMessage); return; }

            if (!HasMoneyForOnePercent()) { StopRefueling(noMoneyMessage); return; }

            MoneyManager.instance.RemoveMoney(costPerPercent);
            dockedVehicle.AddFuel(1f);
            stepAccumulator -= 1f;
        }

        // Live status readout.
        if (RefuelUI.Instance != null)
            RefuelUI.Instance.ShowStatus("Refueling... " + dockedVehicle.GetFuel().ToString("F0") +
                                         "%   (-Rs " + costPerPercent + " / 1%)");
    }

    private void StopRefueling(string message)
    {
        if (!isRefueling)
        {
            if (message != null && RefuelUI.Instance != null) RefuelUI.Instance.ShowMessage(message);
            return;
        }

        isRefueling = false;
        stepAccumulator = 0f;

        if (RefuelUI.Instance != null)
        {
            RefuelUI.Instance.HideStatus();
            if (message != null) RefuelUI.Instance.ShowMessage(message);
        }

        if (dockedVehicle != null)
            Debug.Log("[PetrolPump] Refueling stopped at " + dockedVehicle.GetFuel().ToString("F0") +
                      "%" + (message != null ? " (" + message + ")" : "") + ".");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private bool HasMoneyForOnePercent()
    {
        return MoneyManager.instance != null && MoneyManager.instance.CurrentMoney >= costPerPercent;
    }

    /// <summary>Shows or hides the idle "Press R" prompt based on the current fuel level.</summary>
    private void RefreshIdlePrompt()
    {
        if (RefuelUI.Instance == null || dockedVehicle == null) return;

        if (dockedVehicle.GetFuel() >= 100f)
            RefuelUI.Instance.HidePrompt();
        else
            RefuelUI.Instance.ShowPrompt(promptMessage);
    }

    /// <summary>Finds the VehicleController for an entering collider (root, parent, or rigidbody).</summary>
    private static VehicleController ResolveVehicle(Collider other)
    {
        if (other == null) return null;
        var v = other.GetComponentInParent<VehicleController>();
        if (v != null) return v;
        if (other.attachedRigidbody != null)
            v = other.attachedRigidbody.GetComponent<VehicleController>();
        return v;
    }
}