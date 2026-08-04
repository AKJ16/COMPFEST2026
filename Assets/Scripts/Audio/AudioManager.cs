using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] private int sfxPoolSize = 8;

    [Header("SFX Library — isi nama + clip di Inspector")]
    [SerializeField] private List<NamedClip> sfxLibrary = new List<NamedClip>();

    [Serializable]
    public class NamedClip
    {
        public string name;
        public AudioClip clip;
    }

    private readonly Dictionary<string, AudioClip> _sfxLookup = new Dictionary<string, AudioClip>();
    private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = Instantiate(sfxSourcePrefab, transform);
            src.playOnAwake = false;
            _sfxPool.Enqueue(src);
        }

        foreach (var entry in sfxLibrary)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.name) && entry.clip != null)
                _sfxLookup[entry.name] = entry.clip;
        }
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        var src = _sfxPool.Dequeue();
        src.clip = clip;
        src.volume = volume;
        src.Play();
        _sfxPool.Enqueue(src); // cycle back to end of pool (round-robin reuse)
    }

    // Overload baru: panggil pakai nama, gak perlu drag AudioClip manual di tiap script.
    public void PlaySFX(string clipName, float volume = 1f)
    {
        if (_sfxLookup.TryGetValue(clipName, out var clip))
        {
            PlaySFX(clip, volume);
        }
        else
        {
            Debug.LogWarning($"AudioManager: SFX '{clipName}' tidak ditemukan di sfxLibrary.");
        }
    }
}