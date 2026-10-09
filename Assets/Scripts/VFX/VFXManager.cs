using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Central place for screen/camera "juice" and temporary screen VFX.
// Screen effects are intentionally soft: they fade in/out around the edges
// instead of looking like a hard UI frame or a flashing presentation overlay.
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private ParticleSystem hitParticlesPrefab;
    [SerializeField] private ParticleSystem deathParticlesPrefab;

    [Header("Hourglass")]
    [Tooltip("Rewind clock drawn on the middle of the enemy while the Hourglass is active.")]
    [SerializeField] private bool showEnemyClock = true;
    [Tooltip("The old stopwatch in the middle of the screen. Off = only the clock on the enemy.")]
    [SerializeField] private bool showScreenStopwatch = false;

    [Header("Books")]
    [Tooltip("Small + / x signs floating along the screen border when a book boosts an attack.")]
    [SerializeField] private bool showBorderSigns = true;

    // Posisi stopwatch: di tengah layar, sedikit ke bawah (satuan canvas 1920x1080).
    private const float SymbolBaseY = -60f;

    private Vector3 _originalCamPos;
    private float _shakeTimer;
    private float _shakeIntensity;

    private Canvas _screenCanvas;
    private Image _screenEdgeFade;
    private Image _hourglassSymbol;   // wajah stopwatch
    private Image _stopwatchHand;     // jarum yang berputar (anak dari wajah)
    private Coroutine _screenEffectRoutine;
    private bool _rewindActive;
    private int _rewindSessionId = 0;

    private Sprite _softEdgeSprite;
    private Sprite _stopwatchSprite;
    private Sprite _handSprite;

    private Sprite _plusSprite;
    private RectTransform _signRoot;
    private readonly List<Image> _signPool = new List<Image>();

    private enum ScreenEffectType
    {
        Addition,
        Multiplication
    }

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

    public void PlayWeaponEffect(ParticleSystem prefab, Vector3 position)
    {
        SpawnTemporary(prefab, position);
    }

    // ------------------------------------------------------------------
    // SCREEN MAGIC
    // ------------------------------------------------------------------

    // Book of Addition: soft pink magical pressure around the ENTIRE screen.
    public void PlayAdditionScreenEffect(float duration = 1.1f)
    {
        PlayScreenEffect(ScreenEffectType.Addition, duration);
    }

    // Book of Multiplication: soft amber/orange magical pressure around the
    // ENTIRE screen. No hard rectangular border.
    public void PlayMultiplicationScreenEffect(float duration = 1.1f)
    {
        PlayScreenEffect(ScreenEffectType.Multiplication, duration);
    }

    // ------------------------------------------------------------------
    // HOURGLASS REWIND (stopwatch)
    // ------------------------------------------------------------------

    // Tampilkan stopwatch yang jarumnya berputar terbalik sebagai tanda
    // "semua serangan sedang di-rewind". Tetap tampil sampai
    // EndHourglassRewind() dipanggil (ada batas aman 25 detik).
    public void BeginHourglassRewind()
    {
        EnsureScreenOverlay();

        // Immediately stop and override any existing screen routine
        if (_screenEffectRoutine != null)
        {
            StopCoroutine(_screenEffectRoutine);
            _screenEffectRoutine = null;
        }

        _rewindActive = true;
        if (showEnemyClock) WeaponVfxHourglass.BeginClock();
        _screenEffectRoutine = StartCoroutine(RewindRoutine());
    }

    public void EndHourglassRewind()
    {
        _rewindActive = false;
        WeaponVfxHourglass.EndClock();
    }

    // Versi lama: tampil sebentar lalu hilang sendiri.
    public void PlayHourglassScreenEffect(float duration = 1.05f)
    {
        BeginHourglassRewind();
        StartCoroutine(EndRewindAfter(duration));
    }

    private IEnumerator EndRewindAfter(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        EndHourglassRewind();
    }

    private IEnumerator RewindRoutine()
    {
        const float fadeIn = 0.25f;
        const float fadeOut = 0.35f;
        const float maxHold = 25f;
        const float handSpeed = 540f; // derajat/detik, berlawanan arah jarum jam

        _screenEdgeFade.gameObject.SetActive(true);
        _hourglassSymbol.gameObject.SetActive(showScreenStopwatch);

        float elapsed = 0f;
        float handAngle = 0f;
        float visible = 0f;

        // Fade in lalu tahan selama rewind berlangsung.
        while (_rewindActive && elapsed < maxHold)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            handAngle += handSpeed * dt;

            visible = Smooth01(elapsed / fadeIn);
            ApplyRewindFrame(visible, elapsed, handAngle);

            yield return null;
        }

        // Fade out.
        float startVisible = visible;
        float outTimer = 0f;

        while (outTimer < fadeOut)
        {
            float dt = Time.unscaledDeltaTime;
            outTimer += dt;
            elapsed += dt;
            handAngle += handSpeed * dt;

            visible = startVisible * (1f - Smooth01(outTimer / fadeOut));
            ApplyRewindFrame(visible, elapsed, handAngle);

            yield return null;
        }

        _screenEdgeFade.color = Color.clear;
        _screenEdgeFade.gameObject.SetActive(false);
        _hourglassSymbol.gameObject.SetActive(false);
        _stopwatchHand.rectTransform.localRotation = Quaternion.identity;
        _hourglassSymbol.rectTransform.localScale = Vector3.one;
        _hourglassSymbol.rectTransform.anchoredPosition = new Vector2(0f, SymbolBaseY);

        _rewindActive = false;
        _screenEffectRoutine = null;
    }

    private void ApplyRewindFrame(float visible, float elapsed, float handAngle)
    {
        // Samar di pinggir layar (sama seperti efek lain), stopwatch lebih jelas.
        _screenEdgeFade.color = new Color(0.55f, 0.66f, 0.82f, 0.105f * visible);
        _hourglassSymbol.color = new Color(0.78f, 0.88f, 1f, 0.75f * visible);
        _stopwatchHand.color = new Color(0.9f, 0.95f, 1f, 0.9f * visible);

        float scale = 1f + Mathf.Sin(elapsed * 3f) * 0.03f;
        _hourglassSymbol.rectTransform.anchoredPosition = new Vector2(0f, SymbolBaseY);
        _hourglassSymbol.rectTransform.localScale = Vector3.one * scale;

        // Sudut positif = berlawanan arah jarum jam (rewind).
        _stopwatchHand.rectTransform.localRotation = Quaternion.Euler(0f, 0f, handAngle);
    }

    // ------------------------------------------------------------------
    // BOOK SCREEN EFFECT
    // ------------------------------------------------------------------

    private void PlayScreenEffect(ScreenEffectType type, float duration)
    {
        EnsureScreenOverlay();

<<<<<<< HEAD
        _rewindActive = false;
        WeaponVfxHourglass.EndClock();
=======
        // If an hourglass rewind is running, don't kill it with book vignette
        if (_rewindActive) return;
>>>>>>> origin/main

        if (_screenEffectRoutine != null)
        {
            StopCoroutine(_screenEffectRoutine);
            _screenEffectRoutine = null;
        }

        _screenEffectRoutine = StartCoroutine(ScreenEffectRoutine(type, duration));
    }

    private IEnumerator ScreenEffectRoutine(ScreenEffectType type, float duration)
    {
        _screenEdgeFade.gameObject.SetActive(true);
        _hourglassSymbol.gameObject.SetActive(false);

        Color effectColor;
        float maxAlpha;

        switch (type)
        {
            case ScreenEffectType.Addition:
                // Dusty rose rather than neon pink.
                effectColor = new Color(0.90f, 0.38f, 0.58f, 1f);
                maxAlpha = 0.15f;
                break;

            default:
                // Warm amber rather than bright orange UI.
                effectColor = new Color(0.93f, 0.56f, 0.22f, 1f);
                maxAlpha = 0.14f;
                break;
        }

        if (showBorderSigns)
            StartCoroutine(BorderSignsRoutine(type == ScreenEffectType.Addition, effectColor, duration));

        float fadeIn = Mathf.Min(0.20f, duration * 0.28f);
        float fadeOut = Mathf.Min(0.34f, duration * 0.38f);
        float holdEnd = Mathf.Max(fadeIn, duration - fadeOut);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float alpha;
            if (elapsed < fadeIn)
            {
                alpha = Smooth01(elapsed / Mathf.Max(0.001f, fadeIn));
            }
            else if (elapsed > holdEnd)
            {
                alpha = 1f - Smooth01((elapsed - holdEnd) / Mathf.Max(0.001f, fadeOut));
            }
            else
            {
                // Keep it nearly stable instead of pulsing.
                alpha = 1f;
            }

            _screenEdgeFade.color = new Color(effectColor.r, effectColor.g, effectColor.b, maxAlpha * alpha);

            yield return null;
        }

        _screenEdgeFade.color = Color.clear;
        _screenEdgeFade.gameObject.SetActive(false);
        _screenEffectRoutine = null;
    }

    private void EnsureScreenOverlay()
    {
        if (_screenCanvas == null)
        {
            GameObject canvasObject = new GameObject("VFX_ScreenCanvas");
            canvasObject.transform.SetParent(transform, false);

            _screenCanvas = canvasObject.AddComponent<Canvas>();
            _screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _screenCanvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // No GraphicRaycaster: this overlay is purely visual.
        }

        if (_screenEdgeFade == null)
        {
            GameObject edgeObject = new GameObject("Screen_MagicEdgeFade");
            edgeObject.transform.SetParent(_screenCanvas.transform, false);

            _screenEdgeFade = edgeObject.AddComponent<Image>();
            _screenEdgeFade.sprite = GetSoftEdgeSprite();
            _screenEdgeFade.raycastTarget = false;
            _screenEdgeFade.color = Color.clear;

            RectTransform rect = _screenEdgeFade.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        if (_signRoot == null)
        {
            GameObject signRootObject = new GameObject("Border_Signs", typeof(RectTransform));
            signRootObject.transform.SetParent(_screenCanvas.transform, false);

            _signRoot = signRootObject.GetComponent<RectTransform>();
            _signRoot.anchorMin = Vector2.zero;
            _signRoot.anchorMax = Vector2.one;
            _signRoot.offsetMin = Vector2.zero;
            _signRoot.offsetMax = Vector2.zero;
        }

        if (_hourglassSymbol == null)
        {
            GameObject symbolObject = new GameObject("Stopwatch_Face");
            symbolObject.transform.SetParent(_screenCanvas.transform, false);

            _hourglassSymbol = symbolObject.AddComponent<Image>();
            _hourglassSymbol.sprite = GetStopwatchSprite();
            _hourglassSymbol.raycastTarget = false;
            _hourglassSymbol.color = Color.clear;

            RectTransform rect = _hourglassSymbol.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, SymbolBaseY);
            rect.sizeDelta = new Vector2(150f, 150f);

            // Jarum: anak dari wajah, berputar dari pusat wajah.
            GameObject handObject = new GameObject("Stopwatch_Hand");
            handObject.transform.SetParent(symbolObject.transform, false);

            _stopwatchHand = handObject.AddComponent<Image>();
            _stopwatchHand.sprite = GetHandSprite();
            _stopwatchHand.raycastTarget = false;
            _stopwatchHand.color = Color.clear;

            RectTransform handRect = _stopwatchHand.rectTransform;
            handRect.anchorMin = new Vector2(0.5f, 0.5f);
            handRect.anchorMax = new Vector2(0.5f, 0.5f);
            handRect.pivot = new Vector2(0.5f, 0f);
            handRect.anchoredPosition = Vector2.zero;
            handRect.sizeDelta = new Vector2(6f, 46f);

            symbolObject.SetActive(false);
        }
    }

    // Generates a soft vignette/edge rather than a hard rectangular frame.
    // Alpha increases smoothly toward all four edges and corners.
    private Sprite GetSoftEdgeSprite()
    {
        if (_softEdgeSprite != null)
            return _softEdgeSprite;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)(size - 1);
                float ny = y / (float)(size - 1);

                float edgeDistance = Mathf.Min(nx, 1f - nx, ny, 1f - ny);

                // The center is completely transparent. The effect gradually
                // appears only as the viewer approaches the screen edge.
                float edge = 1f - Mathf.Clamp01(edgeDistance / 0.24f);
                edge = Smooth01(edge);

                // Slightly soften corners so it feels like magical atmosphere.
                float corner = Mathf.Abs(nx - 0.5f) * Mathf.Abs(ny - 0.5f) * 1.35f;
                edge *= Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(corner));

                pixels[y * size + x] = new Color(1f, 1f, 1f, edge);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        _softEdgeSprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f);

        _softEdgeSprite.name = "Generated_Soft_Magic_Edge";
        return _softEdgeSprite;
    }

    // Wajah stopwatch (tanpa jarum): cincin, tombol atas, tombol samping,
    // dan 12 tanda waktu. Tidak butuh asset gambar.
    private Sprite GetStopwatchSprite()
    {
        if (_stopwatchSprite != null)
            return _stopwatchSprite;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Vector2 c = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        // Cincin luar.
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d >= 41f && d <= 46f)
                    pixels[y * size + x] = Color.white;
            }
        }

        // Titik pusat tempat jarum berputar.
        DrawFilledCircle(pixels, size, c, 4f);

        // Tombol atas (batang + tutup).
        DrawThickLine(pixels, size, c + new Vector2(0f, 47f), c + new Vector2(0f, 52f), 2);
        DrawThickLine(pixels, size, c + new Vector2(-7f, 56f), c + new Vector2(7f, 56f), 3);

        // Tombol samping miring.
        DrawThickLine(pixels, size, c + new Vector2(32f, 32f), c + new Vector2(39f, 39f), 3);

        // 12 tanda waktu; yang di 12/3/6/9 lebih panjang.
        for (int i = 0; i < 12; i++)
        {
            float ang = i * 30f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang));
            bool major = i % 3 == 0;

            float inner = major ? 31f : 35f;
            float outer = 38f;

            DrawThickLine(pixels, size, c + dir * inner, c + dir * outer, major ? 2 : 1);
        }

        texture.SetPixels(pixels);
        texture.Apply();

        _stopwatchSprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f);

        _stopwatchSprite.name = "Generated_Stopwatch_Face";
        return _stopwatchSprite;
    }

    // Jarum: batang tipis dengan pivot di pangkal (bawah).
    private Sprite GetHandSprite()
    {
        if (_handSprite != null)
            return _handSprite;

        const int w = 8;
        const int h = 64;

        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();

        _handSprite = Sprite.Create(
            texture,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0f),
            100f);

        _handSprite.name = "Generated_Stopwatch_Hand";
        return _handSprite;
    }

    // ------------------------------------------------------------------
    // BORDER SIGNS (+ for Addition, x for Multiplication)
    // ------------------------------------------------------------------

    // Small signs pop up along the four screen edges, drift inward and fade,
    // to show "your attack has been added / multiplied".
    private IEnumerator BorderSignsRoutine(bool plus, Color color, float duration)
    {
        EnsureScreenOverlay();

        RectTransform canvasRect = (RectTransform)_screenCanvas.transform;
        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;

        const int count = 18;
        const float driftAmount = 36f;

        float life = Mathf.Max(0.4f, duration * 0.6f);
        float maxDelay = Mathf.Max(0f, duration - life);
        Color signColor = Color.Lerp(color, Color.white, 0.25f);

        Image[] signs = new Image[count];
        Vector2[] start = new Vector2[count];
        Vector2[] drift = new Vector2[count];
        float[] delay = new float[count];
        float[] baseRotation = new float[count];

        for (int i = 0; i < count; i++)
        {
            float inset = Random.Range(40f, 120f);
            float along = Random.Range(-0.92f, 0.92f);

            // Spread evenly over the four edges: top, bottom, left, right.
            switch (i % 4)
            {
                case 0:
                    start[i] = new Vector2(along * halfW, halfH - inset);
                    drift[i] = new Vector2(0f, -driftAmount);
                    break;
                case 1:
                    start[i] = new Vector2(along * halfW, -halfH + inset);
                    drift[i] = new Vector2(0f, driftAmount);
                    break;
                case 2:
                    start[i] = new Vector2(-halfW + inset, along * halfH);
                    drift[i] = new Vector2(driftAmount, 0f);
                    break;
                default:
                    start[i] = new Vector2(halfW - inset, along * halfH);
                    drift[i] = new Vector2(-driftAmount, 0f);
                    break;
            }

            delay[i] = Random.Range(0f, maxDelay);

            // The "x" is the same sprite turned 45 degrees.
            baseRotation[i] = (plus ? 0f : 45f) + Random.Range(-12f, 12f);

            float size = Random.Range(34f, 64f);
            signs[i] = RentSign();
            signs[i].rectTransform.sizeDelta = new Vector2(size, size);
            signs[i].rectTransform.anchoredPosition = start[i];
            signs[i].color = new Color(signColor.r, signColor.g, signColor.b, 0f);
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < count; i++)
            {
                if (signs[i] == null) continue;

                float t = (elapsed - delay[i]) / life;

                if (t < 0f || t > 1f)
                {
                    signs[i].color = new Color(signColor.r, signColor.g, signColor.b, 0f);
                    continue;
                }

                float alpha = Smooth01(t / 0.25f) * (1f - Smooth01((t - 0.55f) / 0.45f));
                float scale = Mathf.Lerp(0.5f, 1f, Smooth01(t / 0.3f));

                RectTransform rt = signs[i].rectTransform;
                rt.anchoredPosition = start[i] + drift[i] * t;
                rt.localScale = Vector3.one * scale;
                rt.localRotation = Quaternion.Euler(0f, 0f, baseRotation[i] + t * 25f);

                signs[i].color = new Color(signColor.r, signColor.g, signColor.b, 0.9f * alpha);
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (signs[i] != null)
                signs[i].gameObject.SetActive(false);
        }
    }

    private Image RentSign()
    {
        for (int i = 0; i < _signPool.Count; i++)
        {
            Image existing = _signPool[i];
            if (existing != null && !existing.gameObject.activeSelf)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }
        }

        GameObject go = new GameObject("BorderSign");
        go.transform.SetParent(_signRoot, false);

        Image img = go.AddComponent<Image>();
        img.sprite = GetPlusSprite();
        img.raycastTarget = false;
        img.color = Color.clear;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        _signPool.Add(img);
        return img;
    }

    // White "+" with a soft glow. Tinted per effect; turned 45 degrees it becomes "x".
    private Sprite GetPlusSprite()
    {
        if (_plusSprite != null)
            return _plusSprite;

        const int size = 64;
        const float halfArm = 24f;
        const float halfThick = 5f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - c);
                float dy = Mathf.Abs(y - c);

                // Distance to the two crossing segments that form the plus.
                float dH = dx <= halfArm ? dy : Mathf.Sqrt((dx - halfArm) * (dx - halfArm) + dy * dy);
                float dV = dy <= halfArm ? dx : Mathf.Sqrt((dy - halfArm) * (dy - halfArm) + dx * dx);
                float d = Mathf.Min(dH, dV);

                float core = Mathf.Clamp01(halfThick - d + 0.5f);
                float glow = Mathf.Clamp01(1f - d / (halfThick + 7f)) * 0.3f;

                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Max(core, glow));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        _plusSprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f);

        _plusSprite.name = "Generated_Plus_Sign";
        return _plusSprite;
    }

    private static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static void DrawThickLine(Color[] pixels, int size, Vector2 a, Vector2 b, int thickness)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * 2f);

        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : i / (float)steps;
            DrawFilledCircle(pixels, size, Vector2.Lerp(a, b, t), thickness);
        }
    }

    private static void DrawFilledCircle(Color[] pixels, int size, Vector2 center, float radius)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius));

        float radiusSqr = radius * radius;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if ((new Vector2(x, y) - center).sqrMagnitude <= radiusSqr)
                    pixels[y * size + x] = Color.white;
            }
        }
    }

    private void SpawnTemporary(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null) return;

        ParticleSystem fx = Instantiate(prefab, position, Quaternion.identity);
        ParticleSystem.MainModule main = fx.main;
        float lifetime = main.duration + main.startLifetime.constantMax + 0.5f;
        Destroy(fx.gameObject, lifetime);
    }
}