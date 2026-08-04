using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class WinManager : MonoBehaviour
{
    public static WinManager Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private UIMenuTransition transition;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Optional Display")]
    [SerializeField] private TextMeshProUGUI victoryText;

    private bool isWin = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TriggerWin(float initialDelay = 0.8f)
    {
        if (isWin) return;
        isWin = true;

        StartCoroutine(WinSequence(initialDelay));
    }

    private IEnumerator WinSequence(float initialDelay)
    {
        // Wait for final boss death animation (bounce up + red + fade out) to complete
        yield return new WaitForSecondsRealtime(initialDelay);

        if (transition != null)
        {
            transition.Show();
        }

        yield return StartCoroutine(SlowTimeAndFreeze());
    }

    private IEnumerator SlowTimeAndFreeze()
    {
        float elapsed = 0f;
        float duration = transition != null ? transition.GetDimDuration() : 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        Time.timeScale = 0f;
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}