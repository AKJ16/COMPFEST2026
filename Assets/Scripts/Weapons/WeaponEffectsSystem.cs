using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Otak dari mekanik "Order in Disorder".
//
// ATURAN PENTING:
// - Attack weapon (Sword, Staff, Poison Dagger) LANGSUNG nembak enemy begitu ditaro.
// - Book of Multiplier/Addition = BUFF FORWARD: begitu ditaro, dia nyimpen bonus
//   untuk weapon target (misal Staff) yang ditaro SESUDAHNYA. Weapon yang udah
//   kena taro & nembak SEBELUM Book ini, tidak ke-buff (attack-nya udah kejadian).
//   -> Ini yang bikin "order" krusial: Book harus ditaro sebelum Staff.
// - Hourglass = REPEAT BACKWARD + SPATIAL: begitu ditaro, dia lihat neighbor yang
//   SUDAH ada di grid (siapapun, kapanpun ditaronya) dan me-replay damage yang
//   sudah mereka hasilkan (ResolvedDamage), termasuk re-trigger poison-nya.
public class WeaponEffectsSystem : MonoBehaviour
{
    public static WeaponEffectsSystem Instance { get; private set; }

    [SerializeField] private EnemyHealth currentEnemy;

    // Buff aktif per WeaponData target, terkumpul secara berurutan sepanjang stage.
    // Reset tiap StartStage.
    private readonly Dictionary<WeaponData, float> _activeMultiplier = new Dictionary<WeaponData, float>();
    private readonly Dictionary<WeaponData, float> _activeAddition = new Dictionary<WeaponData, float>();

    private void Awake()
    {
        Instance = this;
    }

    public void StartStage(EnemyHealth enemy)
    {
        currentEnemy = enemy;
        _activeMultiplier.Clear();
        _activeAddition.Clear();
    }

    // Dipanggil InventorySystem SEGERA setelah sebuah weapon berhasil ditaro di grid.
    public void ResolvePlacement(WeaponInstance instance)
    {
        switch (instance.Data.category)
        {
            case WeaponCategory.Attack:
                ResolveAttack(instance);
                break;

            case WeaponCategory.Modifier:
                if (instance.Data.modifierType == ModifierType.Repeat)
                    ResolveHourglass(instance);
                else
                    ResolveBuff(instance);
                break;

            case WeaponCategory.Utility:
                // slot buat mekanik stage 3+ nanti
                break;
        }
    }

    private void ResolveAttack(WeaponInstance instance)
    {
        float multiplier = _activeMultiplier.GetValueOrDefault(instance.Data, 1f);
        float addition = _activeAddition.GetValueOrDefault(instance.Data, 0f);

        int finalDamage = Mathf.RoundToInt((instance.Data.baseDamage + addition) * multiplier);
        instance.ResolvedDamage = finalDamage;

        currentEnemy.TakeDamage(finalDamage);

        if (instance.Data.appliesPoison)
            currentEnemy.ApplyPoison(instance.Data.poisonDamagePerTick);

        PlayAttackFeedback(instance.Data);
    }

    // SFX + VFX spesifik per-weapon. Dipanggil tiap kali sebuah Attack weapon
    // beneran ngedeal damage — baik pas ditaro pertama kali, maupun pas
    // di-replay sama Hourglass.
    private void PlayAttackFeedback(WeaponData data)
    {
        if (currentEnemy == null) return;

        Vector3 feedbackPosition = currentEnemy.transform.position;

        if (data.attackSfx != null)
            AudioManager.Instance.PlaySFX(data.attackSfx);

        VFXManager.Instance.PlayWeaponEffect(data.attackVfxPrefab, feedbackPosition);
    }

    // Book: tidak damage, cuma nge-set buff untuk placement attack berikutnya.
    private void ResolveBuff(WeaponInstance bookInstance)
    {
        var targets = bookInstance.Data.targets;
        if (targets == null || targets.Length == 0) return;

        foreach (var targetData in targets)
        {
            switch (bookInstance.Data.modifierType)
            {
                case ModifierType.Multiplier:
                    float currentMult = _activeMultiplier.GetValueOrDefault(targetData, 1f);
                    _activeMultiplier[targetData] = currentMult * bookInstance.Data.modifierValue;
                    break;

                case ModifierType.Addition:
                    float currentAdd = _activeAddition.GetValueOrDefault(targetData, 0f);
                    _activeAddition[targetData] = currentAdd + bookInstance.Data.modifierValue;
                    break;
            }
        }
    }

    // Hourglass: replay attack weapon yang SUDAH ditaro di sekitarnya.
    private void ResolveHourglass(WeaponInstance hourglassInstance)
    {
        var neighbors = WeaponGridManager.Instance.GetNeighborsOf(hourglassInstance)
            .Where(n => n.Data.category == WeaponCategory.Attack);

        foreach (var neighbor in neighbors)
        {
            // Replay damage yang SUDAH ter-resolve waktu neighbor itu ditaro dulu,
            // bukan dihitung ulang dari buff sekarang — supaya konsisten dengan
            // "attack terjadi instan saat itu juga, lalu bisa di-replay apa adanya".
            currentEnemy.TakeDamage(neighbor.ResolvedDamage);

            if (neighbor.Data.appliesPoison)
                currentEnemy.ApplyPoison(neighbor.Data.poisonDamagePerTick);

            PlayAttackFeedback(neighbor.Data);
        }
    }
}