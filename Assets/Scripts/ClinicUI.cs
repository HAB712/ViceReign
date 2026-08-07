using System.Collections;
using UnityEngine;
using TMPro;

public class ClinicUI : MonoBehaviour
{
    public static ClinicUI Instance;

    [Header("UI")]
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI messageText;

    [SerializeField] float messageTime = 2f;

    Coroutine routine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        HidePrompt();

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    public void ShowPrompt(string msg)
    {
        if (promptText == null) return;

        promptText.text = msg;
        promptText.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    public void ShowMessage(string msg)
    {
        if (messageText == null) return;

        messageText.text = msg;
        messageText.gameObject.SetActive(true);

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(HideMessage());
    }

    IEnumerator HideMessage()
    {
        yield return new WaitForSeconds(messageTime);

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }
}