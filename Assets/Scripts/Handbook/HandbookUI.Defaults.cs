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
    // BUILT-IN PAGE CONTENT (used unless overridden in Page Texts)
    // The order of this list is also the page order.
    // =========================================================

    private class PageData
    {
        public string effect = "";
        public List<StatChip> stats = new List<StatChip>();
        public Vector2Int footprint;   // squares the item occupies
        public Vector2Int range;       // area it affects
        public string comboNote = "";  // short result shown under a combo line
    }

    private class DefaultEntry
    {
        public readonly string key;    // matched against the weapon name (case-insensitive)
        public readonly PageData data;
        public DefaultEntry(string key, PageData data) { this.key = key; this.data = data; }
    }

    private static StatChip Chip(string value, string label) =>
        new StatChip { value = value, label = label };

    private static readonly DefaultEntry[] Defaults =
    {
        new DefaultEntry("sword", new PageData
        {
            effect = "A reliable blade that hits hard for its size. It takes up two squares, " +
                     "and the Hourglass can redo its placement.",
            stats = new List<StatChip> { Chip("4", "Damage"), Chip("2", "Squares") },
            footprint = new Vector2Int(2, 1),
        }),
        new DefaultEntry("staff", new PageData
        {
            effect = "A magic staff that starts weak but grows with the right support. " +
                     "It stands upright and takes up two squares. Books can boost its damage, " +
                     "and the Hourglass can redo its placement.",
            stats = new List<StatChip> { Chip("2", "Damage"), Chip("2", "Squares") },
            footprint = new Vector2Int(1, 2),   // 1 wide x 2 tall (vertical)
        }),
        new DefaultEntry("add", new PageData
        {
            effect = "Adds +2 damage to a Staff inside its 3x3 area. It takes up one square, " +
                     "Can only be placed 2 times per stage.",
            stats = new List<StatChip> { Chip("+2", "Staff Damage"), Chip("1", "Square"), Chip("3x3", "Range") },
            footprint = new Vector2Int(1, 1),
            range = new Vector2Int(3, 3),
            comboNote = "Staff damage +2",
        }),
        new DefaultEntry("multi", new PageData
        {
            effect = "Multiplies the damage of a Staff inside its 3x3 area by 2. It takes up one square, " +
                     "Can only be placed 2 times per stage.",
            stats = new List<StatChip> { Chip("x2", "Staff Damage"), Chip("1", "Square"), Chip("3x3", "Range") },
            footprint = new Vector2Int(1, 1),
            range = new Vector2Int(3, 3),
            comboNote = "Staff damage x2",
        }),
        new DefaultEntry("poison", new PageData
        {
            effect = "Deals 1 damage, then applies a tick of poison every time you take an action. " +
                     "Poison ticks stack, so repeated actions add up. It takes up one square, " +
                     "and the Hourglass can redo its placement.",
            stats = new List<StatChip> { Chip("1", "Damage"), Chip("+1", "Poison / Action"), Chip("1", "Square") },
            footprint = new Vector2Int(1, 1),
        }),
        new DefaultEntry("hour", new PageData
        {
            effect = "Lets you redo the placement of any weapon inside its 3x4 area. " +
                     "It takes up two squares.",
            stats = new List<StatChip> { Chip("2", "Squares"), Chip("3x4", "Range") },
            footprint = new Vector2Int(1, 2),
            range = new Vector2Int(3, 4),
            comboNote = "Redo placement in 3x4 area",
        }),
    };

    private static int FindDefaultIndex(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return -1;
        string n = weaponName.ToLowerInvariant();
        for (int i = 0; i < Defaults.Length; i++)
            if (n.Contains(Defaults[i].key)) return i;
        return -1;
    }
}
