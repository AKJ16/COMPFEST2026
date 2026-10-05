using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class HandbookUI
{
    // =========================================================
    // PAGE NAVIGATION
    // =========================================================

    private void GoToPage(int index)
    {
        if (_isFlipping) return;

        index = Mathf.Clamp(index, 0, TotalPages - 1);
        if (index == _pageIndex) return;

        bool forward = index > _pageIndex;
        PlaySfx(flipSfx);

        if (flipDuration <= 0f || !isActiveAndEnabled)
        {
            _pageIndex = index;
            RenderPage();
            return;
        }

        _flipRoutine = StartCoroutine(FlipTo(index, forward));
    }

    private IEnumerator FlipTo(int newIndex, bool forward)
    {
        _isFlipping = true;

        // Turning forward lifts the right page; turning back lifts the left page.
        var page = forward ? _rightPage : _leftPage;
        float half = flipDuration * 0.5f;

        yield return ScaleX(page, 1f, 0f, half);
        _pageIndex = newIndex;
        RenderPage();
        yield return ScaleX(page, 0f, 1f, half);

        _isFlipping = false;
        _flipRoutine = null;
    }

    private IEnumerator ScaleX(RectTransform rect, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            rect.localScale = new Vector3(to, 1f, 1f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // works even if the game is paused
            float k = Mathf.Clamp01(t / duration);
            rect.localScale = new Vector3(Mathf.Lerp(from, to, k), 1f, 1f);
            yield return null;
        }
        rect.localScale = new Vector3(to, 1f, 1f);
    }

    private void ResetFlipState()
    {
        if (_flipRoutine != null)
        {
            StopCoroutine(_flipRoutine);
            _flipRoutine = null;
        }
        _isFlipping = false;
        if (_leftPage != null) _leftPage.localScale = Vector3.one;
        if (_rightPage != null) _rightPage.localScale = Vector3.one;
    }


    // =========================================================
    // RENDERING
    // =========================================================

    private void RebuildWeaponList()
    {
        _weapons.Clear();
        if (allWeapons == null) return;

        var list = allWeapons.Where(w => w != null);

        if (sortByDefaultOrder)
        {
            // OrderBy is stable: unknown weapons keep their original order at the end.
            list = list.OrderBy(w =>
            {
                int i = FindDefaultIndex(w.weaponName);
                return i < 0 ? int.MaxValue : i;
            });
        }

        _weapons.AddRange(list);
    }

    private void RenderPage()
    {
        if (_nameLabel == null) return;

        _pageIndex = Mathf.Clamp(_pageIndex, 0, TotalPages - 1);

        if (_pageIndex == 0) RenderIntroPage();
        else RenderWeaponPage(_weapons[_pageIndex - 1]);

        if (_pageNumberLabel != null)
            _pageNumberLabel.text = $"{_pageIndex + 1} / {TotalPages}";
        if (_prevButton != null) _prevButton.interactable = _pageIndex > 0;
        if (_nextButton != null) _nextButton.interactable = _pageIndex < TotalPages - 1;
    }

    // Weapon pages show stats + diagrams + combos; intro/locked pages show text only.
    private void SetRightLayout(bool weaponLayout)
    {
        _statsRow.gameObject.SetActive(weaponLayout);
        _diagramArea.gameObject.SetActive(weaponLayout);
        _combosLabel.gameObject.SetActive(weaponLayout);

        var r = _descLabel.rectTransform;
        if (weaponLayout)
        {
            r.anchorMin = new Vector2(0.08f, 0.52f);
            r.anchorMax = new Vector2(0.92f, 0.79f);
        }
        else
        {
            r.anchorMin = new Vector2(0.10f, 0.08f);
            r.anchorMax = new Vector2(0.90f, 0.90f);
        }
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private Sprite _emblemCache;
    private bool _emblemWarned;

    // Works even if HandbookEmblem.png is imported as a normal Texture (Default) instead of
    // Sprite (2D and UI): Resources.Load<Sprite> returns null in that case, so fall back to
    // loading the texture and wrapping it in a sprite.
    private Sprite LoadEmblem()
    {
        if (introEmblem != null) return introEmblem;
        if (_emblemCache != null) return _emblemCache;

        // If the PNG is imported as Sprite Mode = Multiple, it holds several sprites (pieces).
        // Pick the biggest one, which is the whole emblem.
        Sprite sprite = null;
        var all = Resources.LoadAll<Sprite>("HandbookEmblem");
        float best = 0f;
        for (int i = 0; i < all.Length; i++)
        {
            float area = all[i].rect.width * all[i].rect.height;
            if (area > best) { best = area; sprite = all[i]; }
        }

        if (sprite == null)
        {
            var tex = Resources.Load<Texture2D>("HandbookEmblem");
            if (tex != null)
                sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
        }

        if (sprite == null && !_emblemWarned)
        {
            _emblemWarned = true;
            Debug.LogWarning("Handbook: emblem not found. Put HandbookEmblem.png in Assets/Resources/ " +
                             "or drag it into the 'Intro Emblem' field on HandbookUI.");
        }
        _emblemCache = sprite;
        return sprite;
    }

    private void RenderIntroPage()
    {
        var emblem = LoadEmblem();
        if (emblem != null)
        {
            _icon.enabled = true;
            _icon.sprite = emblem;
            _icon.color = Color.white;
        }
        else
        {
            _icon.enabled = false;
        }
        _icon.rectTransform.localScale = Vector3.one * emblemScale;
        _iconInitial.text = "";

        _nameLabel.text = introTitle;
        _tagLabel.text = introSubtitle.ToUpper();
        _tagLabel.color = inkColor;

        SetRightLayout(false);
        _descLabel.text = introBlurb;
    }

    private void RenderWeaponPage(WeaponData weapon)
    {
        _icon.rectTransform.localScale = Vector3.one;
        if (!IsUnlocked(weapon))
        {
            _icon.enabled = false;
            _iconInitial.text = "?";
            _nameLabel.text = "???";
            _tagLabel.text = "UNDISCOVERED";
            _tagLabel.color = inkColor;

            SetRightLayout(false);
            _descLabel.text = "This weapon has not yet been discovered.\nContinue your adventure to unlock it.";
            return;
        }

        // Left page
        if (weapon.icon != null)
        {
            _icon.enabled = true;
            _icon.sprite = weapon.icon;
            _icon.color = Color.white;
            _iconInitial.text = "";
        }
        else
        {
            _icon.enabled = false;
            _iconInitial.text = GetInitial(weapon.weaponName);
        }

        _nameLabel.text = weapon.weaponName;
        bool isAttack = weapon.category == WeaponCategory.Attack;
        _tagLabel.text = isAttack ? "ATTACK WEAPON" : "MODIFIER";
        _tagLabel.color = isAttack ? attackTagColor : modifierTagColor;

        // Right page
        var data = GetPageData(weapon);
        SetRightLayout(true);

        _descLabel.text = SectionHeading("Effect") + "\n" +
            (string.IsNullOrWhiteSpace(data.effect) ? "No description yet." : data.effect);

        BuildStatChips(data.stats);
        BuildDiagrams(data);
        _combosLabel.text = BuildCombosText(weapon);
    }

    // Built-in content first, then any Inspector overrides on top.
    private PageData GetPageData(WeaponData weapon)
    {
        var d = new PageData();

        int idx = FindDefaultIndex(weapon.weaponName);
        if (idx >= 0)
        {
            var src = Defaults[idx].data;
            d.effect = src.effect;
            d.stats = new List<StatChip>(src.stats);
            d.footprint = src.footprint;
            d.range = src.range;
            d.comboNote = src.comboNote;
        }

        if (pageTexts != null)
        {
            foreach (var info in pageTexts)
            {
                if (info == null || info.weapon != weapon) continue;

                if (!string.IsNullOrWhiteSpace(info.effectText)) d.effect = info.effectText;
                if (info.stats != null && info.stats.Length > 0) d.stats = new List<StatChip>(info.stats);
                if (info.footprint.x > 0 && info.footprint.y > 0) d.footprint = info.footprint;
                if (info.range.x > 0 && info.range.y > 0) d.range = info.range;
                break;
            }
        }

        return d;
    }

    // ---------- Stat boxes ----------

    private void BuildStatChips(List<StatChip> stats)
    {
        ClearChildren(_statsRow);
        if (stats == null || stats.Count == 0) return;

        int n = Mathf.Min(stats.Count, 4);
        float gap = 0.025f;
        float w = (1f - gap * (n - 1)) / n;

        for (int i = 0; i < n; i++)
        {
            float x0 = i * (w + gap);

            // Border
            var (_, outer, outerImg) = CreateBox("Chip", _statsRow, accentColor);
            outerImg.raycastTarget = false;
            outer.anchorMin = new Vector2(x0, 0f);
            outer.anchorMax = new Vector2(x0 + w, 1f);
            outer.offsetMin = outer.offsetMax = Vector2.zero;

            // Fill
            var (_, fill, fillImg) = CreateBox("Fill", outer, chipColor);
            fillImg.raycastTarget = false;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);

            // Value (big)
            var value = CreateLabel(fill, stats[i].value, chipValueFontSize, inkColor,
                new Vector2(0f, 0.40f), new Vector2(1f, 1f));
            value.alignment = TextAlignmentOptions.Center;
            value.enableAutoSizing = true;
            value.fontSizeMin = 20f;
            value.fontSizeMax = chipValueFontSize;
            value.fontStyle = titleFont != null ? FontStyles.Normal : FontStyles.Bold;
            ApplyFont(value, titleFont);

            // Label (small caps)
            var label = CreateLabel(fill, (stats[i].label ?? "").ToUpper(), chipLabelFontSize, accentColor,
                new Vector2(0f, 0.04f), new Vector2(1f, 0.40f));
            label.alignment = TextAlignmentOptions.Center;
            label.characterSpacing = 3f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = chipLabelFontSize;
            label.fontStyle = FontStyles.Bold;
            ApplyFont(label, SerifBody);
        }
    }
}