using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Satu slot di inventory bar.
// Menampilkan icon + jumlah weapon.
// Slot tetap muncul walaupun jumlah = 0 (icon menjadi abu-abu, tidak bisa diklik).
// Bisa diklik untuk memilih weapon, dan menampilkan tooltip saat di-hover.
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Size")]
    [SerializeField] private float slotSize = 80f;

    private Image _backgroundImage;
    private Image _iconImage;
    private TextMeshProUGUI _countText;

    private readonly Color _normalBackground = new Color(0f, 0f, 0f, 0.35f);
    private readonly Color _selectedBackground = new Color(1f, 0.8f, 0.2f, 0.75f);

    public WeaponData Data { get; private set; }
    public int CurrentCount { get; private set; }

    // Dipanggil InventoryUIController tiap slot ini diklik.
    public event System.Action<InventorySlotUI> OnSlotClicked;

    private void Awake()
    {
        var existingIcon = transform.Find("Icon");
        var existingCount = transform.Find("CountText");

        _backgroundImage = GetComponent<Image>();

        if (_backgroundImage == null)
            _backgroundImage = gameObject.AddComponent<Image>();

        _backgroundImage.color = _normalBackground;

        if (existingIcon != null && existingCount != null)
        {
            _iconImage = existingIcon.GetComponent<Image>();
            _countText = existingCount.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            BuildLayout();
        }
    }

    private void BuildLayout()
    {
        RectTransform rect = GetComponent<RectTransform>();

        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();

        rect.sizeDelta = new Vector2(slotSize, slotSize);

        _backgroundImage.color = _normalBackground;

        GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(transform, false);

        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6, 6);
        iconRect.offsetMax = new Vector2(-6, -6);

        _iconImage = iconGO.AddComponent<Image>();
        _iconImage.preserveAspect = true;

        GameObject countGO = new GameObject("CountText", typeof(RectTransform));
        countGO.transform.SetParent(transform, false);

        RectTransform countRect = countGO.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1, 0);
        countRect.anchorMax = new Vector2(1, 0);
        countRect.pivot = new Vector2(1, 0);
        countRect.sizeDelta = new Vector2(40, 20);
        countRect.anchoredPosition = new Vector2(-2, 2);

        _countText = countGO.AddComponent<TextMeshProUGUI>();
        _countText.fontSize = 16;
        _countText.alignment = TextAlignmentOptions.BottomRight;
        _countText.color = Color.white;
        _countText.text = "x0";
    }

    public void Setup(WeaponData data, int count)
    {
        Data = data;
        _iconImage.sprite = data.icon;
        UpdateCount(count);
        SetSelected(false);
    }

    public void UpdateCount(int count)
    {
        CurrentCount = count;
        _countText.text = "x" + count;

        if (count > 0)
        {
            _iconImage.color = Color.white;
            _countText.color = Color.white;
        }
        else
        {
            _iconImage.color = new Color(1f, 1f, 1f, 0.25f);
            _countText.color = new Color(1f, 1f, 1f, 0.45f);
        }
    }

    public void SetSelected(bool selected)
    {
        if (_backgroundImage == null)
            return;

        _backgroundImage.color = selected ? _selectedBackground : _normalBackground;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CurrentCount <= 0) return;
        OnSlotClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Data == null) return;
        WeaponTooltipUI.Instance.Show(Data, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        WeaponTooltipUI.Instance.Hide();
    }
}