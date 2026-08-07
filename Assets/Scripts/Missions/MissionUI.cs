using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Drives all mission-related on-screen text. If the TMP elements are not
/// assigned in the inspector, they are created automatically at runtime under
/// the existing game HUD canvas (or a new canvas if none exists).
/// </summary>
public class MissionUI : MonoBehaviour
{
    [Header("Text Elements (auto-created if missing)")]
    [SerializeField] private TextMeshProUGUI objectiveText; // "Find 3 clues..."
    [SerializeField] private TextMeshProUGUI progressText;  // "Clues Found: 0/3"
    [SerializeField] private TextMeshProUGUI startPrompt;   // "Press M to start..."
    [SerializeField] private TextMeshProUGUI rewardPopup;   // "+ Rs 150"
    [SerializeField] private TextMeshProUGUI missionComplete;// "Mission Completed"

    [Header("Timings")]
    [SerializeField] private float rewardPopupDuration = 3f;
    [SerializeField] private float missionCompleteDuration = 4f;

    private void Awake()
    {
        EnsureUIExists();
        // Everything starts hidden; the manager turns elements on as needed.
        SetActiveSafe(objectiveText, false);
        SetActiveSafe(progressText, false);
        SetActiveSafe(startPrompt, false);
        SetActiveSafe(rewardPopup, false);
        SetActiveSafe(missionComplete, false);
    }

    // ------------------------------------------------------------------
    // Public API (called by MissionManager / MissionStartPoint)
    // ------------------------------------------------------------------

    public void ShowStartPrompt(string message)
    {
        if (startPrompt == null) return;
        startPrompt.text = message;
        startPrompt.gameObject.SetActive(true);
    }

    public void HideStartPrompt() => SetActiveSafe(startPrompt, false);

    public void ShowObjective(string objective)
    {
        if (objectiveText != null)
        {
            objectiveText.text = "Objective: " + objective;
            objectiveText.gameObject.SetActive(true);
        }
        // Progress is only meaningful during the clue phase.
        SetActiveSafe(progressText, true);
    }

    /// <summary>
    /// Shows an objective line WITHOUT the clue-progress counter. Used by
    /// missions that don't collect counted items (e.g. the delivery job).
    /// </summary>
    public void ShowObjectiveNoProgress(string objective)
    {
        if (objectiveText != null)
        {
            objectiveText.text = "Objective: " + objective;
            objectiveText.gameObject.SetActive(true);
        }
        SetActiveSafe(progressText, false);
    }


    public void ClearObjective()
    {
        SetActiveSafe(objectiveText, false);
        SetActiveSafe(progressText, false);
    }

    public void UpdateProgress(int found, int total)
    {
        if (progressText == null) return;
        progressText.text = "Clues Found: " + found + "/" + total;
        progressText.gameObject.SetActive(true);
    }

    public void ShowRewardPopup(int amount)
    {
        if (rewardPopup == null) return;
        rewardPopup.text = "+ Rs " + amount;
        rewardPopup.gameObject.SetActive(true);
        RestartRoutine(ref rewardRoutine, HideAfter(rewardPopup, rewardPopupDuration));
    }

    public void ShowMissionComplete()
    {
        if (missionComplete == null) return;
        missionComplete.text = "Mission Completed";
        missionComplete.gameObject.SetActive(true);
        RestartRoutine(ref completeRoutine, HideAfter(missionComplete, missionCompleteDuration));
    }

    // ------------------------------------------------------------------
    // Internal helpers
    // ------------------------------------------------------------------

    private Coroutine rewardRoutine;
    private Coroutine completeRoutine;

    private void RestartRoutine(ref Coroutine handle, IEnumerator routine)
    {
        if (handle != null) StopCoroutine(handle);
        handle = StartCoroutine(routine);
    }

    private IEnumerator HideAfter(TextMeshProUGUI t, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (t != null) t.gameObject.SetActive(false);
    }

    private static void SetActiveSafe(Component c, bool active)
    {
        if (c != null) c.gameObject.SetActive(active);
    }

    /// <summary>
    /// Creates the TMP hierarchy if it does not already exist. Re-uses the
    /// existing "GameUI" canvas when present so mission text shares the HUD.
    /// </summary>
    private void EnsureUIExists()
    {
        // If all references are present we have nothing to build.
        if (objectiveText && progressText && startPrompt && rewardPopup && missionComplete)
            return;

        Canvas canvas = FindHudCanvas();
        RectTransform root = new GameObject("MissionUIPanel", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        Stretch(root);

        if (objectiveText == null)
            objectiveText = CreateText(root, "ObjectiveText", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(560, 40), 26, TextAlignmentOptions.TopLeft, Color.white);

        if (progressText == null)
            progressText = CreateText(root, "ProgressText", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -64), new Vector2(560, 36), 22, TextAlignmentOptions.TopLeft, new Color(1f, 0.9f, 0.4f));

        if (startPrompt == null)
            startPrompt = CreateText(root, "StartPrompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 140), new Vector2(700, 50), 30, TextAlignmentOptions.Center, Color.white);

        if (rewardPopup == null)
            rewardPopup = CreateText(root, "RewardPopup", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -60), new Vector2(400, 60), 40, TextAlignmentOptions.Center, new Color(0.3f, 1f, 0.3f));

        if (missionComplete == null)
            missionComplete = CreateText(root, "MissionComplete", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 40), new Vector2(700, 70), 48, TextAlignmentOptions.Center, new Color(0.3f, 1f, 0.5f));
    }

    private Canvas FindHudCanvas()
    {
        // Prefer a canvas literally named "GameUI" (the project's HUD).
        foreach (var c in FindObjectsOfType<Canvas>(true))
            if (c.name == "GameUI") return c;

        // Otherwise use any existing canvas.
        var any = FindObjectOfType<Canvas>();
        if (any != null) return any;

        // Last resort: build a fresh overlay canvas.
        var go = new GameObject("MissionCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        return canvas;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin; // pivot follows anchor so offsets read intuitively
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.enableWordWrapping = true;
        tmp.text = string.Empty;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
