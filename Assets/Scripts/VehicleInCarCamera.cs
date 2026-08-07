using UnityEngine;

/// <summary>
/// 3-mode in-car camera. Attach to InCarCameraRig parented to the car.
/// Press C to cycle: Hood -> Full Car -> Cockpit
/// </summary>
public class VehicleInCarCamera : MonoBehaviour
{
    [Header("Camera")]
    public Camera rigCamera;
    public Transform vehicleBody;

    [Header("Mode 0: Hood (half-car visible)")]
    public Vector3 hoodOffset = new Vector3(0f, 1.8f, -3.5f);
    public float hoodFOV = 65f;
    public float hoodPitch = 8f;

    [Header("Mode 1: Full Car + Road")]
    public Vector3 fullCarOffset = new Vector3(0f, 3.5f, -8f);
    public float fullCarFOV = 60f;
    public float fullCarPitch = 12f;

    [Header("Mode 2: Cockpit")]
    public Vector3 cockpitOffset = new Vector3(0.3f, 1.1f, 0.5f);
    public float cockpitFOV = 75f;

    [Header("Smoothing")]
    public float posSmooth = 8f;
    public float rotSmooth = 6f;

    private int mode = 0;
    private bool active = false;
    private readonly string[] modeNames = { "Hood Cam", "Full Car Cam", "Cockpit Cam" };

    void Awake()
    {
        if (rigCamera == null) rigCamera = GetComponentInChildren<Camera>();
    }

    public void OnEnterVehicle(Transform player)
    {
        active = true;
        mode = 0;
        SetFOV();
        SnapCamera();
        Debug.Log("[VehicleInCarCamera] Active. Press C to cycle cameras.");
    }

    public void OnExitVehicle()
    {
        active = false;
    }

    void Update()
    {
        if (!active) return;
        if (Input.GetKeyDown(KeyCode.C))
        {
            mode = (mode + 1) % 3;
            SetFOV();
            Debug.Log($"[VehicleInCarCamera] Mode: {modeNames[mode]}");
        }
    }

    void LateUpdate()
    {
        if (!active || vehicleBody == null || rigCamera == null) return;
        Vector3 tPos;
        Quaternion tRot;
        GetTargetPosRot(out tPos, out tRot);
        rigCamera.transform.position = Vector3.Lerp(rigCamera.transform.position, tPos, posSmooth * Time.deltaTime);
        rigCamera.transform.rotation = Quaternion.Slerp(rigCamera.transform.rotation, tRot, rotSmooth * Time.deltaTime);
    }

    void GetTargetPosRot(out Vector3 pos, out Quaternion rot)
    {
        if (mode == 0)
        {
            pos = vehicleBody.TransformPoint(hoodOffset);
            rot = vehicleBody.rotation * Quaternion.Euler(hoodPitch, 0f, 0f);
        }
        else if (mode == 1)
        {
            pos = vehicleBody.TransformPoint(fullCarOffset);
            rot = vehicleBody.rotation * Quaternion.Euler(fullCarPitch, 0f, 0f);
        }
        else
        {
            pos = vehicleBody.TransformPoint(cockpitOffset);
            rot = vehicleBody.rotation;
        }
    }

    void SetFOV()
    {
        if (rigCamera == null) return;
        if (mode == 0) rigCamera.fieldOfView = hoodFOV;
        else if (mode == 1) rigCamera.fieldOfView = fullCarFOV;
        else rigCamera.fieldOfView = cockpitFOV;
    }

    void SnapCamera()
    {
        if (vehicleBody == null || rigCamera == null) return;
        Vector3 offset = mode == 0 ? hoodOffset : mode == 1 ? fullCarOffset : cockpitOffset;
        rigCamera.transform.position = vehicleBody.TransformPoint(offset);
        rigCamera.transform.rotation = vehicleBody.rotation;
    }

    void OnGUI()
    {
        if (!active) return;
        GUI.Label(new Rect(Screen.width - 190, 10, 180, 30),
            "[C] " + modeNames[mode],
            new GUIStyle { normal = new GUIStyleState { textColor = Color.white }, fontSize = 14, fontStyle = FontStyle.Bold });
    }
}