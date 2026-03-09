using UnityEngine;

public class MusicStarter : MonoBehaviour
{
    [SerializeField] private AudioClip bgm;
    [SerializeField, Range(0f, 1f)] private float volume = 0.3f;

    private void Start()
    {
        if (AudioManager.Instance != null && bgm != null)
            AudioManager.Instance.PlayMusic(bgm, volume, true);
    }
}
