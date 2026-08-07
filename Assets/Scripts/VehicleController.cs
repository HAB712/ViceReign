using UnityEngine;

/// <summary>
/// Replaces the old VehicleSystem.cs. Owns everything that ISN'T driving physics:
///   - Enter (E, via VehicleInteraction) / Exit (F, handled here)
///   - Hiding the real player, showing the seated DriverModel clone
///   - Camera swap (in-car rig OR basic fallback camera)
///   - Fuel burn/UI, fed by PrometeoCarController's own speed value
///   - Stability (anti-tip rotation freeze, downforce, upward-velocity cap)
///   - Parked resistance to pushing (kinematic while not occupied)
///
/// Actual driving (wheels, torque, steering, drifting, sounds, speed UI) is
/// 100% owned by PrometeoCarController, which stays disabled until the player
/// enters the vehicle.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Driving (Prometeo)")]
    public PrometeoCarController prometeoController;
    public Rigidbody vehicleRigidbody;


    [Tooltip("Where the player is placed when exiting. Beside the vehicle, at ground level.")]
    public Transform exitPoint;

    [Header("Camera (Basic Fallback)")]
    public Camera mainCamera;
    public Vector3 cameraOffsetInVehicle = new Vector3(0, 1.5f, -5f);

    [Header("In-Car Camera System")]
    public GameObject inCarCameraRig; // VehicleInCarCamera lives on this, handles C-key swap

    [Header("Fuel")]
    [Range(0f, 100f)] public float fuelAmount = 100f;
    public float fuelBurnRate = 2f;       // fuel % per second while moving
    public float fuelIdleBurnRate = 0.2f; // fuel % per second while idle in car
    [Tooltip("Prometeo carSpeed (km/h) above which the 'moving' burn rate applies.")]
    public float movingSpeedThreshold = 1f;

    [Header("Exit Interaction")]
    public KeyCode exitKey = KeyCode.F;

    [Header("Stability (kept from the old VehicleSystem)")]
    [Tooltip("Extra downward acceleration (m/s^2) applied while driving to prevent airtime.")]
    public float downforce = 30f;
    [Tooltip("Max upward speed (m/s) allowed while driving, so the car can't launch off bumps/curbs.")]
    public float maxUpwardSpeed = 2f;

    // Internal state
    private bool isOccupied;
    private bool wasOccupied;
    private Transform playerTransform;
    private Animator playerAnimator;
    private CharacterController playerCharController;
    private Collider playerCollider;
    private Rigidbody playerRigidbody;
    private MonoBehaviour playerController;
    private MonoBehaviour playerInput;
    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;
    private bool usingRigCamera;
    private SkinnedMeshRenderer[] playerRenderers;

   private void Awake()
{
    if (vehicleRigidbody == null) vehicleRigidbody = GetComponent<Rigidbody>();
    if (prometeoController == null) prometeoController = GetComponent<PrometeoCarController>();
    if (mainCamera == null) mainCamera = Camera.main;

    if (vehicleRigidbody != null)
    {
        vehicleRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        vehicleRigidbody.interpolation = RigidbodyInterpolation.Interpolate; // smooths out physics pops

        vehicleRigidbody.drag = 0.3f;
        vehicleRigidbody.angularDrag = 0.5f;

    }

    if (prometeoController != null)
        prometeoController.bodyMassCenter = new Vector3(0f, 0.2f, 0f); // tweak Y to sit just above the chassis floor

    if (prometeoController != null)
        prometeoController.enabled = false;

    if (inCarCameraRig != null)
        inCarCameraRig.SetActive(false);

    fuelAmount = Mathf.Clamp(fuelAmount, 0f, 100f);
}


    private void Update()
    {
        if (!isOccupied) return;

        if (Input.GetKeyDown(exitKey))
        {
            ExitVehicle();
            return;
        }

        BurnFuel();
    }

    private void FixedUpdate()
    {
        if (!isOccupied || vehicleRigidbody == null) return;

        // Cap upward velocity and apply constant downforce, same behaviour as the old VehicleSystem,
        // so Prometeo's wheel physics don't launch the car off bumps at speed.
        Vector3 v = vehicleRigidbody.velocity;
        if (v.y > maxUpwardSpeed) v.y = maxUpwardSpeed;
        vehicleRigidbody.velocity = v;
        vehicleRigidbody.AddForce(Vector3.down * downforce, ForceMode.Acceleration);
    }

    private void BurnFuel()
    {
        if (fuelAmount <= 0f)
        {
            if (prometeoController != null) prometeoController.fuelAvailable = false;
            if (FuelUI.Instance != null) FuelUI.Instance.UpdateFuel(0f);
            return;
        }

        float speed = prometeoController != null ? Mathf.Abs(prometeoController.carSpeed) : 0f;
        float burn = speed > movingSpeedThreshold ? fuelBurnRate : fuelIdleBurnRate;
        fuelAmount = Mathf.Clamp(fuelAmount - burn * Time.deltaTime, 0f, 100f);

        if (prometeoController != null)
            prometeoController.fuelAvailable = fuelAmount > 0f;

        if (FuelUI.Instance != null)
            FuelUI.Instance.UpdateFuel(fuelAmount);
    }

    /// <summary>
    /// Hides or shows the REAL player's visual meshes without touching its
    /// transform, animator, or colliders. Used so the seated DriverModel clone
    /// can stand in for the player while driving.
    /// </summary>
    private void SetPlayerVisible(bool visible)
    {
        if (playerTransform == null) return;
        if (playerRenderers == null || playerRenderers.Length == 0)
            playerRenderers = playerTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var r in playerRenderers)
            if (r != null) r.enabled = visible;
    }

    public void EnterVehicle(Transform pTransform, Animator pAnimator)
    {
        if (isOccupied)
        {
            Debug.LogWarning("[VehicleController] Vehicle already occupied!");
            return;
        }
        if (pTransform == null)
        {
            Debug.LogError("[VehicleController] EnterVehicle called with null player transform!");
            return;
        }

        playerTransform = pTransform;
        playerAnimator = pAnimator;
        playerRenderers = null; // refresh cache for this player

        playerCharController = playerTransform.GetComponent<CharacterController>();
        playerRigidbody = playerTransform.GetComponent<Rigidbody>();
        playerCollider = playerTransform.GetComponent<Collider>();

        foreach (var mb in playerTransform.GetComponents<MonoBehaviour>())
        {
            string t = mb.GetType().Name;
            if (t == "ThirdPersonController") playerController = mb;
            if (t == "PlayerInput") playerInput = mb;
        }

        if (playerCharController != null) playerCharController.enabled = false;
        if (playerController != null) playerController.enabled = false;
        if (playerInput != null) playerInput.enabled = false;
        if (playerCollider != null) playerCollider.enabled = false;
        if (playerRigidbody != null) { playerRigidbody.isKinematic = true; playerRigidbody.velocity = Vector3.zero; }

        // Keep the (now frozen) real player attached so it travels with the car while hidden.
        playerTransform.SetParent(this.transform);

        // Player disappears entirely while driving - no seated clone needed.
        SetPlayerVisible(false);

        // --- Camera Setup ---
        usingRigCamera = false;
        if (inCarCameraRig != null)
        {
            inCarCameraRig.SetActive(true);
            var vc = inCarCameraRig.GetComponent<VehicleInCarCamera>();
            if (vc != null) vc.OnEnterVehicle(playerTransform);
            usingRigCamera = true;
            if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        }
        else if (mainCamera != null)
        {
            originalCameraParent = mainCamera.transform.parent;
            originalCameraLocalPos = mainCamera.transform.localPosition;
            originalCameraLocalRot = mainCamera.transform.localRotation;
            mainCamera.transform.SetParent(this.transform);
            mainCamera.transform.localPosition = cameraOffsetInVehicle;
            mainCamera.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        }

        if (playerAnimator != null) playerAnimator.SetBool("IsInVehicle", true);

        if (FuelUI.Instance != null)
        {
            FuelUI.Instance.ShowFuelBar(true);
            FuelUI.Instance.UpdateFuel(fuelAmount);
        }

        // --- Hand control over to Prometeo ---
          if (vehicleRigidbody != null)
    {
        vehicleRigidbody.isKinematic = false;      // was already there
        vehicleRigidbody.drag = 0.3f;              // driving drag
        vehicleRigidbody.angularDrag = 0.5f;
    }
    if (prometeoController != null)
    {
        prometeoController.fuelAvailable = fuelAmount > 0f;
        prometeoController.enabled = true;
    }

    isOccupied = true;
    wasOccupied = true;
        Debug.Log($"[VehicleController] Player entered vehicle: {playerTransform.name}");
    }

    public void ExitVehicle()
    {
        if (!isOccupied || playerTransform == null) return;

        isOccupied = false;
        wasOccupied = false;

        // Stop driving and lock the car in place so it can't be pushed while parked.
        if (prometeoController != null) prometeoController.enabled = false;
        if (vehicleRigidbody != null)
    {
        vehicleRigidbody.velocity = Vector3.zero;
        vehicleRigidbody.angularVelocity = Vector3.zero;
        vehicleRigidbody.drag = 6f;         // heavy parked drag instead of isKinematic
        vehicleRigidbody.angularDrag = 6f;
        // NOT setting isKinematic = true — let it stay dynamic so it can
        // rest naturally on the ground and doesn't "pop" next time it's entered.
    }

        // Player reappears at the exit point.
        SetPlayerVisible(true);

        playerTransform.SetParent(null);

        if (exitPoint != null)
        {
            playerTransform.position = exitPoint.position;
            playerTransform.rotation = Quaternion.Euler(0f, exitPoint.eulerAngles.y, 0f);
        }
        else
        {
            Vector3 exitPos = transform.position + transform.right * 2.5f + Vector3.up * 0.1f;
            playerTransform.position = exitPos;
            playerTransform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        if (playerCollider != null) playerCollider.enabled = true;
        if (playerCharController != null) playerCharController.enabled = true;
        if (playerController != null) playerController.enabled = true;
        if (playerInput != null) playerInput.enabled = true;
        if (playerRigidbody != null) { playerRigidbody.isKinematic = false; playerRigidbody.velocity = Vector3.zero; }

        // Restore camera
        if (usingRigCamera && inCarCameraRig != null)
        {
            var vc = inCarCameraRig.GetComponent<VehicleInCarCamera>();
            if (vc != null) vc.OnExitVehicle();
            inCarCameraRig.SetActive(false);
            if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        }
        else if (mainCamera != null)
        {
            mainCamera.transform.SetParent(originalCameraParent);
            mainCamera.transform.localPosition = originalCameraLocalPos;
            mainCamera.transform.localRotation = originalCameraLocalRot;
        }

        if (playerAnimator != null) playerAnimator.SetBool("IsInVehicle", false);

        if (FuelUI.Instance != null)
            FuelUI.Instance.ShowFuelBar(false);

        playerTransform = null; playerAnimator = null; playerCharController = null;
        playerCollider = null; playerController = null; playerInput = null;
        Debug.Log("[VehicleController] Player exited vehicle.");
    }

    public bool IsOccupied() => isOccupied;
    public float GetCurrentSpeed() => prometeoController != null ? prometeoController.carSpeed : 0f;
    public float GetMaxSpeed() => prometeoController != null ? prometeoController.maxSpeed : 0f;
    public float GetFuel() => fuelAmount;

    /// <summary>Adds fuel (percent, clamped 0-100) and refreshes the fuel bar + Prometeo's fuel gate.</summary>
    public void AddFuel(float percent)
    {
        if (Mathf.Approximately(percent, 0f)) return;
        fuelAmount = Mathf.Clamp(fuelAmount + percent, 0f, 100f);
        if (prometeoController != null) prometeoController.fuelAvailable = fuelAmount > 0f;
        if (FuelUI.Instance != null) FuelUI.Instance.UpdateFuel(fuelAmount);
    }
}