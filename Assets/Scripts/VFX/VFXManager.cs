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
        if (hitParticlesPrefab != null)
            Instantiate(hitParticlesPrefab, position, Quaternion.identity);
        ShakeCamera();
    }

    public void SpawnDeathEffect(Vector3 position)
    {
        if (deathParticlesPrefab != null)
            Instantiate(deathParticlesPrefab, position, Quaternion.identity);
        ShakeCamera(0.25f, 0.2f);
    }

    // BARU: buat efek spesifik per-weapon (mis. api di Staff, slash horizontal di Sword),
    // beda dari SpawnHitEffect generic di atas. prefab boleh null (misal weapon belum
    // punya VFX sendiri) — kalau null cuma di-skip, gak error.
    public void PlayWeaponEffect(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null) return;
        Instantiate(prefab, position, Quaternion.identity);
    }
}