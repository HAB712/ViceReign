using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// </summary>
public abstract class BaseSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Parent Transform whose children all have SpawnPoint components.")]
    [SerializeField] protected Transform spawnPointsParent;
    [Tooltip("Used instead of / in addition to Spawn Points Parent.")]
    [SerializeField] protected List<SpawnPoint> manualSpawnPoints = new List<SpawnPoint>();

    [Header("Prefabs")]
    [Tooltip("One or more prefab variants — a random one is chosen per point.")]
    [SerializeField] protected GameObject[] prefabVariants;

    [Header("Spawn Behaviour")]
    [SerializeField] protected bool spawnOnStart = true;
    [Tooltip("OFF for permanent world features (fuel stations). ON for consumable pickups.")]
    [SerializeField] protected bool allowRespawn = false;
    [SerializeField] protected float respawnDelay = 30f;

    [Header("Open-World Optimization (optional)")]
    [Tooltip("Periodically disables renderers/colliders beyond Cull Distance. Coroutine-driven, never Update().")]
    [SerializeField] protected bool enableDistanceCulling = false;
    [SerializeField] protected float cullDistance = 120f;
    [SerializeField] protected float cullCheckInterval = 2f;

    protected readonly Dictionary<SpawnPoint, GameObject> activeSpawns = new Dictionary<SpawnPoint, GameObject>();
    protected readonly List<SpawnPoint> resolvedPoints = new List<SpawnPoint>();

    private readonly Dictionary<GameObject, Renderer[]> rendererCache = new Dictionary<GameObject, Renderer[]>();
    private readonly Dictionary<GameObject, Collider[]> colliderCache = new Dictionary<GameObject, Collider[]>();

    protected Transform player;

    protected virtual void Awake()
    {
        ResolveSpawnPoints();
    }

    protected virtual void Start()
    {
        if (player == null)
        {
            var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
            if (tpc != null) player = tpc.transform;
        }

        if (SpawnManager.Instance != null)
            SpawnManager.Instance.RegisterSpawner(this);

        if (spawnOnStart)
            SpawnAll();

        if (enableDistanceCulling)
            StartCoroutine(CullRoutine());
    }

    protected virtual void OnDestroy()
    {
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.UnregisterSpawner(this);
    }

   private void ResolveSpawnPoints()
{
    resolvedPoints.Clear();

    if (spawnPointsParent != null)
    {
        foreach (Transform child in spawnPointsParent)
        {
            var sp = child.GetComponent<SpawnPoint>();
            if (sp != null)
            {
                sp.isOccupied = false; // NEW — self-heal against stale saved state
                resolvedPoints.Add(sp);
            }
        }
    }

    foreach (var sp in manualSpawnPoints)
    {
        if (sp != null && !resolvedPoints.Contains(sp))
        {
            sp.isOccupied = false; // NEW
            resolvedPoints.Add(sp);
        }
    }
}
    // ---- PUBLIC API ----

    public void SpawnAll()
    {
        foreach (var point in resolvedPoints)
            SpawnAt(point);
    }

    public GameObject SpawnAt(SpawnPoint point)
    {
        if (point == null) return null;

        if (activeSpawns.TryGetValue(point, out var existing) && existing != null)
            return existing;

        // Global duplicate guard — shared across ALL spawner types.
        if (point.isOccupied)
        {
            Debug.LogWarning($"[{GetType().Name}] SpawnPoint '{point.pointLabel}' already occupied — skipping.");
            return null;
        }

        if (prefabVariants == null || prefabVariants.Length == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] No prefab variants assigned.");
            return null;
        }

        GameObject prefab = prefabVariants[Random.Range(0, prefabVariants.Length)];
        GameObject instance = Instantiate(prefab, point.transform.position, point.transform.rotation);
        instance.name = string.IsNullOrEmpty(point.pointLabel)
            ? $"{prefab.name}_{point.transform.position}"
            : $"{prefab.name}_{point.pointLabel}";

        point.isOccupied = true;
        activeSpawns[point] = instance;

        ConfigureSpawnedObject(instance, point);

        if (enableDistanceCulling)
            CacheCullComponents(instance);

        if (allowRespawn)
            StartCoroutine(WatchForDestruction(instance, point));

        return instance;
    }

    public void DespawnAt(SpawnPoint point)
    {
        if (point == null) return;

        if (activeSpawns.TryGetValue(point, out var instance) && instance != null)
            Destroy(instance);

        activeSpawns.Remove(point);
        point.isOccupied = false;
    }

    /// <summary>Override to attach/verify type-specific components (PetrolPump, CashPickup, etc.).</summary>
    protected virtual void ConfigureSpawnedObject(GameObject instance, SpawnPoint point) { }

    private IEnumerator WatchForDestruction(GameObject instance, SpawnPoint point)
    {
        yield return new WaitUntil(() => instance == null);

        activeSpawns.Remove(point);
        point.isOccupied = false;
        rendererCache.Remove(instance);
        colliderCache.Remove(instance);

        yield return new WaitForSeconds(respawnDelay);

        if (!activeSpawns.ContainsKey(point) || activeSpawns[point] == null)
            SpawnAt(point);
    }

    private void CacheCullComponents(GameObject instance)
    {
        rendererCache[instance] = instance.GetComponentsInChildren<Renderer>(true);
        colliderCache[instance] = instance.GetComponentsInChildren<Collider>(true);
    }

    private IEnumerator CullRoutine()
    {
        var wait = new WaitForSeconds(cullCheckInterval);
        while (true)
        {
            yield return wait;
            if (player == null) continue;

            foreach (var kvp in activeSpawns)
            {
                GameObject obj = kvp.Value;
                if (obj == null) continue;

                float dist = Vector3.Distance(player.position, obj.transform.position);
                ApplyCullState(obj, dist <= cullDistance);
            }
        }
    }

    private void ApplyCullState(GameObject obj, bool visible)
    {
        if (rendererCache.TryGetValue(obj, out var renderers))
            foreach (var r in renderers)
                if (r != null) r.enabled = visible;

        if (colliderCache.TryGetValue(obj, out var colliders))
            foreach (var c in colliders)
                if (c != null) c.enabled = visible;
    }

    public IReadOnlyList<SpawnPoint> GetSpawnPoints() => resolvedPoints;
    public IReadOnlyDictionary<SpawnPoint, GameObject> GetActiveSpawns() => activeSpawns;
}