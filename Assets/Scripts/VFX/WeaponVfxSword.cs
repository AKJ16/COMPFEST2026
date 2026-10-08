using UnityEngine;

// Sword: satu goresan putih runcing yang melintang di seluruh tubuh musuh.
// Ukuran diambil dari SpriteRenderer musuh terdekat, jadi menyesuaikan
// musuh yang kecil maupun yang besar.
public static class WeaponVfxSword
{
    public static void Play(Vector3 position)
    {
        Bounds b = GetEnemyBounds(position);

        Vector3 center = b.center;
        center.z = 0f;

        Vector3 ext = b.extents * 1.05f;

        float width = Mathf.Clamp(b.size.magnitude * 0.05f, 0.15f, 0.5f);

        Color white = new Color(1f, 1f, 1f, 0.75f);

        WeaponVfxUtility.Streak("Sword_Slash_Body",
            center + new Vector3(-ext.x, -ext.y, 0f),
            center + new Vector3(ext.x, ext.y, 0f),
            white, width, 0.35f, 0.5f);
    }

    // Dipakai juga oleh efek lain (mis. Staff) supaya ukurannya mengikuti musuh.
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

        // Cadangan kalau musuh tidak ketemu
        return new Bounds(position, new Vector3(2.5f, 2.5f, 0f));
    }
}