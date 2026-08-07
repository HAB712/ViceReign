using UnityEngine;

/// <summary>
/// </summary>
public class FuelSpawner : BaseSpawner
{
    protected override void Awake()
    {
        allowRespawn = false; // fuel stations are permanent
        base.Awake();
    }

    protected override void ConfigureSpawnedObject(GameObject instance, SpawnPoint point)
    {
        if (instance.GetComponent<PetrolPump>() == null)
            instance.AddComponent<PetrolPump>();
    }
}