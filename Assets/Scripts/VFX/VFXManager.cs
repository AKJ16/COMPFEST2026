using UnityEngine;

// Central place for "juice": screen shake + particle spawning.
// Hook this up by subscribing to EnemyHealth.OnDamaged / OnStateChanged per enemy instance.
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private ParticleSystem hitParticlesPrefab;
    [SerializeField] private ParticleSystem deathParticlesPrefab;

    private Vector3 _originalCamPos;
    private float _shakeTimer;
    private float _shakeIntensity;

    private void Awake()
    {
        Instance = this;
        if (cameraTransform != null)
            _originalCamPos = cameraTransform.localPosition;
    }

    private void Update()
    {
        if (_shakeTimer <= 0f) return;

        _shakeTimer -= Time.deltaTime;
        if (cameraTransform != null)
        {
            Vector3 offset = Random.insideUnitSphere * _shakeIntensity;
            offset.z = 0f;
            cameraTransform.localPosition = _originalCamPos + offset;
        }

        if (_shakeTimer <= 0f && cameraTransform != null)
            cameraTransform.localPosition = _originalCamPos;
    }

    public void ShakeCamera(float duration = 0.15f, float intensity = 0.1f)
    {
        _shakeTimer = duration;
        _shakeIntensity = intensity;
    }

    public void SpawnHitEffect(Vector3 position)
    {
        SpawnTemporary(hitParticlesPrefab, position);
        ShakeCamera();
    }

    public void SpawnDeathEffect(Vector3 position)
    {
        SpawnTemporary(deathParticlesPrefab, position);
        ShakeCamera(0.25f, 0.2f);
    }

    // Efek spesifik per-weapon (mis. api di Staff, slash di Sword).
    // prefab boleh null (weapon belum punya VFX sendiri) — kalau null cuma di-skip.
    public void PlayWeaponEffect(ParticleSystem prefab, Vector3 position)
    {
        SpawnTemporary(prefab, position);
    }

    // Spawn efek lalu hapus otomatis setelah selesai, supaya tidak menumpuk di scene.
    private void SpawnTemporary(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null) return;

        ParticleSystem fx = Instantiate(prefab, position, Quaternion.identity);
        ParticleSystem.MainModule main = fx.main;
        float lifetime = main.duration + main.startLifetime.constantMax + 0.5f;
        Destroy(fx.gameObject, lifetime);
    }
}