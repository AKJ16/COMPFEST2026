using System.Collections.Generic;
using UnityEngine;

public enum GridOrientation { Single, Horizontal, Vertical }
public enum WeaponCategory { Attack, Modifier, Utility }
public enum ModifierType { None, Multiplier, Addition, Repeat }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "OrderInDisorder/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName;
    public WeaponCategory category;

    [Header("Description (buat tooltip UI)")]
    [TextArea(2, 5)]
    public string description;

    [Header("Sprite (placeholder until assets are ready)")]
    public Sprite icon;

    [Header("Base Stats")]
    public int baseDamage;
    public int gridSize = 1;
    public GridOrientation orientation = GridOrientation.Single;

    [Header("Stage Unlock")]
    public int unlockStage = 1;

    [Header("Relations")]
    [Tooltip("Weapons this weapon affects, e.g. Book of Multiplier -> Staff")]
    public WeaponData[] targets;

    [Header("Modifier Behavior (only used when category == Modifier)")]
    public ModifierType modifierType = ModifierType.None;
    [Tooltip("Multiplier: factor applied (e.g. 2 = double damage). Addition: flat amount added.")]
    public float modifierValue;

    [Header("Status Effect (Stage 2+)")]
    public bool appliesPoison;
    public int poisonDamagePerTick;

    [Header("Attack Feedback (placeholder until final SFX/VFX are ready)")]
    [Tooltip("Sound played through AudioManager.PlaySFX when this weapon fires.")]
    public AudioClip attackSfx;
    [Tooltip("Particle effect spawned via VFXManager when this weapon fires, e.g. fire burst for Staff, slash trail for Sword. Leave null until art/VFX is ready.")]
    public ParticleSystem attackVfxPrefab;

    // Reusable: daftar cell (relatif ke origin) yang bakal ditempatin weapon ini.
    // Dipakai WeaponInstance (buat placement asli) dan bisa dipakai Adriel
    // buat hover-preview di grid (ghost shape sebelum weapon disimpan).
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