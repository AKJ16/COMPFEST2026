using UnityEngine;

// Script SEMENTARA buat testing manual: taro 1 weapon per klik lewat Inspector,
// biar bisa coba kombinasi urutan (misal Book duluan baru Staff) tanpa drag & drop.
// Attach ke GameObject "TestPlacer". Hapus/nonaktifin kalau sistem Adriel udah jalan.
public class TestPlacer : MonoBehaviour
{
    [Header("Setup Stage (isi lalu klik kanan header script -> Start Test Stage)")]
    [SerializeField] private EnemyHealth enemyToTest;
    [SerializeField] private int stageNumberForTest = 0;

    [Header("Kasih Semua Weapon ke Inventory buat Testing")]
    [SerializeField] private WeaponData[] allTestWeapons;
    [SerializeField] private int countPerWeapon = 5;

    [Header("Weapon yang mau ditaro sekarang")]
    [SerializeField] private WeaponData weaponToPlace;
    [SerializeField] private Vector2Int gridPosition;

    [ContextMenu("1. Start Test Stage")]
    public void StartTestStage()
    {
        if (enemyToTest == null)
        {
            Debug.LogError("TestPlacer: enemyToTest belum diisi di Inspector.");
            return;
        }

        StageManager.Instance.StartStage(stageNumberForTest, enemyToTest);

        InventorySystem.Instance.ResetInventory();
        foreach (var weapon in allTestWeapons)
        {
            if (weapon != null)
                InventorySystem.Instance.AddWeapon(weapon, countPerWeapon);
        }

        Debug.Log($"Test stage dimulai. Enemy: {enemyToTest.name}, HP: {enemyToTest.CurrentHealth}/{enemyToTest.MaxHealth}");
    }

    [ContextMenu("2. Place Weapon Now")]
    public void PlaceWeaponNow()
    {
        if (weaponToPlace == null)
        {
            Debug.LogError("TestPlacer: weaponToPlace belum diisi di Inspector.");
            return;
        }

        var instance = InventorySystem.Instance.PlaceFromInventory(weaponToPlace, gridPosition);

        if (instance == null)
        {
            Debug.LogWarning($"Gagal taro {weaponToPlace.weaponName} di {gridPosition} (posisi penuh/keluar grid/stok habis).");
            return;
        }

        Debug.Log($"Taro {weaponToPlace.weaponName} di {gridPosition} (urutan ke-{instance.SequenceIndex}). " +
                   $"Enemy HP sekarang: {enemyToTest.CurrentHealth}/{enemyToTest.MaxHealth}. " +
                   $"Stage result: {StageManager.Instance.Result}");
    }
}