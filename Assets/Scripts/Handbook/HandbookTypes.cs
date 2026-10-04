using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One stat box on a weapon page, e.g. value "3", label "Damage".
[Serializable]
public class StatChip
{
    public string value;
    public string label;
}

// Optional per-weapon overrides. Anything you leave empty falls back to the
// built-in text/stats/diagrams below, so you only fill in what you want to change.
[Serializable]
public class WeaponPageInfo
{
    public WeaponData weapon;
    [TextArea(3, 8)] public string effectText;
    [Tooltip("Stat boxes at the top of the page (max 4).")]
    public StatChip[] stats;
    [Tooltip("Squares the item takes on the grid: width x height. 0 = use default / hide.")]
    public Vector2Int footprint;
    [Tooltip("Area the item affects: width x height. 0 = use default / none.")]
    public Vector2Int range;
}

// Optional: your own short name for one combo. Wins over the built-in names.
[Serializable]
public class ComboNameInfo
{
    public WeaponData modifier;
    public WeaponData target;
    [Tooltip("Keep it short, 1-2 words. Example: Surge")]
    public string comboName;
}