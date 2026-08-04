using UnityEngine;

// Jalan otomatis saat scene mulai: isi inventory dengan weapon starting,
// lalu mulai stage 1. Ini sementara buat testing manual sebelum ada
// menu/level-select beneran.
public class GameBootstrap : MonoBehaviour
{
    [Header("Enemy untuk stage ini")]
    [SerializeField] private EnemyHealth enemy;

    [Header("Weapon starting + jumlahnya")]
    [SerializeField] private WeaponData[] startingWeapons;
    [SerializeField] private int[] startingAmounts;

    [Header("Stage yang mau di-mulai")]
    [SerializeField] private int stageNumber = 1;

    private void Start()
    {
        InventorySystem.Instance.ResetInventory();

        for (int i = 0; i < startingWeapons.Length; i++)
        {
            int amount = (i < startingAmounts.Length) ? startingAmounts[i] : 1;
            InventorySystem.Instance.AddWeapon(startingWeapons[i], amount);
        }

        StageManager.Instance.StartStage(stageNumber, enemy);
    }
}