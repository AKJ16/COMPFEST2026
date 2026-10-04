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
    // COMBO DISCOVERY (static, callable from game code without a reference)
    // =========================================================

    private const string ComboKeyPrefix = "LIGMA.Combo.";
    private const string ComboIndexKey = "LIGMA.Combo.Index";   // list of saved keys, used by ResetDiscoveries

    // One PlayerPrefs key per modifier+target pair. Uses the asset name, so renaming the
    // display name (weaponName) later will not wipe saved discoveries.
    private static string ComboKey(WeaponData modifier, WeaponData target) =>
        ComboKeyPrefix + modifier.name + "|" + target.name;

    // Call this the moment a modifier REALLY affects a target during play
    // (damage boosted, Hourglass replay used). Adjacent placement alone must not call it.
    // detail is shown in the book once discovered, e.g. "Staff damage went from 2 to 4".
    public static void ReportCombo(WeaponData modifier, WeaponData target, string detail = null)
    {
        if (modifier == null || target == null) return;

        string key = ComboKey(modifier, target);
        detail = CleanDetail(detail);

        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetString(key, detail);
            AddToIndex(key);
            PlayerPrefs.Save();

            if (s_instance != null) s_instance.OnComboDiscovered(modifier, target);
        }
        else if (detail.Length > 0 && PlayerPrefs.GetString(key, "").Length == 0)
        {
            // Discovered earlier without any detail text: fill it in now.
            PlayerPrefs.SetString(key, detail);
            PlayerPrefs.Save();
        }
    }

    public static bool IsComboDiscovered(WeaponData modifier, WeaponData target) =>
        modifier != null && target != null && PlayerPrefs.HasKey(ComboKey(modifier, target));

    // Wipes every saved discovery. For testing; also in the component's right-click menu.
    [ContextMenu("Reset Combo Discoveries")]
    private void ResetDiscoveriesFromMenu() => ResetDiscoveries();

    public static void ResetDiscoveries()
    {
        string index = PlayerPrefs.GetString(ComboIndexKey, "");
        foreach (var key in index.Split('\n'))
            if (key.Length > 0) PlayerPrefs.DeleteKey(key);

        PlayerPrefs.DeleteKey(ComboIndexKey);
        PlayerPrefs.Save();

        if (s_instance != null && s_instance._isOpen) s_instance.RenderPage();
    }

    // Discoveries are wiped every time the game launches (and every time Play mode starts).
    // Set to false to keep them between sessions. Restarting a stage does NOT wipe them.
    private static readonly bool WipeDiscoveriesOnLaunch = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WipeOnLaunch()
    {
        s_instance = null;
        if (WipeDiscoveriesOnLaunch) ResetDiscoveries();
    }

    private static string GetSavedDetail(WeaponData modifier, WeaponData target) =>
        PlayerPrefs.GetString(ComboKey(modifier, target), "");

    // Rich-text tags in a detail string would break the page layout.
    private static string CleanDetail(string detail) =>
        string.IsNullOrWhiteSpace(detail) ? "" : detail.Trim().Replace('<', '(').Replace('>', ')').Replace('\n', ' ');

    private static void AddToIndex(string key)
    {
        string index = PlayerPrefs.GetString(ComboIndexKey, "");
        PlayerPrefs.SetString(ComboIndexKey, index.Length == 0 ? key : index + "\n" + key);
    }

    private void OnComboDiscovered(WeaponData modifier, WeaponData target)
    {
        if (_isOpen) RenderPage();
        ShowToast(modifier, target);
    }

    // ---------- Toast ----------

    private void BuildToast()
    {
        var (root, rect, img) = CreateBox("ComboToast", _uiRoot, coverColor);
        img.raycastTarget = false;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -40f);
        rect.sizeDelta = toastSize;

        _toastLabel = CreateLabel(root.transform, "", toastFontSize, buttonTextColor, Vector2.zero, Vector2.one);
        _toastLabel.alignment = TextAlignmentOptions.Center;
        _toastLabel.richText = true;
        _toastLabel.enableAutoSizing = true;
        _toastLabel.fontSizeMin = 16f;
        _toastLabel.fontSizeMax = toastFontSize;
        _toastLabel.rectTransform.offsetMin = new Vector2(16f, 8f);
        _toastLabel.rectTransform.offsetMax = new Vector2(-16f, -8f);
        ApplyFont(_toastLabel, SerifBody);

        _toastRoot = root;
        _toastRoot.SetActive(false);
    }

    // Toasts are queued: if two combos are discovered at once (or while a toast is showing),
    // they are shown one after another instead of replacing each other.
    private readonly Queue<KeyValuePair<WeaponData, WeaponData>> _toastQueue =
        new Queue<KeyValuePair<WeaponData, WeaponData>>();

    private void ShowToast(WeaponData modifier, WeaponData target)
    {
        if (!showDiscoveryToast || _toastRoot == null || !isActiveAndEnabled) return;

        _toastQueue.Enqueue(new KeyValuePair<WeaponData, WeaponData>(modifier, target));
        if (_toastRoutine == null) _toastRoutine = StartCoroutine(ToastQueueRoutine());
    }

    private IEnumerator ToastQueueRoutine()
    {
        while (_toastQueue.Count > 0)
        {
            var pair = _toastQueue.Dequeue();
            _toastLabel.text = "<b>New combo discovered!</b>\n" + GetComboName(pair.Key, pair.Value);
            _toastRoot.transform.SetAsLastSibling();   // draw above the rest of the HUD
            _toastRoot.SetActive(true);

            yield return WaitUnscaled(toastSeconds);

            _toastRoot.SetActive(false);
            if (_toastQueue.Count > 0)
                yield return WaitUnscaled(toastGapSeconds);   // short pause so the next one reads as new
        }
        _toastRoutine = null;
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // Called when the book object is switched off: Unity stops coroutines then.
    private void ResetToasts()
    {
        _toastQueue.Clear();
        _toastRoutine = null;
        if (_toastRoot != null) _toastRoot.SetActive(false);
    }

    // ---------- Combo names ----------

    // Short built-in names, matched against the weapon names (case-insensitive "contains").
    private static readonly (string modifier, string target, string name)[] DefaultComboNames =
    {
        ("add",    "staff",  "Surge"),
        ("multi",  "staff",  "Overload"),
        ("hour",   "sword",  "Echo Strike"),
        ("hour",   "staff",  "Echo Bolt"),
        ("hour",   "poison", "Echo Venom"),
    };

    // Name shown in the book and in the toast. Order: your override in Combo Names,
    // then the built-in name, then "Modifier + Target" as a last resort.
    private string GetComboName(WeaponData modifier, WeaponData target)
    {
        if (comboNames != null)
            foreach (var c in comboNames)
                if (c != null && c.modifier == modifier && c.target == target && !string.IsNullOrWhiteSpace(c.comboName))
                    return c.comboName.Trim();

        string m = modifier.weaponName != null ? modifier.weaponName.ToLowerInvariant() : "";
        string t = target.weaponName != null ? target.weaponName.ToLowerInvariant() : "";
        foreach (var d in DefaultComboNames)
            if (m.Contains(d.modifier) && t.Contains(d.target)) return d.name;

        return modifier.weaponName + " + " + target.weaponName;
    }

    // Keeps only the first sentence of the saved detail and tightens "went from X to Y".
    // "Staff damage went from 2 to 4 (+2). With every book in range the hit was 6."
    //   becomes "Staff damage: 2 to 4 (+2)".
    private static string SimplifyDetail(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) return "";

        string s = detail.Trim();
        int dot = s.IndexOf(". ", StringComparison.Ordinal);
        if (dot > 0) s = s.Substring(0, dot);
        s = s.TrimEnd('.');
        return System.Text.RegularExpressions.Regex.Replace(
            s, @"\s+went from\s+", ": ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    // ---------- Combos ----------

    private string BuildCombosText(WeaponData weapon)
    {
        string accent = ColorUtility.ToHtmlStringRGB(accentColor);
        var combos = GetCombosInvolving(weapon);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(SectionHeading("Combos"));

        if (combos.Count == 0)
        {
            sb.AppendLine("No combos available for this weapon yet.");
            return sb.ToString();
        }

        foreach (var (modifier, target) in combos)
        {
            if (!IsComboDiscovered(modifier, target))
            {
                // Hidden until the game reports it through ReportCombo.
                sb.AppendLine("• ??? + ???");
                sb.AppendLine($"   <size=80%><color=#{accent}>Combo not yet discovered</color></size>");
                continue;
            }

            sb.AppendLine($"• <b>{GetComboName(modifier, target)}</b>");

            // Saved detail from the run that discovered it; fall back to the short built-in note.
            string detail = GetSavedDetail(modifier, target);
            if (string.IsNullOrEmpty(detail))
            {
                int idx = FindDefaultIndex(modifier.weaponName);
                detail = idx >= 0 ? Defaults[idx].data.comboNote : "";
            }
            detail = SimplifyDetail(detail);
            if (!string.IsNullOrEmpty(detail))
                sb.AppendLine($"   <size=80%><color=#{accent}>{detail}</color></size>");
        }
        return sb.ToString();
    }

    // Every modifier+target pair that involves the selected weapon (as modifier or as target).
    // The Hourglass (Repeat) has no target list: it pairs with every Attack weapon in the book,
    // so each pair is discovered on its own.
    private List<(WeaponData modifier, WeaponData target)> GetCombosInvolving(WeaponData selected)
    {
        var pairs = new List<(WeaponData, WeaponData)>();

        void Add(WeaponData modifier, WeaponData target)
        {
            if (modifier != selected && target != selected) return;
            if (pairs.Contains((modifier, target))) return;
            pairs.Add((modifier, target));
        }

        foreach (var mod in _weapons)
        {
            if (mod.category != WeaponCategory.Modifier) continue;

            if (mod.modifierType == ModifierType.Repeat)
            {
                foreach (var w in _weapons)
                    if (w.category == WeaponCategory.Attack) Add(mod, w);
                continue;
            }

            if (mod.targets == null) continue;

            foreach (var target in mod.targets)
                if (target != null) Add(mod, target);
        }

        return pairs;
    }

    // Small spaced capitals in the accent color.
    private string SectionHeading(string title)
    {
        string accent = ColorUtility.ToHtmlStringRGB(accentColor);
        return $"<size=75%><color=#{accent}><b><cspace=0.15em>{title.ToUpper()}</cspace></b></color></size>";
    }

    private bool IsUnlocked(WeaponData weapon) =>
        StageManager.Instance == null ||
        weapon.unlockStage <= StageManager.Instance.CurrentStageNumber;
}