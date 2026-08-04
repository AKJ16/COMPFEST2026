using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handbook bergaya "Almanac" (PvZ-like): grid icon weapon di kiri (klik buat
// pilih), detail + daftar combo di kanan. Weapon yang belum unlock di stage
// sekarang tampil sebagai "?" dan gak bisa diklik buat lihat detail.
public class HandbookUI : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Drag SEMUA WeaponData di sini, urutan = urutan tampil di grid")]
    [SerializeField] private WeaponData[] allWeapons;
    [Tooltip("Drag Canvas (root) di sini. Jika kosong, otomatis mencari Canvas terdekat.")]
    [SerializeField] private Transform canvasParent;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioClip openSfx;
    [SerializeField] private AudioClip closeSfx;

    [Header("Tombol Close (X)")]
    [SerializeField] private Color closeButtonColor = new Color(0.5f, 0.15f, 0.15f);
    [SerializeField] private float closeButtonFontSize = 34f;
    [SerializeField] private Vector2 closeButtonSize = new Vector2(44f, 44f);
    [SerializeField] private Vector2 closeButtonAnchoredPosition = new Vector2(-10f, -10f);

    [Header("Panel Utama")]
    [SerializeField] private Vector2 panelSize = new Vector2(1700f, 900f);
    [SerializeField] private Color panelBackgroundColor = new Color(0.09f, 0.07f, 0.05f, 0.97f);

    [Header("Grid Weapon (kiri)")]
    [SerializeField] private Vector2 gridCellSize = new Vector2(160f, 160f);
    [SerializeField] private Vector2 gridSpacing = new Vector2(20f, 20f);
    [SerializeField] private int gridColumnCount = 2;
    [SerializeField] private float slotFontSize = 48f;
    [SerializeField] private Color attackWeaponColor = new Color(0.6f, 0.2f, 0.2f);
    [SerializeField] private Color modifierWeaponColor = new Color(0.3f, 0.3f, 0.7f);
    [SerializeField] private Color lockedSlotColor = new Color(0.2f, 0.2f, 0.2f);

    [Header("Panel Detail (kanan)")]
    [SerializeField] private Color detailBackgroundColor = new Color(0.15f, 0.12f, 0.08f, 0.6f);
    [SerializeField] private float detailTitleFontSize = 42f;
    [SerializeField] private float detailBodyFontSize = 28f;
    [SerializeField] private float detailSpacing = 12f;

    [Header("Lore Organisasi (blurb default sebelum pilih weapon)")]
    [TextArea(3, 6)]
    [SerializeField]
    private string organizationBlurb =
        "Published by L.I.G.M.A. (League of Interdimensional Grimoire & Magical Artifacts).\n\n" +
        "This tome was compiled to help adventurers identify weapons found within the ruins, " +
        "along with their combination abilities. Select a weapon on the left to view its details.";

    private GameObject _panelRoot;
    private Transform _leftGridContainer;
    private TextMeshProUGUI _detailTitle;
    private TextMeshProUGUI _detailBody;
    private bool _isOpen;

    private void Start()
    {
        if (canvasParent == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvasParent = canvas.transform;
            else canvasParent = transform;
        }

        BuildPanel();
        ShowDefaultBlurb();
        _panelRoot.SetActive(false);
    }

    private void BuildPanel()
    {
        var (root, rootRect, _) = CreateBox("HandbookPanel", canvasParent, panelBackgroundColor);
        _panelRoot = root;
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = panelSize;

        // Close Button (X)
        CreateAnchoredButton(_panelRoot.transform, "CloseButton", closeButtonColor, "X", closeButtonFontSize,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            closeButtonAnchoredPosition, closeButtonSize, ClosePanel);

        // Left Grid Container
        var (leftGO, leftRect, _) = CreateBox("LeftGrid", _panelRoot.transform, Color.clear);
        _leftGridContainer = leftGO.transform;
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.42f, 1f);
        leftRect.offsetMin = new Vector2(16f, 16f);
        leftRect.offsetMax = new Vector2(-8f, -16f);

        var grid = leftGO.AddComponent<GridLayoutGroup>();
        grid.cellSize = gridCellSize;
        grid.spacing = gridSpacing;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = gridColumnCount;

        RefreshGridSlots();

        // Right Detail Container
        var (rightGO, rightRect, _) = CreateBox("RightDetail", _panelRoot.transform, detailBackgroundColor);
        rightRect.anchorMin = new Vector2(0.42f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.offsetMin = new Vector2(8f, 16f);
        rightRect.offsetMax = new Vector2(-16f, -48f);

        var vLayout = rightGO.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(16, 16, 16, 16);
        vLayout.spacing = detailSpacing;
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.childControlHeight = true;
        vLayout.childControlWidth = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        _detailTitle = CreateLabel(rightGO.transform, "", detailTitleFontSize);
        _detailTitle.fontStyle = FontStyles.Bold;

        _detailBody = CreateLabel(rightGO.transform, "", detailBodyFontSize);
        _detailBody.textWrappingMode = TextWrappingModes.Normal;
    }

    private void RefreshGridSlots()
    {
        if (_leftGridContainer == null) return;

        // Destroy existing slots before rebuilding
        foreach (Transform child in _leftGridContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var weapon in allWeapons)
        {
            if (weapon != null)
                BuildWeaponSlot(_leftGridContainer, weapon);
        }
    }

    private void BuildWeaponSlot(Transform parent, WeaponData weapon)
    {
        bool isUnlocked = StageManager.Instance == null ||
                           weapon.unlockStage <= StageManager.Instance.CurrentStageNumber;

        var bgColor = isUnlocked ? GetPlaceholderColor(weapon) : lockedSlotColor;
        System.Action action = isUnlocked ? () => ShowWeaponDetail(weapon) : () => ShowLockedMessage();

        var (slotGO, rect, bgImg) = CreateBox($"Slot_{weapon.weaponName}", parent, bgColor);

        var button = slotGO.AddComponent<Button>();
        button.targetGraphic = bgImg;
        button.onClick.AddListener(() => action());

        // Create Icon Image
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(slotGO.transform, false);

        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8, 8);
        iconRect.offsetMax = new Vector2(-8, -8);

        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;

        var label = CreateLabel(slotGO.transform, "", slotFontSize);
        label.alignment = TextAlignmentOptions.Center;

        if (isUnlocked)
        {
            if (weapon.icon != null)
            {
                iconImg.sprite = weapon.icon;
                iconImg.color = Color.white;
                label.text = "";
            }
            else
            {
                iconImg.enabled = false;
                label.text = GetInitial(weapon.weaponName);
            }
        }
        else
        {
            iconImg.enabled = false;
            label.text = "?";
            label.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        }
    }

    // =========================================================
    // PUBLIC CONTROLS (Hook to UI Button OnClick Events)
    // =========================================================

    public void OpenPanel()
    {
        _isOpen = true;
        if (_panelRoot != null) _panelRoot.SetActive(true);

        RefreshGridSlots();
        ShowDefaultBlurb();

        if (AudioManager.Instance != null && openSfx != null)
            AudioManager.Instance.PlaySFX(openSfx);
    }

    public void ClosePanel()
    {
        _isOpen = false;
        if (_panelRoot != null) _panelRoot.SetActive(false);

        if (AudioManager.Instance != null && closeSfx != null)
            AudioManager.Instance.PlaySFX(closeSfx);
    }

    public void TogglePanel()
    {
        if (_isOpen) ClosePanel();
        else OpenPanel();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private (GameObject go, RectTransform rect, Image img) CreateBox(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return (go, rect, img);
    }

    private void CreateAnchoredButton(Transform parent, string name, Color color, string label, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        var (go, rect, img) = CreateBox(name, parent, color);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        var lbl = CreateLabel(go.transform, label, fontSize);
        lbl.alignment = TextAlignmentOptions.Center;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;

        return tmp;
    }

    private string GetInitial(string weaponName) =>
        string.IsNullOrEmpty(weaponName) ? "?" : weaponName.Substring(0, 1).ToUpper();

    private Color GetPlaceholderColor(WeaponData weapon) =>
        weapon.category == WeaponCategory.Attack ? attackWeaponColor : modifierWeaponColor;

    private void ShowDefaultBlurb()
    {
        if (_detailTitle != null) _detailTitle.text = "L.I.G.M.A. Almanac";
        if (_detailBody != null) _detailBody.text = organizationBlurb;
    }

    private void ShowLockedMessage()
    {
        if (_detailTitle != null) _detailTitle.text = "???";
        if (_detailBody != null) _detailBody.text = "This weapon has not yet been discovered. Continue your adventure to unlock it.";
    }

    private void ShowWeaponDetail(WeaponData weapon)
    {
        if (_detailTitle != null) _detailTitle.text = weapon.weaponName;

        var combos = GetCombosInvolving(weapon);
        if (_detailBody != null)
            _detailBody.text = combos.Count > 0 ? string.Join("\n", combos) : "No combos available for this weapon yet.";
    }

    private List<string> GetCombosInvolving(WeaponData selected)
    {
        var lines = new List<string>();

        foreach (var other in allWeapons)
        {
            if (other == null || other.category != WeaponCategory.Modifier) continue;

            if (other.modifierType == ModifierType.Repeat)
            {
                if (selected == other)
                    lines.Add($"{other.weaponName} + Any adjacent Attack Weapon");
                else if (selected.category == WeaponCategory.Attack)
                    lines.Add($"{selected.weaponName} + {other.weaponName}");

                continue;
            }

            if (other.targets == null) continue;

            foreach (var target in other.targets)
            {
                if (target == null) continue;
                if (other != selected && target != selected) continue;

                lines.Add($"{other.weaponName} + {target.weaponName}");
            }
        }

        return lines;
    }
}