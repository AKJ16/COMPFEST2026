using UnityEngine;

// Book of Addition / Multiplier, at the enemy: only small sparkles.
// The big + and x signs were removed from the enemy. They now float along the
// screen border (see VFXManager.BorderSignsRoutine).
public static class WeaponVfxBooks
{
    public static void PlayAddition(Vector3 position)
    {
        Sparkles(position, new Color(1f, 0.4f, 0.7f, 0.7f));
    }

    public static void PlayMultiplier(Vector3 position)
    {
        Sparkles(position, new Color(1f, 0.6f, 0.2f, 0.7f));
    }

    private static void Sparkles(Vector3 position, Color color)
    {
        WeaponVfxSparks.Burst(position, color,
            count: 12, radius: 0.4f,
            speedMin: 0.5f, speedMax: 1.5f,
            lifeMin: 0.6f, lifeMax: 1.2f,
            sizeMin: 0.05f, sizeMax: 0.10f,
            gravity: -0.3f);
    }
}