using UnityEngine;

// Poison Dagger: a quick slash, then green poison droplets and rising bubbles.
// Change the colours below to restyle it.
public static class WeaponVfxPoison
{
    private static readonly Color SlashColor = new Color(0.65f, 0.25f, 1f, 0.75f);
    private static readonly Color DropletColor = new Color(0.35f, 0.95f, 0.30f, 0.95f);
    private static readonly Color BubbleColor = new Color(0.55f, 1f, 0.45f, 0.70f);

    public static void Play(Vector3 position)
    {
        position.z = 0f;

        // The slash.
        WeaponVfxUtility.Streak("Dagger_Slash",
            position + new Vector3(-0.7f, 0.6f, 0f),
            position + new Vector3(0.7f, -0.6f, 0f),
            SlashColor, 0.14f, 0.28f, 0.5f);

        // Green droplets splash out and fall.
        WeaponVfxSparks.Burst(position, DropletColor,
            count: 18, radius: 0.35f,
            speedMin: 1.2f, speedMax: 3.5f,
            lifeMin: 0.35f, lifeMax: 0.7f,
            sizeMin: 0.06f, sizeMax: 0.13f,
            gravity: 1.2f, delay: 0.06f);

        // Bubbles drift upward (negative gravity).
        WeaponVfxSparks.Burst(position, BubbleColor,
            count: 9, radius: 0.5f,
            speedMin: 0.2f, speedMax: 0.7f,
            lifeMin: 0.8f, lifeMax: 1.4f,
            sizeMin: 0.08f, sizeMax: 0.16f,
            gravity: -0.2f, delay: 0.10f);
    }
}