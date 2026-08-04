using System.Collections;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Screen Overlay")]
    [SerializeField] private CanvasGroup blackScreenOverlay;
    [SerializeField] private float screenFadeInDuration = 0.6f;

    [Header("Logo / Title Animation")]
    [SerializeField] private RectTransform logoRect;
    [SerializeField] private Vector2 logoStartPos = new Vector2(0, 1000);
    [SerializeField] private Vector2 logoEndPos = new Vector2(0, 250);
    [SerializeField] private float logoDropDuration = 0.8f;
    [SerializeField] private float delayBeforeButtons = 0.2f;

    [Header("Buttons Setup")]
    [SerializeField] private CanvasGroup buttonGroup;
    [SerializeField] private float buttonFadeDuration = 0.5f;
    [SerializeField] private GameObject quitButton;

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private void Awake()
    {
        Time.timeScale = 1f;

        // Initial State: Screen completely black
        if (blackScreenOverlay != null)
        {
            blackScreenOverlay.alpha = 1f;
            blackScreenOverlay.blocksRaycasts = true;
        }

        // Initial State: Logo off-screen
        if (logoRect != null)
        {
            logoRect.anchoredPosition = logoStartPos;
        }

        // Initial State: Buttons invisible & non-interactable
        if (buttonGroup != null)
        {
            buttonGroup.alpha = 0f;
            buttonGroup.interactable = false;
            buttonGroup.blocksRaycasts = false;
        }

#if UNITY_WEBGL
        if (quitButton != null)
        {
            quitButton.SetActive(false);
        }
#endif
    }

    private void Start()
    {
        StartCoroutine(MainMenuSequence());
    }

    private IEnumerator MainMenuSequence()
    {
        float elapsed = 0f;

        // Fade Screen from Black
        if (blackScreenOverlay != null)
        {
            while (elapsed < screenFadeInDuration)
            {
                elapsed += Time.deltaTime;
                blackScreenOverlay.alpha = 1f - (elapsed / screenFadeInDuration);
                yield return null;
            }
            blackScreenOverlay.alpha = 0f;
            blackScreenOverlay.blocksRaycasts = false;
        }

        // Title / Logo Drops Down with Bounce
        elapsed = 0f;
        while (elapsed < logoDropDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / logoDropDuration;

            // Ease-out back / bounce curve math
            float s = 1.70158f;
            float t2 = t - 1.0f;
            float bounce = (t2 * t2 * ((s + 1) * t2 + s) + 1.0f);

            if (logoRect != null)
            {
                logoRect.anchoredPosition = Vector2.LerpUnclamped(logoStartPos, logoEndPos, bounce);
            }

            yield return null;
        }

        if (logoRect != null)
        {
            logoRect.anchoredPosition = logoEndPos;
        }

        yield return new WaitForSeconds(delayBeforeButtons);

        // Buttons Fade In
        elapsed = 0f;
        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.deltaTime;
            if (buttonGroup != null)
            {
                buttonGroup.alpha = elapsed / buttonFadeDuration;
            }
            yield return null;
        }

        if (buttonGroup != null)
        {
            buttonGroup.alpha = 1f;
            buttonGroup.interactable = true;
            buttonGroup.blocksRaycasts = true;
        }
    }

    public void PlayGame()
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(gameplaySceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
        }
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}