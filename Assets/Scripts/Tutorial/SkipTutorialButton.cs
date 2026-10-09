using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SkipTutorialButton : MonoBehaviour
{
    [Header("Button Appearance")]
    [SerializeField] private string buttonText = "Skip to Game >";
    [SerializeField] private float fontSize = 24f;
    [SerializeField] private Vector2 buttonSize = new Vector2(210f, 52f);
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Typography")]
    [SerializeField] private TMP_FontAsset customFont;

    [Header("Positioning (Top-Right Default)")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(-30f, -30f);

    [Header("Layering")]
    [SerializeField] private bool renderAboveTutorial = true;
    [SerializeField] private int sortingOrder = 2000;

    [Header("Fallback Scene")]
    [SerializeField] private string fallbackSceneName = "Gameplay";

    private GameObject _spawnedCanvasGo;
    private CanvasGroup _canvasGroup;
    private Button _button;
    private bool _isSkipping = false;
    private int _lastKnownStage = -1;

    private void Start()
    {
        BuildButton();

        if (StageManager.Instance != null)
        {
            _lastKnownStage = StageManager.Instance.CurrentStageNumber;
            // Listen for stage win to fade out automatically
            StageManager.Instance.OnStageEnded += HandleStageEnded;
        }
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageEnded -= HandleStageEnded;
        }
    }

    private void Update()
    {
        // If moving to a new stage (e.g. Stage 1 -> Stage 2) and didn't manually skip, restore button
        if (StageManager.Instance != null)
        {
            int currentStage = StageManager.Instance.CurrentStageNumber;
            if (currentStage != _lastKnownStage)
            {
                _lastKnownStage = currentStage;
                if (!_isSkipping && _canvasGroup != null && _canvasGroup.alpha < 1f)
                {
                    RestoreButton();
                }
            }
        }
    }

    private void HandleStageEnded(StageResult result)
    {
        // When the stage is won, smoothly fade out the skip button!
        if (result == StageResult.Win)
        {
            if (_button != null)
                _button.interactable = false;

            StartCoroutine(FadeOutRoutine());
        }
    }

    private void RestoreButton()
    {
        StopAllCoroutines();
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }
        if (_button != null)
        {
            _button.interactable = true;
        }
    }

    private void BuildButton()
    {
        if (customFont == null)
        {
            GameObject healthText = GameObject.Find("Health Text");
            if (healthText != null)
            {
                TMP_Text tmp = healthText.GetComponent<TMP_Text>();
                if (tmp != null) customFont = tmp.font;
            }
        }

        Transform rootParent = transform;

        if (renderAboveTutorial)
        {
            _spawnedCanvasGo = new GameObject("SkipButton_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _spawnedCanvasGo.transform.SetParent(transform, false);

            Canvas c = _spawnedCanvasGo.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortingOrder;

            CanvasScaler scaler = _spawnedCanvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            rootParent = _spawnedCanvasGo.transform;
        }

        Sprite buttonSprite = MakeRoundedSprite(48, 16,
            new Color(0.45f, 0.27f, 0.12f, 1f),
            new Color(0.2f, 0.1f, 0.03f, 1f), 3);

        GameObject btnGo = new GameObject("Btn_SkipTutorial", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        btnGo.transform.SetParent(rootParent, false);

        _canvasGroup = btnGo.GetComponent<CanvasGroup>();

        RectTransform rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = buttonSize;

        Image img = btnGo.GetComponent<Image>();
        img.sprite = buttonSprite;
        img.type = Image.Type.Sliced;
        img.raycastTarget = true;

        _button = btnGo.GetComponent<Button>();
        _button.targetGraphic = img;
        _button.onClick.AddListener(OnSkipClicked);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(btnGo.transform, false);

        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        if (customFont != null) label.font = customFont;
        label.text = buttonText;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.93f, 0.75f, 1f);
        label.raycastTarget = false;
    }

    private void OnSkipClicked()
    {
        if (_isSkipping) return;
        _isSkipping = true;

        if (_button != null)
            _button.interactable = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        Time.timeScale = 1f;

        if (TutorialGuide.Instance != null)
        {
            TutorialGuide.Instance.DismissImmediately();
        }

        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.StopTimer();
        }

        if (StageRandomizer.Instance != null)
        {
            StageRandomizer.Instance.LoadRandomStage();
        }
        else if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(fallbackSceneName);
        }
        else
        {
            SceneManager.LoadScene(fallbackSceneName);
        }

        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
        }
    }

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
}