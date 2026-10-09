using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Hourglass:
//  - Play(): a small puff of sand on the enemy (called per hourglass in the chain).
//  - BeginClock()/EndClock(): a rewind clock drawn on the middle of the enemy.
//    VFXManager calls these from BeginHourglassRewind()/EndHourglassRewind(),
//    so the clock stays up for the whole Hourglass chain, then fades out.
//
// The clock is made of pooled LineRenderers: a ring, 12 ticks, two hands that
// spin counter-clockwise (rewind), and two circular arrows around it.
public static class WeaponVfxHourglass
{
    private static readonly Color ClockColor = new Color(0.72f, 0.88f, 1f, 1f);
    private static readonly Color HandColor = new Color(0.92f, 0.97f, 1f, 1f);

    private const int RingSegments = 48;
    private const int ArcPoints = 16;

    private const float FadeIn = 0.2f;
    private const float FadeOut = 0.3f;
    private const float MaxHold = 25f;         // safety: never stay up forever

    private const float MinuteHandSpeed = 720f; // degrees per second, counter-clockwise
    private const float HourHandSpeed = 60f;
    private const float ArcSpeed = 140f;
    private const float ArcSpan = 80f;

    private static bool _clockWanted;
    private static bool _clockRunning;
    private static WeaponVfxPooler _clockHost;

    // =========================================================
    // SAND
    // =========================================================

    public static void Play(Vector3 position)
    {
        position.z = 0f;

        Color timeColor = new Color(0.7f, 0.85f, 1f, 0.8f);
        SandParticles(position, timeColor);
    }

