using UnityEngine;

/// <summary>
/// Generic trigger zone for a delivery-style mission. Configured as either a
/// Pickup or a Delivery point and wired to a <see cref="Mission2DeliveryJob"/>
/// controller. On player entry it routes to the controller, which enforces the
/// parcel rules. Reusable for any future pickup/deliver mission.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionPointTrigger : MonoBehaviour
{
    public enum PointType { Pickup, Delivery }

    [Tooltip("Whether this zone gives the parcel (Pickup) or completes it (Delivery).")]
    public PointType pointType = PointType.Pickup;

    private Mission2DeliveryJob controller;
    private bool consumed = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    /// <summary>Called by the controller to bind itself and the point type.</summary>
    public void Init(Mission2DeliveryJob owner, PointType type)
    {
        controller = owner;
        pointType = type;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed || controller == null || other == null || !other.CompareTag("Player"))
            return;

        if (pointType == PointType.Pickup)
            controller.OnReachedPickup();
        else
            controller.OnReachedDelivery();
    }

    /// <summary>Disables this point so it can no longer be triggered, and hides its visual.</summary>
    public void Consume()
    {
        consumed = true;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        // Hide any visual child marker too.
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }
}
