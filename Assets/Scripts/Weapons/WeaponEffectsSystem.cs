using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponEffectsSystem : MonoBehaviour
{
    public static WeaponEffectsSystem Instance { get; private set; }

    [SerializeField] private EnemyHealth currentEnemy;

    private void Awake()
    {
        Instance = this;
    }

    public void StartStage(EnemyHealth enemy)
    {
        currentEnemy = enemy;
    }

    public void ResolvePlacement(WeaponInstance instance)
    {
        // 1. Tick existing poison at start of placement turn
        if (currentEnemy != null)
        {
            currentEnemy.TickPoisonTurn();
        }

        // 2. Resolve placement effects & trigger item feedback
        switch (instance.Data.category)
        {
            case WeaponCategory.Attack:
                ResolveAttack(instance);
                break;

            case WeaponCategory.Modifier:
                if (instance.Data.modifierType == ModifierType.Repeat)
                {
                    ResolveHourglass(instance);
                }
                else
                {
                    // FIXED: Play placement SFX/VFX for Books (Multiplier & Addition)
                    PlayAttackFeedback(instance.Data);
                }
                break;

            case WeaponCategory.Utility:
                PlayAttackFeedback(instance.Data);
                break;
        }
    }

    private void ResolveAttack(WeaponInstance attackInstance)
    {
        float multiplier = 1f;
        float addition = 0f;

        // Check surrounding 8-directional neighbors (orthogonal + diagonals) for Books
        var adjacentModifiers = WeaponGridManager.Instance.GetNeighborsOf(attackInstance)
            .Where(n => n.Data.category == WeaponCategory.Modifier && n.Data.modifierType != ModifierType.Repeat);

        foreach (var modifier in adjacentModifiers)
        {
            if (modifier.Data.targets != null && modifier.Data.targets.Contains(attackInstance.Data))
            {
                switch (modifier.Data.modifierType)
                {
                    case ModifierType.Multiplier:
                        multiplier *= modifier.Data.modifierValue;
                        break;

                    case ModifierType.Addition:
                        addition += modifier.Data.modifierValue;
                        break;
                }
            }
        }

        // Formula: (BaseDamage * Multiplier) + Addition
        int finalDamage = Mathf.RoundToInt((attackInstance.Data.baseDamage * multiplier) + addition);
        attackInstance.ResolvedDamage = finalDamage;

        if (currentEnemy != null)
        {
            currentEnemy.TakeDamage(finalDamage);

            if (attackInstance.Data.appliesPoison)
                currentEnemy.ApplyPoison(attackInstance, attackInstance.Data.poisonDamagePerTick);
        }

        PlayAttackFeedback(attackInstance.Data);
    }

    private void PlayAttackFeedback(WeaponData data)
    {
        if (data == null) return;

        Vector3 feedbackPosition = currentEnemy != null ? currentEnemy.transform.position : transform.position;

        // Play weapon / item placement SFX
        if (data.attackSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(data.attackSfx);

        // Play weapon / item placement VFX (if assigned in WeaponData)
        if (data.attackVfxPrefab != null && VFXManager.Instance != null)
            VFXManager.Instance.PlayWeaponEffect(data.attackVfxPrefab, feedbackPosition);
    }

    private void ResolveHourglass(WeaponInstance hourglassInstance)
    {
        // FIXED: Play the Hourglass's OWN placement sound/VFX first!
        PlayAttackFeedback(hourglassInstance.Data);

        var neighbors = WeaponGridManager.Instance.GetNeighborsOf(hourglassInstance)
            .Where(n => n.Data.category == WeaponCategory.Attack);

        foreach (var neighbor in neighbors)
        {
            if (currentEnemy != null)
            {
                currentEnemy.TakeDamage(neighbor.ResolvedDamage);

                if (neighbor.Data.appliesPoison)
                    currentEnemy.ApplyPoison(neighbor, neighbor.Data.poisonDamagePerTick);
            }

            // Replay neighbor attack feedback
            PlayAttackFeedback(neighbor.Data);
        }
    }
}