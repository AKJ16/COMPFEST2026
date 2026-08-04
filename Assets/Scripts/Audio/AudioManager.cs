using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] private int sfxPoolSize = 8;

    [Header("Default Settings & Audio Clips")]
    [SerializeField] private AudioClip buttonClickSfx;
    [Range(0f, 1f)][SerializeField] private float defaultMusicVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] private float defaultSfxVolume = 1f;

    private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
    private Coroutine _musicFadeRoutine;

    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Load saved volumes or use defaults
        MusicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, defaultMusicVolume);
        SFXVolume = PlayerPrefs.GetFloat(SFX_KEY, defaultSfxVolume);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = Instantiate(sfxSourcePrefab, transform);
            src.playOnAwake = false;
            _sfxPool.Enqueue(src);
        }

        ApplyMusicVolume();
    }

    #region Volume Control

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MUSIC_KEY, MusicVolume);
        PlayerPrefs.Save();

        ApplyMusicVolume();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_KEY, SFXVolume);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume()
    {
        if (musicSource != null && _musicFadeRoutine == null)
        {
            musicSource.volume = MusicVolume;
        }
    }

    #endregion

    #region Music Playback & Crossfading

    /// <summary>
    /// Fades out the current track and fades in the new track over the fadeDuration.
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = 0.8f)
    {
        if (clip == null || musicSource == null) return;

        // If the same music is already playing, keep it playing seamlessly
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            musicSource.loop = loop;
            return;
        }

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(CrossfadeMusicRoutine(clip, loop, fadeDuration));
    }

    public void FadeOutMusic(float duration = 0.5f)
    {
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(FadeOutMusicRoutine(duration));
    }

    public void StopMusic()
    {
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        if (musicSource != null) musicSource.Stop();
    }

    private IEnumerator CrossfadeMusicRoutine(AudioClip newClip, bool loop, float duration)
    {
        float halfDuration = duration / 2f;

        // 1. Fade out current music track
        if (musicSource.isPlaying && musicSource.volume > 0f && halfDuration > 0f)
        {
            float startVol = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDuration);
                yield return null;
            }
        }

        // 2. Swap music track
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();

        // 3. Fade in new track up to saved MusicVolume
        if (halfDuration > 0f)
        {
            float elapsed = 0f;
            float targetVol = MusicVolume;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, targetVol, elapsed / halfDuration);
                yield return null;
            }
        }

        musicSource.volume = MusicVolume;
        _musicFadeRoutine = null;
    }

    private IEnumerator FadeOutMusicRoutine(float duration)
    {
        if (musicSource.isPlaying && duration > 0f)
        {
            float startVol = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.volume = MusicVolume;
        _musicFadeRoutine = null;
    }

    #endregion

    #region SFX Playback

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _sfxPool.Count == 0) return;

        var src = _sfxPool.Dequeue();
        src.clip = clip;
        src.volume = volume * SFXVolume; // Scaled by SFX Volume setting
        src.Play();
        _sfxPool.Enqueue(src);
    }

    public void PlayButtonClick()
    {
        if (buttonClickSfx != null)
        {
            PlaySFX(buttonClickSfx);
        }
    }

    #endregion
}