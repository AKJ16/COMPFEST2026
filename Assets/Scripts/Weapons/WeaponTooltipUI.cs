using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tooltip sederhana yang muncul di atas slot inventory saat di-hover,
// nampilin nama + deskripsi weapon. Dibangun sepenuhnya lewat kode
// (sama seperti InventorySlotUI), jadi cukup taro GameObject kosong
// di scene dan attach komponen ini.
public class WeaponTooltipUI : MonoBehaviour
{
    public static WeaponTooltipUI Instance { get; private set; }

    [Header("Tooltip Size")]
    [SerializeField] private float width = 220f;
    [SerializeField] private float padding = 10f;

    private RectTransform _rect;
    private Image _background;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _descriptionText;

    private void Awake()
    {
        Instance = this;
        BuildLayout();
        Hide();
    }

    private void BuildLayout()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect == null)
            _rect = gameObject.AddComponent<RectTransform>();

        _rect.pivot = new Vector2(0.5f, 0f); // muncul di ATAS titik acuan
        _rect.sizeDelta = new Vector2(width, 70f);

        _background = gameObject.GetComponent<Image>();
        if (_background == null)
            _background = gameObject.AddComponent<Image>();
        _background.color = new Color(0f, 0f, 0f, 0.9f);
        _background.raycastTarget = false; // tooltip gak boleh nge-block klik

        var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
        vlg.spacing = 4f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject nameGO = new GameObject("NameText", typeof(RectTransform));
        nameGO.transform.SetParent(transform, false);
        _nameText = nameGO.AddComponent<TextMeshProUGUI>();
        _nameText.fontSize = 18;
        _nameText.fontStyle = FontStyles.Bold;
        _nameText.color = Color.white;
        _nameText.raycastTarget = false;

        GameObject descGO = new GameObject("DescriptionText", typeof(RectTransform));
        descGO.transform.SetParent(transform, false);
        _descriptionText = descGO.AddComponent<TextMeshProUGUI>();
        _descriptionText.fontSize = 14;
        _descriptionText.color = new Color(0.85f, 0.85f, 0.85f);
        _descriptionText.raycastTarget = false;
        _descriptionText.textWrappingMode = TextWrappingModes.Normal;
    }

    // anchorWorldPosition = posisi world slot yang di-hover (pakai slot.transform.position).
    public void Show(WeaponData data, Vector3 anchorWorldPosition)
    {
        gameObject.SetActive(true);

        _nameText.text = data.weaponName;
        _descriptionText.text = string.IsNullOrEmpty(data.description)
            ? "(belum ada deskripsi)"
            : data.description;

        // Posisikan tooltip sedikit di atas slot yang di-hover.
        _rect.position = anchorWorldPosition + new Vector3(0f, 55f, 0f);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}