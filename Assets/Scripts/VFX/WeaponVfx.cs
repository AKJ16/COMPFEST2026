using UnityEngine;

public class WeaponVfx : MonoBehaviour
{
    public static WeaponVfx Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log("[WeaponVfx] READY");
    }

    public void Play(WeaponData weaponData, Vector3 targetPosition)
    {
        if (weaponData == null)
        {
            Debug.LogWarning("[WeaponVfx] WeaponData NULL!");
            return;
        }

        targetPosition.z = 0f;

        string weaponName = !string.IsNullOrEmpty(weaponData.weaponName)
            ? weaponData.weaponName.ToLower()
            : weaponData.name.ToLower();

        Debug.Log(
            "[WeaponVfx] PLAY REQUEST = " + weaponName
        );

        // =====================================================
        // SWORD
        // =====================================================
        if (weaponName.Contains("sword") ||
            weaponName.Contains("pedang") ||
            weaponName.Contains("blade"))
        {
            Debug.Log("[WeaponVfx] CALLING SWORD");

            WeaponVfxSword.Play(targetPosition);

            Debug.Log("[WeaponVfx] SWORD CALLED");
            return;
        }

        // =====================================================
        // POISON DAGGER
        // =====================================================
        if (weaponName.Contains("poison") &&
            weaponName.Contains("dagger"))
        {
            Debug.Log("[WeaponVfx] CALLING POISON DAGGER");

            WeaponVfxPoison.Play(targetPosition);

            Debug.Log("[WeaponVfx] POISON DAGGER CALLED");
            return;
        }

        if (weaponName.Contains("dagger") ||
            weaponName.Contains("belati") ||
            weaponName.Contains("knife"))
        {
            Debug.Log("[WeaponVfx] CALLING DAGGER");

            WeaponVfxPoison.Play(targetPosition);

            Debug.Log("[WeaponVfx] DAGGER CALLED");
            return;
        }

        // =====================================================
        // STAFF
        // =====================================================
        if (weaponName.Contains("staff") ||
            weaponName.Contains("tongkat") ||
            weaponName.Contains("wand"))
        {
            Debug.Log("[WeaponVfx] CALLING STAFF");

            WeaponVfxStaff.Play(targetPosition);

            Debug.Log("[WeaponVfx] STAFF CALLED");
            return;
        }

        // =====================================================
        // HOURGLASS
        // =====================================================
        if (weaponName.Contains("hourglass") ||
            weaponName.Contains("hour glass") ||
            weaponName.Contains("glass") ||
            weaponName.Contains("jam pasir") ||
            weaponData.modifierType == ModifierType.Repeat)
        {
            Debug.Log("[WeaponVfx] CALLING HOURGLASS");

            WeaponVfxHourglass.Play(targetPosition);
            return;
        }

        // =====================================================
        // BOOK
        // =====================================================
        if (weaponName.Contains("book") ||
            weaponName.Contains("buku"))
        {
            Debug.Log("[WeaponVfx] CALLING BOOK");

            if (weaponName.Contains("addition") ||
                weaponName.Contains("add"))
            {
                WeaponVfxBooks.PlayAddition(targetPosition);
            }
            else if (weaponName.Contains("multiplier") ||
                     weaponName.Contains("multiply"))
            {
                WeaponVfxBooks.PlayMultiplier(targetPosition);
            }

            return;
        }

        Debug.Log(
            "[WeaponVfx] NO SPECIAL VFX FOR: " + weaponName
        );
    }
}