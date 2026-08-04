using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private UIMenuTransition transition;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Optional Display")]
    [SerializeField] private TextMeshProUGUI scoreText;

    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.onTimeUp.AddListener(TriggerGameOver);
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // 1. WAIT 0.4s FIRST so enemy bounce-down animation plays completely on screen
        yield return new WaitForSecondsRealtime(0.5f);

        // 2. NOW show the Game Over transition & screen dim
        if (transition != null)
        {
            transition.Show();
        }

        // 3. Smoothly pause time
        yield return StartCoroutine(SlowTimeAndFreeze());
    }

    public void SetScore(int finalScore)
    {
        if (scoreText != null)
        {
            scoreText.text = finalScore.ToString();
        }
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