using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("UI Fade References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.4f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSequence(sceneName));
    }

    /// <summary>
    /// Fades to black, executes a setup action (like advancing stages), then fades back in.
    /// </summary>
    public void FadeOutIn(Action onBlackScreen)
    {
        StartCoroutine(FadeOutInSequence(onBlackScreen));
    }

    private IEnumerator FadeOutInSequence(Action onBlackScreen)
    {
        Time.timeScale = 1f;

        if (canvasGroup == null)
        {
            onBlackScreen?.Invoke();
            yield break;
        }

        canvasGroup.blocksRaycasts = true;

        // 1. Fade to 100% Black
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            elapsed += dt;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 2. Execute Stage Setup Callback while screen is 100% black
        onBlackScreen?.Invoke();

        // Wait 2 frames for scene objects to update
        yield return null;
        yield return null;

        // 3. Fade In from Black
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            elapsed += dt;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator LoadSequence(string sceneName)
    {
        Time.timeScale = 1f;

        if (canvasGroup == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            elapsed += dt;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            canvasGroup.alpha = 1f;
            yield return null;
        }

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            canvasGroup.alpha = 1f;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        yield return null;
        canvasGroup.alpha = 1f;
        yield return null;

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            elapsed += dt;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}