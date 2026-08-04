using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StageBannerUI : MonoBehaviour
{
    public static StageBannerUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private RectTransform bannerRect;

    [Header("Stage Names")]
    [SerializeField]
    private List<string> stageNames = new List<string>()
    {
        "STAGE - 1",
        "STAGE - 2",
        "STAGE - 3"
    };

    [Header("Animation Settings")]
    [SerializeField] private Vector2 startPos = new Vector2(0, 1000);
    [SerializeField] private Vector2 endPos = new Vector2(0, 350);
    [SerializeField] private float dropDuration = 0.7f;
    [SerializeField] private float delayBeforeDrop = 0.2f;

    private Coroutine _animRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (bannerRect == null)
            bannerRect = GetComponent<RectTransform>();

        ResetBanner();
    }

    public void ResetBanner()
    {
        if (_animRoutine != null)
            StopCoroutine(_animRoutine);

        if (bannerRect != null)
            bannerRect.anchoredPosition = startPos;
    }

    public void ShowStageBanner(int stageNumber)
    {
        ResetBanner();

        if (stageText != null)
        {
            // stageNumber is assumed to start at 1
            int index = stageNumber - 1;

            if (index >= 0 && index < stageNames.Count)
            {
                stageText.text = stageNames[index];
            }
            else
            {
                // Fallback if no name is assigned
                stageText.text = $"STAGE - {stageNumber}";
            }
        }

        _animRoutine = StartCoroutine(DropSequence());
    }

    private IEnumerator DropSequence()
    {
        if (delayBeforeDrop > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeDrop);

        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dropDuration;

            float s = 1f;
            float t2 = t - 1f;
            float bounce = (t2 * t2 * ((s + 1) * t2 + s) + 1f);

            if (bannerRect != null)
                bannerRect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, bounce);

            yield return null;
        }

        if (bannerRect != null)
            bannerRect.anchoredPosition = endPos;
    }
}