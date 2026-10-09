using UnityEngine;
using UnityEngine.Events;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; }

    [Header("Timer Settings")]
    [SerializeField] private float maxTime = 60f;
    [SerializeField] private float timeRemaining;
    private bool isRunning = false;

    [Header("Events")]
    public UnityEvent onTimeUp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        timeRemaining = maxTime;
    }

    private void Start()
    {
        StartTimer();
    }

    private void Update()
    {
        if (!isRunning) return;

        // 1. Time continues counting down normally (does NOT freeze during animations)
        timeRemaining -= Time.deltaTime;

        // 2. When time runs out:
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;

            if (WeaponEffectsSystem.IsBusy)
            {
                return;
            }

            // FIX: If the boss was killed or stage was won, do not trigger Time's Up!
            if (StageManager.Instance != null && StageManager.Instance.Result == StageResult.Win)
            {
                isRunning = false;
                return;
            }

            isRunning = false;
            Debug.Log("Time's Up!");
            onTimeUp?.Invoke();
        }
    }

    public void ResetTimer()
    {
        timeRemaining = maxTime;
    }

    public void StartTimer()
    {
        timeRemaining = maxTime;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public float GetTime() => timeRemaining;
    public float GetMaxTime() => maxTime;
    public float GetTimeNormalized() => timeRemaining / maxTime;
    public bool IsRunning() => isRunning;
}