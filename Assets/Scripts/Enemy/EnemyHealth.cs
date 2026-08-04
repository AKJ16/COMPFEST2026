using System.Collections;
using UnityEngine;

public enum EnemyState { Idle, Hurt, Dead }

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;

    [Header("Boss Phase (opsional)")]
    [SerializeField] private bool hasTwoPhases = false;
    [Range(0.1f, 0.9f)]
    [SerializeField] private float phase2ThresholdPercent = 0.5f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public EnemyState State { get; private set; } = EnemyState.Idle;
    public bool IsInPhase2 { get; private set; } = false;

    public event System.Action<EnemyState> OnStateChanged;
    public event System.Action<int> OnDamaged;
    public event System.Action<int, bool> OnDamagedDetail; // (damageAmount, isPoison)
    public event System.Action OnPhaseTwoStarted;

    private readonly System.Collections.Generic.Dictionary<WeaponInstance, int> _poisonSources = new System.Collections.Generic.Dictionary<WeaponInstance, int>();

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TickPoisonTurn()
    {
        if (State == EnemyState.Dead || _poisonSources.Count == 0) return;

        int totalPoisonDamage = 0;
        foreach (var kvp in _poisonSources)
        {
            totalPoisonDamage += kvp.Value;
        }

        if (totalPoisonDamage > 0)
        {
            TakeDamage(totalPoisonDamage, isPoisonTick: true);
        }
    }

    public void TakeDamage(int amount, bool isPoisonTick = false)
    {
        if (State == EnemyState.Dead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnDamaged?.Invoke(amount);
        OnDamagedDetail?.Invoke(amount, isPoisonTick);

        if (CurrentHealth <= 0)
        {
            SetState(EnemyState.Dead);
            return;
        }

        CheckForPhaseTransition();

        if (!isPoisonTick)
            SetState(EnemyState.Hurt);
    }

    public void ApplyPoison(WeaponInstance sourceInstance, int damagePerTick)
    {
        if (IsInPhase2 || sourceInstance == null) return;

        _poisonSources[sourceInstance] = damagePerTick;
    }

    private void CheckForPhaseTransition()
    {
        if (!hasTwoPhases || IsInPhase2) return;

        float healthPercent = (float)CurrentHealth / maxHealth;
        if (healthPercent <= phase2ThresholdPercent)
        {
            IsInPhase2 = true;
            _poisonSources.Clear();
            OnPhaseTwoStarted?.Invoke();
        }
    }

    private void SetState(EnemyState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);

        if (newState == EnemyState.Dead)
        {
            _poisonSources.Clear();
        }
    }
}