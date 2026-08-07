using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [Header("Settings")]
    [SerializeField] private int startingMoney = 500;

    public static MoneyManager instance { get; private set; }
    private int currentMoney;
    public int CurrentMoney => currentMoney;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void Start()
    {
        currentMoney = startingMoney;
        UpdateUI();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        UpdateUI();
        Debug.Log("Money +Rs." + amount + "  Total: Rs." + currentMoney);
    }

    public void RemoveMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney = Mathf.Max(0, currentMoney - amount);
        UpdateUI();
        Debug.Log("Money -Rs." + amount + "  Total: Rs." + currentMoney);
    }

    private void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "Rs. " + currentMoney.ToString("N0");
    }
}
