using System.Collections.Generic;
using UnityEngine;

public enum GridOrientation { Single, Horizontal, Vertical }
public enum WeaponCategory { Attack, Modifier, Utility }
public enum ModifierType { None, Multiplier, Addition, Repeat }
public enum WeaponAnimType { Auto, SwordSwing, WandGlow, BookPop, DaggerStab, HourglassSpin }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "OrderInDisorder/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName;
    public WeaponCategory category;

    [Header("Animation")]
    public WeaponAnimType animType = WeaponAnimType.Auto;

    [Header("Usage Cap (0 = Unlimited)")]
    [Tooltip("Max times this weapon can be placed per stage. Books default to 2.")]
    public int maxUsagePerStage = 0;

    // Helper: Books automatically default to 2 uses if maxUsagePerStage is 0
    public int MaxUsage => maxUsagePerStage > 0 ? maxUsagePerStage : (category == WeaponCategory.Modifier && modifierType != ModifierType.Repeat ? 3 : 0);
    public bool HasUsageCap => MaxUsage > 0;

    [Header("Description")]
    [TextArea(2, 5)]
    public string description;

    [Header("Sprite")]
    public Sprite icon;

    [Header("Base Stats")]
    public int baseDamage;
    public int gridSize = 1;
    public GridOrientation orientation = GridOrientation.Single;

    public int Width => orientation == GridOrientation.Horizontal ? gridSize : 1;
    public int Height => orientation == GridOrientation.Vertical ? gridSize : 1;

    [Header("Stage Unlock")]
    public int unlockStage = 1;

    [Header("Relations")]
    public WeaponData[] targets;

    [Header("Modifier Behavior")]
    public ModifierType modifierType = ModifierType.None;
    public float modifierValue;

    [Header("Status Effect")]
    public bool appliesPoison;
    public int poisonDamagePerTick;

    [Header("Attack Feedback")]
    public AudioClip attackSfx;
    public ParticleSystem attackVfxPrefab;

    public List<Vector2Int> GetOccupiedCells(Vector2Int origin)
    {
        var cells = new List<Vector2Int> { origin };

        for (int i = 1; i < gridSize; i++)
        {
            Vector2Int offset = orientation == GridOrientation.Horizontal
                ? new Vector2Int(i, 0)
                : orientation == GridOrientation.Vertical
                    ? new Vector2Int(0, i)
                    : Vector2Int.zero;

            cells.Add(origin + offset);
        }

        return cells;
    }
}