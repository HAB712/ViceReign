using UnityEngine;

public class PayFineManager : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            PayFine();
        }
    }

    void PayFine()
    {
        if (WantedSystem.Instance == null || MoneyManager.instance == null)
            return;

        int wanted = WantedSystem.Instance.GetWantedLevel();

        if (wanted == 0)
            return;

        int fine = 0;

        switch (wanted)
        {
            case 1:
                fine = 100;
                break;

            case 2:
                fine = 200;
                break;

            case 3:
                fine = 500;
                break;
        }

        if (MoneyManager.instance.CurrentMoney >= fine)
        {
            MoneyManager.instance.RemoveMoney(fine);

            WantedSystem.Instance.ClearWantedLevel();

            if (RewardPopup.Instance != null)
                RewardPopup.Instance.Show("Fine Paid -Rs." + fine);
        }
        else
        {
            if (RewardPopup.Instance != null)
                RewardPopup.Instance.Show("You don't have enough money");
        }
    }
}