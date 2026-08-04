using UnityEngine;

// Visual placeholder untuk enemy: lingkaran solid + reaksi warna
// (flash putih saat Hurt, abu-abu transparan saat Dead).
// Begitu ada art final: drag sprite-nya ke field "Custom Sprite" di Inspector —
// placeholder lingkaran otomatis di-skip, gak perlu ubah kode lagi.
[RequireComponent(typeof(EnemyHealth))]
public class EnemyVisual : MonoBehaviour
{
    [Header("Placeholder Look (dipakai kalau sprite di bawah kosong)")]
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private float size = 1.5f;

    [Header("Real Art (opsional, isi kalau sudah ada asset)")]
    [Tooltip("Sprite normal/idle. Kalau kosong, pakai placeholder lingkaran.")]
    [SerializeField] private Sprite idleSprite;
    [Tooltip("Sprite saat kena hit. Kalau kosong, fallback ke flash putih di sprite idle.")]
    [SerializeField] private Sprite hurtSprite;
    [Tooltip("Sprite saat mati. Kalau kosong, fallback ke tint abu-abu transparan.")]
    [SerializeField] private Sprite deadSprite;
    [Tooltip("Sprite taunt/pose menang enemy — dipakai saat PLAYER kalah (StageManager.Result == Lose), bukan saat enemy mati.")]
    [SerializeField] private Sprite tauntSprite;

    private SpriteRenderer _spriteRenderer;
    private EnemyHealth _health;
    private Sprite _baseSprite;
    private Color _baseColor;
    private float _hurtFlashTimer;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();

        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        // Pakai sprite idle asli kalau ada, kalau nggak baru bikin placeholder lingkaran.
        _baseSprite = idleSprite != null ? idleSprite : CreatePlaceholderSprite();
        _spriteRenderer.sprite = _baseSprite;

        // Kalau pakai sprite asli, biasanya kamu mau warna aslinya (no-tint),
        // bukan di-tint merah kayak placeholder.
        _baseColor = idleSprite != null ? Color.white : enemyColor;
        _spriteRenderer.color = _baseColor;

        transform.localScale = Vector3.one * size;

        _health.OnStateChanged += HandleStateChanged;

        if (StageManager.Instance != null)
            StageManager.Instance.OnStageEnded += HandleStageEnded;
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnStateChanged -= HandleStateChanged;

        if (StageManager.Instance != null)
            StageManager.Instance.OnStageEnded -= HandleStageEnded;
    }

    private void Update()
    {
        if (_hurtFlashTimer > 0f)
        {
            _hurtFlashTimer -= Time.deltaTime;
            if (_hurtFlashTimer <= 0f)
            {
                _spriteRenderer.sprite = _baseSprite;
                _spriteRenderer.color = _baseColor;
            }
        }
    }

    private void HandleStateChanged(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Hurt:
                if (hurtSprite != null)
                {
                    _spriteRenderer.sprite = hurtSprite;
                    _spriteRenderer.color = Color.white;
                }
                else
                {
                    // Fallback kalau belum ada sprite hurt: flash putih di sprite yang sama.
                    _spriteRenderer.color = Color.white;
                }
                _hurtFlashTimer = 0.1f;
                break;

            case EnemyState.Dead:
                if (deadSprite != null)
                {
                    _spriteRenderer.sprite = deadSprite;
                    _spriteRenderer.color = Color.white;
                }
                else
                {
                    // Fallback kalau belum ada sprite dead: tint abu-abu transparan.
                    _spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
                break;
        }
    }

    // Enemy menang = player kalah. Ini kebalikan dari Dead, jadi datangnya dari
    // StageManager (level stage), bukan dari EnemyHealth (level enemy itu sendiri).
    private void HandleStageEnded(StageResult result)
    {
        if (result != StageResult.Lose || tauntSprite == null) return;

        _spriteRenderer.sprite = tauntSprite;
        _spriteRenderer.color = Color.white;
    }

    // Bikin sprite lingkaran solid lewat kode (gak butuh file gambar).
    // Cuma dipakai kalau customSprite kosong.
    private Sprite CreatePlaceholderSprite()
    {
        int resolution = 64;
        Texture2D tex = new Texture2D(resolution, resolution);
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= radius ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }
}