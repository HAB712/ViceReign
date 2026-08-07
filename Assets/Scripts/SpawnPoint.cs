using UnityEngine;

/// <summary>
/// Marks a valid spawn location in the world. Place empty GameObjects with
/// this component as children of a parent (e.g. "HospitalSpawnPoints") and
/// assign that parent to the matching spawner's "Spawn Points Parent" field.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Optional label for debugging / naming spawned instances.")]
    public string pointLabel;

    // Shared occupied-flag: this is what guarantees NO spawner (of any type)
    // can ever double-spawn on the same point.
    [HideInInspector] public bool isOccupied = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 1f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
    }
}