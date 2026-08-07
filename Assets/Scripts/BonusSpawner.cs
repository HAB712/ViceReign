using UnityEngine;

/// <summary>
/// </summary>
public class BonusSpawner : BaseSpawner
{
    protected override void Awake()
    {
        allowRespawn = true;
        base.Awake();
    }

    protected override void ConfigureSpawnedObject(GameObject instance, SpawnPoint point)
    {
        if (instance.GetComponent<CashPickup>() == null)
            instance.AddComponent<CashPickup>();

        if (instance.GetComponent<Collider>() == null)
            Debug.LogWarning($"[BonusSpawner] '{instance.name}' has no Collider — CashPickup's trigger will never fire.");
    }
}