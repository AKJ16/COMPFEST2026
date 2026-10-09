using System.Collections;
using UnityEngine;

// Small burst of dots (sparks, droplets, bubbles) taken from the shared particle pool.
// Used by the Sword (sparks after the slash), the Poison Dagger (green droplets)
// and the Books (sparkles).
//
// The pooled ParticleSystems are reused by every effect, so each burst resets the
// modules other effects may have switched on.
public static class WeaponVfxSparks
{
    public static void Burst(
        Vector3 position,
        Color color,
        int count = 16,
        float radius = 0.3f,
        float speedMin = 1.5f,
        float speedMax = 4f,
        float lifeMin = 0.3f,
        float lifeMax = 0.6f,
        float sizeMin = 0.05f,
        float sizeMax = 0.11f,
        float gravity = 0.5f,
        float delay = 0f)
    {
        WeaponVfxPooler pooler = WeaponVfxPooler.Instance;
        if (pooler == null) return;

        if (delay > 0f)
        {
            pooler.StartCoroutine(BurstAfter(delay, position, color, count, radius,
                speedMin, speedMax, lifeMin, lifeMax, sizeMin, sizeMax, gravity));
            return;
        }

        Emit(pooler, position, color, count, radius,
            speedMin, speedMax, lifeMin, lifeMax, sizeMin, sizeMax, gravity);
    }

    private static IEnumerator BurstAfter(
        float delay, Vector3 position, Color color, int count, float radius,
        float speedMin, float speedMax, float lifeMin, float lifeMax,
        float sizeMin, float sizeMax, float gravity)
    {
        yield return new WaitForSeconds(delay);

        WeaponVfxPooler pooler = WeaponVfxPooler.Instance;
        if (pooler == null) yield break;

        Emit(pooler, position, color, count, radius,
            speedMin, speedMax, lifeMin, lifeMax, sizeMin, sizeMax, gravity);
    }

    private static void Emit(
        WeaponVfxPooler pooler, Vector3 position, Color color, int count, float radius,
        float speedMin, float speedMax, float lifeMin, float lifeMax,
        float sizeMin, float sizeMax, float gravity)
    {
        position.z = 0f;

        ParticleSystem ps = pooler.GetParticleSystem();
        ps.name = "Sparks_Pool";
        ps.transform.position = position;
        ps.transform.rotation = Quaternion.identity;

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = color;
        main.gravityModifier = gravity;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)count)
        });

        // Circle shape throws particles outward in the XY plane (what a 2D camera sees).
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f;
        shape.arc = 360f;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = Vector3.one;

        // Reset modules that other pooled effects may have left on.
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = false;

        var force = ps.forceOverLifetime;
        force.enabled = false;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        colorOver.color = new ParticleSystem.MinMaxGradient(FadeOutGradient());

        ps.Play();
    }

    private static Gradient FadeOutGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        return g;
    }
}