using System.Collections.Generic;
using UnityEngine;

public enum EnemyState { Idle, Hurt, Dead }

// Handles enemy health, poison DoT ticking, and state transitions.
// WeaponEffectsSystem calls TakeDamage / ApplyPoison; VFX/Audio hook into OnStateChanged.
//
// BOSS PHASE (opsional, dipakai Enemy_Stage3):
// Kalau hasTwoPhases dicentang, begitu HP turun ke phase2ThresholdPercent (misal 50%),
// enemy masuk Phase 2: poison yang lagi jalan langsung berhenti, dan gak bisa
// di-apply lagi setelahnya. Maksa player ganti strategi ke damage langsung.
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private float poisonTickInterval = 1f;

    [Header("Boss Phase (opsional, kosongkan/uncheck untuk enemy biasa)")]
    [SerializeField] private bool hasTwoPhases = false;
    [Range(0.1f, 0.9f)]
    [SerializeField] private float phase2ThresholdPercent = 0.5f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public EnemyState State { get; private set; } = EnemyState.Idle;
    public bool IsInPhase2 { get; private set; } = false;

    public event System.Action<EnemyState> OnStateChanged;
    public event System.Action<int> OnDamaged;
    public event System.Action OnPhaseTwoStarted;

    private int _poisonPerTick;
    private float _poisonTimer;
    private bool _isPoisoned;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Update()
    {
        if (!_isPoisoned || State == EnemyState.Dead) return;

        _poisonTimer += Time.deltaTime;
        if (_poisonTimer >= poisonTickInterval)
        {
            _poisonTimer = 0f;
            TakeDamage(_poisonPerTick, isPoisonTick: true);
        }
    }

    public void TakeDamage(int amount, bool isPoisonTick = false)
    {
        if (State == EnemyState.Dead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnDamaged?.Invoke(amount);

        if (CurrentHealth <= 0)
        {
            SetState(EnemyState.Dead);
            return;
        }

        CheckForPhaseTransition();

        if (!isPoisonTick)
            SetState(EnemyState.Hurt);
    }

    public void ApplyPoison(int damagePerTick)
    {
        if (IsInPhase2) return;

        _isPoisoned = true;
        _poisonPerTick = Mathf.Max(_poisonPerTick, damagePerTick);
    }

    private void CheckForPhaseTransition()
    {
        if (!hasTwoPhases || IsInPhase2) return;

        float healthPercent = (float)CurrentHealth / maxHealth;
        if (healthPercent <= phase2ThresholdPercent)
        {
            IsInPhase2 = true;
            _isPoisoned = false;
            _poisonPerTick = 0;
            OnPhaseTwoStarted?.Invoke();
        }
    }

    private void SetState(EnemyState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);

        if (newState == EnemyState.Dead)
        {
            _isPoisoned = false;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("EnemyDeath");
        }
    }
}