    private static void SandParticles(Vector3 position, Color color)
    {
        if (WeaponVfxPooler.Instance == null) return;

        ParticleSystem ps = WeaponVfxPooler.Instance.GetParticleSystem();
        ps.name = "HourglassSand_Pool";
        ps.transform.position = position;

        var force = ps.forceOverLifetime;
        force.enabled = false;

        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = color;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // Velocity curves x, y, z must all use the same mode.
        WeaponVfxUtility.SetSafeVelocityOverLifetime(
            ps,
            new Vector3(0f, -0.5f, 0f),
            new Vector3(0f, 0.5f, 0f));

        var velocity = ps.velocityOverLifetime;
        velocity.space = ParticleSystemSimulationSpace.World;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = false;

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        colorOver.color = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 1f, 1f, 0f));

        ps.Play();
    }

    // =========================================================
    // REWIND CLOCK ON THE ENEMY
    // =========================================================

    public static void BeginClock()
    {
        WeaponVfxPooler pooler = WeaponVfxPooler.Instance;
        if (pooler == null) return;

        _clockWanted = true;

        // Already showing on this pooler: just keep it alive.
        if (_clockRunning && _clockHost == pooler) return;

        _clockHost = pooler;
        _clockRunning = true;
        pooler.StartCoroutine(ClockRoutine(pooler));
    }

    public static void EndClock()
    {
        _clockWanted = false;
    }

    private static Vector3 GetViewCenter()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 p = cam.transform.position;
        p.z = 0f;
        return p;
    }

    private static LineRenderer MakeLine(WeaponVfxPooler pooler, string name,
        int points, bool loop, float width)
    {
        LineRenderer lr = pooler.GetLineRenderer();
        lr.name = name;
        lr.loop = loop;
        lr.useWorldSpace = true;
        lr.positionCount = points;

        // Pooled lines may still carry a tapered width curve from another effect.
        lr.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        lr.widthMultiplier = width;

        lr.startColor = new Color(1f, 1f, 1f, 0f);
        lr.endColor = new Color(1f, 1f, 1f, 0f);
        return lr;
    }

    private static void SetAlpha(LineRenderer lr, Color baseColor, float alpha)
    {
        if (lr == null) return;

        Color c = baseColor;
        c.a = alpha;
        lr.startColor = c;
        lr.endColor = c;
    }

    private static IEnumerator ClockRoutine(WeaponVfxPooler pooler)
    {
        // Centre and size follow the enemy.
        Bounds b = WeaponVfxSword.GetEnemyBounds(GetViewCenter());
        Vector3 center = b.center;
        center.z = 0f;

        float radius = Mathf.Clamp(Mathf.Min(b.extents.x, b.extents.y) * 0.8f, 0.7f, 1.8f);

        var softLines = new List<LineRenderer>();
        var handLines = new List<LineRenderer>();

        LineRenderer ring = MakeLine(pooler, "Clock_Ring", RingSegments, true, radius * 0.07f);
        softLines.Add(ring);

        LineRenderer[] ticks = new LineRenderer[12];
        for (int i = 0; i < ticks.Length; i++)
        {
            bool major = i % 3 == 0;
            ticks[i] = MakeLine(pooler, "Clock_Tick", 2, false, radius * (major ? 0.06f : 0.035f));
            softLines.Add(ticks[i]);
        }

        LineRenderer minuteHand = MakeLine(pooler, "Clock_MinuteHand", 2, false, radius * 0.06f);
        LineRenderer hourHand = MakeLine(pooler, "Clock_HourHand", 2, false, radius * 0.09f);
        handLines.Add(minuteHand);
        handLines.Add(hourHand);

        LineRenderer[] arcs = new LineRenderer[2];
        LineRenderer[] heads = new LineRenderer[2];
        for (int k = 0; k < 2; k++)
        {
            arcs[k] = MakeLine(pooler, "Clock_Arc", ArcPoints, false, radius * 0.05f);
            heads[k] = MakeLine(pooler, "Clock_ArcHead", 3, false, radius * 0.05f);
            softLines.Add(arcs[k]);
            softLines.Add(heads[k]);
        }

        Vector3[] ringPts = new Vector3[RingSegments];
        Vector3[] arcPts = new Vector3[ArcPoints];
        Vector3[] headPts = new Vector3[3];

        float visible = 0f;
        float time = 0f;

        while (true)
        {
            float dt = Time.deltaTime;
            time += dt;

            bool wanted = _clockWanted && time < MaxHold;

            if (wanted)
            {
                visible = Mathf.MoveTowards(visible, 1f, dt / FadeIn);
            }
            else
            {
                visible = Mathf.MoveTowards(visible, 0f, dt / FadeOut);
                if (visible <= 0f) break;
            }

            float pulse = 1f + Mathf.Sin(time * 4f) * 0.025f;
            float r = radius * pulse;

            // Ring
            for (int i = 0; i < RingSegments; i++)
            {
                float ang = (float)i / RingSegments * Mathf.PI * 2f;
                ringPts[i] = center + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
            }
            if (ring != null) ring.SetPositions(ringPts);

            // Ticks (12 o'clock first, every 30 degrees)
            for (int i = 0; i < ticks.Length; i++)
            {
                if (ticks[i] == null) continue;

                float ang = (90f - i * 30f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                float inner = r * (i % 3 == 0 ? 0.74f : 0.82f);
                float outer = r * 0.94f;

                ticks[i].SetPosition(0, center + dir * inner);
                ticks[i].SetPosition(1, center + dir * outer);
            }

            // Hands spin counter-clockwise: the angle keeps increasing.
            float minuteAng = (90f + time * MinuteHandSpeed) * Mathf.Deg2Rad;
            float hourAng = (90f + time * HourHandSpeed) * Mathf.Deg2Rad;

            if (minuteHand != null)
            {
                minuteHand.SetPosition(0, center);
                minuteHand.SetPosition(1, center +
                    new Vector3(Mathf.Cos(minuteAng), Mathf.Sin(minuteAng), 0f) * (r * 0.78f));
            }

            if (hourHand != null)
            {
                hourHand.SetPosition(0, center);
                hourHand.SetPosition(1, center +
                    new Vector3(Mathf.Cos(hourAng), Mathf.Sin(hourAng), 0f) * (r * 0.5f));
            }

            // Two circular arrows around the clock, also turning counter-clockwise.
            float arcRadius = r * 1.25f;
            for (int k = 0; k < 2; k++)
            {
                if (arcs[k] == null || heads[k] == null) continue;

                float startDeg = time * ArcSpeed + k * 180f;

                for (int p = 0; p < ArcPoints; p++)
                {
                    float a = (startDeg + (float)p / (ArcPoints - 1) * ArcSpan) * Mathf.Deg2Rad;
                    arcPts[p] = center + new Vector3(Mathf.Cos(a) * arcRadius, Mathf.Sin(a) * arcRadius, 0f);
                }
                arcs[k].SetPositions(arcPts);

                // Arrowhead at the leading (counter-clockwise) end.
                float endA = (startDeg + ArcSpan) * Mathf.Deg2Rad;
                Vector3 outward = new Vector3(Mathf.Cos(endA), Mathf.Sin(endA), 0f);
                Vector3 tangent = new Vector3(-Mathf.Sin(endA), Mathf.Cos(endA), 0f);
                Vector3 tip = center + outward * arcRadius;

                headPts[0] = tip - outward * (r * 0.09f);
                headPts[1] = tip + tangent * (r * 0.16f);
                headPts[2] = tip + outward * (r * 0.09f);
                heads[k].SetPositions(headPts);
            }

            for (int i = 0; i < softLines.Count; i++)
                SetAlpha(softLines[i], ClockColor, 0.85f * visible);

            for (int i = 0; i < handLines.Count; i++)
                SetAlpha(handLines[i], HandColor, 0.95f * visible);

            yield return null;
        }

        // Give every line back to the pool.
        WeaponVfxPooler current = WeaponVfxPooler.Instance;

        for (int i = 0; i < softLines.Count; i++)
        {
            if (softLines[i] != null && current != null)
                current.ReturnLineToPool(softLines[i]);
        }

        for (int i = 0; i < handLines.Count; i++)
        {
            if (handLines[i] != null && current != null)
                current.ReturnLineToPool(handLines[i]);
        }

        _clockRunning = false;
    }
}