using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Overlay canvas, blurred backdrop and game freeze for the book.
public partial class HandbookUI
{
    private Transform _uiRoot;              // parent of the blur, dim, book and toast
    private GameObject _overlayCanvasGo;
    private RawImage _blurImage;
    private RenderTexture _blurTexture;
    private Coroutine _openRoutine;
    private bool _frozen;
    private float _prevTimeScale = 1f;

    // ---------- Own canvas ----------

    // A dedicated root canvas drawn above everything, so a HUD canvas with a higher sort order
    // (or a nested canvas with Override Sorting) can no longer draw over the dim.
    private void CreateUiRoot()
    {
        if (!useOverlayCanvas)
        {
            _uiRoot = canvasParent;
            return;
        }

        var source = canvasParent != null ? canvasParent.GetComponentInParent<Canvas>() : null;
        if (source != null) source = source.rootCanvas;

        _overlayCanvasGo = new GameObject("HandbookOverlayCanvas", typeof(RectTransform));
        var canvas = _overlayCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = overlaySortingOrder;

        // Match the game's canvas scaling so panelSize looks the same as before.
        var scaler = _overlayCanvasGo.AddComponent<CanvasScaler>();
        var srcScaler = source != null ? source.GetComponent<CanvasScaler>() : null;
        if (srcScaler != null)
        {
            scaler.uiScaleMode = srcScaler.uiScaleMode;
            scaler.referenceResolution = srcScaler.referenceResolution;
            scaler.screenMatchMode = srcScaler.screenMatchMode;
            scaler.matchWidthOrHeight = srcScaler.matchWidthOrHeight;
            scaler.scaleFactor = srcScaler.scaleFactor;
            scaler.referencePixelsPerUnit = srcScaler.referencePixelsPerUnit;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        _overlayCanvasGo.AddComponent<GraphicRaycaster>();
        _uiRoot = _overlayCanvasGo.transform;
    }

    // ---------- Show the book ----------

    private IEnumerator OpenAfterCapture()
    {
        // The book and dim are still hidden here, so the screenshot shows only the game.
        yield return new WaitForEndOfFrame();
        CaptureBackdrop();
        _openRoutine = null;
        if (_isOpen) ShowBook();   // may have been closed during this frame
    }

    private void ShowBook()
    {
        FitPanelToScreen();   // before the first draw, so the book never clips
        // Order, bottom to top: blur, dim, book, toast.
        if (_blurImage != null && _blurImage.gameObject.activeSelf) _blurImage.transform.SetAsLastSibling();
        if (_dimRoot != null)
        {
            _dimRoot.SetActive(true);
            _dimRoot.transform.SetAsLastSibling();
        }
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(true);
            _panelRoot.transform.SetAsLastSibling();
        }
        if (_toastRoot != null && _toastRoot.activeSelf)
            _toastRoot.transform.SetAsLastSibling();

        ResetFlipState();
        RebuildWeaponList();
        _pageIndex = 0;
        RenderPage();

        PlaySfx(openSfx);
    }

    private int _fitW, _fitH;

    // Scales the whole book down so it fits inside the screen (never up). The size is read after
    // the canvas has updated, which is why the first open used to clip.
    private void FitPanelToScreen()
    {
        if (_panelRoot == null) return;
        var canvasRect = _uiRoot as RectTransform;
        if (!fitToScreen || canvasRect == null)
        {
            if (_panelRoot != null) _panelRoot.transform.localScale = Vector3.one;
            return;
        }

        Canvas.ForceUpdateCanvases();
        Rect r = canvasRect.rect;
        _fitW = Screen.width;
        _fitH = Screen.height;
        if (r.width <= 1f || r.height <= 1f || _panelSizeActual.x <= 0f || _panelSizeActual.y <= 0f) return;

        if (_skinned && fullScreenBook)
        {
            // Fit the book body (not the empty strip around it) plus the buttons under it, and allow scaling up.
            float bodyW = _panelSizeActual.x * (BodyRight - BodyLeft);
            float overhang = Mathf.Max(0f, navButtonSize.y - 8f);       // buttons hang below the book
            float fullH = _panelSizeActual.y + overhang;
            float full = Mathf.Min(r.width * fullScreenFill / bodyW, r.height * fullScreenFill / fullH);

            _panelRoot.transform.localScale = new Vector3(full, full, 1f);
            // Centre book + buttons together.
            ((RectTransform)_panelRoot.transform).anchoredPosition = new Vector2(0f, overhang * full * 0.5f);
            return;
        }

        float s = Mathf.Min(1f, r.width * screenFill / _panelSizeActual.x, r.height * screenFill / _panelSizeActual.y);
        _panelRoot.transform.localScale = new Vector3(s, s, 1f);
    }

