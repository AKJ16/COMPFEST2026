using UnityEngine;

// Hourglass: tanda "rewind" utamanya adalah gambar stopwatch di tengah layar
// (VFXManager.BeginHourglassRewind). Di sini hanya ada butiran pasir kecil di
// musuh. Lingkaran/kilau besar yang dulu muncul sudah dihapus.
public static class WeaponVfxHourglass
{
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

        // Kurva velocity x, y, z harus semuanya mode yang sama.
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
}