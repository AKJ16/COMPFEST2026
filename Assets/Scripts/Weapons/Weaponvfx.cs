using UnityEngine;

// Placeholder VFX untuk semua weapon, dibuat 100% dari kode (tanpa prefab).
// Dipakai oleh WeaponEffectsSystem.PlayAttackFeedback kalau data.attackVfxPrefab kosong:
//     else WeaponVfx.Play(data, feedbackPosition);
// Kalau sebuah weapon punya prefab di "Attack Vfx Prefab", prefab itu yang dipakai.
public static class WeaponVfx
{
    private static Material _material;
    private static Texture2D _softCircle;

    private const int SortingOrder = 100;

    // ------------------------------------------------------------------
    // ENTRY POINT
    // ------------------------------------------------------------------
    public static void Play(WeaponData data, Vector3 position)
    {
        if (data == null) return;

        position.z = 0f;
        string n = string.IsNullOrEmpty(data.weaponName) ? "" : data.weaponName.ToLowerInvariant();

        if (data.modifierType == ModifierType.Multiplier || n.Contains("multipl"))
            BookGlow(position, new Color(1f, 0.55f, 0.15f), new Color(1f, 0.15f, 0.35f));
        else if (data.modifierType == ModifierType.Addition || n.Contains("addition"))
            BookGlow(position, new Color(0.55f, 1f, 0.45f), new Color(1f, 0.95f, 0.4f));
        else if (data.modifierType == ModifierType.Repeat || n.Contains("hour"))
            Hourglass(position);
        else if (data.appliesPoison || n.Contains("poison") || n.Contains("dagger"))
            Poison(position);
        else if (n.Contains("staff"))
            Staff(position);
        else
            Sword(position); // Sword dan semua weapon attack lain yang belum punya efek sendiri
    }

    // ------------------------------------------------------------------
    // EFFECTS
    // ------------------------------------------------------------------

