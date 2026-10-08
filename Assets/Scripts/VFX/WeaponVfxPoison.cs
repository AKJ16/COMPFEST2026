using UnityEngine;

// Poison Dagger: hanya satu goresan ungu sederhana.
// (Efek racun hijau sudah dihapus; poison damage di gameplay tetap jalan.)
public static class WeaponVfxPoison
{
    public static void Play(Vector3 position)
    {
        position.z = 0f;

        Color purple = new Color(0.65f, 0.25f, 1f, 0.75f);

        WeaponVfxUtility.Streak("Dagger_Slash",
            position + new Vector3(-0.7f, 0.6f, 0f),
            position + new Vector3(0.7f, -0.6f, 0f),
            purple, 0.14f, 0.28f, 0.5f);
    }
}