using UnityEngine;
using TMPro;
using System.Collections;

public class RewardPopup : MonoBehaviour
{
    public static RewardPopup Instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float moveDistance = 80f;

    private Vector3 startPosition;

    private void Awake()
    {
        Instance = this;

        startPosition = transform.localPosition;

        gameObject.SetActive(false);
    }

    public void Show(string message)
    {
        StopAllCoroutines();

        rewardText.text = message;

        transform.localPosition = startPosition;

        canvasGroup.alpha = 1;

        gameObject.SetActive(true);

        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        float timer = 0;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t = timer / duration;

            transform.localPosition =
                startPosition + Vector3.up * (moveDistance * t);

            canvasGroup.alpha = Mathf.Lerp(1, 0, t);

            yield return null;
        }

        transform.localPosition = startPosition;

        gameObject.SetActive(false);
    }
}