    // Sword: garis tebasan putih-biru + percikan.
    private static void Sword(Vector3 pos)
    {
        // Garis tebasan (elips tipis yang diputar)
        var slash = Make("Sword_Slash", pos, 0.1f, 0.22f);
        var m = slash.main;
        m.startSize3D = true;
        m.startSizeX = 1.5f;
        m.startSizeY = 0.14f;
        m.startSizeZ = 1f;
        m.startRotation = 35f * Mathf.Deg2Rad;
        m.startSpeed = 0f;
        SetBurst(slash, 1);
        SizeOverLife(slash, new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.35f, 1.1f), new Keyframe(1f, 0f)));
        FadeColor(slash, Color.white, new Color(0.5f, 0.85f, 1f));
        Begin(slash);

        // Percikan
        var sparks = Make("Sword_Sparks", pos, 0.1f, 0.25f);
        var sm = sparks.main;
        sm.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        SetCircleShape(sparks, 0.1f, 360f, 0f, 0f);
        SetBurst(sparks, 10);
        FadeColor(sparks, Color.white, new Color(0.6f, 0.9f, 1f));
        SizeOverLife(sparks, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var sr = sparks.GetComponent<ParticleSystemRenderer>();
        sr.renderMode = ParticleSystemRenderMode.Stretch;
        sr.velocityScale = 0.05f;
        sr.lengthScale = 3f;
        Begin(sparks);
    }

    // Staff: kilau sihir ungu-biru yang melayang.
    private static void Staff(Vector3 pos)
    {
        var glow = Make("Staff_Glow", pos, 0.1f, 0.4f);
        var gm = glow.main;
        gm.startSize = 0.5f;
        gm.startSpeed = 0f;
        SetBurst(glow, 1);
        SizeOverLife(glow, new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1.3f), new Keyframe(1f, 0.2f)));
        FadeColor(glow, new Color(0.7f, 0.45f, 1f), new Color(0.3f, 0.6f, 1f));
        Begin(glow);

        var sparkle = Make("Staff_Sparkles", pos, 0.15f, 0.7f);
        var sm = sparkle.main;
        sm.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.5f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        sm.gravityModifier = -0.15f;
        SetCircleShape(sparkle, 0.2f, 360f, 0f, 1f);
        SetBurst(sparkle, 18);
        FadeColor(sparkle, new Color(0.85f, 0.6f, 1f), new Color(0.3f, 0.8f, 1f));
        SizeOverLife(sparkle, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        Begin(sparkle);
    }

    // Poison Dagger: tusukan hijau + gelembung racun naik.
    private static void Poison(Vector3 pos)
    {
        var stab = Make("Poison_Stab", pos, 0.1f, 0.2f);
        var m = stab.main;
        m.startSize3D = true;
        m.startSizeX = 1.0f;
        m.startSizeY = 0.1f;
        m.startSizeZ = 1f;
        m.startRotation = -50f * Mathf.Deg2Rad;
        m.startSpeed = 0f;
        SetBurst(stab, 1);
        SizeOverLife(stab, new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
        FadeColor(stab, new Color(0.8f, 1f, 0.6f), new Color(0.3f, 0.9f, 0.2f));
        Begin(stab);

        var bubbles = Make("Poison_Bubbles", pos, 0.2f, 0.9f);
        var bm = bubbles.main;
        bm.startSpeed = 0f;
        bm.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.24f);
        SetCircleShape(bubbles, 0.3f, 360f, 0f, 1f);
        SetBurst(bubbles, 14);

        var vel = bubbles.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0f);
        vel.y = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        vel.z = new ParticleSystem.MinMaxCurve(0f);

        var noise = bubbles.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 1.5f;

        FadeColor(bubbles, new Color(0.45f, 1f, 0.3f), new Color(0.15f, 0.6f, 0.2f));
        SizeOverLife(bubbles, new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.8f)));
        Begin(bubbles);
    }

    // Hourglass: pasir emas berputar masuk ke tengah.
    private static void Hourglass(Vector3 pos)
    {
        var flash = Make("Hourglass_Flash", pos, 0.1f, 0.5f);
        var fm = flash.main;
        fm.startSize = 0.6f;
        fm.startSpeed = 0f;
        SetBurst(flash, 1);
        SizeOverLife(flash, new AnimationCurve(
            new Keyframe(0f, 1.2f), new Keyframe(1f, 0.1f)));
        FadeColor(flash, new Color(1f, 0.9f, 0.5f), new Color(1f, 0.7f, 0.2f));
        Begin(flash);

        var sand = Make("Hourglass_Sand", pos, 0.45f, 0.7f);
        var m = sand.main;
        m.startSpeed = 0f;
        m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);

        var em = sand.emission;
        em.rateOverTime = 55f;

        SetCircleShape(sand, 0.8f, 360f, 0f, 0f);

        var vel = sand.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(0f);
        vel.y = new ParticleSystem.MinMaxCurve(0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f);
        vel.orbitalX = new ParticleSystem.MinMaxCurve(0f);
        vel.orbitalY = new ParticleSystem.MinMaxCurve(0f);
        vel.orbitalZ = new ParticleSystem.MinMaxCurve(6f);
        vel.radial = new ParticleSystem.MinMaxCurve(-1.1f);

        FadeColor(sand, new Color(1f, 0.85f, 0.35f), new Color(0.9f, 0.6f, 0.15f));
        SizeOverLife(sand, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
        Begin(sand);
    }

    // Book of Addition / Multiplier: cahaya lembut membesar + kilau naik.
    private static void BookGlow(Vector3 pos, Color a, Color b)
    {
        var glow = Make("Book_Glow", pos, 0.1f, 0.55f);
        var gm = glow.main;
        gm.startSize = 0.7f;
        gm.startSpeed = 0f;
        SetBurst(glow, 1);
        SizeOverLife(glow, new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.4f, 1.4f), new Keyframe(1f, 1.8f)));
        FadeColor(glow, a, b);
        Begin(glow);

        var sparkle = Make("Book_Sparkles", pos, 0.15f, 0.8f);
        var sm = sparkle.main;
        sm.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        sm.gravityModifier = -0.2f;
        SetCircleShape(sparkle, 0.35f, 360f, 0f, 1f);
        SetBurst(sparkle, 12);
        FadeColor(sparkle, a, b);
        SizeOverLife(sparkle, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        Begin(sparkle);
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    // Buat GameObject + ParticleSystem. GameObject dibiarkan NONAKTIF selama
    // konfigurasi; Begin() yang mengaktifkannya (supaya duration boleh diubah).
    private static ParticleSystem Make(string name, Vector3 pos, float duration, float lifetime)
    {
        var go = new GameObject("WeaponVfx_" + name);
        go.SetActive(false);
        go.transform.position = pos;

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = lifetime;
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = 150;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetMaterial();
        renderer.sortingOrder = SortingOrder;

        // Jaring pengaman kalau stopAction tidak jalan
        Object.Destroy(go, duration + lifetime + 1.5f);
        return ps;
    }

    private static void Begin(ParticleSystem ps)
    {
        ps.gameObject.SetActive(true);
        ps.Play();
    }

    private static void SetBurst(ParticleSystem ps, int count)
    {
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    // Circle shape ada di bidang XY (cocok untuk game 2D).
    // radiusThickness 0 = dari tepi lingkaran, 1 = isi penuh.
    private static void SetCircleShape(ParticleSystem ps, float radius, float arc, float rotationZ, float radiusThickness)
    {
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = radiusThickness;
        shape.arc = arc;
        shape.rotation = new Vector3(0f, 0f, rotationZ);
    }

    private static void FadeColor(ParticleSystem ps, Color from, Color to)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;

        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    private static void SizeOverLife(ParticleSystem ps, AnimationCurve curve)
    {
        var s = ps.sizeOverLifetime;
        s.enabled = true;
        s.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static Material GetMaterial()
    {
        if (_material != null) return _material;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

        _material = new Material(shader) { name = "WeaponVfx_Material" };
        _material.mainTexture = GetSoftCircle();
        return _material;
    }

    // Tekstur lingkaran lembut dibuat lewat kode (tidak butuh file gambar).
    private static Texture2D GetSoftCircle()
    {
        if (_softCircle != null) return _softCircle;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a); // smoothstep
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        _softCircle = tex;
        return tex;
    }
}