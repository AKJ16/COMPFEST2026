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

    [Header("Stage Settings")]
    [Tooltip("Check this if this is a tutorial scene. (Auto-detects if scene name contains 'tutorial')")]
    [SerializeField] private bool isTutorialScene = false;
    public bool IsTutorialScene => isTutorialScene || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Contains("tutorial");

    [Header("Stage Configurations")]
    [SerializeField] private List<StageConfig> stages = new List<StageConfig>();

    [Header("Audio BGM & SFX")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip stageWinSfx;

    private EnemyHealth _currentEnemy;
    private int _currentStageIndex = 0;
    private bool _isStageEnding = false; // LOCK: Prevents multiple EndStage calls from overlapping
    private Coroutine _advanceRoutine;

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
        _isStageEnding = false; // Reset lock for the new stage
        _currentStageIndex = stageIndex;

        // Reset Book usage limits for this stage
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ResetStageUsage();
        }

        if (AudioManager.Instance != null && gameplayMusic != null)
        {
            AudioManager.Instance.PlayMusic(gameplayMusic);
        }

        if (overrideEnemy != null)
        {
            _currentEnemy = overrideEnemy;
        }
        else if (stageIndex < stages.Count && stages[stageIndex].stageEnemy != null)
        {
            _currentEnemy = stages[stageIndex].stageEnemy;
        }

        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].stageEnemy != null)
            {
                stages[i].stageEnemy.gameObject.SetActive(stages[i].stageEnemy == _currentEnemy);
            }
        }

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

        // Add Stage Bonus Weapons for THIS specific stage
        if (!isInitialStart && stageIndex < stages.Count && stages[stageIndex].bonusWeapons != null)
        {
            Debug.Log($"[StageManager] Setting up Stage {stageIndex + 1}. Granting rewards for Stage {stageIndex + 1}...");

            foreach (var reward in stages[stageIndex].bonusWeapons)
            {
                if (reward.weaponData != null && reward.amount > 0)
                {
                    InventorySystem.Instance.AddWeapon(reward.weaponData, reward.amount);
                    Debug.Log($"[StageManager] Granted +{reward.amount} {reward.weaponData.weaponName} for Stage {stageIndex + 1}");
                }
            }
        }

        WeaponEffectsSystem.Instance.StartStage(_currentEnemy);

        if (_currentEnemy != null)
        {
            _currentEnemy.OnStateChanged += HandleEnemyStateChanged;
        }

        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.ResetTimer();
            TimerManager.Instance.StartTimer();
        }

        if (StageBannerUI.Instance != null)
        {
            StageBannerUI.Instance.ShowStageBanner(CurrentStageNumber);
        }

        if (BossIntroUI.Instance != null && _currentEnemy != null)
        {
            bool isBoss = stageIndex < stages.Count && stages[stageIndex].isBossStage;
            string p2Desc = stageIndex < stages.Count ? stages[stageIndex].phase2MechanicDescription : "";
            BossIntroUI.Instance.BindEnemy(_currentEnemy, isBoss, p2Desc);
        }
    }

    public void CheckForEndOfStage()
    {
        // Ignore if stage is already ending or animations are active
        if (Result != StageResult.InProgress || _isStageEnding) return;
        if (WeaponEffectsSystem.IsBusy) return;

        // 1. Check Win
        if (_currentEnemy != null && (_currentEnemy.State == EnemyState.Dead || _currentEnemy.CurrentHealth <= 0))
        {
            EndStage(StageResult.Win);
            return;
        }

        // 2. Check Lose
        if (!InventorySystem.Instance.HasAnyValidMove())
        {
            EndStage(StageResult.Lose);
        }
    }

    private void HandleEnemyStateChanged(EnemyState state)
    {
        if (state == EnemyState.Dead)
        {
            EndStage(StageResult.Win);
        }
    }

    private void EndStage(StageResult result)
    {
        // FIX: Prevent multiple/rapid calls from overlapping
        if (_isStageEnding) return;
        _isStageEnding = true;

        Result = result;
        OnStageEnded?.Invoke(result);

        if (_currentEnemy != null)
            _currentEnemy.OnStateChanged -= HandleEnemyStateChanged;

        if (TimerManager.Instance != null)
            TimerManager.Instance.StopTimer();

        if (result == StageResult.Win)
        {
            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.CancelGameOver();
            }

            if (AudioManager.Instance != null && stageWinSfx != null)
            {
                AudioManager.Instance.PlaySFX(stageWinSfx);
            }

            if (_advanceRoutine != null) StopCoroutine(_advanceRoutine);
            _advanceRoutine = StartCoroutine(AdvanceToNextStageSequence());
        }
        else if (result == StageResult.Lose)
        {
            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.TriggerGameOver(0.60f);
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

                    SetupStageInternal(nextIndex);
                });
            }
            else
            {
                SetupStageInternal(nextIndex);
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

        _advanceRoutine = null;
    }
}