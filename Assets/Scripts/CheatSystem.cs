using System.Collections.Generic;
using UnityEngine;
using CombatSystem;

/// <summary>
/// GTA-style typed cheat code system. Listens to raw keyboard input every
/// frame (Input.inputString) and matches against registered cheat codes —
/// same mechanism classic GTA games use. This is one of the few legitimate
/// uses of Update() in the project: text-buffer polling has no event-based
/// alternative with the legacy Input Manager. The per-frame cost is trivial
/// (a few string ops), so it does not affect open-world performance.
///
/// All cheats are FREE — no MoneyManager interaction.
/// fastspeed / slowspeed are TOGGLES: typing the same code again reverts
/// the player back to normal (base) speed.
/// </summary>
public class CheatSystem : MonoBehaviour
{
    public static CheatSystem Instance;

    [Header("Cheat Codes")]
    [SerializeField] private string fullHealthCode = "health";
    [SerializeField] private string fuelFullCode    = "petrol";
    [SerializeField] private string freeMeCode      = "goaway";
    [SerializeField] private string fastSpeedCode   = "turbospeed";
    [SerializeField] private string slowSpeedCode   = "slowspeed";

    [Header("Speed Cheat Multipliers")]
    [SerializeField] private float fastSpeedMultiplier = 2f;
    [SerializeField] private float slowSpeedMultiplier = 0.5f;

    [Header("Feedback")]
    [SerializeField] private bool showRewardPopup = true;
    [SerializeField] private int maxBufferLength = 24;

    private readonly List<string> allCodes = new List<string>();
    private string inputBuffer = "";

    // Player refs (auto-found, same pattern as rest of the project)
    private Transform playerTransform;
    private HealthSystem playerHealth;
    private StarterAssets.ThirdPersonController playerController;

    // Cached ORIGINAL speeds so repeated cheat presses don't stack multiplicatively
    private float baseMoveSpeed = -1f;
    private float baseSprintSpeed = -1f;

