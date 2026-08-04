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

    private CanvasGroup rootCanvasGroup;

    private void Awake()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
        ResetUI();
    }

    /// Resets all UI elements to fully transparent and non-blocking.
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

        if (mainPanelCanvasGroup == null)
        {
            Debug.LogWarning($"[UIMenuTransition] 'Main Panel Canvas Group' is NOT assigned on {gameObject.name}! Please assign it in the Inspector.", gameObject);
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
        }

        // Fade in background dim overlay ONLY (Keep main panel strictly invisible)
        float elapsed = 0f;
        while (elapsed < dimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dimDuration;

            if (dimOverlay != null)
            {
                dimOverlay.alpha = t;
            }

            // Force main panel to stay completely invisible during dimming
            if (mainPanelCanvasGroup != null)
            {
                mainPanelCanvasGroup.alpha = 0f;
            }

            yield return null;
        }

        if (dimOverlay != null) dimOverlay.alpha = 1f;

        // Fade in main panel now that screen is dimmed
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

        // Enable raycast blocking once fully visible
        if (mainPanelCanvasGroup != null)
        {
            mainPanelCanvasGroup.alpha = 1f;
            mainPanelCanvasGroup.blocksRaycasts = true;
            mainPanelCanvasGroup.interactable = true;
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.blocksRaycasts = true;
            rootCanvasGroup.interactable = true;
        }
    }

    private IEnumerator HideSequence()
    {
        // Immediately disable raycast blocking when hide starts
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

        // Fade out main panel
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

        // Fade out dim overlay
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