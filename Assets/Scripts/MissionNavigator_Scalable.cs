using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple GPS-style arrow pointer. Does NOT run its own mission logic -
/// it just rotates a UI arrow toward whatever target it's told to point at.
/// Your existing MissionManager / Mission2DeliveryJob scripts call
/// PointTo(...) and ClearTarget() at the right moments.
/// </summary>
public class MissionNavigator : MonoBehaviour
{
    public static MissionNavigator Instance { get; private set; }

    [Header("References")]
    public Transform player;
    public RectTransform arrowUI;
    public Text infoText; // optional, can leave empty

    private Transform currentTarget;
    private string currentLabel = "Objective";

    private void Awake()
    {
        Instance = this;
        SetArrowVisible(false);
    }

    private void Update()
    {
        if (currentTarget == null || player == null) return;
        UpdateArrow();
    }

    /// <summary>Point the arrow at a new target with an optional label.</summary>
    public void PointTo(Transform target, string label = "Objective")
    {
        currentTarget = target;
        currentLabel = label;
        SetArrowVisible(target != null);
    }

    /// <summary>Hide the arrow (no active objective).</summary>
    public void ClearTarget()
    {
        currentTarget = null;
        SetArrowVisible(false);
    }

    private void UpdateArrow()
    {
        if (arrowUI == null) return;

        Vector3 dir = currentTarget.position - player.position;
        dir.y = 0;

        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - player.eulerAngles.y;
        arrowUI.localEulerAngles = new Vector3(0, 0, -angle);

        if (infoText != null)
        {
            float dist = Vector3.Distance(player.position, currentTarget.position);
            infoText.text = $"{currentLabel} - {Mathf.RoundToInt(dist)}m";
        }
    }

    private void SetArrowVisible(bool visible)
    {
        if (arrowUI != null) arrowUI.gameObject.SetActive(visible);
    }
}