using System.Collections;
using UnityEngine;
using TMPro;

public class BossIntroUI : MonoBehaviour
{
    public static BossIntroUI Instance { get; private set; }

    [Header("Huge Popup Announcement")]
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private float popupDelay = 1.5f;
    [SerializeField] private float popupHoldDuration = 1.2f;

    [Header("Persistent Phase 2 Mechanic Banner")]
    [SerializeField] private CanvasGroup phase2BannerCanvasGroup;
    [SerializeField] private TextMeshProUGUI phase2DescriptionText;

    [Header("Audio SFX")]
    [SerializeField] private AudioClip bossIntroSfx;
    [SerializeField] private AudioClip phase2IntroSfx;

    private EnemyHealth _boundEnemy;
    private string _currentPhase2Description;
    private Coroutine _popupRoutine;
    private Coroutine _bannerRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ResetUI();
    }

    private void ResetUI()
    {
        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.blocksRaycasts = false;
        }
        if (phase2BannerCanvasGroup != null)
        {
            phase2BannerCanvasGroup.alpha = 0f;
            phase2BannerCanvasGroup.blocksRaycasts = false;
        }
    }

    public void BindEnemy(EnemyHealth enemy, bool isBossStage, string phase2Description)
    {
        if (_boundEnemy != null)
        {
            _boundEnemy.OnPhaseTwoStarted -= HandlePhaseTwoStarted;
            _boundEnemy.OnStateChanged -= HandleStateChanged;
        }

        _boundEnemy = enemy;
        _currentPhase2Description = phase2Description;
        ResetUI();

        if (_boundEnemy == null) return;

        _boundEnemy.OnPhaseTwoStarted += HandlePhaseTwoStarted;
        _boundEnemy.OnStateChanged += HandleStateChanged;

        if (isBossStage)
        {
            TriggerPopup("BOSS FIGHT!", popupDelay, bossIntroSfx);
        }
    }

    public void TriggerPopup(string text, float delay, AudioClip sfx = null)
    {
        if (_popupRoutine != null) StopCoroutine(_popupRoutine);
        _popupRoutine = StartCoroutine(PopupRoutine(text, delay, sfx));
    }

    private IEnumerator PopupRoutine(string text, float delay, AudioClip sfx = null)
    {
        ResetPopup();

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (popupText != null)
        {
            popupText.text = text;
        }

        // Play Intro SFX when popup appears
        if (sfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sfx);
        }

        float elapsed = 0f;
        float popInDuration = 0.35f;
        Vector3 startScale = Vector3.one * 0.4f;
        Vector3 targetScale = Vector3.one;

        while (elapsed < popInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / popInDuration;

            float s = 1.7f;
            float t2 = t - 1.0f;
            float bounce = (t2 * t2 * ((s + 1) * t2 + s) + 1.0f);

            if (popupRect != null) popupRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, bounce);
            if (popupCanvasGroup != null) popupCanvasGroup.alpha = t;

            yield return null;
        }

        if (popupRect != null) popupRect.localScale = targetScale;
        if (popupCanvasGroup != null) popupCanvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(popupHoldDuration);

        elapsed = 0f;
        float fadeDuration = 0.4f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDuration;

            if (popupCanvasGroup != null) popupCanvasGroup.alpha = 1f - t;
            yield return null;
        }

        ResetPopup();
    }

    private void ResetPopup()
    {
        if (popupCanvasGroup != null) popupCanvasGroup.alpha = 0f;
        if (popupRect != null) popupRect.localScale = Vector3.one;
    }

    private void HandlePhaseTwoStarted()
    {
        // Trigger PHASE 2 popup + SFX
        TriggerPopup("PHASE 2!", 0.5f, phase2IntroSfx);

        if (phase2BannerCanvasGroup != null && phase2DescriptionText != null)
        {
            phase2DescriptionText.text = string.IsNullOrEmpty(_currentPhase2Description)
                ? "PHASE 2 STARTED!"
                : _currentPhase2Description;

            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            _bannerRoutine = StartCoroutine(FadeBanner(true));
        }
    }

    private void HandleStateChanged(EnemyState state)
    {
        if (state == EnemyState.Dead)
        {
            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            _bannerRoutine = StartCoroutine(FadeBanner(false));
        }
    }

    private IEnumerator FadeBanner(bool fadeIn)
    {
        if (phase2BannerCanvasGroup == null) yield break;

        float elapsed = 0f;
        float duration = 0.4f;
        float startAlpha = phase2BannerCanvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            phase2BannerCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        phase2BannerCanvasGroup.alpha = targetAlpha;
    }

    private void OnDestroy()
    {
        if (_boundEnemy != null)
        {
            _boundEnemy.OnPhaseTwoStarted -= HandlePhaseTwoStarted;
            _boundEnemy.OnStateChanged -= HandleStateChanged;
        }
    }
}