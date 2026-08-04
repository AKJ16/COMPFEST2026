using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI phaseText;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = new Color(0.75f, 0.15f, 0.15f);
    [SerializeField] private Color phase2Color = new Color(0.85f, 0.55f, 0.05f);

    [Header("Drain Animation Settings")]
    [SerializeField] private float drainDuration = 0.4f; // Time to drain bar & count numbers down

    private EnemyHealth _boundEnemy;
    private Coroutine _drainRoutine;
    private float _displayedHealth;

    private void Start()
    {
        if (phaseText != null)
        {
            phaseText.gameObject.SetActive(false);
        }
        if (fillImage != null)
        {
            fillImage.color = normalColor;
        }
    }

    private void Update()
    {
        var activeEnemy = StageManager.Instance != null ? StageManager.Instance.CurrentEnemy : null;
        if (activeEnemy != _boundEnemy)
        {
            BindToEnemy(activeEnemy);
        }
    }

    private void BindToEnemy(EnemyHealth newEnemy)
    {
        if (_boundEnemy != null)
        {
            _boundEnemy.OnDamaged -= HandleDamaged;
            _boundEnemy.OnStateChanged -= HandleStateChanged;
            _boundEnemy.OnPhaseTwoStarted -= HandlePhaseTwoStarted;
        }

        _boundEnemy = newEnemy;

        if (_boundEnemy == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        _boundEnemy.OnDamaged += HandleDamaged;
        _boundEnemy.OnStateChanged += HandleStateChanged;
        _boundEnemy.OnPhaseTwoStarted += HandlePhaseTwoStarted;

        if (phaseText != null)
        {
            phaseText.gameObject.SetActive(_boundEnemy.IsInPhase2);
        }

        if (fillImage != null)
        {
            fillImage.color = _boundEnemy.IsInPhase2 ? phase2Color : normalColor;
        }

        // Instant snap when first spawning/binding to an enemy
        SnapBarInstant();
    }

    private void HandleDamaged(int amount) => RefreshBar();
    private void HandleStateChanged(EnemyState state) => RefreshBar();

    private void HandlePhaseTwoStarted()
    {
        if (phaseText != null)
        {
            phaseText.gameObject.SetActive(true);
        }
        if (fillImage != null)
        {
            fillImage.color = phase2Color;
        }
    }

    private void RefreshBar()
    {
        if (_boundEnemy == null) return;

        if (_drainRoutine != null) StopCoroutine(_drainRoutine);
        _drainRoutine = StartCoroutine(DrainBarRoutine());
    }

    private void SnapBarInstant()
    {
        if (_boundEnemy == null) return;
        _displayedHealth = _boundEnemy.CurrentHealth;

        if (fillImage != null)
        {
            fillImage.fillAmount = (float)_boundEnemy.CurrentHealth / _boundEnemy.MaxHealth;
        }

        if (hpText != null)
        {
            hpText.text = $"{_boundEnemy.CurrentHealth}/{_boundEnemy.MaxHealth}";
        }
    }

    /// <summary>
    /// Smoothly animates fillAmount and counts down the HP numbers
    /// </summary>
    private IEnumerator DrainBarRoutine()
    {
        float startHp = _displayedHealth;
        float targetHp = _boundEnemy.CurrentHealth;
        float maxHp = _boundEnemy.MaxHealth;
        float elapsed = 0f;

        while (elapsed < drainDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / drainDuration;

            // Smooth lerp value
            _displayedHealth = Mathf.Lerp(startHp, targetHp, t);

            if (fillImage != null)
            {
                fillImage.fillAmount = _displayedHealth / maxHp;
            }

            if (hpText != null)
            {
                hpText.text = $"{Mathf.RoundToInt(_displayedHealth)}/{maxHp}";
            }

            yield return null;
        }

        _displayedHealth = targetHp;

        if (fillImage != null)
        {
            fillImage.fillAmount = targetHp / maxHp;
        }

        if (hpText != null)
        {
            hpText.text = $"{targetHp}/{maxHp}";
        }
    }

    private void OnDestroy()
    {
        if (_boundEnemy != null)
        {
            _boundEnemy.OnDamaged -= HandleDamaged;
            _boundEnemy.OnStateChanged -= HandleStateChanged;
            _boundEnemy.OnPhaseTwoStarted -= HandlePhaseTwoStarted;
        }
    }
}