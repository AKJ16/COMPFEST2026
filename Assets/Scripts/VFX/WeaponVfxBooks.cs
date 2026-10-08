using UnityEngine;
using System.Collections;

// Book of Addition / Multiplier: simbol + (plus / silang) dan percikan kecil.
// Lingkaran glow besar yang dulu muncul sudah dihapus.
public static class WeaponVfxBooks
{
    public static void PlayAddition(Vector3 position)
    {
        position.z = 0f;

        Color primary = new Color(1f, 0.4f, 0.7f, 1f);
        PlusSymbol(position, WithAlpha(primary, 0.6f));
        FloatingParticles(position, WithAlpha(primary, 0.7f));
    }

    public static void PlayMultiplier(Vector3 position)
    {
        position.z = 0f;

        Color primary = new Color(1f, 0.6f, 0.2f, 1f);
        XSymbol(position, WithAlpha(primary, 0.6f));
        FloatingParticles(position, WithAlpha(primary, 0.7f));
    }

    private static Color WithAlpha(Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }

    private static void PlusSymbol(Vector3 position, Color color)
    {
        if (WeaponVfxPooler.Instance == null) return;

        float size = 0.45f;
        CreatePooledLine("Plus_H", position + new Vector3(-size, 0, 0), position + new Vector3(size, 0, 0), color, 0.08f, 0.7f);
        CreatePooledLine("Plus_V", position + new Vector3(0, -size, 0), position + new Vector3(0, size, 0), color, 0.08f, 0.7f);
    }

    private static void XSymbol(Vector3 position, Color color)
    {
        if (WeaponVfxPooler.Instance == null) return;

        float size = 0.4f;
        CreatePooledLine("X_1", position + new Vector3(-size, -size, 0), position + new Vector3(size, size, 0), color, 0.08f, 0.7f);
        CreatePooledLine("X_2", position + new Vector3(-size, size, 0), position + new Vector3(size, -size, 0), color, 0.08f, 0.7f);
    }

    private static void FloatingParticles(Vector3 position, Color color)
    {
        if (WeaponVfxPooler.Instance == null) return;

        ParticleSystem ps = WeaponVfxPooler.Instance.GetParticleSystem();
        ps.name = "BookParticles_Pool";
        ps.transform.position = position;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = false;

        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startColor = color;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = false;

        // Kurva force x, y, z harus semuanya mode yang sama
        // (kalau tidak, Unity spam error "must all be in the same mode").
        var force = ps.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.World;
        force.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        force.y = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        force.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        colorOver.color = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 1f, 1f, 0f));

        ps.Play();
    }

    private static void CreatePooledLine(string name, Vector3 start, Vector3 end, Color color, float width, float duration)
    {
        if (WeaponVfxPooler.Instance == null) return;

        LineRenderer lr = WeaponVfxPooler.Instance.GetLineRenderer();
        lr.name = name;
        lr.loop = false;
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = color;

        WeaponVfxPooler.Instance.StartCoroutine(FadeLineRoutine(lr, duration));
    }

    private static IEnumerator FadeLineRoutine(LineRenderer lr, float duration)
    {
        float timer = 0f;
        Color startC = lr.startColor;
        while (timer < duration)
        {
            if (lr == null) yield break;

            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startC.a, 0f, timer / duration);
            lr.startColor = lr.endColor = new Color(startC.r, startC.g, startC.b, alpha);
            yield return null;
        }

        if (lr != null && WeaponVfxPooler.Instance != null)
        {
            WeaponVfxPooler.Instance.ReturnLineToPool(lr);
        }
    }
}