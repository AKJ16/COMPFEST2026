using System.Collections;
using UnityEngine;

public static class WeaponVfxUtility
{
    private static Material _defaultParticleMaterial;
    private static Material _lineMaterial;

    public static Material GetDefaultParticleMaterial()
    {
        if (_defaultParticleMaterial == null)
        {
            // Sprites/Default mendukung alpha blending; tekstur titik lembut
            // dipasang supaya partikel tidak tampil sebagai kotak abu-abu.
            Shader particleShader = Shader.Find("Sprites/Default");
            if (particleShader == null)
            {
                particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }
            _defaultParticleMaterial = new Material(particleShader);
            _defaultParticleMaterial.mainTexture = GetSoftDot();
        }
        return _defaultParticleMaterial;
    }

    private static Texture2D _softDot;

    private static Texture2D GetSoftDot()
    {
        if (_softDot != null) return _softDot;

        const int size = 64;
        _softDot = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _softDot.wrapMode = TextureWrapMode.Clamp;
        _softDot.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float half = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                float a = Mathf.Clamp01(1f - d);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
            }
        }

        _softDot.SetPixels(pixels);
        _softDot.Apply();
        return _softDot;
    }

    // Material khusus LineRenderer. Sprites/Default selalu mendukung
    // vertex color + alpha dan tampil di URP 2D.
    public static Material GetLineMaterial()
    {
        if (_lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }
            _lineMaterial = new Material(shader);
        }
        return _lineMaterial;
    }

    public static void SetDefaultRendererSettings(ParticleSystemRenderer renderer, int sortingOrder = 1000)
    {
        if (renderer == null) return;
        renderer.material = GetDefaultParticleMaterial();
        renderer.sortingOrder = sortingOrder; // Mengatur agar muncul di atas Sprite
    }

    public static void SafePlayParticle(ParticleSystem ps, float duration)
    {
        if (ps == null) return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = Mathf.Max(0.01f, duration);

        // Pastikan sorting order selalu di depan (Order in Layer 1000)
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            if (renderer.material == null || renderer.material.name.Contains("Default-Material"))
            {
                renderer.material = GetDefaultParticleMaterial();
            }
            renderer.sortingOrder = 1000;
        }

        ps.Play();
        ps.Emit(30); // Memaksa 30 partikel keluar secara instan
    }

    public static void SetSafeVelocityOverLifetime(ParticleSystem ps, Vector3 minVel, Vector3 maxVel)
    {
        if (ps == null) return;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;

        vel.x = new ParticleSystem.MinMaxCurve(minVel.x, maxVel.x) { mode = ParticleSystemCurveMode.TwoConstants };
        vel.y = new ParticleSystem.MinMaxCurve(minVel.y, maxVel.y) { mode = ParticleSystemCurveMode.TwoConstants };
        vel.z = new ParticleSystem.MinMaxCurve(minVel.z, maxVel.z) { mode = ParticleSystemCurveMode.TwoConstants };
    }

    // =========================================================
    // LINE HELPERS (dipakai Sword, Poison, Staff)
    // =========================================================

    // Garis lurus yang memudar lalu dikembalikan ke pool.
    public static LineRenderer Line(string name, Vector3 a, Vector3 b,
        Color color, float width, float duration)
    {
        return Poly(name, new[] { a, b }, color, width, width * 0.25f, duration, false);
    }

    // Garis dengan banyak titik (slash melengkung, dll).
    public static LineRenderer Poly(string name, Vector3[] points, Color color,
        float startWidth, float endWidth, float duration, bool loop)
    {
        WeaponVfxPooler pooler = WeaponVfxPooler.Instance;
        if (pooler == null) return null;

        LineRenderer lr = pooler.GetLineRenderer();
        if (lr == null) return null;

        lr.name = name;
        lr.useWorldSpace = true;
        lr.loop = loop;
        lr.positionCount = points.Length;
        lr.SetPositions(points);
        lr.startWidth = startWidth;
        lr.endWidth = endWidth;
        lr.startColor = color;
        lr.endColor = color;

        pooler.StartCoroutine(FadeRoutine(lr, color, duration));
        return lr;
    }

    // Lingkaran yang membesar/mengecil, berputar, dan memudar.
    public static void Ring(Vector3 center, float startRadius, float endRadius,
        float turns, Color color, float width, float duration, int segments = 32)
    {
        WeaponVfxPooler pooler = WeaponVfxPooler.Instance;
        if (pooler == null) return;

        LineRenderer lr = pooler.GetLineRenderer();
        if (lr == null) return;

        lr.name = "VFX_Ring";
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = segments;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = color;
        lr.endColor = color;

        pooler.StartCoroutine(RingRoutine(lr, center, startRadius, endRadius,
            turns, color, duration, segments));
    }

    private static IEnumerator RingRoutine(LineRenderer lr, Vector3 center,
        float startRadius, float endRadius, float turns, Color color,
        float duration, int segments)
    {
        Vector3[] pts = new Vector3[segments];
        float timer = 0f;

        while (timer < duration)
        {
            if (lr == null) yield break;

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float radius = Mathf.Lerp(startRadius, endRadius, t);
            float rot = t * turns * Mathf.PI * 2f;

            for (int i = 0; i < segments; i++)
            {
                float ang = (float)i / segments * Mathf.PI * 2f + rot;
                pts[i] = center + new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f);
            }
            lr.SetPositions(pts);

            float a = 1f - Mathf.SmoothStep(0f, 1f, t);
            Color c = new Color(color.r, color.g, color.b, color.a * a);
            lr.startColor = c;
            lr.endColor = c;

            yield return null;
        }

        ReturnLine(lr);
    }

    private static IEnumerator FadeRoutine(LineRenderer lr, Color color, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (lr == null) yield break;

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float a = 1f - Mathf.SmoothStep(0f, 1f, t);

            Color c = new Color(color.r, color.g, color.b, color.a * a);
            lr.startColor = c;
            lr.endColor = c;

            yield return null;
        }

        ReturnLine(lr);
    }

    // Goresan runcing di kedua ujung (paling tebal di dekat "peak"),
    // memanjang cepat dari 'from' ke 'to', lalu memudar.
    public static void Streak(string name, Vector3 from, Vector3 to,
        Color color, float maxWidth, float duration, float peak = 0.4f)
    {
        WeaponVfxPooler pooler = WeaponVfxPooler.Instance;
        if (pooler == null) return;

        LineRenderer lr = pooler.GetLineRenderer();
        if (lr == null) return;

        const int n = 8;

        lr.name = name;
        lr.loop = false;
        lr.useWorldSpace = true;
        lr.positionCount = n;
        lr.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(peak, 1f),
            new Keyframe(1f, 0f));
        lr.widthMultiplier = maxWidth;
        lr.startColor = color;
        lr.endColor = color;

        for (int i = 0; i < n; i++)
            lr.SetPosition(i, from);

        pooler.StartCoroutine(StreakRoutine(lr, from, to, color, duration));
    }

    private static IEnumerator StreakRoutine(LineRenderer lr, Vector3 from,
        Vector3 to, Color color, float duration)
    {
        int n = lr.positionCount;
        Vector3[] pts = new Vector3[n];
        float grow = Mathf.Max(0.01f, duration * 0.25f);
        float timer = 0f;

        while (timer < duration)
        {
            if (lr == null) yield break;

            timer += Time.deltaTime;

            float g = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / grow));
            for (int i = 0; i < n; i++)
                pts[i] = Vector3.Lerp(from, to, (float)i / (n - 1) * g);
            lr.SetPositions(pts);

            float fade = Mathf.Clamp01((timer - grow) / Mathf.Max(0.01f, duration - grow));
            float a = 1f - Mathf.SmoothStep(0f, 1f, fade);
            Color c = new Color(color.r, color.g, color.b, color.a * a);
            lr.startColor = c;
            lr.endColor = c;

            yield return null;
        }

        ReturnLine(lr);
    }

    private static void ReturnLine(LineRenderer lr)
    {
        if (lr == null) return;
        if (WeaponVfxPooler.Instance != null)
            WeaponVfxPooler.Instance.ReturnLineToPool(lr);
    }
}