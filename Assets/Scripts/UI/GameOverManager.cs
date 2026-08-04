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
            // Time Out: Wait 0.6s for brief pause + bounce down before opening menu
            TimerManager.Instance.onTimeUp.AddListener(() => TriggerGameOver(0.6f));
        }
    }

    public void TriggerGameOver(float initialDelay = 1.0f)
    {
        if (isGameOver) return;
        isGameOver = true;

        StartCoroutine(GameOverSequence(initialDelay));
    }

    private IEnumerator GameOverSequence(float initialDelay)
    {
        // Wait for Attack + Pause + Bounce Down sequence to complete
        yield return new WaitForSecondsRealtime(initialDelay);

        if (transition != null)
        {
            transition.Show();
        }
    }

    public void SetScore(int finalScore)
    {
        if (scoreText != null)
        {
            scoreText.text = finalScore.ToString();
        }
    }

    //private IEnumerator SlowTimeAndFreeze()
    //{
    //    float elapsed = 0f;
    //    float duration = transition != null ? transition.GetDimDuration() : 0.3f;

    //    while (elapsed < duration)
    //    {
    //        elapsed += Time.unscaledDeltaTime;
    //        Time.timeScale = Mathf.Lerp(1f, 0f, elapsed / duration);
    //        yield return null;
    //    }

    //    Time.timeScale = 0f;
    //}

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