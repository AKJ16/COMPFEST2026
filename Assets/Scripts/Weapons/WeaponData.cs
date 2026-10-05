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
    [Tooltip("Unique animation played on placement/attack. 'Auto' selects based on weapon type.")]
    public WeaponAnimType animType = WeaponAnimType.Auto;

    [Header("Description (buat tooltip UI)")]
    [TextArea(2, 5)]
    public string description;

    [Header("Sprite (placeholder until assets are ready)")]
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

    [Header("Modifier Behavior (only used when category == Modifier)")]
    public ModifierType modifierType = ModifierType.None;
    public float modifierValue;

    [Header("Status Effect (Stage 2+)")]
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