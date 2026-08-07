using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Dedicated UI for the petrol-pump refuel feature. Auto-creates its TMP
/// elements under the existing "GameUI" HUD canvas if they are not assigned,
/// so it never interferes with the existing FuelUI bar or other prompts.
///
/// Singleton, like FuelUI. Any PetrolPump talks to RefuelUI.Instance.
/// </summary>
public class RefuelUI : MonoBehaviour
{
    public static RefuelUI Instance { get; private set; }

    [Header("Text Elements (auto-created if missing)")]
    [SerializeField] private TextMeshProUGUI promptText;   // "Press R to Refuel"
    [SerializeField] private TextMeshProUGUI statusText;    // "Refueling... 43%"
    [SerializeField] private TextMeshProUGUI messageText;   // "Fuel Tank Full" / "Not Enough Money"

    [SerializeField] private float messageDuration = 2.5f;

    private Coroutine messageRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        EnsureUIExists();
        HidePrompt();
        HideStatus();
        SetActiveSafe(messageText, false);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public void ShowPrompt(string msg)
    {
        if (promptText == null) return;
        promptText.text = msg;
        promptText.gameObject.SetActive(true);
    }

    public void HidePrompt() => SetActiveSafe(promptText, false);

    public void ShowStatus(string msg)
    {
        if (statusText == null) return;
        statusText.text = msg;
        statusText.gameObject.SetActive(true);
    }

    public void HideStatus() => SetActiveSafe(statusText, false);

    public void ShowMessage(string msg)
    {
        if (messageText == null) return;
        messageText.text = msg;
        messageText.gameObject.SetActive(true);
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(HideMessageAfter(messageDuration));
    }

    private IEnumerator HideMessageAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    private static void SetActiveSafe(Component c, bool active)
    {
        if (c != null) c.gameObject.SetActive(active);
    }

    // ------------------------------------------------------------------
    // Auto-creation (mirrors the project's MissionUI approach)
    // ------------------------------------------------------------------

    private void EnsureUIExists()
    {
        if (promptText && statusText && messageText) return;

        Canvas canvas = FindHudCanvas();
        var root = new GameObject("RefuelUIPanel", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;

        if (promptText == null)
            promptText = CreateText(root, "RefuelPrompt", new Vector2(0.5f, 0f),
                new Vector2(0, 200), new Vector2(600, 50), 30, Color.white);

        if (statusText == null)
            statusText = CreateText(root, "RefuelStatus", new Vector2(0.5f, 0f),
                new Vector2(0, 250), new Vector2(600, 44), 26, new Color(0.4f, 0.9f, 1f));

        if (messageText == null)
            messageText = CreateText(root, "RefuelMessage", new Vector2(0.5f, 0.5f),
                new Vector2(0, 90), new Vector2(700, 60), 40, new Color(1f, 0.85f, 0.2f));
    }

    private Canvas FindHudCanvas()
    {
        foreach (var c in FindObjectsOfType<Canvas>(true))
            if (c.name == "GameUI") return c;
        var any = FindObjectOfType<Canvas>();
        if (any != null) return any;
        var go = new GameObject("RefuelCanvas", typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        return canvas;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchor,
        Vector2 anchoredPos, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.text = string.Empty;
        return tmp;
    }
}
