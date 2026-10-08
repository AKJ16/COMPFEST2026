using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class HandbookUI
{
    // ---------- Size / range diagrams ----------

    private void BuildDiagrams(PageData data)
    {
        ClearChildren(_diagramArea);

        bool hasFootprint = data.footprint.x > 0 && data.footprint.y > 0;
        bool hasRange = data.range.x > 0 && data.range.y > 0;
        if (!hasFootprint && !hasRange) return;

        var heading = CreateLabel(_diagramArea, SectionHeading("Placement"), bodyFontSize, inkColor,
            Vector2.zero, Vector2.one);
        heading.alignment = TextAlignmentOptions.TopLeft;
        heading.richText = true;
        ApplyFont(heading, SerifBody);
        PlaceTopLeft(heading.rectTransform, 0f, 0f, 220f, 32f);

        float x = 0f;
        float y = 36f;

        // Hitung cell size secara dinamis agar grid muat secara utuh di dalam kolom Kiri (PLACEMENT)
        float availableWidth = _diagramArea.rect.width > 0f ? _diagramArea.rect.width : 220f;
        
        // Ukuran cell optimal (proporsional & tidak melebar ke kolom kanan)
        float effectiveCell = Mathf.Min(diagramCellSize, 24f);

        if (hasFootprint && hasRange)
        {
            float totalCols = data.footprint.x + data.range.x;
            float projectedWidth = (data.footprint.x * (effectiveCell + 3f)) + 16f + (data.range.x * (effectiveCell + 3f));
            if (projectedWidth > availableWidth && totalCols > 0)
            {
                effectiveCell = Mathf.Clamp((availableWidth - 24f) / totalCols - 3f, 14f, 24f);
            }
        }

        if (hasFootprint)
            x = DrawGrid(_diagramArea, x, y, "Size", data.footprint, data.footprint, false, effectiveCell);

        if (hasRange)
        {
            var item = hasFootprint ? data.footprint : new Vector2Int(1, 1);
            DrawGrid(_diagramArea, x, y, "Range", data.range, item, true, effectiveCell);
        }
    }

    // Draws a small grid. In range mode the item's own squares are centred inside
    // the range area and drawn in the darker colour. Returns the next free x.
    private float DrawGrid(RectTransform parent, float x, float y, string caption,
        Vector2Int gridSize, Vector2Int itemSize, bool rangeMode, float cellSize)
    {
        float cell = cellSize;
        float spacing = 3f;
        float step = cell + spacing;
        float gridW = gridSize.x * step - spacing;
        float slotW = Mathf.Max(gridW, 50f);

        var cap = CreateLabel(parent, caption.ToUpper(), 18f, inkColor, Vector2.zero, Vector2.one);
        cap.alignment = TextAlignmentOptions.TopLeft;
        cap.characterSpacing = 3f;
        cap.fontStyle = FontStyles.Bold;
        ApplyFont(cap, SerifBody);
        PlaceTopLeft(cap.rectTransform, x, y, slotW, 22f);

        float gridTop = y + 24f;
        int ox = rangeMode ? (gridSize.x - itemSize.x) / 2 : 0;
        int oy = rangeMode ? (gridSize.y - itemSize.y) / 2 : 0;

        for (int gy = 0; gy < gridSize.y; gy++)
        {
            for (int gx = 0; gx < gridSize.x; gx++)
            {
                bool isItem = !rangeMode ||
                              (gx >= ox && gx < ox + itemSize.x && gy >= oy && gy < oy + itemSize.y);

                var (_, r, img) = CreateBox("Cell", parent, isItem ? gridItemColor : gridRangeColor);
                img.raycastTarget = false;
                PlaceTopLeft(r, x + gx * step, gridTop + gy * step, cell, cell);
            }
        }

        return x + slotW + 16f;
    }
}