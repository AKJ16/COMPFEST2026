using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Layered book art: Book_Base (cover + lower pages) with Book_Left / Book_Right on top.
// Textures are drawn with RawImage so Unity cannot trim or slice them.
// All three share one canvas, so the numbers below place the top pages exactly.
public partial class HandbookUI
{
    private const float BookArtAspect = 586f / 1348f;   // height / width of the shared canvas
    private const float BodyLeft = 0.1046f;             // book body edges as a fraction of the width,
    private const float BodyRight = 0.8991f;            // the ribbon sticks out to the right of this

    // Where the top pages sit inside that canvas (0..1, measured from the bottom-left).
    private static readonly Rect LeftPageRect = Rect.MinMaxRect(0.14318f, 0.09898f, 0.50223f, 1f);
    private static readonly Rect RightPageRect = Rect.MinMaxRect(0.49629f, 0.09727f, 0.85534f, 1f);

    private Texture2D _artBase, _artLeft, _artRight;
    private bool _artWarned;

    private bool TryLoadBookArt()
    {
        _artBase = LoadArt(bookBaseTexture, "Book_Base");
        _artLeft = LoadArt(leftPageTexture, "Book_Left");
        _artRight = LoadArt(rightPageTexture, "Book_Right");

        bool ok = _artBase != null && _artLeft != null && _artRight != null;
        if (!ok && !_artWarned)
        {
            _artWarned = true;
            Debug.LogWarning("Handbook: book art not found (Book_Base, Book_Left, Book_Right). " +
                             "Put them in Assets/Resources/Book/ or assign them on HandbookUI. Using the plain book.");
        }
        return ok;
    }

    // The art is drawn with RawImage, which stretches the WHOLE picture over its rectangle.
    // (A Sprite can be trimmed or sliced by Unity's importer, which shifted the pages before.)
    // Uses the assigned texture, else looks in Resources/Book, Resources and Resources/Image.
    private static Texture2D LoadArt(Texture2D assigned, string file)
    {
        if (assigned != null) return assigned;

        foreach (var path in new[] { "Book/" + file, file, "Image/" + file })
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null) return tex;
        }
        return null;
    }

    private RawImage CreateArtLayer(string name, Transform parent, Texture2D art)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var raw = go.AddComponent<RawImage>();
        raw.texture = art;
        raw.color = Color.white;
        raw.raycastTarget = false;
        return raw;
    }

    private RectTransform CreateArtPage(string name, Transform parent, Rect area, Vector2 pivot, Texture2D art)
    {
        var layer = CreateArtLayer(name, parent, art);
        var rect = layer.rectTransform;
        rect.anchorMin = area.min;
        rect.anchorMax = area.max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = pivot;
        return rect;
    }

    // Holds the page text, kept inside the padding so it stays on the paper.
    private RectTransform CreateContentRect(RectTransform page)
    {
        var (_, rect, img) = CreateBox("Content", page, Color.clear);
        img.raycastTarget = false;
        rect.anchorMin = new Vector2(pagePaddingLeft, pagePaddingBottom);
        rect.anchorMax = new Vector2(1f - pagePaddingRight, 1f - pagePaddingTop);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
}