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
        originalScale = pulseTarget.localScale;
    }

    private void Start()
    {
        if (timer == null) timer = TimerManager.Instance;

        if (timer != null)
        {
            maxTime = timer.GetMaxTime();
        }

        if (fillImage != null)
        {
            fillImage.color = normalColor;
        }
    }

    private void Update()
    {
        if (timer == null || fillImage == null) return;

        float time = timer.GetTime();
        fillImage.fillAmount = timer.GetTimeNormalized();

        // Halfway Warning Pulse (triggers once when passing 50%)
        if (time <= (maxTime / 2f) && !pulsingHalf && time > 10f)
        {
            pulsingHalf = true;
            StartPulse(warningColor, continuous: false);
        }

        // Critical Time Warning Pulse (continuously pulses when <= 10s)
        if (time <= 10f && time > 0f && !pulsingCritical)
        {
            pulsingCritical = true;
            StartPulse(criticalColor, continuous: true);
        }
    }

    private void StartPulse(Color color, bool continuous)
    {
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseRoutine(color, continuous));
    }

    private IEnumerator PulseRoutine(Color color, bool continuous)
    {
        fillImage.color = color;

        do
        {
            float t = 0f;
            while (t < pulseDuration)
            {
                t += Time.deltaTime;
                float ping = Mathf.Sin((t / pulseDuration) * Mathf.PI);

                pulseTarget.localScale = Vector3.Lerp(originalScale, originalScale * pulseScale, ping);

                yield return null;
            }

            pulseTarget.localScale = originalScale;

            if (continuous)
            {
                yield return new WaitForSeconds(0.5f);
            }
        } while (continuous);
    }
}