using UnityEngine;

/// <summary>
/// A single collectable clue for Mission 1. It can only be picked up while the
/// mission is active; collecting it advances mission progress and destroys the
/// clue object.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionClue : MonoBehaviour
{
    [Tooltip("Optional id for debugging / identification.")]
    public int clueId = 0;

    [Tooltip("Degrees per second of idle spin (purely cosmetic).")]
    public float spinSpeed = 60f;

    private bool collected = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        // Light cosmetic spin so clues are easy to spot.
        if (spinSpeed != 0f)
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        if (MissionManager.Instance == null) return;

        // Clues cannot be collected before the mission has started.
        if (!MissionManager.Instance.CanCollectClues())
            return;

        collected = true;
        MissionManager.Instance.CollectClue();
        Destroy(gameObject);
    }
}
