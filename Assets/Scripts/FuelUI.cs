using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fuel HUD bar. Shows in corner when player is in any car.
/// Attach to a Canvas GameObject (FuelUICanvas).
/// </summary>
public class FuelUI : MonoBehaviour
{
    public static FuelUI Instance;

    [Header("UI References")]
    public GameObject fuelBarPanel;      // The whole fuel bar panel
    public Image fuelFillImage;          // Image with Fill method (horizontal)
    public TextMeshProUGUI fuelText;     // Optional: shows percentage
    public Image fuelBarBg;             // Background bar

    [Header("Color Thresholds")]
    public Color fullColor   = new Color(0.2f, 0.9f, 0.2f);   // green
    public Color medColor    = new Color(1f,   0.7f, 0f);      // orange
    public Color lowColor    = new Color(0.9f, 0.1f, 0.1f);   // red
    public float lowThreshold = 25f;
    public float medThreshold = 60f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        ShowFuelBar(false);
    }

    public void ShowFuelBar(bool show)
    {
        if (fuelBarPanel != null)
            fuelBarPanel.SetActive(show);
    }

    public void UpdateFuel(float amount)
    {
        float pct = Mathf.Clamp01(amount / 100f);

        if (fuelFillImage != null)
        {
            fuelFillImage.fillAmount = pct;
            // Color based on level
            if (amount <= lowThreshold)
                fuelFillImage.color = lowColor;
            else if (amount <= medThreshold)
                fuelFillImage.color = medColor;
            else
                fuelFillImage.color = fullColor;
        }

        if (fuelText != null)
            fuelText.text = $"Fuel: {amount:F0}%";
    }
}