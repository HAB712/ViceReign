using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry for all world-object spawners (Hospitals, Fuel Stations,
/// Bonus pickups, and any future type — Shops, Vendors, Police Stations,
/// Safe Houses, etc). Mirrors the auto-discovery style already used by
/// PoliceManager: each spawner registers itself in Start(), zero manual
/// wiring needed as new spawner types get added.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    private readonly List<BaseSpawner> registeredSpawners = new List<BaseSpawner>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterSpawner(BaseSpawner spawner)
    {
        if (!registeredSpawners.Contains(spawner))
            registeredSpawners.Add(spawner);
    }

    public void UnregisterSpawner(BaseSpawner spawner)
    {
        registeredSpawners.Remove(spawner);
    }

    public IReadOnlyList<BaseSpawner> GetAllSpawners() => registeredSpawners;

    [ContextMenu("Respawn All")]
    public void RespawnAll()
    {
        foreach (var spawner in registeredSpawners)
            spawner.SpawnAll();
    }
}