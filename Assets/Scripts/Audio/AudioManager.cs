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

    [Header("Music Ducking (Menu / Win / Lose)")]
    [Tooltip("Volume multiplier when a menu panel is open (0.3 = 30% of normal music volume)")]
    [Range(0f, 1f)][SerializeField] private float duckedVolumeMultiplier = 0.3f;

    private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
    private Coroutine _musicFadeRoutine;
    private Coroutine _duckRoutine;
    private bool _isDucked = false;

    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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
        if (musicSource != null && _musicFadeRoutine == null && _duckRoutine == null)
        {
            musicSource.volume = _isDucked ? MusicVolume * duckedVolumeMultiplier : MusicVolume;
        }
    }

    #endregion

    #region Music Ducking (Menu / Win / Lose)

    /// <summary>
    /// Smoothly lowers the music volume (called when Pause, Game Over, Win, or Handbook opens).
    /// </summary>
    public void DuckMusic(float duration = 0.3f)
    {
        _isDucked = true;
        if (_duckRoutine != null) StopCoroutine(_duckRoutine);
        _duckRoutine = StartCoroutine(DuckMusicRoutine(MusicVolume * duckedVolumeMultiplier, duration));
    }

    /// <summary>
    /// Smoothly restores the music volume back to 100%.
    /// </summary>
    public void UnduckMusic(float duration = 0.3f)
    {
        _isDucked = false;
        if (_duckRoutine != null) StopCoroutine(_duckRoutine);
        _duckRoutine = StartCoroutine(DuckMusicRoutine(MusicVolume, duration));
    }

    private IEnumerator DuckMusicRoutine(float targetVolume, float duration)
    {
        if (musicSource == null) yield break;

        float startVol = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        _duckRoutine = null;
    }

    #endregion

    #region Music Playback & Crossfading

    public void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = 0.8f)
    {
        if (clip == null || musicSource == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            musicSource.loop = loop;
            return;
        }

        _isDucked = false;
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(CrossfadeMusicRoutine(clip, loop, fadeDuration));
    }

    public void FadeOutMusic(float duration = 0.5f)
    {
        _isDucked = false;
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(FadeOutMusicRoutine(duration));
    }

    public void StopMusic()
    {
        _isDucked = false;
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        if (musicSource != null) musicSource.Stop();
    }

    private IEnumerator CrossfadeMusicRoutine(AudioClip newClip, bool loop, float duration)
    {
        float halfDuration = duration / 2f;

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

        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();

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
        src.volume = volume * SFXVolume;
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