    // Toggle state tracking for speed cheats
    private enum SpeedState { Normal, Fast, Slow }
    private SpeedState currentSpeedState = SpeedState.Normal;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        allCodes.Add(fullHealthCode);
        allCodes.Add(fuelFullCode);
        allCodes.Add(freeMeCode);
        allCodes.Add(fastSpeedCode);
        allCodes.Add(slowSpeedCode);
    }

    private void Start()
    {
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        playerController = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            playerHealth = playerController.GetComponent<HealthSystem>();

            if (baseMoveSpeed < 0f)
            {
                baseMoveSpeed = playerController.MoveSpeed;
                baseSprintSpeed = playerController.SprintSpeed;
            }
        }
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(Input.inputString)) return;

        foreach (char c in Input.inputString)
        {
            if (char.IsLetterOrDigit(c))
                inputBuffer += char.ToLower(c);

            if (inputBuffer.Length > maxBufferLength)
                inputBuffer = inputBuffer.Substring(inputBuffer.Length - maxBufferLength);
        }

        CheckBufferForCheats();
    }

    private void CheckBufferForCheats()
    {
        foreach (var code in allCodes)
        {
            if (inputBuffer.EndsWith(code))
            {
                ApplyCheat(code);
                inputBuffer = "";
                break;
            }
        }
    }

    // ---- CHEAT EFFECTS ----

    private void ApplyCheat(string code)
    {
        if (code == fullHealthCode) DoFullHealth();
        else if (code == fuelFullCode) DoFuelFull();
        else if (code == freeMeCode) DoFreeMe();
        else if (code == fastSpeedCode) ToggleFastSpeed();
        else if (code == slowSpeedCode) ToggleSlowSpeed();
    }

    private void DoFullHealth()
    {
        if (playerHealth == null) ResolvePlayer();
        if (playerHealth == null)
        {
            Debug.LogWarning("[CheatSystem] fullhealth — player HealthSystem not found.");
            return;
        }

        float missing = playerHealth.GetMaxHealth() - playerHealth.GetCurrentHealth();
        if (missing > 0f) playerHealth.Heal(missing);

        Debug.Log("[CheatSystem] CHEAT ACTIVATED: Full Health");
        Notify("Cheat: Full Health");
    }

    private void DoFuelFull()
    {
        // UPDATED: VehicleSystem was replaced by VehicleController project-wide.
        VehicleController occupiedVehicle = null;
        foreach (var v in FindObjectsOfType<VehicleController>())
        {
            if (v.IsOccupied())
            {
                occupiedVehicle = v;
                break;
            }
        }

        if (occupiedVehicle == null)
        {
            Debug.LogWarning("[CheatSystem] fuelfull — player is not in a vehicle.");
            Notify("Cheat Failed: Not In Vehicle");
            return;
        }

        float missing = 100f - occupiedVehicle.GetFuel();
        if (missing > 0f) occupiedVehicle.AddFuel(missing);

        Debug.Log("[CheatSystem] CHEAT ACTIVATED: Full Fuel");
        Notify("Cheat: Full Fuel");
    }

    private void DoFreeMe()
    {
        if (WantedSystem.Instance != null)
        {
            WantedSystem.Instance.ClearWantedLevel();
            Debug.Log("[CheatSystem] CHEAT ACTIVATED: Wanted Level Cleared");
            Notify("Cheat: Wanted Level Cleared");
        }
        else
        {
            Debug.LogWarning("[CheatSystem] freeme — WantedSystem.Instance is null.");
        }
    }

    // ---- SPEED TOGGLES ----

    private void ToggleFastSpeed()
    {
        if (playerController == null) ResolvePlayer();
        if (playerController == null)
        {
            Debug.LogWarning("[CheatSystem] fastspeed — ThirdPersonController not found.");
            return;
        }

        if (currentSpeedState == SpeedState.Fast)
        {
            SetBaseSpeed();
            currentSpeedState = SpeedState.Normal;
            Debug.Log("[CheatSystem] CHEAT DEACTIVATED: Fast Speed (back to normal)");
            Notify("Cheat Off: Normal Speed");
        }
        else
        {
            playerController.MoveSpeed   = baseMoveSpeed   * fastSpeedMultiplier;
            playerController.SprintSpeed = baseSprintSpeed * fastSpeedMultiplier;
            currentSpeedState = SpeedState.Fast;
            Debug.Log("[CheatSystem] CHEAT ACTIVATED: Fast Speed (2x)");
            Notify("Cheat: Fast Speed");
        }
    }

    private void ToggleSlowSpeed()
    {
        if (playerController == null) ResolvePlayer();
        if (playerController == null)
        {
            Debug.LogWarning("[CheatSystem] slowspeed — ThirdPersonController not found.");
            return;
        }

        if (currentSpeedState == SpeedState.Slow)
        {
            SetBaseSpeed();
            currentSpeedState = SpeedState.Normal;
            Debug.Log("[CheatSystem] CHEAT DEACTIVATED: Slow Speed (back to normal)");
            Notify("Cheat Off: Normal Speed");
        }
        else
        {
            playerController.MoveSpeed   = baseMoveSpeed   * slowSpeedMultiplier;
            playerController.SprintSpeed = baseSprintSpeed * slowSpeedMultiplier;
            currentSpeedState = SpeedState.Slow;
            Debug.Log("[CheatSystem] CHEAT ACTIVATED: Slow Speed (0.5x)");
            Notify("Cheat: Slow Speed");
        }
    }

    private void SetBaseSpeed()
    {
        playerController.MoveSpeed   = baseMoveSpeed;
        playerController.SprintSpeed = baseSprintSpeed;
    }

    private void Notify(string message)
    {
        if (showRewardPopup && RewardPopup.Instance != null)
            RewardPopup.Instance.Show(message);
    }
}