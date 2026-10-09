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

    [Header("Audio BGM & SFX")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip stageWinSfx;

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
        SetupStageInternal(_currentStageIndex, enemy);
    }

    private void SetupStageInternal(int stageIndex, EnemyHealth overrideEnemy = null)
    {
        Result = StageResult.InProgress;

        // Reset Book usage limits for this new stage!
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

    /// <summary>
    /// Grants bonus weapons for the current stage so popups appear on screen.
    /// </summary>
    public void GrantStageBonusWeapons(int stageIndex)
    {
        if (stageIndex < stages.Count && stages[stageIndex].bonusWeapons != null)
        {
            foreach (var reward in stages[stageIndex].bonusWeapons)
            {
                if (reward.weaponData != null)
                {
                    InventorySystem.Instance.AddWeapon(reward.weaponData, reward.amount);
                }
            }
        }
    }

    public void CheckForEndOfStage()
    {
        if (Result != StageResult.InProgress) return;

        // NEVER check for Game Over while attacks or Hourglasses are still resolving!
        if (WeaponEffectsSystem.IsBusy) return;

        // 1. ALWAYS check if the boss died first!
        if (_currentEnemy != null && (_currentEnemy.State == EnemyState.Dead || _currentEnemy.CurrentHealth <= 0))
        {
            EndStage(StageResult.Win);
            return;
        }

        // 2. Only lose if the boss is definitely alive AND you have no moves left
        if (!InventorySystem.Instance.HasAnyValidMove())
        {
            EndStage(StageResult.Lose);
        }
    }

    private void HandleEnemyStateChanged(EnemyState state)
    {
        // FIX: If the boss dies, WIN ALWAYS WINS (even if Lose was pending)!
        if (state == EnemyState.Dead)
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
            // CRITICAL FIX: Instantly cancel and abort any Game Over that was waiting!
            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.CancelGameOver();
            }

            if (AudioManager.Instance != null && stageWinSfx != null)
            {
                AudioManager.Instance.PlaySFX(stageWinSfx);
            }

            StartCoroutine(AdvanceToNextStageSequence());
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
                // 1. Fade out to black and set up board
                LoadingManager.Instance.FadeOutIn(() =>
                {
                    if (StageBannerUI.Instance != null)
                    {
                        StageBannerUI.Instance.ResetBanner();
                    }

                    _currentStageIndex = nextIndex;
                    SetupStageInternal(_currentStageIndex);
                });

                // 2. WAIT for black screen to finish fading in (approx 0.5s)
                yield return new WaitForSecondsRealtime(0.5f);

                // 3. NOW award bonus weapons on screen so the +1 popup floats up visibly!
                GrantStageBonusWeapons(_currentStageIndex);
            }
            else
            {
                _currentStageIndex = nextIndex;
                SetupStageInternal(_currentStageIndex);
                GrantStageBonusWeapons(_currentStageIndex);
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