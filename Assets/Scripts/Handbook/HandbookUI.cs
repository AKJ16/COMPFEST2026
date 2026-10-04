using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;


// L.I.G.M.A. Almanac: an open book on parchment with dark-brown serif ink.
// Page 0 = intro spread. Pages 1..N = one weapon each:
//   left  = icon, name, divider, category
//   right = stat boxes, description, size/range diagrams, combos
// Public API is unchanged: OpenPanel / ClosePanel / TogglePanel / NextPage / PreviousPage.
//
// Combos work as a discovery log. A combo shows as "??? + ???" until the game calls
//   HandbookUI.ReportCombo(modifier, target, detail)
// at the moment the modifier really affects the target. Discoveries are saved in
// PlayerPrefs (one key per modifier+target pair) and read whenever a page renders.
public partial class HandbookUI : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Drag ALL WeaponData here. With 'Sort By Default Order' on, the book orders them itself.")]
    [SerializeField] private WeaponData[] allWeapons;
    [Tooltip("Sword, Staff, Book Add, Book Multi, Poison Dagger, Hour Glass.")]
    [SerializeField] private bool sortByDefaultOrder = true;
    [Tooltip("Optional overrides for text, stat boxes and diagrams.")]
    [SerializeField] private WeaponPageInfo[] pageTexts;
    [Tooltip("Drag the root Canvas here. If empty, the nearest Canvas is used.")]
    [SerializeField] private Transform canvasParent;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip openSfx;
    [SerializeField] private AudioClip closeSfx;
    [SerializeField] private AudioClip flipSfx;

    [Header("Book Look")]
    [SerializeField] private Vector2 panelSize = new Vector2(1500f, 900f);
    [Tooltip("Optional art for the whole book. If it already contains pages, set Page Color alpha to 0.")]
    [SerializeField] private Sprite bookSprite;
    [SerializeField] private Color coverColor = new Color(0.27f, 0.13f, 0.06f, 1f);
    [SerializeField] private Color pageColor = new Color(0.90f, 0.85f, 0.70f, 1f);
    [SerializeField] private Color spineColor = new Color(0.27f, 0.13f, 0.06f, 1f);
    [SerializeField] private Color inkColor = new Color(0.29f, 0.14f, 0.06f, 1f);
    [Tooltip("Divider lines, small headings and stat box borders.")]
    [SerializeField] private Color accentColor = new Color(0.66f, 0.42f, 0.15f, 1f);
    [SerializeField] private Color attackTagColor = new Color(0.60f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color modifierTagColor = new Color(0.20f, 0.25f, 0.55f, 1f);

    [Header("Stat Boxes & Diagrams")]
    [SerializeField] private Color chipColor = new Color(0.85f, 0.77f, 0.58f, 1f);
    [SerializeField] private float chipValueFontSize = 46f;
    [SerializeField] private float chipLabelFontSize = 18f;
    [Tooltip("Colour of the item's own squares in the diagrams.")]
    [SerializeField] private Color gridItemColor = new Color(0.40f, 0.22f, 0.10f, 1f);
    [Tooltip("Colour of the squares the item affects (its range).")]
    [SerializeField] private Color gridRangeColor = new Color(0.82f, 0.62f, 0.32f, 1f);
    [SerializeField] private float diagramCellSize = 40f;

    [Header("Typography")]
    [Tooltip("Bold serif for titles and weapon names.")]
    [FormerlySerializedAs("headingFont")]
    [SerializeField] private TMP_FontAsset titleFont;
    [Tooltip("Serif for body text and subtitles. Empty = use Title Font.")]
    [SerializeField] private TMP_FontAsset bodyFont;
    [Tooltip("Easiest option: drag HandbookTitle.ttf here. If empty, loads Assets/Resources/HandbookTitle.")]
    [SerializeField] private Font titleTtf;
    [Tooltip("Easiest option: drag HandbookBody.ttf here. If empty, loads Assets/Resources/HandbookBody.")]
    [SerializeField] private Font bodyTtf;
    [SerializeField] private float nameFontSize = 76f;
    [SerializeField] private float tagFontSize = 26f;
    [Tooltip("Letter spacing of the small caps line under the divider.")]
    [SerializeField] private float tagLetterSpacing = 10f;
    [SerializeField] private float bodyFontSize = 32f;
    [SerializeField] private float initialFontSize = 200f;

    [Header("Intro Page")]
    [Tooltip("Drag HandbookEmblem here. If empty, loads Assets/Resources/HandbookEmblem.")]
    [SerializeField] private Sprite introEmblem;
    [Tooltip("Size of the emblem inside its slot. Lower = smaller.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float emblemScale = 0.75f;
    [TextArea(1, 3)]
    [SerializeField] private string introTitle = "L.I.G.M.A.\nAlmanac";
    [SerializeField] private string introSubtitle = "Weapon Compendium";
    [TextArea(3, 6)]
    [SerializeField]
    private string introBlurb =
        "Published by L.I.G.M.A. (League of Interdimensional Grimoire & Magical Artifacts).\n\n" +
        "This tome was compiled to help adventurers identify weapons found within the ruins, " +
        "along with their combination abilities. Turn the page to begin.";

    [Header("Page Flip")]
    [Tooltip("Total seconds for one page turn. 0 = instant.")]
    [SerializeField] private float flipDuration = 0.3f;

    [Header("Buttons")]
    [Tooltip("Optional plank sprite for the < > buttons.")]
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Color buttonColor = new Color(0.45f, 0.25f, 0.12f, 1f);
    [SerializeField] private Color buttonTextColor = new Color(1f, 0.93f, 0.85f, 1f);
    [SerializeField] private Color closeButtonColor = new Color(0.5f, 0.15f, 0.15f, 1f);
    [SerializeField] private Vector2 navButtonSize = new Vector2(120f, 60f);
    [SerializeField] private Vector2 closeButtonSize = new Vector2(48f, 48f);

    [Header("Combo Discovery")]
    [Tooltip("Show a small 'New combo discovered!' banner when a combo is triggered for the first time.")]
    [SerializeField] private bool showDiscoveryToast = true;
    [SerializeField] private float toastSeconds = 2.5f;
    [Tooltip("Pause between two toasts when several combos are discovered at once.")]
    [SerializeField] private float toastGapSeconds = 0.3f;
    [Tooltip("Optional short names for combos. Anything left out uses the built-in name.")]
    [SerializeField] private ComboNameInfo[] comboNames;
    [SerializeField] private Vector2 toastSize = new Vector2(640f, 110f);
    [SerializeField] private float toastFontSize = 32f;

    [Header("Background Dim")]
    [Tooltip("Full-screen overlay behind the book while it is open. Alpha = how dark.")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.7f);
    [Tooltip("Clicking the dark area outside the book closes it.")]
    [SerializeField] private bool closeOnDimClick = true;

    [Tooltip("Blur the game behind the book. Takes a screenshot, shrinks it and stretches it back up, so it works on any render pipeline with no shader.")]
    [SerializeField] private bool blurBackground = true;
    [Tooltip("How many times the screenshot is halved. Higher = blurrier.")]
    [Range(1, 6)]
    [SerializeField] private int blurAmount = 4;
    [Tooltip("Pause the game (Time.timeScale = 0) while the book is open, and restore it on close.")]
    [SerializeField] private bool freezeGameWhileOpen = true;
    [Tooltip("Draw the book on its own full-screen canvas above everything, so no HUD canvas can cover the dim.")]
    [SerializeField] private bool useOverlayCanvas = true;
    [SerializeField] private int overlaySortingOrder = 5000;
    [Tooltip("Shrink the book if it is bigger than the screen, so it never clips. It is never enlarged.")]
    [SerializeField] private bool fitToScreen = true;
    [Tooltip("How much of the screen the book may fill when it has to shrink.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float screenFill = 0.94f;

    // =========================================================
    // STATE
    // =========================================================

    private GameObject _dimRoot;
    private GameObject _panelRoot;
    private RectTransform _leftPage;
    private RectTransform _rightPage;
    private Image _icon;
    private TextMeshProUGUI _iconInitial;
    private TextMeshProUGUI _nameLabel;
    private TextMeshProUGUI _tagLabel;
    private TextMeshProUGUI _descLabel;
    private TextMeshProUGUI _combosLabel;
    private RectTransform _statsRow;
    private RectTransform _diagramArea;
    private TextMeshProUGUI _pageNumberLabel;
    private Button _prevButton;
    private Button _nextButton;

    private readonly List<WeaponData> _weapons = new List<WeaponData>();
    private int _pageIndex;          // 0 = intro, 1..N = weapons
    private bool _isOpen;
    private bool _isFlipping;
    private Coroutine _flipRoutine;

    // Toast banner for "New combo discovered!"
    private GameObject _toastRoot;
    private TextMeshProUGUI _toastLabel;
    private Coroutine _toastRoutine;

    // Lets the static ReportCombo reach the live book (to refresh it and show the toast).
    private static HandbookUI s_instance;

    private int TotalPages => _weapons.Count + 1;
    private TMP_FontAsset SerifBody => bodyFont != null ? bodyFont : titleFont;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        s_instance = this;
    }

    private void Start()
    {
        if (canvasParent == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            canvasParent = canvas != null ? canvas.transform : transform;
        }

        PrepareFonts();
        CreateUiRoot();      // own overlay canvas (see HandbookUI.Overlay.cs)
        BuildBlurLayer();    // first child, so it sits under the dim and the book
        BuildBook();
        BuildToast();
        RebuildWeaponList();
        RenderPage();
        _panelRoot.SetActive(false);
        _dimRoot.SetActive(false);
    }

    private void OnDisable()
    {
        ResetToasts();
        // The book lives on its own canvas, so hide it and un-freeze if this object is switched off.
        if (_isOpen) CloseInternal();
        else Unfreeze();
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
        ReleaseBackdrop();
        Unfreeze();
        if (_overlayCanvasGo != null) Destroy(_overlayCanvasGo);
    }

    // =========================================================
    // PUBLIC CONTROLS (hook to UI Button OnClick events)
    // =========================================================

    // True while the book is open. Game scripts that read input directly
    // (Update / OnMouseDown) can check this and ignore input.
    public static bool IsOpen => s_instance != null && s_instance._isOpen;

    public void OpenPanel()
    {
        if (_isOpen) return;
        _isOpen = true;
        Freeze();

        if (blurBackground && _blurImage != null && isActiveAndEnabled)
            _openRoutine = StartCoroutine(OpenAfterCapture());   // needs the finished frame to screenshot
        else
            ShowBook();
    }

    public void ClosePanel()
    {
        bool wasOpen = _isOpen;
        CloseInternal();
        if (wasOpen) PlaySfx(closeSfx);
    }

    public void TogglePanel()
    {
        if (_isOpen) ClosePanel();
        else OpenPanel();
    }

    public void NextPage() => GoToPage(_pageIndex + 1);
    public void PreviousPage() => GoToPage(_pageIndex - 1);
}