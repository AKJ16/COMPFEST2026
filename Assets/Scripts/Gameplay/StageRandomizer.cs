using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageRandomizer : MonoBehaviour
{
    public static StageRandomizer Instance { get; private set; }

    [Header("Stage Pool")]
    [Tooltip("List of gameplay scene names to pick from randomly.")]
    [SerializeField] private string[] stageScenes = new string[] { "Stage1", "Stage2", "Stage3" };

    [Tooltip("Avoid picking the exact same stage twice in a row.")]
    [SerializeField] private bool avoidImmediateRepeat = true;

    [Header("Fallback")]
    [SerializeField] private string fallbackScene = "Gameplay";

    private string _lastPickedScene = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persists across scenes so history and pool stay active!
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Returns a random scene name from the pool.
    /// </summary>
    public string GetRandomStage()
    {
        if (stageScenes == null || stageScenes.Length == 0)
        {
            Debug.LogWarning("[StageRandomizer] Stage pool is empty! Using fallback: " + fallbackScene);
            return fallbackScene;
        }

        if (stageScenes.Length == 1)
        {
            return stageScenes[0];
        }

        List<string> candidates = new List<string>(stageScenes);
        if (avoidImmediateRepeat && !string.IsNullOrEmpty(_lastPickedScene))
        {
            candidates.Remove(_lastPickedScene);
        }

        string picked = candidates[Random.Range(0, candidates.Count)];
        _lastPickedScene = picked;
        return picked;
    }

    /// <summary>
    /// Loads a random scene via LoadingManager.
    /// </summary>
    public void LoadRandomStage()
    {
        string targetScene = GetRandomStage();

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(targetScene);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }
}