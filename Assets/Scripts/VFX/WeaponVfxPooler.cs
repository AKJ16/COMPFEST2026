using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponVfxPooler : MonoBehaviour
{
    private static WeaponVfxPooler _instance;

    // Kalau belum ada di scene, pooler dibuat otomatis.
    // Jadi VFX tidak diam-diam gagal hanya karena objeknya lupa dipasang.
    public static WeaponVfxPooler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<WeaponVfxPooler>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("WeaponVfxPooler (auto)");
                    _instance = go.AddComponent<WeaponVfxPooler>();
                    Debug.Log("[WeaponVfxPooler] Tidak ada di scene, dibuat otomatis.");
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    private readonly Queue<ParticleSystem> particlePool =
        new Queue<ParticleSystem>();

    private readonly Queue<LineRenderer> linePool =
        new Queue<LineRenderer>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning(
                "[WeaponVfxPooler] Duplicate pooler found. Destroying duplicate."
            );

            Destroy(gameObject);
            return;
        }

        _instance = this;

        Debug.Log(
            "[WeaponVfxPooler] Instance initialized successfully."
        );
    }

    // =========================================================
    // PARTICLE SYSTEM
    // =========================================================

    public ParticleSystem GetParticleSystem()
    {
        while (particlePool.Count > 0)
        {
            ParticleSystem ps = particlePool.Dequeue();

            if (ps != null)
            {
                ps.gameObject.SetActive(true);

                ps.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );

                SetupForUse(ps);

                return ps;
            }
        }

        GameObject go =
            new GameObject("Pooled_ParticleSystem");

        go.transform.SetParent(transform);

        ParticleSystem newPs =
            go.AddComponent<ParticleSystem>();

        ParticleSystemRenderer renderer =
            go.GetComponent<ParticleSystemRenderer>();

        if (renderer != null)
        {
            renderer.material =
                WeaponVfxUtility.GetDefaultParticleMaterial();

            renderer.sortingOrder = 1000;
        }

        // ParticleSystem baru otomatis langsung Play. Hentikan dulu supaya
        // pemanggil bisa mengubah duration tanpa error.
        newPs.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        SetupForUse(newPs);

        return newPs;
    }

    // Tidak looping, tidak play otomatis, dan dikembalikan ke pool sendiri
    // setelah 3 detik (sebelumnya tidak pernah dikembalikan).
    private void SetupForUse(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;

        StartCoroutine(AutoReturnParticle(ps, 3f));
    }

    private IEnumerator AutoReturnParticle(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnParticleToPool(ps);
    }

    public void ReturnParticleToPool(
        ParticleSystem ps)
    {
        if (ps == null)
            return;

        ps.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        ps.gameObject.SetActive(false);

        particlePool.Enqueue(ps);
    }

    // =========================================================
    // LINE RENDERER
    // =========================================================

    public LineRenderer GetLineRenderer()
    {
        while (linePool.Count > 0)
        {
            LineRenderer lr =
                linePool.Dequeue();

            if (lr != null)
            {
                lr.gameObject.SetActive(true);

                ResetLineRenderer(lr);

                return lr;
            }
        }

        GameObject go =
            new GameObject("Pooled_LineRenderer");

        go.transform.SetParent(transform);

        LineRenderer newLr =
            go.AddComponent<LineRenderer>();

        SetupLineRenderer(newLr);

        return newLr;
    }

    public void ReturnLineToPool(
        LineRenderer lr)
    {
        if (lr == null)
            return;

        lr.positionCount = 0;
        lr.loop = false;

        lr.gameObject.SetActive(false);

        linePool.Enqueue(lr);
    }

    // =========================================================
    // LINE SETUP
    // =========================================================

    private void SetupLineRenderer(
        LineRenderer lr)
    {
        if (lr == null)
            return;

        lr.useWorldSpace = true;

        lr.sharedMaterial =
            WeaponVfxUtility.GetLineMaterial();

        lr.textureMode =
            LineTextureMode.Stretch;

        lr.alignment =
            LineAlignment.View;

        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;

        lr.sortingOrder = 1000;

        lr.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        lr.receiveShadows = false;
    }

    private void ResetLineRenderer(
        LineRenderer lr)
    {
        if (lr == null)
            return;

        lr.positionCount = 0;
        lr.loop = false;

        lr.useWorldSpace = true;

        lr.widthMultiplier = 1f;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        lr.startColor = Color.white;
        lr.endColor = Color.white;

        lr.sharedMaterial =
            WeaponVfxUtility.GetLineMaterial();

        lr.sortingOrder = 1000;

        lr.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        lr.receiveShadows = false;
    }
}