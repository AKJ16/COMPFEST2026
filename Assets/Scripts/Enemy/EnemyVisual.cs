using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyVisual : MonoBehaviour
{
    [Header("Hit / Hurt Juice")]
    [SerializeField] private float hurtBounceHeight = 0.35f;
    [SerializeField] private float hurtDuration = 0.2f;

    [Header("Timer Out Juice")]
    [SerializeField] private float timeUpDropDistance = 0.4f;
    [SerializeField] private float timeUpDuration = 0.5f;

    [Header("Death Juice")]
    [SerializeField] private float deathBounceHeight = 0.5f;
    [SerializeField] private float deathDuration = 0.6f;

    private SpriteRenderer _spriteRenderer;
    private EnemyHealth _health;
    private Vector3 _originalPos;
    private Coroutine _currentAnimRoutine;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        // Default color is White
        _spriteRenderer.color = Color.white;
        _originalPos = transform.localPosition;
    }

    private void Start()
    {
        _health.OnStateChanged += HandleStateChanged;

        if (StageManager.Instance != null)
            StageManager.Instance.OnStageEnded += HandleStageEnded;

        if (TimerManager.Instance != null)
            TimerManager.Instance.onTimeUp.AddListener(HandleTimeUp);
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnStateChanged -= HandleStateChanged;

        if (StageManager.Instance != null)
            StageManager.Instance.OnStageEnded -= HandleStageEnded;

        if (TimerManager.Instance != null)
            TimerManager.Instance.onTimeUp.RemoveListener(HandleTimeUp);
    }

    private void HandleStateChanged(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Hurt:
                PlayRoutine(HurtRoutine());
                break;

            case EnemyState.Dead:
                PlayRoutine(DeathRoutine());
                break;
        }
    }

    private void HandleTimeUp()
    {
        if (_health != null && _health.State != EnemyState.Dead)
        {
            PlayRoutine(TimeUpBounceDownRoutine());
        }
    }

    private void HandleStageEnded(StageResult result)
    {
        if (result == StageResult.Lose && _health != null && _health.State != EnemyState.Dead)
        {
            PlayRoutine(TimeUpBounceDownRoutine());
        }
    }

    private void PlayRoutine(IEnumerator routine)
    {
        if (_currentAnimRoutine != null)
            StopCoroutine(_currentAnimRoutine);

        _currentAnimRoutine = StartCoroutine(routine);
    }

    // 1. HIT: Bounce UP + Flash RED -> Return to White
    private IEnumerator HurtRoutine()
    {
        float elapsed = 0f;
        _spriteRenderer.color = Color.red;

        while (elapsed < hurtDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / hurtDuration;

            // Parabolic jump arc UP
            float yOffset = Mathf.Sin(t * Mathf.PI) * hurtBounceHeight;
            transform.localPosition = _originalPos + new Vector3(0f, yOffset, 0f);

            // Lerp color from RED back to default WHITE
            _spriteRenderer.color = Color.Lerp(Color.red, Color.white, t);

            yield return null;
        }

        transform.localPosition = _originalPos;
        _spriteRenderer.color = Color.white;
    }

    // 2. TIMER OUT: Bounce DOWN
    private IEnumerator TimeUpBounceDownRoutine()
    {
        float elapsed = 0f;

        while (elapsed < timeUpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / timeUpDuration;

            // Dip DOWN arc
            float yOffset = -Mathf.Sin(t * Mathf.PI) * timeUpDropDistance;
            transform.localPosition = _originalPos + new Vector3(0f, yOffset, 0f);

            yield return null;
        }

        transform.localPosition = _originalPos;
    }

    // 3. DEAD: Bounce UP + Turn RED + Fade Out Alpha (1 -> 0)
    private IEnumerator DeathRoutine()
    {
        float elapsed = 0f;

        while (elapsed < deathDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / deathDuration;

            // Arc jump UP
            float yOffset = Mathf.Sin(t * Mathf.PI) * deathBounceHeight;
            transform.localPosition = _originalPos + new Vector3(0f, yOffset, 0f);

            // Red color with fading Alpha (1 -> 0)
            float alpha = Mathf.Lerp(1f, 0f, t);
            _spriteRenderer.color = new Color(1f, 0f, 0f, alpha);

            yield return null;
        }

        _spriteRenderer.color = new Color(1f, 0f, 0f, 0f);
        gameObject.SetActive(false);
    }
}