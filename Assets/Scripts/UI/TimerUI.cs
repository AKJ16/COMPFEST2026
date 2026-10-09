using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimerManager timer;
    [SerializeField] private Image fillImage;
    [Tooltip("Optional: Assign a specific UI element to scale/pulse. Leave empty to pulse this object.")]
    [SerializeField] private RectTransform pulseTarget;

    [Header("Audio SFX")]
    [SerializeField] private AudioClip tickSound;

    [Header("Visual Settings")]
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.25f;
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    private float maxTime;
    private bool pulsingHalf = false;
    private bool pulsingCritical = false;
    private Coroutine pulseRoutine;
    private Vector3 originalScale;

    private void Awake()
    {
        if (pulseTarget == null)
        {
            pulseTarget = GetComponent<RectTransform>();
        }
        originalScale = pulseTarget != null ? pulseTarget.localScale : Vector3.one;
    }

    private void Start()
    {
        if (timer == null) timer = TimerManager.Instance;

        if (timer != null)
        {
            maxTime = timer.GetMaxTime();

            // Auto-stop pulse and sound when time runs out
            timer.onTimeUp.AddListener(StopPulseAndReset);
        }

        if (StageManager.Instance != null)
        {
            // Auto-stop pulse and sound whenever the stage ends (Win or Lose)
            StageManager.Instance.OnStageEnded += (result) => StopPulseAndReset();
        }

        if (fillImage != null)
        {
            fillImage.color = normalColor;
        }
    }

    private void OnDestroy()
    {
        if (timer != null)
        {
            timer.onTimeUp.RemoveListener(StopPulseAndReset);
        }
    }

    private void Update()
    {
        if (timer == null || fillImage == null) return;

        float time = timer.GetTime();
        maxTime = timer.GetMaxTime();
        fillImage.fillAmount = timer.GetTimeNormalized();

        // FIX 1: If the timer stopped or hit 0 (Game Over / Won), immediately kill the pulse & audio!
        if (!timer.IsRunning() || time <= 0f)
        {
            if (pulsingCritical || pulseRoutine != null)
            {
                StopPulseAndReset();
            }
            return;
        }

        // 1. Halfway Warning (50%): Pulses yellow + plays tickSound ONCE
        if (time <= (maxTime / 2f) && !pulsingHalf && time > 10f)
        {
            pulsingHalf = true;
            StartPulse(warningColor, continuous: false);

            if (AudioManager.Instance != null && tickSound != null)
            {
                AudioManager.Instance.PlaySFX(tickSound);
            }
        }
        else if (time > (maxTime / 2f))
        {
            pulsingHalf = false;
            if (!pulsingCritical)
            {
                fillImage.color = normalColor;
            }
        }

        // 2. Critical Warning (<= 10s): Pulses red + continuous ticking sound
        if (time <= 10f && time > 0f && !pulsingCritical)
        {
            pulsingCritical = true;
            StartPulse(criticalColor, continuous: true);
        }
        else if (time > 10f && pulsingCritical)
        {
            pulsingCritical = false;
            StopPulseAndReset();
        }
    }

    private void StartPulse(Color color, bool continuous = false)
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }
        pulseRoutine = StartCoroutine(PulseRoutine(color, continuous));
    }

    public void StopPulseAndReset()
    {
        pulsingCritical = false;
        pulsingHalf = false;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (pulseTarget != null)
        {
            pulseTarget.localScale = originalScale;
        }

        if (fillImage != null)
        {
            float time = timer != null ? timer.GetTime() : 0f;
            fillImage.color = (time <= 0f) ? criticalColor : (time <= (maxTime / 2f) ? warningColor : normalColor);
        }
    }

    private IEnumerator PulseRoutine(Color color, bool continuous)
    {
        if (fillImage != null) fillImage.color = color;

        do
        {
            // FIX 2: If timer stops running or hits 0 mid-pulse, abort immediately!
            if (timer != null && (!timer.IsRunning() || timer.GetTime() <= 0f))
            {
                break;
            }

            if (continuous && AudioManager.Instance != null && tickSound != null)
            {
                AudioManager.Instance.PlaySFX(tickSound);
            }

            float t = 0f;
            while (t < pulseDuration)
            {
                t += Time.unscaledDeltaTime;
                float ping = Mathf.Sin((t / pulseDuration) * Mathf.PI);

                if (pulseTarget != null)
                {
                    pulseTarget.localScale = Vector3.Lerp(originalScale, originalScale * pulseScale, ping);
                }

                yield return null;
            }

            if (pulseTarget != null)
            {
                pulseTarget.localScale = originalScale;
            }

            if (continuous)
            {
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, 1f - pulseDuration));
            }
        } while (continuous && timer != null && timer.IsRunning() && timer.GetTime() > 0f);

        pulseRoutine = null;
    }
}