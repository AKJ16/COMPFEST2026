using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class HandbookUI
{
    // Builds TextMesh Pro fonts at runtime from plain .ttf files, so no
    // Font Asset Creator step is needed. Assigned TMP font assets win.
    private void PrepareFonts()
    {
        if (titleFont == null) titleFont = MakeFontAsset(titleTtf, "HandbookTitle");
        if (bodyFont == null) bodyFont = MakeFontAsset(bodyTtf, "HandbookBody");
    }

    private TMP_FontAsset MakeFontAsset(Font ttf, string resourceName)
    {
        var font = ttf != null ? ttf : (Resources.Load<Font>(resourceName) ?? Resources.Load<Font>("Font/" + resourceName));
        return font != null ? TMP_FontAsset.CreateFontAsset(font) : null;
    }

    private void BuildBook()
    {
        // Dark overlay behind the book. Created first so the book draws on top of it.
        var (dim, dimRect, dimImg) = CreateBox("HandbookDim", _uiRoot, dimColor);
        _dimRoot = dim;
        dimImg.raycastTarget = true;   // also stops clicks from reaching the game behind the book
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
        if (closeOnDimClick)
        {
            var dimButton = dim.AddComponent<Button>();
            dimButton.targetGraphic = dimImg;
            dimButton.transition = Selectable.Transition.None;
            dimButton.navigation = new Navigation { mode = Navigation.Mode.None };
            dimButton.onClick.AddListener(ClosePanel);
        }

        _skinned = useBookArt && TryLoadBookArt();

        // Cover / frame
        var (root, rootRect, rootImg) = CreateBox("HandbookPanel", _uiRoot, _skinned ? Color.clear : coverColor);
        _panelRoot = root;
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        if (_skinned)
        {
            rootRect.sizeDelta = new Vector2(bookArtWidth, bookArtWidth * BookArtAspect);
            rootRect.anchoredPosition = new Vector2(0f, navButtonSize.y * 0.4f);   // room for the buttons under the book
            rootImg.color = Color.clear;   // invisible, but still blocks clicks; the art is a child below
            CreateArtLayer("BookBase", root.transform, _artBase);
        }
        else
        {
            rootRect.sizeDelta = panelSize;
            if (bookSprite != null)
            {
                rootImg.sprite = bookSprite;
                rootImg.color = Color.white;
            }
        }
        _panelSizeActual = rootRect.sizeDelta;

        if (_skinned)
        {
            // Top pages: they scale on X around the spine, so they fold like a page turning.
            _leftPage = CreateArtPage("LeftPage", root.transform, LeftPageRect, new Vector2(1f, 0.5f), _artLeft);
            _rightPage = CreateArtPage("RightPage", root.transform, RightPageRect, new Vector2(0f, 0.5f), _artRight);
            _leftContent = CreateContentRect(_leftPage);
            _rightContent = CreateContentRect(_rightPage);
        }
        else
        {
            // Pages. Pivot sits on the spine side so scaling X looks like a page turning.
            _leftPage = CreatePage("LeftPage", root.transform,
                new Vector2(0.025f, 0.10f), new Vector2(0.495f, 0.95f), new Vector2(1f, 0.5f));
            _rightPage = CreatePage("RightPage", root.transform,
                new Vector2(0.505f, 0.10f), new Vector2(0.975f, 0.95f), new Vector2(0f, 0.5f));
            _leftContent = _leftPage;
            _rightContent = _rightPage;

            // Spine
            var (_, spineRect, _) = CreateBox("Spine", root.transform, spineColor);
            spineRect.anchorMin = new Vector2(0.495f, 0.09f);
            spineRect.anchorMax = new Vector2(0.505f, 0.96f);
            spineRect.offsetMin = spineRect.offsetMax = Vector2.zero;
        }

        BuildLeftPageContent();
        BuildRightPageContent();
        BuildNavigation(root.transform);
    }

    private RectTransform CreatePage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        var (_, rect, _) = CreateBox(name, parent, pageColor);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void BuildLeftPageContent()
    {
        // Emblem (intro) / weapon icon
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(_leftContent, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.15f, 0.46f);
        iconRect.anchorMax = new Vector2(0.85f, 0.97f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
        _icon = iconGO.AddComponent<Image>();
        _icon.preserveAspect = true;
        _icon.raycastTarget = false;

        // Big letter / "?" when there is no icon
        _iconInitial = CreateLabel(_leftContent, "", initialFontSize, inkColor,
            new Vector2(0.15f, 0.46f), new Vector2(0.85f, 0.97f));
        _iconInitial.alignment = TextAlignmentOptions.Center;
        _iconInitial.fontStyle = titleFont != null ? FontStyles.Normal : FontStyles.Bold;
        ApplyFont(_iconInitial, titleFont);

        // Title / weapon name (can wrap to 2 lines on the intro page)
        _nameLabel = CreateLabel(_leftContent, "", nameFontSize, inkColor,
            new Vector2(0.04f, 0.23f), new Vector2(0.96f, 0.45f));
        _nameLabel.alignment = TextAlignmentOptions.Center;
        _nameLabel.fontStyle = titleFont != null ? FontStyles.Normal : FontStyles.Bold;
        _nameLabel.enableAutoSizing = true;
        _nameLabel.fontSizeMin = 28f;
        _nameLabel.fontSizeMax = nameFontSize;
        ApplyFont(_nameLabel, titleFont);

        // Ornamental divider: line - diamond - line
        CreateDivider(_leftContent, new Vector2(0.12f, 0.175f), new Vector2(0.88f, 0.225f));

        // Subtitle / category, spaced capitals
        _tagLabel = CreateLabel(_leftContent, "", tagFontSize, inkColor,
            new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.16f));
        _tagLabel.alignment = TextAlignmentOptions.Center;
        _tagLabel.characterSpacing = tagLetterSpacing;
        ApplyFont(_tagLabel, SerifBody);
    }

    private void BuildRightPageContent()
    {
        // Stat boxes row (top)
        var (_, statsRect, statsImg) = CreateBox("Stats", _rightContent, Color.clear);
        statsImg.raycastTarget = false;
        statsRect.anchorMin = new Vector2(0.05f, 0.83f);
        statsRect.anchorMax = new Vector2(0.95f, 0.95f);
        statsRect.offsetMin = statsRect.offsetMax = Vector2.zero;
        _statsRow = statsRect;

        // Description (EFFECT section)
        _descLabel = CreateLabel(_rightContent, "", bodyFontSize, inkColor,
            new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.80f));
        _descLabel.alignment = TextAlignmentOptions.TopLeft;
        _descLabel.textWrappingMode = TextWrappingModes.Normal;
        _descLabel.richText = true;
        _descLabel.lineSpacing = 6f;
        _descLabel.paragraphSpacing = 10f;
        _descLabel.enableAutoSizing = true;
        _descLabel.fontSizeMin = 18f;
        _descLabel.fontSizeMax = bodyFontSize;
        _descLabel.overflowMode = TextOverflowModes.Ellipsis;
        ApplyFont(_descLabel, SerifBody);

        // Placement area (bottom left column: SIZE & RANGE GRID)
        var (_, diagRect, diagImg) = CreateBox("Diagrams", _rightContent, Color.clear);
        diagImg.raycastTarget = false;
        diagRect.anchorMin = new Vector2(0.05f, 0.04f);
        diagRect.anchorMax = new Vector2(0.48f, 0.47f);
        diagRect.offsetMin = diagRect.offsetMax = Vector2.zero;
        _diagramArea = diagRect;

        // Combos area (bottom right column: COMBOS)
        _combosLabel = CreateLabel(_rightContent, "", bodyFontSize * 0.9f, inkColor,
            new Vector2(0.52f, 0.04f), new Vector2(0.95f, 0.47f));
        _combosLabel.alignment = TextAlignmentOptions.TopLeft;
        _combosLabel.textWrappingMode = TextWrappingModes.Normal;
        _combosLabel.richText = true;
        _combosLabel.lineSpacing = 4f;
        _combosLabel.enableAutoSizing = true;
        _combosLabel.fontSizeMin = 16f;
        _combosLabel.fontSizeMax = bodyFontSize * 0.9f;
        _combosLabel.overflowMode = TextOverflowModes.Ellipsis;
        ApplyFont(_combosLabel, SerifBody);
    }

    private void BuildNavigation(Transform root)
    {
        // With the book art, the buttons sit around the book body instead of on a bottom bar.
        float w = bookArtWidth;
        float sideGap = w * BodyLeft;            // empty strip left of the book body
        float rightGap = w * (1f - BodyRight);   // empty strip right of the book body (the ribbon is here)

        // Close (top right)
        CreateAnchoredButton(root, "CloseButton", closeButtonColor, "X", 30f,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            _skinned ? new Vector2(-rightGap + 14f, -14f) : new Vector2(-12f, -12f),
            closeButtonSize, ClosePanel, false);

        // Previous (bottom left) - Center-pivoted and flipped horizontally (scale.x = -1) to face left
        Vector2 prevPivot = new Vector2(0.5f, 0.5f);
        Vector2 prevPos = _skinned
            ? new Vector2(sideGap + 30f + navButtonSize.x * 0.5f, 8f - navButtonSize.y * 0.5f)
            : new Vector2(80f + navButtonSize.x * 0.5f, 14f + navButtonSize.y * 0.5f);

        _prevButton = CreateAnchoredButton(root, "PrevButton", buttonColor, "", 0f,
            new Vector2(0, 0), new Vector2(0, 0), prevPivot,
            prevPos, navButtonSize, PreviousPage);

        // Flip the sprite horizontally so it points left!
        _prevButton.transform.localScale = new Vector3(-1f, 1f, 1f);


        // Next (bottom right) - Center-pivoted, text removed
        Vector2 nextPivot = new Vector2(0.5f, 0.5f);
        Vector2 nextPos = _skinned
            ? new Vector2(-rightGap - 30f - navButtonSize.x * 0.5f, 8f - navButtonSize.y * 0.5f)
            : new Vector2(-80f - navButtonSize.x * 0.5f, 14f + navButtonSize.y * 0.5f);

        _nextButton = CreateAnchoredButton(root, "NextButton", buttonColor, "", 0f,
            new Vector2(1, 0), new Vector2(1, 0), nextPivot,
            nextPos, navButtonSize, NextPage);

        // Page number (bottom center, just under the book when it has art)
        _pageNumberLabel = CreateLabel(root, "", 26f, buttonTextColor,
            _skinned ? new Vector2(0.4f, -0.075f) : new Vector2(0.4f, 0f),
            _skinned ? new Vector2(0.6f, 0.005f) : new Vector2(0.6f, 0.09f));
        _pageNumberLabel.alignment = TextAlignmentOptions.Center;
        ApplyFont(_pageNumberLabel, SerifBody);
    }

    // Thin line, small diamond, thin line.
    private void CreateDivider(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var (_, rect, img) = CreateBox("Divider", parent, Color.clear);
        img.raycastTarget = false;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        MakeLine(rect, 0f, 0.46f);
        MakeLine(rect, 0.54f, 1f);

        var (_, dRect, dImg) = CreateBox("Diamond", rect, accentColor);
        dImg.raycastTarget = false;
        dRect.anchorMin = dRect.anchorMax = new Vector2(0.5f, 0.5f);
        dRect.sizeDelta = new Vector2(14f, 14f);
        dRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private void MakeLine(RectTransform parent, float x0, float x1)
    {
        var (_, r, img) = CreateBox("Line", parent, accentColor);
        img.raycastTarget = false;
        r.anchorMin = new Vector2(x0, 0.5f);
        r.anchorMax = new Vector2(x1, 0.5f);
        r.offsetMin = new Vector2(0f, -1.5f);
        r.offsetMax = new Vector2(0f, 1.5f);
    }
}