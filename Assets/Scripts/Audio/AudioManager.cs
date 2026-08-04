using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] private int sfxPoolSize = 8;

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
}
