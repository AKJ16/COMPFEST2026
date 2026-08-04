using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handbook bergaya "Almanac" (PvZ-like): grid icon weapon di kiri (klik buat
// pilih), detail + daftar combo di kanan. Weapon yang belum unlock di stage
// sekarang tampil sebagai "?" dan gak bisa diklik buat lihat detail.
// Semua ukuran/warna/font bisa diatur langsung di Inspector.
public class HandbookUI : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Drag SEMUA WeaponData di sini, urutan = urutan tampil di grid")]
    [SerializeField] private WeaponData[] allWeapons;
    [Tooltip("Drag Canvas (root) di sini")]
    [SerializeField] private Transform canvasParent;

    [Header("Tombol BOOK (pembuka handbook)")]
    [SerializeField] private string bookButtonLabel = "BOOK";
    [SerializeField] private Color bookButtonColor = new Color(0.35f, 0.25f, 0.1f);
    [SerializeField] private float bookButtonFontSize = 22f;
    [SerializeField] private Vector2 bookButtonSize = new Vector2(56f, 56f);
    [SerializeField] private Vector2 bookButtonAnchoredPosition = new Vector2(-24f, -24f);

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
    [SerializeField] private string organizationBlurb =
        "Published by L.I.G.M.A. (League of Interdimensional Grimoire & Magical Artifacts).\n\n" +
        "This tome was compiled to help adventurers identify weapons found within the ruins, " +
        "along with their combination abilities. Select a weapon on the left to view its details.";

    private GameObject _panelRoot;
    private TextMeshProUGUI _detailTitle;
    private TextMeshProUGUI _detailBody;
    private bool _isOpen;

    private void Start()
    {
        if (canvasParent == null)
        {
            Debug.LogError("HandbookUI: canvasParent belum di-assign.");
            return;
        }

        CreateAnchoredButton(canvasParent, "HandbookButton", bookButtonColor, bookButtonLabel, bookButtonFontSize,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            bookButtonAnchoredPosition, bookButtonSize, TogglePanel);

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

        CreateAnchoredButton(_panelRoot.transform, "CloseButton", closeButtonColor, "X", closeButtonFontSize,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            closeButtonAnchoredPosition, closeButtonSize, TogglePanel);

        var (leftGO, leftRect, _) = CreateBox("LeftGrid", _panelRoot.transform, Color.clear);
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

        foreach (var weapon in allWeapons)
        {
            if (weapon != null)
                BuildWeaponSlot(leftGO.transform, weapon);
        }

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

    private void BuildWeaponSlot(Transform parent, WeaponData weapon)
    {
        bool isUnlocked = StageManager.Instance == null ||
                           weapon.unlockStage <= StageManager.Instance.CurrentStageNumber;

        var color = isUnlocked ? GetPlaceholderColor(weapon) : lockedSlotColor;
        var label = isUnlocked ? GetInitial(weapon.weaponName) : "?";
        System.Action action = isUnlocked ? () => ShowWeaponDetail(weapon) : () => ShowLockedMessage();

        CreateStretchButton(parent, $"Slot_{weapon.weaponName}", color, label, slotFontSize, action);
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

    // Tombol yang ngisi penuh parent-nya (dikontrol GridLayoutGroup) — dipakai slot weapon.
    private void CreateStretchButton(Transform parent, string name, Color color, string label, float fontSize,
        System.Action action)
    {
        var (go, rect, img) = CreateBox(name, parent, color);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(() => action());

        var lbl = CreateLabel(go.transform, label, fontSize);
        lbl.alignment = TextAlignmentOptions.Center;
    }

    // Tombol posisi bebas (BOOK, X) — anchor & posisi manual.
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
        _detailTitle.text = "L.I.G.M.A. Almanac";
        _detailBody.text = organizationBlurb;
    }

    private void ShowLockedMessage()
    {
        _detailTitle.text = "???";
        _detailBody.text = "This weapon has not yet been discovered. Continue your adventure to unlock it.";
    }

    private void ShowWeaponDetail(WeaponData weapon)
    {
        _detailTitle.text = weapon.weaponName;

        var combos = GetCombosInvolving(weapon);
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
                    lines.Add($"{other.weaponName} + Weapon Attack manapun di sebelahnya");
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

    public void TogglePanel()
    {
        _isOpen = !_isOpen;
        _panelRoot.SetActive(_isOpen);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(_isOpen ? "HandbookOpen" : "HandbookClose");

        if (_isOpen)
            ShowDefaultBlurb();
    }
}