using System.Collections;
using UnityEngine;

// Staff: pusaran sihir (vortex) yang berputar di tubuh musuh, terinspirasi
// dari gambar referensi (lengan-lengan melengkung yang runcing). Warna diacak
// tiap serangan: ungu, biru, atau oranye.
public static class WeaponVfxStaff
{
    private static readonly Color[] MagicColors =
    {
        new Color(0.65f, 0.25f, 1f, 0.7f),   // ungu
        new Color(0.30f, 0.60f, 1f, 0.7f),   // biru
        new Color(1f, 0.60f, 0.15f, 0.7f)    // oranye
    };

    private const int ArmCount = 6;
    private const int PointsPerArm = 16;
    private const float Duration = 0.85f;
    private const float Turns = 0.6f;

    public static void Play(Vector3 position)
    {
        WeaponVfxPooler pooler = WeaponVfxPooler.Instance;
        if (pooler == null) return;

        // Ukuran pusaran mengikuti ukuran musuh.
        Bounds bounds = WeaponVfxSword.GetEnemyBounds(position);

        Vector3 center = bounds.center;
        center.z = 0f;

        float radius = Mathf.Clamp(
            Mathf.Min(bounds.extents.x, bounds.extents.y) * 0.9f,
            0.8f,
            2.4f);

        Color main = MagicColors[Random.Range(0, MagicColors.Length)];

        Color light = Color.Lerp(main, Color.white, 0.55f);
        light.a = main.a * 0.75f;

        // Arah putar acak, sudut awal acak.
        float dir = Random.value > 0.5f ? 1f : -1f;
        float offset = Random.value * Mathf.PI * 2f;
        float step = Mathf.PI * 2f / ArmCount;

        int total = ArmCount * 2;

        LineRenderer[] arms = new LineRenderer[total];
        float[] baseAngle = new float[total];
        float[] lengthScale = new float[total];
        float[] twist = new float[total];
        Color[] colors = new Color[total];

        for (int i = 0; i < ArmCount; i++)
        {
            // Lengan utama (tebal)
            int a = i * 2;
            arms[a] = CreateArm("Staff_Vortex_Arm", main, radius * 0.085f);
            baseAngle[a] = offset + i * step;
            lengthScale[a] = 1f;
            twist[a] = 1.5f;
            colors[a] = main;

            // Lengan tipis di antaranya (lebih pendek, lebih terang)
            int b = a + 1;
            arms[b] = CreateArm("Staff_Vortex_Thin", light, radius * 0.035f);
            baseAngle[b] = offset + i * step + step * 0.5f;
            lengthScale[b] = 0.75f;
            twist[b] = 1.1f;
            colors[b] = light;
        }

        pooler.StartCoroutine(
            VortexRoutine(arms, baseAngle, lengthScale, twist, colors,
                center, radius, dir));
    }

    // Garis runcing: agak tebal di dekat pusat, lancip di ujung luar.
    private static LineRenderer CreateArm(string name, Color color, float width)
    {
        LineRenderer lr = WeaponVfxPooler.Instance.GetLineRenderer();

        lr.name = name;
        lr.loop = false;
        lr.useWorldSpace = true;
        lr.positionCount = PointsPerArm;

        lr.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0f));
        lr.widthMultiplier = width;

        lr.startColor = color;
        lr.endColor = color;

        return lr;
    }

    private static IEnumerator VortexRoutine(
        LineRenderer[] arms,
        float[] baseAngle,
        float[] lengthScale,
        float[] twist,
        Color[] colors,
        Vector3 center,
        float radius,
        float dir)
    {
        Vector3[] pts = new Vector3[PointsPerArm];
        float timer = 0f;

        while (timer < Duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / Duration);

            // Membuka dari pusat, berputar, lalu memudar di akhir.
            float grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
            float rot = dir * t * Turns * Mathf.PI * 2f;
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.55f) / 0.45f));

            for (int a = 0; a < arms.Length; a++)
            {
                LineRenderer lr = arms[a];
                if (lr == null) continue;

                for (int j = 0; j < PointsPerArm; j++)
                {
                    float u = (float)j / (PointsPerArm - 1);
                    float r = radius * lengthScale[a] * grow * (0.12f + 0.88f * u);

                    // Lengan melengkung ke belakang arah putaran (efek pusaran).
                    float ang = baseAngle[a] + rot - dir * u * twist[a];

                    pts[j] = center + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
                }

                lr.SetPositions(pts);

                Color c = colors[a];
                c.a *= fade;
                lr.startColor = c;
                lr.endColor = c;
            }

            yield return null;
        }

        WeaponVfxPooler pooler = WeaponVfxPooler.Instance;

        for (int a = 0; a < arms.Length; a++)
        {
            if (arms[a] != null && pooler != null)
                pooler.ReturnLineToPool(arms[a]);
        }
    }
}