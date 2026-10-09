using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponTooltipUI : MonoBehaviour
{
    public static WeaponTooltipUI Instance { get; private set; }

    [Header("Positioning")]
    [Tooltip("Offset in UI canvas pixels directly above the hovered slot.")]
    [SerializeField] private float yOffsetPixels = 70f;
    [SerializeField] private float width = 230f;
    [SerializeField] private float padding = 10f;

    [Header("UI References (Optional: Assign in Inspector or auto-built)")]
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image background;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tooltipRect == null) tooltipRect = GetComponent<RectTransform>();

        if (nameText == null || descriptionText == null)
        {
            BuildLayout();
        }

        Hide();
    }

    private void BuildLayout()
    {
        if (tooltipRect == null)
            tooltipRect = gameObject.AddComponent<RectTransform>();

        tooltipRect.pivot = new Vector2(0.5f, 0f);
        tooltipRect.sizeDelta = new Vector2(width, 70f);

        if (background == null)
        {
            background = GetComponent<Image>();
            if (background == null) background = gameObject.AddComponent<Image>();
        }
        background.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        background.raycastTarget = false;

        var vlg = GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
        vlg.spacing = 4f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Name Text
        if (nameText == null)
        {
            var existingName = transform.Find("NameText");
            if (existingName != null) nameText = existingName.GetComponent<TextMeshProUGUI>();
        }
        if (nameText == null)
        {
            GameObject nameGO = new GameObject("NameText", typeof(RectTransform));
            nameGO.transform.SetParent(transform, false);
            nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.white;
            nameText.raycastTarget = false;
        }

        // Description Text
        if (descriptionText == null)
        {
            var existingDesc = transform.Find("DescriptionText");
            if (existingDesc != null) descriptionText = existingDesc.GetComponent<TextMeshProUGUI>();
        }
        if (descriptionText == null)
        {
            GameObject descGO = new GameObject("DescriptionText", typeof(RectTransform));
            descGO.transform.SetParent(transform, false);
            descriptionText = descGO.AddComponent<TextMeshProUGUI>();
            descriptionText.fontSize = 14;
            descriptionText.color = new Color(0.85f, 0.85f, 0.85f);
            descriptionText.raycastTarget = false;
            descriptionText.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    public void Show(WeaponData data, Vector3 slotWorldPosition)
    {
        if (data == null) return;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (nameText != null)
        {
            nameText.text = data.weaponName;
        }

        if (descriptionText != null)
        {
            string baseDesc = string.IsNullOrEmpty(data.description)
                ? "(No description yet)"
                : data.description;

            // DYNAMIC USAGE CAP TEXT
            if (data.HasUsageCap && InventorySystem.Instance != null)
            {
                if (InventorySystem.Instance.IsLimitReached(data))
                {
                    baseDesc += $"\n\n<color=#FF4444><b>[LIMIT REACHED]</b>\n(0/{data.MaxUsage} Uses Left)</color>";
                }
                else
                {
                    int left = InventorySystem.Instance.GetRemainingUses(data);
                    baseDesc += $"\n\n<color=#FFD700><b>Uses Remaining: {left}/{data.MaxUsage}</b></color>";
                }
            }

            descriptionText.text = baseDesc;
        }

        if (tooltipRect != null)
        {
            tooltipRect.position = slotWorldPosition;
            tooltipRect.anchoredPosition += new Vector2(0f, yOffsetPixels);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}