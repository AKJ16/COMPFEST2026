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
    // HELPERS
    // =========================================================

    private void PlaySfx(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
            AudioManager.Instance.PlaySFX(clip);
    }

    private void ApplyFont(TextMeshProUGUI t, TMP_FontAsset f)
    {
        if (t != null && f != null) t.font = f;
    }

    private void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var c = t.GetChild(i).gameObject;
            c.SetActive(false);
            Destroy(c);
        }
    }

    // Positions a rect from the top-left corner of its parent, in pixels.
    private void PlaceTopLeft(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 1f);
        r.sizeDelta = new Vector2(w, h);
        r.anchoredPosition = new Vector2(x, -y);
    }

    private (GameObject go, RectTransform rect, Image img) CreateBox(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return (go, rect, img);
    }

    private Button CreateAnchoredButton(Transform parent, string name, Color color, string label, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size,
        UnityEngine.Events.UnityAction onClick, bool useSprite = true)
    {
        var (go, rect, img) = CreateBox(name, parent, color);
        if (useSprite && buttonSprite != null)
        {
            img.sprite = buttonSprite;
            img.color = Color.white;
        }
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        var lbl = CreateLabel(go.transform, label, fontSize, buttonTextColor, Vector2.zero, Vector2.one);
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.fontStyle = SerifBody != null ? FontStyles.Normal : FontStyles.Bold;
        ApplyFont(lbl, SerifBody);
        return button;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private string GetInitial(string weaponName) =>
        string.IsNullOrEmpty(weaponName) ? "?" : weaponName.Substring(0, 1).ToUpper();
}
