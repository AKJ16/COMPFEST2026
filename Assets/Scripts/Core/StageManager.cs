using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StageResult { InProgress, Win, Lose }

[System.Serializable]
public class StageReward
{
    public WeaponData weaponData;
    public int amount = 1;
}

[System.Serializable]
public class StageConfig
{
    public int stageNumber = 1;
    public EnemyHealth stageEnemy;

    [Header("Grid Dimensions for this Stage")]
    public int gridColumns = 5;
    public int gridRows = 5;

    [Header("Disabled Tiles & Bonus Weapons")]
    public List<Vector2Int> disabledGridCells;
    public StageReward[] bonusWeapons;
}

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Configurations")]
    [SerializeField] private List<StageConfig> stages = new List<StageConfig>();

    private EnemyHealth _currentEnemy;
    private int _currentStageIndex = 0;

    public StageResult Result { get; private set; } = StageResult.InProgress;
    public int CurrentStageNumber => _currentStageIndex + 1;
    public EnemyHealth CurrentEnemy => _currentEnemy;

    public event Action<StageResult> OnStageEnded;

    private void Awake()
    {
        Instance = this;
    }

    public void StartStage(int stageNumber, EnemyHealth enemy)
    {
        _currentStageIndex = Mathf.Max(0, stageNumber - 1);
        SetupStageInternal(_currentStageIndex, enemy, isInitialStart: true);
    }

    private void SetupStageInternal(int stageIndex, EnemyHealth overrideEnemy = null, bool isInitialStart = false)
    {
        Result = StageResult.InProgress;

        // 1. Determine Enemy
        if (overrideEnemy != null)
        {
            _currentEnemy = overrideEnemy;
        }
        else if (stageIndex < stages.Count && stages[stageIndex].stageEnemy != null)
        {
            _currentEnemy = stages[stageIndex].stageEnemy;
        }

        // 2. Toggle active enemy in scene
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].stageEnemy != null)
            {
                stages[i].stageEnemy.gameObject.SetActive(stages[i].stageEnemy == _currentEnemy);
            }
        }

        // 3. Determine Grid Dimensions
        int targetColumns = stageIndex < stages.Count ? stages[stageIndex].gridColumns : 5;
        int targetRows = stageIndex < stages.Count ? stages[stageIndex].gridRows : 5;
        var targetDisabled = stageIndex < stages.Count ? stages[stageIndex].disabledGridCells : null;

        // 4. Update Backend & Visual Grid Dimensions
        if (WeaponGridManager.Instance != null)
        {
            WeaponGridManager.Instance.SetGridSize(targetColumns, targetRows);
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.SetGridDimensions(targetColumns, targetRows, targetDisabled);
        }

        // 5. Add Stage Bonus Weapons (PERSISTING existing inventory!)
        if (!isInitialStart && stageIndex < stages.Count && stages[stageIndex].bonusWeapons != null)
        {
            foreach (var reward in stages[stageIndex].bonusWeapons)
            {
                if (reward.weaponData != null)
                {
                    InventorySystem.Instance.AddWeapon(reward.weaponData, reward.amount);
                    Debug.Log($"Added bonus weapon to inventory: +{reward.amount} {reward.weaponData.weaponName}");
                }
            }
        }

        // 6. Reset systems
        WeaponEffectsSystem.Instance.StartStage(_currentEnemy);

        if (_currentEnemy != null)
        {
            _currentEnemy.OnStateChanged += HandleEnemyStateChanged;
        }

        // 7. Restart Timer
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.StartTimer();
        }
    }

    public void CheckForEndOfStage()
    {
        if (Result != StageResult.InProgress) return;

        if (_currentEnemy != null && _currentEnemy.State == EnemyState.Dead)
        {
            EndStage(StageResult.Win);
            return;
        }

        if (!InventorySystem.Instance.HasAnyValidMove())
        {
            EndStage(StageResult.Lose);
        }
    }

    private void HandleEnemyStateChanged(EnemyState state)
    {
        if (state == EnemyState.Dead && Result == StageResult.InProgress)
        {
            EndStage(StageResult.Win);
        }
    }

    private void EndStage(StageResult result)
    {
        Result = result;
        OnStageEnded?.Invoke(result);

        if (_currentEnemy != null)
            _currentEnemy.OnStateChanged -= HandleEnemyStateChanged;

        if (result == StageResult.Win)
        {
            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.StopTimer();
            }

            StartCoroutine(AdvanceToNextStageSequence());
        }
    }

    private IEnumerator AdvanceToNextStageSequence()
    {
        yield return new WaitForSecondsRealtime(0.7f);

        int nextIndex = _currentStageIndex + 1;

        if (nextIndex < stages.Count)
        {
            if (LoadingManager.Instance != null)
            {
                LoadingManager.Instance.FadeOutIn(() =>
                {
                    _currentStageIndex = nextIndex;
                    SetupStageInternal(_currentStageIndex);
                });
            }
            else
            {
                _currentStageIndex = nextIndex;
                SetupStageInternal(_currentStageIndex);
            }
        }
        else
        {
            Debug.Log("🎉 ALL STAGES CLEARED! VICTORY!");
        }
    }
}