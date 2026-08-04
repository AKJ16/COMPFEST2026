using System;
using UnityEngine;

public enum StageResult { InProgress, Win, Lose }

// Menggantikan TurnManager lama. TIDAK ADA hitungan turn/countdown.
// Satu stage = satu fase: player taro weapon sebanyak yang dia mau/bisa,
// tiap taro langsung nembak/apply efek (lihat WeaponEffectsSystem).
// Fase berakhir ketika:
//   - Enemy mati -> Win
//   - Tidak ada lagi kemungkinan taro (grid penuh / inventory habis) dan enemy
//     masih hidup -> Lose
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    private EnemyHealth _currentEnemy;

    public StageResult Result { get; private set; } = StageResult.InProgress;
    public int CurrentStageNumber { get; private set; } = 1;
    public EnemyHealth CurrentEnemy => _currentEnemy;

    public event Action<StageResult> OnStageEnded;

    [Header("Manual Testing Only (hapus kalau sistem stage Adriel sudah jadi)")]
    [Tooltip("Index array = nomor stage. Index 0 = Enemy_Stage0 (tutorial), index 1 = Enemy_Stage1, dst.")]
    [SerializeField] private EnemyHealth[] testEnemiesByStage;
    [Tooltip("Kalau dicentang, begitu Play dimulai otomatis masuk ke Stage 0 tanpa perlu klik context menu.")]
    [SerializeField] private bool autoStartStage0OnPlay = true;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (autoStartStage0OnPlay)
            GoToStageForTesting(0);
    }

    // Panggil ini setiap masuk stage baru (spawn enemy, reset grid & inventory duluan
    // sebelum ini di-call).
    public void StartStage(int stageNumber, EnemyHealth enemy)
    {
        CurrentStageNumber = stageNumber;
        Result = StageResult.InProgress;
        _currentEnemy = enemy;

        WeaponGridManager.Instance.ResetGrid();
        WeaponEffectsSystem.Instance.StartStage(enemy);

        _currentEnemy.OnStateChanged += HandleEnemyStateChanged;
    }

    // Dipanggil InventorySystem setiap kali habis ada placement.
    public void CheckForEndOfStage()
    {
        if (Result != StageResult.InProgress) return;

        if (_currentEnemy.State == EnemyState.Dead)
        {
            EndStage(StageResult.Win);
            return;
        }

        if (!InventorySystem.Instance.HasAnyValidMove())
        {
            EndStage(StageResult.Lose);
        }
    }

    // Tombol "commit"/nyerah manual kalau mau, opsional dipanggil dari UI.
    public void ForceEndTurn()
    {
        if (Result != StageResult.InProgress) return;

        EndStage(_currentEnemy.State == EnemyState.Dead ? StageResult.Win : StageResult.Lose);
    }

    private void HandleEnemyStateChanged(EnemyState state)
    {
        if (state == EnemyState.Dead && Result == StageResult.InProgress)
            EndStage(StageResult.Win);
    }

    private void EndStage(StageResult result)
    {
        Result = result;
        OnStageEnded?.Invoke(result);

        if (_currentEnemy != null)
            _currentEnemy.OnStateChanged -= HandleEnemyStateChanged;
    }

    // =========================================================
    // MANUAL TESTING — klik kanan komponen ini di Inspector saat Play
    // =========================================================

    [ContextMenu("TEST: Go To Stage 0 (Tutorial)")]
    private void TestGoToStage0() => GoToStageForTesting(0);

    [ContextMenu("TEST: Go To Stage 1")]
    private void TestGoToStage1() => GoToStageForTesting(1);

    [ContextMenu("TEST: Go To Stage 2")]
    private void TestGoToStage2() => GoToStageForTesting(2);

    [ContextMenu("TEST: Go To Stage 3")]
    private void TestGoToStage3() => GoToStageForTesting(3);

    private void GoToStageForTesting(int stageNumber)
    {
        if (testEnemiesByStage == null || stageNumber < 0 || stageNumber >= testEnemiesByStage.Length)
        {
            Debug.LogWarning($"StageManager: 'Test Enemies By Stage' belum di-setup lengkap untuk stage {stageNumber}. Cek ukuran array & isi Inspector.");
            return;
        }

        var enemy = testEnemiesByStage[stageNumber];
        if (enemy == null)
        {
            Debug.LogWarning($"StageManager: slot index {stageNumber} di 'Test Enemies By Stage' masih kosong. Drag Enemy_Stage{stageNumber} ke situ.");
            return;
        }

        // Matikan semua enemy test dulu, baru nyalain yang punya stage ini —
        // biar cuma satu enemy yang aktif/keliatan di scene setiap saat.
        for (int i = 0; i < testEnemiesByStage.Length; i++)
        {
            if (testEnemiesByStage[i] != null)
                testEnemiesByStage[i].gameObject.SetActive(i == stageNumber);
        }

        StartStage(stageNumber, enemy);
        Debug.Log($"[TEST] Pindah ke Stage {stageNumber} (enemy: {enemy.name})");
    }
}