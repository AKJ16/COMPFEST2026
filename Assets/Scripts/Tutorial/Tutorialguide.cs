using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panduan tutorial untuk scene StageTutorial.
//
// - Panduan muncul sebagai scroll (gulungan kertas) yang terbuka dari atas.
// - Panah kuning berdenyut menunjuk ke Timer, HP musuh, buku Almanac,
//   slot senjata, dan grid.
// - Ada satu langkah yang menampilkan gambar musuh yang kena serang.
// - Timer dihentikan selama panduan tampil, lalu dimulai ulang di akhir.
//
// Semua UI dibuat lewat kode, jadi tidak ada yang perlu diatur di Inspector.
// Pasang script ini di GameObject kosong di scene StageTutorial.
public class TutorialGuide : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Angka yang boleh kamu ubah kalau tampilannya kurang pas
    // (satuan: canvas 1920 x 1080)
    // ------------------------------------------------------------------
    private const float ScrollTopY = 385f;      // posisi atas scroll dari tengah layar (naik = lebih tinggi)
    private const float ScrollWidth = 840f;     // lebar batang gulungan
    private const float ScrollHeight = 400f;    // tinggi total scroll saat terbuka
    private const float RodHeight = 32f;        // tebal batang gulungan
    private const float PaperWidth = 780f;      // lebar kertas
    private const float ArrowGap = 10f;         // jarak ujung panah ke target
    private const float StartDelay = 1.2f;      // jeda sebelum panduan muncul

    // Gambar musuh kena serang: Assets/Resources/Tutorial/TutorialEnemyHit.png
    private const string HitImageResource = "Tutorial/TutorialEnemyHit";

    private const float PaperHeight = ScrollHeight - 2f * RodHeight;

    private enum ArrowSide { None, Above, Below, Left, Right }
    private enum TargetKind { None, Named, FirstInventorySlot, Grid }

    private class Step
    {
        public string title;
        public string body;
        public TargetKind kind;
        public string targetName;
        public ArrowSide side;
        public bool showHitImage;
    }

    private readonly List<Step> _steps = new List<Step>();

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private RectTransform _paperMask;
    private RectTransform _bottomRod;
    private CanvasGroup _content;
    private RectTransform _textRect;
    private TextMeshProUGUI _text;
    private TextMeshProUGUI _counter;
    private TextMeshProUGUI _nextLabel;
    private Image _hitImage;
    private RectTransform _arrow;
    private Image _arrowImage;

    private TMP_FontAsset _font;
    private Sprite _hitSprite;

    private Vector2 _arrowTip;
    private Vector2 _arrowDir;

    private bool _advance;
    private bool _skip;

    // ------------------------------------------------------------------
    // Mulai
    // ------------------------------------------------------------------

    private void Start()
    {
        BuildSteps();
        StartCoroutine(Run());
    }

    private void OnDestroy()
    {
        if (_canvas != null)
            Destroy(_canvas.gameObject);
    }

    private void BuildSteps()
    {
        AddStep("Welcome to GridMancer!",
            "This quick guide shows you the basics. Press Next to continue.",
            TargetKind.None, null, ArrowSide.None, false);

        AddStep("TIMER",
            "This bar is your time. When it runs out, you lose, so think fast!",
            TargetKind.Named, "Timer Container", ArrowSide.Left, false);

        AddStep("ENEMY HP",
            "This is the enemy's health. Bring it down to 0 to win the stage.",
            TargetKind.Named, "Health Container", ArrowSide.Right, false);

        AddStep("ALMANAC BOOK",
            "Open this book to read about every weapon, like its damage, and the combos you have discovered.",
            TargetKind.Named, "HandBook Button", ArrowSide.Below, false);

        AddStep("YOUR WEAPONS",
            "These are the weapons you can use. Click one to pick it up, then place it on the grid.",
            TargetKind.FirstInventorySlot, null, ArrowSide.Left, false);

        AddStep("THE GRID",
            "These squares are where you place your weapons. A weapon attacks the enemy the moment you place it!",
            TargetKind.Grid, null, ArrowSide.Left, false);

        AddStep("THE ATTACK",
            "Every weapon you place hits the enemy and deals its damage. Like this!",
            TargetKind.None, null, ArrowSide.None, true);

        AddStep("NOW YOU TRY",
            "Place both Swords on the grid and defeat the enemy before time runs out!",
            TargetKind.None, null, ArrowSide.None, false);
    }

    private void AddStep(string title, string body, TargetKind kind, string targetName, ArrowSide side, bool showHitImage)
    {
        _steps.Add(new Step
        {
            title = title,
            body = body,
            kind = kind,
            targetName = targetName,
            side = side,
            showHitImage = showHitImage
        });
    }

    // ------------------------------------------------------------------
    // Alur utama
    // ------------------------------------------------------------------

    private IEnumerator Run()
    {
        // Tunggu sampai stage benar-benar mulai (musuh dan grid sudah dibuat).
        float waited = 0f;
        while (waited < 3f && (StageManager.Instance == null || StageManager.Instance.CurrentEnemy == null))
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        // Beri waktu supaya banner "Stage" selesai tampil.
        yield return new WaitForSecondsRealtime(StartDelay);

        PauseTimer();
        BuildUI();

        _content.alpha = 0f;
        yield return Roll(true);

        for (int i = 0; i < _steps.Count && !_skip; i++)
        {
            yield return ShowStep(i);

            _advance = false;
            while (!_advance && !_skip)
                yield return null;
        }

        yield return Finish();
    }

    private IEnumerator ShowStep(int index)
    {
        Step step = _steps[index];

        yield return FadeContent(0f, 0.12f);

        bool hasImage = step.showHitImage && _hitSprite != null;

        _text.text = "<size=125%><b>" + step.title + "</b></size>\n" + step.body;
        _textRect.offsetMin = new Vector2(44f, 96f);
        _textRect.offsetMax = new Vector2(hasImage ? -300f : -44f, -26f);

        _hitImage.gameObject.SetActive(hasImage);

        _counter.text = (index + 1) + " / " + _steps.Count;
        _nextLabel.text = index == _steps.Count - 1 ? "Let's go!" : "Next >";

        PlaceArrow(step);

        yield return FadeContent(1f, 0.18f);
    }

    private IEnumerator Finish()
    {
        if (_arrow != null)
            _arrow.gameObject.SetActive(false);

        yield return FadeContent(0f, 0.15f);
        yield return Roll(false);

        if (_canvas != null)
            Destroy(_canvas.gameObject);

        ResumeTimer();
    }

    private IEnumerator FadeContent(float to, float duration)
    {
        float from = _content.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _content.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        _content.alpha = to;
    }

    // Scroll terbuka (open = true) atau tergulung lagi (open = false).
    private IEnumerator Roll(bool open)
    {
        float from = open ? 0f : PaperHeight;
        float to = open ? PaperHeight : 0f;
        const float duration = 0.55f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            SetPaperHeight(Mathf.Lerp(from, to, k));
            yield return null;
        }

        SetPaperHeight(to);
    }

    private void SetPaperHeight(float h)
    {
        _paperMask.sizeDelta = new Vector2(PaperWidth, h);
        _bottomRod.anchoredPosition = new Vector2(0f, -(RodHeight + h));
    }

    // ------------------------------------------------------------------
    // Timer
    // ------------------------------------------------------------------

    private void PauseTimer()
    {
        if (TimerManager.Instance == null) return;

        TimerManager.Instance.StopTimer();
        TimerManager.Instance.ResetTimer();
    }

    private void ResumeTimer()
    {
        if (TimerManager.Instance == null) return;

        TimerManager.Instance.ResetTimer();
        TimerManager.Instance.StartTimer();
    }

    // ------------------------------------------------------------------
    // Panah
    // ------------------------------------------------------------------

    private void PlaceArrow(Step step)
    {
        if (step.side == ArrowSide.None || !TryGetTargetRect(step, out Rect r))
        {
            _arrow.gameObject.SetActive(false);
            return;
        }

        Vector2 center = r.center;

        switch (step.side)
        {
            case ArrowSide.Above:
                _arrowTip = new Vector2(center.x, r.yMax + ArrowGap);
                _arrowDir = new Vector2(0f, -1f);
                break;

            case ArrowSide.Below:
                _arrowTip = new Vector2(center.x, r.yMin - ArrowGap);
                _arrowDir = new Vector2(0f, 1f);
                break;

            case ArrowSide.Left:
                _arrowTip = new Vector2(r.xMin - ArrowGap, center.y);
                _arrowDir = new Vector2(1f, 0f);
                break;

            default: // Right
                _arrowTip = new Vector2(r.xMax + ArrowGap, center.y);
                _arrowDir = new Vector2(-1f, 0f);
                break;
        }

        // Gambar panah menunjuk ke atas; putar supaya menunjuk ke arah target.
        float angle = Mathf.Atan2(_arrowDir.y, _arrowDir.x) * Mathf.Rad2Deg - 90f;
        _arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        _arrow.anchoredPosition = _arrowTip;
        _arrow.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_arrow == null || !_arrow.gameObject.activeSelf) return;

        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);

        // Panah maju-mundur ke arah target sambil membesar-mengecil.
        _arrow.anchoredPosition = _arrowTip - _arrowDir * (wave * 18f);

        float s = 1f + 0.10f * wave;
        _arrow.localScale = new Vector3(s, s, 1f);

        _arrowImage.color = new Color(1f, 1f, 1f, 0.8f + 0.2f * (1f - wave));
    }

    // Mencari kotak target di koordinat canvas panduan.
    private bool TryGetTargetRect(Step step, out Rect localRect)
    {
        localRect = default(Rect);

        Vector2 screenMin;
        Vector2 screenMax;

        switch (step.kind)
        {
            case TargetKind.Named:
            {
                GameObject go = GameObject.Find(step.targetName);
                if (go == null) { Debug.LogWarning("[TutorialGuide] Objek tidak ditemukan: " + step.targetName); return false; }

                RectTransform rt = go.transform as RectTransform;
                if (rt == null) return false;

                GetScreenRect(rt, out screenMin, out screenMax);
                break;
            }

            case TargetKind.FirstInventorySlot:
            {
                GameObject bar = GameObject.Find("Inventory Bar");
                if (bar == null) { Debug.LogWarning("[TutorialGuide] 'Inventory Bar' tidak ditemukan."); return false; }

                RectTransform target = bar.transform as RectTransform;

                for (int i = 0; i < bar.transform.childCount; i++)
                {
                    Transform child = bar.transform.GetChild(i);
                    if (child.gameObject.activeInHierarchy && child is RectTransform)
                    {
                        target = (RectTransform)child;
                        break;
                    }
                }

                if (target == null) return false;

                GetScreenRect(target, out screenMin, out screenMax);
                break;
            }

            case TargetKind.Grid:
            {
                if (GridManager.Instance == null) { Debug.LogWarning("[TutorialGuide] GridManager tidak ditemukan."); return false; }

                Renderer[] renderers = GridManager.Instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return false;

                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);

                Camera cam = Camera.main;
                if (cam == null) return false;

                Vector3 p0 = cam.WorldToScreenPoint(b.min);
                Vector3 p1 = cam.WorldToScreenPoint(b.max);

                screenMin = new Vector2(Mathf.Min(p0.x, p1.x), Mathf.Min(p0.y, p1.y));
                screenMax = new Vector2(Mathf.Max(p0.x, p1.x), Mathf.Max(p0.y, p1.y));
                break;
            }

            default:
                return false;
        }

        Vector2 a;
        Vector2 c;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenMin, null, out a);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenMax, null, out c);

        localRect = Rect.MinMaxRect(
            Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y),
            Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));

        return true;
    }

    // Kotak elemen UI di koordinat layar (bisa Overlay maupun Screen Space - Camera).
    private static void GetScreenRect(RectTransform rt, out Vector2 min, out Vector2 max)
    {
        Camera cam = null;
        Canvas canvas = rt.GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvas = canvas.rootCanvas;

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }
        }

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        max = min;

        for (int i = 1; i < 4; i++)
        {
            Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
    }

    // ------------------------------------------------------------------
    // Membuat UI lewat kode
    // ------------------------------------------------------------------

    private void BuildUI()
    {
        // Font: pakai font yang sama dengan teks HP di game kalau ada.
        GameObject healthText = GameObject.Find("Health Text");
        if (healthText != null)
        {
            TMP_Text tmp = healthText.GetComponent<TMP_Text>();
            if (tmp != null) _font = tmp.font;
        }

        // Gambar musuh kena serang (opsional).
        Texture2D tex = Resources.Load<Texture2D>(HitImageResource);
        if (tex != null)
        {
            _hitSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        else
        {
            Debug.LogWarning("[TutorialGuide] Gambar tidak ditemukan di Assets/Resources/" + HitImageResource + ".png");
        }

        // Canvas
        GameObject canvasGo = new GameObject("Tutorial Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 4000;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _canvasRect = canvasGo.GetComponent<RectTransform>();

        // Penghalang klik: selama panduan, pemain tidak bisa menekan tombol lain.
        GameObject blocker = new GameObject("Click Blocker", typeof(RectTransform), typeof(Image));
        blocker.transform.SetParent(canvasGo.transform, false);
        Stretch(blocker.GetComponent<RectTransform>());
        Image blockerImage = blocker.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0f);
        blockerImage.raycastTarget = true;

        // Sprite buatan kode
        Sprite paperSprite = MakePaperSprite();
        Sprite rodSprite = MakeRodSprite();
        Sprite buttonSprite = MakeRoundedSprite(48, 16, new Color(0.45f, 0.27f, 0.12f, 1f), new Color(0.2f, 0.1f, 0.03f, 1f), 3);
        Sprite arrowSprite = MakeArrowSprite();

        // Scroll: akar (atas tetap, terbuka ke bawah)
        GameObject rootGo = new GameObject("Scroll", typeof(RectTransform));
        rootGo.transform.SetParent(canvasGo.transform, false);
        RectTransform root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, ScrollTopY);
        root.sizeDelta = new Vector2(ScrollWidth, ScrollHeight);

        // Kertas (dipotong oleh mask supaya terlihat membuka)
        GameObject maskGo = new GameObject("Paper Mask", typeof(RectTransform), typeof(RectMask2D));
        maskGo.transform.SetParent(rootGo.transform, false);
        _paperMask = maskGo.GetComponent<RectTransform>();
        SetTopAnchored(_paperMask, PaperWidth, 0f, -RodHeight);

        GameObject contentGo = new GameObject("Paper Content", typeof(RectTransform), typeof(CanvasGroup));
        contentGo.transform.SetParent(maskGo.transform, false);
        RectTransform contentRect = contentGo.GetComponent<RectTransform>();
        SetTopAnchored(contentRect, PaperWidth, PaperHeight, 0f);
        _content = contentGo.GetComponent<CanvasGroup>();

        GameObject paperGo = new GameObject("Paper", typeof(RectTransform), typeof(Image));
        paperGo.transform.SetParent(contentGo.transform, false);
        Stretch(paperGo.GetComponent<RectTransform>());
        Image paperImage = paperGo.GetComponent<Image>();
        paperImage.sprite = paperSprite;
        paperImage.raycastTarget = false;

        // Batang gulungan atas dan bawah
        CreateRod("Top Rod", rootGo.transform, rodSprite, 0f);
        _bottomRod = CreateRod("Bottom Rod", rootGo.transform, rodSprite, -RodHeight);

        // Teks utama
        Color ink = new Color(0.24f, 0.13f, 0.05f, 1f);

        _text = MakeText("Text", contentGo.transform, 34f, TextAlignmentOptions.TopLeft, ink, false);
        _text.enableAutoSizing = true;
        _text.fontSizeMin = 22f;
        _text.fontSizeMax = 34f;
        _textRect = _text.rectTransform;
        Stretch(_textRect);

        // Gambar musuh kena serang
        GameObject hitGo = new GameObject("Hit Image", typeof(RectTransform), typeof(Image));
        hitGo.transform.SetParent(contentGo.transform, false);
        RectTransform hitRect = hitGo.GetComponent<RectTransform>();
        hitRect.anchorMin = new Vector2(1f, 0.5f);
        hitRect.anchorMax = new Vector2(1f, 0.5f);
        hitRect.pivot = new Vector2(1f, 0.5f);
        hitRect.anchoredPosition = new Vector2(-34f, 24f);
        hitRect.sizeDelta = new Vector2(240f, 215f);
        _hitImage = hitGo.GetComponent<Image>();
        _hitImage.sprite = _hitSprite;
        _hitImage.preserveAspect = true;
        _hitImage.raycastTarget = false;
        hitGo.SetActive(false);

        // Nomor langkah
        _counter = MakeText("Counter", contentGo.transform, 24f, TextAlignmentOptions.Center, new Color(0.24f, 0.13f, 0.05f, 0.7f), false);
        SetBottomAnchored(_counter.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(140f, 36f));

        // Tombol Next
        Button next = MakeButton("Next Button", contentGo.transform, buttonSprite, "Next >", 30f, new Color(1f, 0.93f, 0.75f, 1f), out _nextLabel);
        SetBottomAnchored((RectTransform)next.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-34f, 22f), new Vector2(190f, 54f));
        next.onClick.AddListener(() => _advance = true);

        // Tombol Skip
        TextMeshProUGUI skipLabel;
        Button skip = MakeButton("Skip Button", contentGo.transform, null, "Skip tutorial", 22f, new Color(0.24f, 0.13f, 0.05f, 0.75f), out skipLabel);
        SetBottomAnchored((RectTransform)skip.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 28f), new Vector2(190f, 40f));
        skip.onClick.AddListener(() => _skip = true);

        // Panah
        GameObject arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowGo.transform.SetParent(canvasGo.transform, false);
        _arrow = arrowGo.GetComponent<RectTransform>();
        _arrow.anchorMin = new Vector2(0.5f, 0.5f);
        _arrow.anchorMax = new Vector2(0.5f, 0.5f);
        _arrow.pivot = new Vector2(0.5f, 0.958f); // ujung panah
        _arrow.sizeDelta = new Vector2(70f, 105f);
        _arrowImage = arrowGo.GetComponent<Image>();
        _arrowImage.sprite = arrowSprite;
        _arrowImage.raycastTarget = false;
        arrowGo.SetActive(false);

        SetPaperHeight(0f);
    }

    private RectTransform CreateRod(string name, Transform parent, Sprite sprite, float y)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        SetTopAnchored(rt, ScrollWidth, RodHeight, y);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;

        return rt;
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, float size, TextAlignmentOptions align, Color color, bool bold)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;

        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        if (bold) t.fontStyle = FontStyles.Bold;

        return t;
    }

    private Button MakeButton(string name, Transform parent, Sprite background, string label, float fontSize, Color textColor, out TextMeshProUGUI labelText)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.raycastTarget = true;

        if (background != null)
        {
            img.sprite = background;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(1f, 1f, 1f, 0f); // tombol tak terlihat, hanya teks
        }

        Button button = go.GetComponent<Button>();
        button.targetGraphic = img;

        labelText = MakeText("Label", go.transform, fontSize, TextAlignmentOptions.Center, textColor, true);
        Stretch(labelText.rectTransform);
        labelText.text = label;

        return button;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Menempel di tengah-atas induk.
    private static void SetTopAnchored(RectTransform rt, float width, float height, float y)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(width, height);
    }

    private static void SetBottomAnchored(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    // ------------------------------------------------------------------
    // Sprite buatan kode (tidak perlu file gambar)
    // ------------------------------------------------------------------

    // Kertas perkamen: krem dengan tepi agak gelap dan sedikit bercak.
    private static Sprite MakePaperSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color baseColor = new Color(0.94f, 0.85f, 0.63f, 1f);
        Color edgeColor = new Color(0.72f, 0.55f, 0.30f, 1f);
        Color[] px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size;
                float ny = (y + 0.5f) / size;

                float d = Mathf.Max(Mathf.Abs(nx - 0.5f), Mathf.Abs(ny - 0.5f)) * 2f;
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.70f, 1f, d));

                float noise = (Mathf.PerlinNoise(nx * 6f, ny * 6f) - 0.5f) * 0.10f;

                Color c = Color.Lerp(baseColor, edgeColor, edge * 0.85f);
                c.r += noise; c.g += noise; c.b += noise;
                c.a = 1f;

                px[y * size + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Batang gulungan: silinder kayu dengan ujung membulat (9-slice horizontal).
    private static Sprite MakeRodSprite()
    {
        const int w = 64;
        const int h = 32;
        const int cap = 16;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color wood = new Color(0.50f, 0.30f, 0.14f, 1f);
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            float shade = Mathf.Lerp(0.45f, 1.2f, Mathf.Sin(v * Mathf.PI));
            shade += Mathf.Exp(-Mathf.Pow((v - 0.68f) / 0.08f, 2f)) * 0.25f; // kilau

            for (int x = 0; x < w; x++)
            {
                float alpha = 1f;

                if (x < cap || x >= w - cap)
                {
                    float cx = x < cap ? cap : w - cap;
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - h * 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(h * 0.5f - dist + 0.5f);
                }

                Color c = new Color(wood.r * shade, wood.g * shade, wood.b * shade, alpha);
                px[y * w + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(cap, 0f, cap, 0f));
    }

    // Persegi membulat untuk tombol (9-slice).
    private static Sprite MakeRoundedSprite(int size, int radius, Color fill, Color outline, int outlineWidth)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px0 = x + 0.5f;
                float py0 = y + 0.5f;

                float cx = Mathf.Clamp(px0, radius, size - radius);
                float cy = Mathf.Clamp(py0, radius, size - radius);

                float d = Vector2.Distance(new Vector2(px0, py0), new Vector2(cx, cy));
                float edge = radius - d;

                Color c = edge < outlineWidth ? outline : fill;
                c.a *= Mathf.Clamp01(edge + 0.5f);

                px[y * size + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    // Panah kuning menunjuk ke atas dengan garis tepi cokelat tua.
    private static Sprite MakeArrowSprite()
    {
        const int w = 64;
        const int h = 96;
        const int outline = 3;

        bool[,] inside = new bool[w, h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - w * 0.5f);

                bool head = y >= 46 && dx <= (h - 4 - y) * (28f / 46f);
                bool stem = y >= 4 && y < 46 && dx <= 11f;

                inside[x, y] = head || stem;
            }
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color lo = new Color(1f, 0.72f, 0.12f, 1f);
        Color hi = new Color(1f, 0.92f, 0.35f, 1f);
        Color edgeColor = new Color(0.30f, 0.15f, 0.02f, 1f);
        Color clear = new Color(0f, 0f, 0f, 0f);

        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (inside[x, y])
                {
                    px[y * w + x] = Color.Lerp(lo, hi, y / (float)(h - 1));
                    continue;
                }

                bool near = false;

                for (int oy = -outline; oy <= outline && !near; oy++)
                {
                    for (int ox = -outline; ox <= outline; ox++)
                    {
                        int nx = x + ox;
                        int ny = y + oy;

                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (ox * ox + oy * oy > outline * outline) continue;

                        if (inside[nx, ny]) { near = true; break; }
                    }
                }

                px[y * w + x] = near ? edgeColor : clear;
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        // Pivot di ujung panah.
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.958f), 100f);
    }
}