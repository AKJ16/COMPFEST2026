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

    [Header("Boss Announcement Settings")]
    public bool isBossStage = false;
    [TextArea(2, 3)]
    public string phase2MechanicDescription = "IMMUNE TO POISON! Direct damage only!";

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

        // 2. Toggle active enemy
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].stageEnemy != null)
            {
                stages[i].stageEnemy.gameObject.SetActive(stages[i].stageEnemy == _currentEnemy);
            }
        }

        // 3. Grid Setup
        int targetColumns = stageIndex < stages.Count ? stages[stageIndex].gridColumns : 5;
        int targetRows = stageIndex < stages.Count ? stages[stageIndex].gridRows : 5;
        var targetDisabled = stageIndex < stages.Count ? stages[stageIndex].disabledGridCells : null;

        if (WeaponGridManager.Instance != null)
        {
            WeaponGridManager.Instance.SetGridSize(targetColumns, targetRows);
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.SetGridDimensions(targetColumns, targetRows, targetDisabled);
        }

        // 4. Add Stage Bonus Weapons
        if (!isInitialStart && stageIndex < stages.Count && stages[stageIndex].bonusWeapons != null)
        {
            foreach (var reward in stages[stageIndex].bonusWeapons)
            {
                if (reward.weaponData != null)
                {
                    InventorySystem.Instance.AddWeapon(reward.weaponData, reward.amount);
                }
            }
        }

        // 5. Reset systems
        WeaponEffectsSystem.Instance.StartStage(_currentEnemy);

        if (_currentEnemy != null)
        {
            _currentEnemy.OnStateChanged += HandleEnemyStateChanged;
        }

        // 6. Reset & Start Timer
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.ResetTimer();
            TimerManager.Instance.StartTimer();
        }

        // 7. Trigger Stage Banner Dropdown
        if (StageBannerUI.Instance != null)
        {
            StageBannerUI.Instance.ShowStageBanner(CurrentStageNumber);
        }

        // 8. Bind Boss Announcement UI
        if (BossIntroUI.Instance != null && _currentEnemy != null)
        {
            bool isBoss = stageIndex < stages.Count && stages[stageIndex].isBossStage;
            string p2Desc = stageIndex < stages.Count ? stages[stageIndex].phase2MechanicDescription : "";
            BossIntroUI.Instance.BindEnemy(_currentEnemy, isBoss, p2Desc);
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

        if (TimerManager.Instance != null)
            TimerManager.Instance.StopTimer();

        if (result == StageResult.Win)
        {
            StartCoroutine(AdvanceToNextStageSequence());
        }
        else if (result == StageResult.Lose)
        {
            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.TriggerGameOver(1.0f);
            }
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
                    if (StageBannerUI.Instance != null)
                    {
                        StageBannerUI.Instance.ResetBanner();
                    }

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

            if (WinManager.Instance != null)
            {
                WinManager.Instance.TriggerWin(0.8f);
            }
        }
    }
}