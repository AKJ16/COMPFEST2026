using System.Collections;
using UnityEngine;

public class UIMenuTransition : MonoBehaviour
{
    [Header("UI Canvas Group References")]
    [SerializeField] private CanvasGroup dimOverlay;
    [SerializeField] private CanvasGroup mainPanelCanvasGroup;

    [Header("Fade Timings")]
    [SerializeField] private float dimDuration = 0.3f;
    [SerializeField] private float panelFadeDuration = 0.3f;

    [Header("Audio Ducking")]
    [Tooltip("Lower BGM volume while this panel is active.")]
    [SerializeField] private bool duckBGMOnShow = true;

    private CanvasGroup rootCanvasGroup;

    private void Awake()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
        ResetUI();
    }

    public void ResetUI()
    {
        if (dimOverlay != null)
        {
            dimOverlay.alpha = 0f;
            dimOverlay.blocksRaycasts = false;
            dimOverlay.interactable = false;
        }

        if (mainPanelCanvasGroup != null)
        {
            mainPanelCanvasGroup.alpha = 0f;
            mainPanelCanvasGroup.blocksRaycasts = false;
            mainPanelCanvasGroup.interactable = false;
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
        }
    }

    public void Show()
    {
        StopAllCoroutines();
        StartCoroutine(ShowSequence());
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(HideSequence());
    }

    private IEnumerator ShowSequence()
    {
        ResetUI();

        // 1. Lower BGM volume smoothly
        if (duckBGMOnShow && AudioManager.Instance != null)
        {
            AudioManager.Instance.DuckMusic(dimDuration);
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.blocksRaycasts = true;
            rootCanvasGroup.interactable = true;
        }

        if (dimOverlay != null)
        {
            dimOverlay.blocksRaycasts = true;
        }

        // STEP 1: Fade in background dim overlay
        float elapsed = 0f;
        while (elapsed < dimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dimDuration;

            if (dimOverlay != null)
            {
                dimOverlay.alpha = t;
            }

            if (mainPanelCanvasGroup != null)
            {
                mainPanelCanvasGroup.alpha = 0f;
            }

            yield return null;
        }

        if (dimOverlay != null) dimOverlay.alpha = 1f;

        // STEP 2: Fade in main panel
        elapsed = 0f;
        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / panelFadeDuration;

            if (mainPanelCanvasGroup != null)
            {
                mainPanelCanvasGroup.alpha = t;
            }
            yield return null;
        }

        if (mainPanelCanvasGroup != null)
        {
            mainPanelCanvasGroup.alpha = 1f;
            mainPanelCanvasGroup.blocksRaycasts = true;
            mainPanelCanvasGroup.interactable = true;
        }
    }

    private IEnumerator HideSequence()
    {
        // 1. Restore BGM volume back to 100%
        if (duckBGMOnShow && AudioManager.Instance != null)
        {
            AudioManager.Instance.UnduckMusic(panelFadeDuration);
        }

        if (mainPanelCanvasGroup != null)
        {
            mainPanelCanvasGroup.blocksRaycasts = false;
            mainPanelCanvasGroup.interactable = false;
        }
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
        }

        float elapsed = 0f;
        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / panelFadeDuration;

            if (mainPanelCanvasGroup != null)
            {
                mainPanelCanvasGroup.alpha = 1f - t;
            }
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < dimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dimDuration;

            if (dimOverlay != null)
            {
                dimOverlay.alpha = 1f - t;
            }
            yield return null;
        }

        ResetUI();
    }

    public float GetDimDuration() => dimDuration;
}