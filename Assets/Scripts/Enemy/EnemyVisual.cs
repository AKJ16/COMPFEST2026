using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyVisual : MonoBehaviour
{
    [Header("Hit / Hurt Juice")]
    [SerializeField] private float hurtBounceHeight = 0.35f;
    [SerializeField] private float hurtDuration = 0.25f;
    [SerializeField] private Color poisonColor = new Color(0.2f, 0.85f, 0.3f);

    [Header("Timer Out Juice")]
    [SerializeField] private float timeUpDropDistance = 0.4f;
    [SerializeField] private float timeUpDuration = 0.35f;

    [Header("Death Juice")]
    [SerializeField] private float deathBounceHeight = 0.5f;
    [SerializeField] private float deathDuration = 0.6f;

    private SpriteRenderer _spriteRenderer;
    private EnemyHealth _health;
    private Vector3 _originalPos;
    private Coroutine _currentAnimRoutine;

    private bool _gotPoisonDamageThisFrame = false;
    private bool _gotNormalDamageThisFrame = false;
    private Coroutine _damageFrameRoutine = null;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        _spriteRenderer.color = Color.white;
        _originalPos = transform.localPosition;
    }

    private void Start()
    {
        _health.OnStateChanged += HandleStateChanged;
        _health.OnDamagedDetail += HandleDamagedDetail;

        if (StageManager.Instance != null)
            StageManager.Instance.OnStageEnded += HandleStageEnded;

        if (TimerManager.Instance != null)
            TimerManager.Instance.onTimeUp.AddListener(HandleTimeUp);
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnStateChanged -= HandleStateChanged;
            _health.OnDamagedDetail -= HandleDamagedDetail;
        }

        if (StageManager.Instance != null)
            StageManager.Instance.OnStageEnded -= HandleStageEnded;

        if (TimerManager.Instance != null)
            TimerManager.Instance.onTimeUp.RemoveListener(HandleTimeUp);
    }

    private void HandleDamagedDetail(int damageAmount, bool isPoison)
    {
        if (isPoison) _gotPoisonDamageThisFrame = true;
        else _gotNormalDamageThisFrame = true;

        if (_damageFrameRoutine == null)
        {
            _damageFrameRoutine = StartCoroutine(ProcessDamageFrame());
        }
    }

    private IEnumerator ProcessDamageFrame()
    {
        yield return new WaitForEndOfFrame();

        if (_health != null && _health.State != EnemyState.Dead)
        {
            Color flashColor;
            bool shouldBounce;

            if (_gotPoisonDamageThisFrame && _gotNormalDamageThisFrame)
            {
                flashColor = poisonColor;
                shouldBounce = true;
            }
            else if (_gotPoisonDamageThisFrame)
            {
                flashColor = poisonColor;
                shouldBounce = false;
            }
            else
            {
                flashColor = Color.red;
                shouldBounce = true;
            }

            PlayRoutine(HurtRoutine(flashColor, shouldBounce));
        }

        _gotPoisonDamageThisFrame = false;
        _gotNormalDamageThisFrame = false;
        _damageFrameRoutine = null;
    }

    private void HandleStateChanged(EnemyState state)
    {
        if (state == EnemyState.Dead)
        {
            PlayRoutine(DeathRoutine());
        }
    }

    private void HandleTimeUp()
    {
        if (_health != null && _health.State != EnemyState.Dead)
        {
            StartCoroutine(DelayedBounceDownSequence(0.15f));
        }
    }

    private void HandleStageEnded(StageResult result)
    {
        if (result == StageResult.Lose && _health != null && _health.State != EnemyState.Dead)
        {
            StartCoroutine(DelayedBounceDownSequence(0.6f));
        }
    }

    private void PlayRoutine(IEnumerator routine)
    {
        if (_currentAnimRoutine != null)
            StopCoroutine(_currentAnimRoutine);

        _currentAnimRoutine = StartCoroutine(routine);
    }

    private IEnumerator HurtRoutine(Color flashColor, bool shouldBounce)
    {
        float elapsed = 0f;
        _spriteRenderer.color = flashColor;

        while (elapsed < hurtDuration)
        {
            // Fixed: Uses Time.deltaTime so pausing smoothly freezes hit movement
            elapsed += Time.deltaTime;
            float t = elapsed / hurtDuration;

            if (shouldBounce)
            {
                float yOffset = Mathf.Sin(t * Mathf.PI) * hurtBounceHeight;
                transform.localPosition = _originalPos + new Vector3(transform.localPosition.x, yOffset, transform.localPosition.y);
            }

            _spriteRenderer.color = Color.Lerp(flashColor, Color.white, t);

            yield return null;
        }

        transform.localPosition = _originalPos;
        _spriteRenderer.color = Color.white;
    }

    private IEnumerator DelayedBounceDownSequence(float delayBeforeBounce)
    {
        if (delayBeforeBounce > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBeforeBounce);
        }

        yield return StartCoroutine(TimeUpBounceDownRoutine());
    }

    private IEnumerator TimeUpBounceDownRoutine()
    {
        float elapsed = 0f;
        _spriteRenderer.color = Color.white;

        while (elapsed < timeUpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / timeUpDuration;

            float yOffset = -Mathf.Sin(t * Mathf.PI) * timeUpDropDistance;
            transform.localPosition = _originalPos + new Vector3(transform.localPosition.x, yOffset, transform.localPosition.y);

            yield return null;
        }

        transform.localPosition = _originalPos + new Vector3(0f, -timeUpDropDistance * 0.4f, 0f);
    }

    private IEnumerator DeathRoutine()
    {
        float elapsed = 0f;

        while (elapsed < deathDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / deathDuration;

            float yOffset = Mathf.Sin(t * Mathf.PI) * deathBounceHeight;
            transform.localPosition = _originalPos + new Vector3(transform.localPosition.x, yOffset, transform.localPosition.y);

            float alpha = Mathf.Lerp(1f, 0f, t);
            _spriteRenderer.color = new Color(1f, 0f, 0f, alpha);

            yield return null;
        }

        _spriteRenderer.color = new Color(1f, 0f, 0f, 0f);
        gameObject.SetActive(false);
    }
}