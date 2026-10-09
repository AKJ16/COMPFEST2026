using UnityEngine;

// Sword: one white slash across the whole enemy, then a burst of sparks.
// Size is taken from the nearest enemy's SpriteRenderers, so it fits small
// and large enemies alike.
public static class WeaponVfxSword
{
    private static readonly Color SlashColor = new Color(1f, 1f, 1f, 0.75f);
    private static readonly Color SparkColor = new Color(1f, 0.95f, 0.75f, 1f);
    private static readonly Color EmberColor = new Color(1f, 0.78f, 0.35f, 1f);

    public static void Play(Vector3 position)
    {
        Bounds b = GetEnemyBounds(position);

        Vector3 center = b.center;
        center.z = 0f;

        Vector3 ext = b.extents * 1.05f;

        float width = Mathf.Clamp(b.size.magnitude * 0.05f, 0.15f, 0.5f);

        // The slash itself.
        WeaponVfxUtility.Streak("Sword_Slash_Body",
            center + new Vector3(-ext.x, -ext.y, 0f),
            center + new Vector3(ext.x, ext.y, 0f),
            SlashColor, width, 0.35f, 0.5f);

        // Sparks fly out just after the slash lands.
        float spread = Mathf.Clamp(Mathf.Min(b.extents.x, b.extents.y) * 0.45f, 0.25f, 1.2f);

        WeaponVfxSparks.Burst(center, SparkColor,
            count: 22, radius: spread,
            speedMin: 2f, speedMax: 5.5f,
            lifeMin: 0.25f, lifeMax: 0.55f,
            sizeMin: 0.05f, sizeMax: 0.12f,
            gravity: 0.8f, delay: 0.07f);

        // A few slower, warmer embers.
        WeaponVfxSparks.Burst(center, EmberColor,
            count: 10, radius: spread * 0.6f,
            speedMin: 1f, speedMax: 3f,
            lifeMin: 0.35f, lifeMax: 0.7f,
            sizeMin: 0.04f, sizeMax: 0.08f,
            gravity: 0.5f, delay: 0.12f);
    }

    // Also used by other effects (Staff, Hourglass) so their size follows the enemy.
    public static Bounds GetEnemyBounds(Vector3 position)
    {
        EnemyHealth best = null;
        float bestDist = float.MaxValue;

        foreach (EnemyHealth e in Object.FindObjectsByType<EnemyHealth>())
        {
            if (e == null || !e.gameObject.activeInHierarchy) continue;

            float d = (e.transform.position - position).sqrMagnitude;
            if (d < bestDist)
            {
                best = e;
                bestDist = d;
            }
        }

        if (best != null)
        {
            SpriteRenderer[] renderers = best.GetComponentsInChildren<SpriteRenderer>();

            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                return b;
            }
        }

        // Fallback if no enemy is found.
        return new Bounds(position, new Vector3(2.5f, 2.5f, 0f));
    }
}