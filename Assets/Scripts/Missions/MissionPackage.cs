using UnityEngine;

/// <summary>
/// The lost package. Hidden until all clues are collected (the MissionManager
/// activates it). Collecting it completes Mission 1. It cannot be collected
/// before activation because the GameObject is inactive until then.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionPackage : MonoBehaviour
{
    [Tooltip("Degrees per second of idle spin (purely cosmetic).")]
    public float spinSpeed = 45f;

    private bool collected = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (spinSpeed != 0f)
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        if (MissionManager.Instance == null) return;

        // Extra guard: only valid while Mission 1 is active.
        if (!MissionManager.Instance.IsMission1Active)
            return;

        collected = true;
        MissionManager.Instance.CollectPackage();
        Destroy(gameObject);
    }
}