    // Re-fit if the window is resized while the book is open.
    private void LateUpdate()
    {
        if (_isOpen && _panelRoot != null && _panelRoot.activeSelf &&
            (Screen.width != _fitW || Screen.height != _fitH))
            FitPanelToScreen();
    }

    private void CloseInternal()
    {
        _isOpen = false;
        if (_openRoutine != null)
        {
            StopCoroutine(_openRoutine);
            _openRoutine = null;
        }
        ResetFlipState();
        if (_panelRoot != null) _panelRoot.SetActive(false);
        if (_dimRoot != null) _dimRoot.SetActive(false);
        ReleaseBackdrop();
        Unfreeze();
    }

    // ---------- Blurred backdrop ----------

    private void BuildBlurLayer()
    {
        var go = new GameObject("HandbookBlur", typeof(RectTransform));
        go.transform.SetParent(_uiRoot, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        _blurImage = go.AddComponent<RawImage>();
        _blurImage.raycastTarget = false;   // the dim on top is what blocks clicks
        go.SetActive(false);
    }

    // Screenshot -> halve the size several times with bilinear filtering -> stretch back to full
    // screen. No shader, package or render-pipeline feature needed. If the screenshot fails,
    // the book just opens with the plain dim.
    private void CaptureBackdrop()
    {
        ReleaseBackdrop();
        if (!blurBackground || _blurImage == null) return;

        Texture2D shot = null;
        try { shot = ScreenCapture.CaptureScreenshotAsTexture(); }
        catch (Exception e) { Debug.LogWarning("Handbook blur skipped: " + e.Message); }
        if (shot == null) return;

        Texture source = shot;
        RenderTexture current = null;
        int w = shot.width;
        int h = shot.height;

        for (int i = 0; i < blurAmount; i++)
        {
            w = Mathf.Max(4, w / 2);
            h = Mathf.Max(4, h / 2);
            var next = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Graphics.Blit(source, next);

            if (current != null)
            {
                current.Release();
                Destroy(current);
            }
            current = next;
            source = next;
        }

        Destroy(shot);
        if (current == null) return;

        _blurTexture = current;
        _blurImage.texture = _blurTexture;
        _blurImage.gameObject.SetActive(true);
    }

    private void ReleaseBackdrop()
    {
        if (_blurImage != null)
        {
            _blurImage.texture = null;
            _blurImage.gameObject.SetActive(false);
        }
        if (_blurTexture != null)
        {
            _blurTexture.Release();
            Destroy(_blurTexture);
            _blurTexture = null;
        }
    }

    // ---------- Freeze ----------

    // Time.timeScale = 0 stops Update/FixedUpdate movement, timers, physics, Animators and particles
    // that use scaled time. Whatever the scale was before (0 if the game was already paused)
    // is restored on close. The book itself animates with unscaled time.
    private void Freeze()
    {
        if (!freezeGameWhileOpen || _frozen) return;
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _frozen = true;
    }

    private void Unfreeze()
    {
        if (!_frozen) return;
        Time.timeScale = _prevTimeScale;
        _frozen = false;
    }
}