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

    private IEnumerator LoadSequence(string sceneName)
    {
        Time.timeScale = 1f;

        if (canvasGroup == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        canvasGroup.blocksRaycasts = true;

        // Fade to 100% Black FIRST
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f); // Prevent lag spike jumps
            elapsed += dt;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Start Async Load in background
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // Wait while Unity loads scene data into memory
        while (operation.progress < 0.9f)
        {
            canvasGroup.alpha = 1f; // Force 100% black overlay
            yield return null;
        }

        // Activate the new scene
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            canvasGroup.alpha = 1f;
            yield return null;
        }

        // WAIT 2 FRAMES for Stage 1 Awake/Start lag spike to settle
        canvasGroup.alpha = 1f;
        yield return null;
        canvasGroup.alpha = 1f;
        yield return null;

        // Smooth Fade In from Black
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            // Clamp DeltaTime so the first frame after loading cannot skip the fade animation
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            elapsed += dt;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}