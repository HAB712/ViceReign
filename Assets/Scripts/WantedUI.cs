using UnityEngine;
using UnityEngine.UI;

public class WantedUI : MonoBehaviour
{
    public Image[] stars;
    public Sprite grayStar;
    public Sprite goldStar;

    [Header("Panel (optional)")]
    [Tooltip("Root panel to hide entirely when wanted level is 0.")]
    public GameObject wantedPanel;

    private void Start()
    {
        UpdateStars(0);

        if (WantedSystem.Instance != null)
            WantedSystem.Instance.OnWantedLevelChanged += UpdateStars;
    }

    private void OnDestroy()
    {
        if (WantedSystem.Instance != null)
            WantedSystem.Instance.OnWantedLevelChanged -= UpdateStars;
    }

    void UpdateStars(int level)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].sprite = (i < level) ? goldStar : grayStar;
        }

        // NEW: actually hide the wanted UI when cleared
        if (wantedPanel != null)
            wantedPanel.SetActive(level > 0);
    }
}