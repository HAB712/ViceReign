using UnityEngine;
using System;

public class WantedSystem : MonoBehaviour
{
    public static WantedSystem Instance;

    [Header("Wanted Settings")]
    [Range(0, 3)]
    public int currentWantedLevel = 0;

    public event Action<int> OnWantedLevelChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddWantedLevel(int stars)
    {
        currentWantedLevel += stars;

        if (currentWantedLevel > 3)
            currentWantedLevel = 3;

      ///  Debug.Log("Wanted Level : " + currentWantedLevel);

        OnWantedLevelChanged?.Invoke(currentWantedLevel);
    }

    public void ClearWantedLevel()
    {
        currentWantedLevel = 0;

      ///  Debug.Log("Wanted Cleared");

        OnWantedLevelChanged?.Invoke(currentWantedLevel);

        if (PoliceManager.Instance != null)
{
    PoliceManager.Instance.CalmAllPolice();
}
    }

    public int GetWantedLevel()
    {
        return currentWantedLevel;
    }
}