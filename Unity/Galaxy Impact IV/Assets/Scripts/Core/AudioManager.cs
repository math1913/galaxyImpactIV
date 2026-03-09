using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (!musicSource) return;

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.loop = loop;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (!sfxSource || clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}
