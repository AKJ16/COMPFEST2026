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
        Debug.Log("Start");
    }

    private void Update()
    {
        if (!isRunning) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            Debug.Log("Time's Up!");
            onTimeUp?.Invoke();
        }
    }

    public void StartTimer()
    {
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