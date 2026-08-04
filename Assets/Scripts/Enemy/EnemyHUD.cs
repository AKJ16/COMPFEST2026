using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD musuh: health bar + angka HP + indikator "PHASE 2" untuk boss
// yang punya hasTwoPhases (misal Enemy_Stage3). Dibangun sepenuhnya
// lewat kode, sama seperti InventorySlotUI/WeaponTooltipUI.
//
// Otomatis "ngikutin" enemy yang lagi aktif di StageManager — begitu
// ganti stage (TEST: Go To Stage X), HUD ini otomatis pindah nempel
// ke EnemyHealth yang baru.
public class EnemyHUD : MonoBehaviour
{
    [Header("Bar Size")]
    [SerializeField] private float barWidth = 300f;
    [SerializeField] private float barHeight = 28f;

    private RectTransform _rect;
    private Image _backgroundBar;
    private Image _fillBar;
    private TextMeshProUGUI _hpText;
    private TextMeshProUGUI _phaseText;

    private EnemyHealth _boundEnemy;

    private readonly Color _fillNormalColor = new Color(0.75f, 0.15f, 0.15f);
    private readonly Color _fillPhase2Color = new Color(0.85f, 0.55f, 0.05f);

    private void Awake()
    {
        BuildLayout();
        _phaseText.gameObject.SetActive(false);
    }

    private void Update()
    {
        var activeEnemy = StageManager.Instance.CurrentEnemy;
        if (activeEnemy != _boundEnemy)
        {
            BindToEnemy(activeEnemy);
        }
    }

    private void BuildLayout()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect == null)
            _rect = gameObject.AddComponent<RectTransform>();

        _rect.anchorMin = new Vector2(0.5f, 1f);
        _rect.anchorMax = new Vector2(0.5f, 1f);
        _rect.pivot = new Vector2(0.5f, 1f);
        _rect.anchoredPosition = new Vector2(0f, -30f);
        _rect.sizeDelta = new Vector2(barWidth, barHeight);

        _backgroundBar = gameObject.GetComponent<Image>();
        if (_backgroundBar == null)
            _backgroundBar = gameObject.AddComponent<Image>();
        _backgroundBar.color = new Color(0f, 0f, 0f, 0.6f);
        _backgroundBar.raycastTarget = false;

        GameObject fillGO = new GameObject("FillBar", typeof(RectTransform));
        fillGO.transform.SetParent(transform, false);

        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3, 3);
        fillRect.offsetMax = new Vector2(-3, -3);

        _fillBar = fillGO.AddComponent<Image>();
        _fillBar.color = _fillNormalColor;
        _fillBar.type = Image.Type.Filled;
        _fillBar.fillMethod = Image.FillMethod.Horizontal;
        _fillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fillBar.fillAmount = 1f;
        _fillBar.raycastTarget = false;

        GameObject hpGO = new GameObject("HPText", typeof(RectTransform));
        hpGO.transform.SetParent(transform, false);

        RectTransform hpRect = hpGO.GetComponent<RectTransform>();
        hpRect.anchorMin = Vector2.zero;
        hpRect.anchorMax = Vector2.one;
        hpRect.offsetMin = Vector2.zero;
        hpRect.offsetMax = Vector2.zero;

        _hpText = hpGO.AddComponent<TextMeshProUGUI>();
        _hpText.fontSize = 16;
        _hpText.alignment = TextAlignmentOptions.Center;
        _hpText.color = Color.white;
        _hpText.raycastTarget = false;
        _hpText.text = "0/0";

        GameObject phaseGO = new GameObject("PhaseText", typeof(RectTransform));
        phaseGO.transform.SetParent(transform, false);

        RectTransform phaseRect = phaseGO.GetComponent<RectTransform>();
        phaseRect.anchorMin = new Vector2(0.5f, 0f);
        phaseRect.anchorMax = new Vector2(0.5f, 0f);
        phaseRect.pivot = new Vector2(0.5f, 1f);
        phaseRect.anchoredPosition = new Vector2(0f, -4f);
        phaseRect.sizeDelta = new Vector2(150f, 24f);

        _phaseText = phaseGO.AddComponent<TextMeshProUGUI>();
        _phaseText.fontSize = 18;
        _phaseText.fontStyle = FontStyles.Bold;
        _phaseText.alignment = TextAlignmentOptions.Center;
        _phaseText.color = _fillPhase2Color;
        _phaseText.raycastTarget = false;
        _phaseText.text = "PHASE 2";
    }

    private void BindToEnemy(EnemyHealth newEnemy)
    {
        if (_boundEnemy != null)
        {
            _boundEnemy.OnDamaged -= HandleDamaged;
            _boundEnemy.OnStateChanged -= HandleStateChanged;
            _boundEnemy.OnPhaseTwoStarted -= HandlePhaseTwoStarted;
        }

        _boundEnemy = newEnemy;
        _phaseText.gameObject.SetActive(false);
        _fillBar.color = _fillNormalColor;

        if (_boundEnemy == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _boundEnemy.OnDamaged += HandleDamaged;
        _boundEnemy.OnStateChanged += HandleStateChanged;
        _boundEnemy.OnPhaseTwoStarted += HandlePhaseTwoStarted;

        if (_boundEnemy.IsInPhase2)
            HandlePhaseTwoStarted();

        RefreshBar();
    }

    private void HandleDamaged(int amount)
    {
        RefreshBar();
    }

    private void HandleStateChanged(EnemyState state)
    {
        RefreshBar();
    }

    private void HandlePhaseTwoStarted()
    {
        _phaseText.gameObject.SetActive(true);
        _fillBar.color = _fillPhase2Color;
    }

    private void RefreshBar()
    {
        if (_boundEnemy == null) return;

        _fillBar.fillAmount = (float)_boundEnemy.CurrentHealth / _boundEnemy.MaxHealth;
        _hpText.text = $"{_boundEnemy.CurrentHealth}/{_boundEnemy.MaxHealth}";
    }
}